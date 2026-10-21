using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

public class ItemsController(ItemService items) : Controller
{
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
    public async Task<IActionResult> Create(ItemForm form)
    {
        if (!ModelState.IsValid)
        {
            return View("Editor", new ItemEditorPage(form, null));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var item = await items.CreateAsync(form, currentUserId);

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
            ContactPreference = item.ContactPreference,
            ImageUrl = item.ImageUrl
        };

        return View("Editor", new ItemEditorPage(form, id));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ItemForm form)
    {
        if (!ModelState.IsValid)
        {
            return View("Editor", new ItemEditorPage(form, id));
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isAdmin = User.IsInRole("Admin");

        var item = await items.UpdateAsync(id, form, currentUserId, isAdmin);
        if (item is null)
        {
            return NotFound();
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

