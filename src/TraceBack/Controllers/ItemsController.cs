using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class ItemsController(IItemService items, ILogger<ItemsController> logger, TimeProvider? clock = null) : Controller
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

        search.Query = CleanFilter(search.Query);
        search.Category = CleanFilter(search.Category);
        search.Location = CleanFilter(search.Location);
        search.Type = search.Type?.Trim().ToUpperInvariant() switch
        {
            "LOST" => "Lost",
            "FOUND" => "Found",
            _ => null
        };
        search.Status = search.Status?.Trim().ToUpperInvariant() switch
        {
            "RESOLVED" => "Resolved",
            "CLAIMED" => "Claimed",
            _ => "Active"
        };

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

    private static string? CleanFilter(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
        return View("Editor", new ItemEditorPage(new ItemForm(clock ?? TimeProvider.System), null));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(ItemForm form, IFormFile? imageFile)
    {
        var editor = new ItemEditorPage(form, null, ReselectImage: imageFile is not null);
        if (form.MapLocation?.Latitude is null && form.MapLocation?.Longitude is null)
        {
            ModelState.AddModelError(nameof(form.MapLocation), MapLocationRules.RequiredMessage);
        }

        ImageUpload? upload = null;
        if (imageFile is not null)
        {
            try { upload = await R2ImageStorage.ReadUploadAsync(imageFile, HttpContext.RequestAborted); }
            catch (ArgumentException error) { ModelState.AddModelError("ImageFile", error.Message); }
        }

        if (!ModelState.IsValid)
        {
            return View("Editor", editor);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Item item;
        try
        {
            item = await items.CreateAsync(form, currentUserId);
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(form.MapLocation), error.Message);
            return View("Editor", editor);
        }

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
                if (!await items.DeleteAsync(item.Id, currentUserId, false) && await items.GetAsync(item.Id) is not null)
                {
                    // Do not offer a new Create submission if the report still exists.
                    TempData["Error"] = "The report was saved, but the image could not be added. Open Edit report to try the photo again.";
                    return RedirectToAction(nameof(Details), new { id = item.Id });
                }
                ModelState.AddModelError("ImageFile", "Could not store the image. Please try again.");
                return View("Editor", editor);
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
            MapLocation = item.MapLocation is null ? null : new MapLocationInput
            {
                Latitude = item.MapLocation.Latitude,
                Longitude = item.MapLocation.Longitude
            },
            DateReported = item.DateReported,
            ContactPreference = item.ContactPreference
        };

        return View("Editor", new ItemEditorPage(form, id, item.DisplayImageUrl, MapLocationRules.SafeStored(item.MapLocation) is not null,
            LockedType: item.Type, EditRevision: item.UpdatedAt.Ticks));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(string id, ItemForm form, IFormFile? imageFile, bool removeImage, [FromForm] long? editRevision)
    {
        var existingItem = await items.GetAsync(id);
        if (existingItem is null) return NotFound();
        if (!CanManage(existingItem)) return Forbid();
        if (existingItem.Status != "Active") return NotFound();
        if (ModelState[nameof(editRevision)]?.Errors.Count > 0 ||
            (editRevision.HasValue && editRevision.Value != existingItem.UpdatedAt.Ticks))
        {
            return EditConflict(id);
        }

        var editor = new ItemEditorPage(form, id, existingItem.DisplayImageUrl,
            MapLocationRules.SafeStored(existingItem.MapLocation) is not null, existingItem.Type,
            ReselectImage: imageFile is not null, PendingImageRemoval: removeImage && imageFile is null,
            EditRevision: existingItem.UpdatedAt.Ticks);
        if (removeImage && imageFile is not null)
        {
            ModelState.AddModelError("ImageFile", "Choose either to replace the current image or remove it, not both.");
        }
        ImageUpload? upload = null;
        if (imageFile is not null)
        {
            try { upload = await R2ImageStorage.ReadUploadAsync(imageFile, HttpContext.RequestAborted); }
            catch (ArgumentException error) { ModelState.AddModelError("ImageFile", error.Message); }
        }

        if (!ModelState.IsValid)
        {
            return View("Editor", editor);
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        Item? item;
        try
        {
            item = await items.UpdateAsync(id, form, currentUserId, isAdmin, existingItem.UpdatedAt.Ticks);
        }
        catch (ArgumentException error)
        {
            ModelState.AddModelError(nameof(form.MapLocation), error.Message);
            return View("Editor", editor);
        }
        catch (ItemUpdateConflictException)
        {
            return EditConflict(id);
        }
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

    private ViewResult EditConflict(string id)
    {
        var result = View("EditConflict", id);
        result.StatusCode = StatusCodes.Status409Conflict;
        return result;
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
