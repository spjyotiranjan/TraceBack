using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ItemsController(IItemService items, ILogger<ItemsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Image(string id)
    {
        StoredImage? image;
        try
        {
            image = await items.GetImageAsync(id, HttpContext.RequestAborted);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Image retrieval failed for report {ItemId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(image.Bytes, image.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ItemSearch search)
    {
        search.Page = Math.Clamp(search.Page, 1, 1000);

        if (search.Type is not (null or "Lost" or "Found"))
        {
            search.Type = null;
        }

        if (search.Status is not (null or "Active" or "Resolved" or "Claimed"))
        {
            search.Status = null;
        }

        try
        {
            var results = await items.SearchAsync(search);
            var hasNext = false;

            if (results.Count == 24 && search.Page < 1000)
            {
                var nextPageSearch = new ItemSearch
                {
                    Query = search.Query,
                    Type = search.Type,
                    Category = search.Category,
                    Location = search.Location,
                    Status = search.Status,
                    Page = search.Page + 1
                };

                hasNext = (await items.SearchAsync(nextPageSearch)).Count > 0;
            }

            return View(new BrowsePage(search, results, hasNext));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Report search failed");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new BrowsePage(search, [], false, true));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var item = await items.GetAsync(id);
        if (item is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("NotFound");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var isOwner = currentUserId == item.PostedByUserId;
        var isAdmin = User.IsInRole("Admin");
        var canClaim = User.Identity?.IsAuthenticated == true && !isOwner && item.Type == "Found" && item.Status == "Active";

        return View(new ItemDetailPage(item, isOwner, isAdmin, canClaim));
    }

    [Authorize]
    [HttpGet]
    public IActionResult Create()
    {
        return View("Editor", new ItemEditorPage(new ItemForm(), null));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(ItemForm form, IFormFile? imageFile)
    {
        ImageUpload? upload = null;
        if (imageFile is not null)
        {
            try { upload = await R2ImageStorage.ReadUploadAsync(imageFile, HttpContext.RequestAborted); }
            catch (ArgumentException error) { ModelState.AddModelError("ImageFile", error.Message); }
        }

        if (!ModelState.IsValid)
        {
            return View("Editor", new ItemEditorPage(form, null));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await items.CreateAsync(form, currentUserId);

        if (upload is not null)
        {
            try
            {
                if (!await items.SetImageAsync(item.Id, upload, currentUserId, false))
                {
                    throw new InvalidOperationException("The new report could not accept an image.");
                }
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                logger.LogError(error, "Image upload failed for report {ItemId}", item.Id);
                await items.DeleteAsync(item.Id, currentUserId, false);
                ModelState.AddModelError("ImageFile", "Could not store the image. Please try again.");
                return View("Editor", new ItemEditorPage(form, null));
            }
        }

        TempData["Success"] = "Your report is live.";
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var item = await items.GetAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (!CanManage(item))
        {
            return Forbid();
        }

        if (item.Status != "Active")
        {
            return BadRequest("Only active reports can be edited.");
        }

        var form = new ItemForm
        {
            Title = item.Title,
            Description = item.Description,
            Type = item.Type,
            Category = item.Category,
            Location = item.Location,
            DateReported = item.DateReported,
            ContactPreference = item.ContactPreference
        };

        return View("Editor", new ItemEditorPage(form, id, item.DisplayImageUrl));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(string id, ItemForm form, IFormFile? imageFile, bool removeImage)
    {
        var existingItem = await items.GetAsync(id);
        if (existingItem is null) return NotFound();
        if (!CanManage(existingItem)) return Forbid();

        ImageUpload? upload = null;
        if (imageFile is not null)
        {
            try { upload = await R2ImageStorage.ReadUploadAsync(imageFile, HttpContext.RequestAborted); }
            catch (ArgumentException error) { ModelState.AddModelError("ImageFile", error.Message); }
        }

        if (!ModelState.IsValid)
        {
            return View("Editor", new ItemEditorPage(form, id, existingItem.DisplayImageUrl));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var item = await items.UpdateAsync(id, form, currentUserId, isAdmin);
        if (item is null)
        {
            return NotFound();
        }

        try
        {
            var imageChanged = upload is not null
                ? await items.SetImageAsync(id, upload, currentUserId, isAdmin)
                : !removeImage || await items.RemoveImageAsync(id, currentUserId, isAdmin);
            if (!imageChanged)
            {
                TempData["Error"] = "Report details were saved, but the image could not be changed. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Image change failed for report {ItemId}", id);
            TempData["Error"] = "Report details were saved, but the image could not be changed. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Your report was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var success = await items.ResolveAsync(id, currentUserId, isAdmin);
        if (!success)
        {
            return NotFound();
        }

        TempData["Success"] = "The report is marked resolved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var item = await items.GetAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (!CanManage(item))
        {
            return Forbid();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var deleted = await items.DeleteAsync(id, currentUserId, isAdmin);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["Success"] = "The report and its claims were deleted.";
        return isAdmin
            ? RedirectToAction("Index", "Admin")
            : RedirectToAction("Index", "Dashboard");
    }

    private bool CanManage(Item item) =>
        User.IsInRole("Admin") || item.PostedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier);
}
