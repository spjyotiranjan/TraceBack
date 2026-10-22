using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ItemService items, ClaimService claims) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var allItems = await items.AllAsync();
        var allClaims = await claims.AllAsync();
        var rows = new List<ClaimRow>();

        foreach (var claim in allClaims)
        {
            var item = await items.GetAsync(claim.ItemId);
            rows.Add(new ClaimRow(claim, item));
        }

        var totalItems = await items.CountAsync();
        var totalClaims = await claims.CountAsync();

        return View(new AdminPage(allItems, rows, totalItems, totalClaims));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(string id, bool approve)
    {
        var success = await claims.DecideAsync(id, approve);
        if (!success)
        {
            return NotFound();
        }

        TempData["Success"] = approve ? "Claim approved." : "Claim rejected.";
        return RedirectToAction(nameof(Index));
    }
}
