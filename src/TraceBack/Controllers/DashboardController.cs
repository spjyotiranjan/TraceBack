using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize]
public class DashboardController(ItemService items, ClaimService claims) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var myItems = await items.MineAsync(userId);
        var myClaims = await claims.MineAsync(userId);
        var claimRows = new List<ClaimRow>();

        foreach (var claim in myClaims)
        {
            var item = await items.GetAsync(claim.ItemId);
            claimRows.Add(new ClaimRow(claim, item));
        }

        return View(new DashboardPage(myItems, claimRows));
    }
}
