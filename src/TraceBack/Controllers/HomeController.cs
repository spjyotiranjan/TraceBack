using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

public class HomeController(Func<IItemService> createItems, ILogger<HomeController> logger) : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return View(new HomePage([]));
        }

        try
        {
            var recent = await createItems().SearchAsync(new ItemSearch());
            return View(new HomePage(recent.Take(6).ToList()));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Recent reports could not load on the home page");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new HomePage([], true));
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult StatusCodePage(int code)
    {
        if (code != StatusCodes.Status404NotFound)
        {
            return StatusCode(code);
        }

        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
