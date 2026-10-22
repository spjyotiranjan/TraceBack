using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

public class HomeController(Func<IItemService> createItems) : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new List<Item>());
        }

        var recent = await createItems().SearchAsync(new ItemSearch());
        return View(recent.Take(6).ToList());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
