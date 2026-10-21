using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(ItemService items, ClaimService claims) : Controller
{
    public async Task<IActionResult> Index()
    {
        var allItems = await items.AllAsync();
        var allClaims = await claims.AllAsync();
        var rows = new List<ClaimRow>();
        foreach (var claim in allClaims) rows.Add(new ClaimRow(claim, await items.GetAsync(claim.ItemId)));
        return View(new AdminPage(allItems, rows, await items.CountAsync(), await claims.CountAsync()));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(string id, bool approve)
    {
        if (!await claims.DecideAsync(id, approve)) return NotFound();
        TempData["Success"] = approve ? "Claim approved." : "Claim rejected.";
        return RedirectToAction(nameof(Index));
    }
}
