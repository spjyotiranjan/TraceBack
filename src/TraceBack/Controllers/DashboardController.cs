using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize]
public class DashboardController(ItemService items, ClaimService claims) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var mine = await items.MineAsync(userId);
        var myClaims = await claims.MineAsync(userId);
        var rows = new List<ClaimRow>();
        foreach (var claim in myClaims) rows.Add(new ClaimRow(claim, await items.GetAsync(claim.ItemId)));
        return View(new DashboardPage(mine, rows));
    }
}
