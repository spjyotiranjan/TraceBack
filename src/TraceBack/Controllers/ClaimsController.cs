using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize]
public class ClaimsController(ClaimService claims, ItemService items) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(string itemId)
    {
        var item = await items.GetAsync(itemId);
        if (item is null || item.Type != "Found" || item.Status != "Active") return NotFound();
        if (item.PostedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        return View(new ClaimPage(item, new ClaimForm()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string itemId, ClaimForm form)
    {
        var item = await items.GetAsync(itemId);
        if (item is null || item.Type != "Found" || item.Status != "Active") return NotFound();
        if (item.PostedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        if (!ModelState.IsValid) return View(new ClaimPage(item, form));
        var claim = await claims.CreateAsync(itemId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, form);
        if (claim is null)
        {
            ModelState.AddModelError(string.Empty, "This claim could not be submitted. You may already have a claim for this item.");
            return View(new ClaimPage(item, form));
        }
        TempData["Success"] = "Your claim was submitted. You can track it in My claims.";
        return RedirectToAction("Index", "Dashboard");
    }
}
