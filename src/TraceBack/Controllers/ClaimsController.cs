using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ClaimsController(ClaimService claims, ItemService items, ILogger<ClaimsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(string itemId)
    {
        var item = await items.GetAsync(itemId);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return EligibilityFailure(item, currentUserId, drawer: false) ?? View(new ClaimPage(item!, new ClaimForm()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string itemId, ClaimForm form)
    {
        var drawer = Request.Headers["X-TraceBack-Claim"] == "drawer";
        var item = await items.GetAsync(itemId);
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (EligibilityFailure(item, currentUserId, drawer) is { } failure)
        {
            return failure;
        }

        if (!ModelState.IsValid)
        {
            var messageError = ModelState[nameof(ClaimForm.Message)]?.Errors.FirstOrDefault()?.ErrorMessage;
            return drawer
                ? UnprocessableEntity(new { messageError, error = "Check the ownership details and try again." })
                : View(new ClaimPage(item!, form));
        }

        try
        {
            if (await claims.CreateAsync(itemId, currentUserId!, form) is null)
            {
                // The service also checks eligibility around the write; distinguish a stale report from a duplicate.
                var latest = await items.GetAsync(itemId);
                return EligibilityFailure(latest, currentUserId, drawer)
                    ?? FormFailure(latest!, form, drawer, StatusCodes.Status409Conflict,
                        "This claim could not be submitted. You may already have a claim for this item. Check My claims on your Dashboard.");
            }
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            // Storage errors can include submitted document values. Do not log the private message or raw exception.
            logger.LogWarning("Claim submission could not be confirmed ({FailureType}).", ex.GetType().Name);
            return FormFailure(item!, form, drawer, StatusCodes.Status503ServiceUnavailable,
                "We couldn't confirm your claim. Check My claims on your Dashboard before trying again.");
        }

        TempData["Success"] = "Your claim was submitted. You can track it in My claims.";
        if (drawer)
        {
            // A fetch-followed redirect would consume the Dashboard flash before the visible navigation.
            return Ok(new { redirectUrl = Url.Action("Index", "Dashboard") });
        }

        return RedirectToAction("Index", "Dashboard");
    }

    private IActionResult? EligibilityFailure(Item? item, string? currentUserId, bool drawer)
    {
        if (item is null || item.Type != "Found" || item.Status != "Active")
        {
            return drawer ? NotFound(new { error = "This report is no longer available for claims." }) : NotFound();
        }

        if (currentUserId is null || item.PostedByUserId == currentUserId)
        {
            return drawer ? StatusCode(StatusCodes.Status403Forbidden, new { error = "You can no longer submit a claim for this report." }) : Forbid();
        }

        return null;
    }

    private IActionResult FormFailure(Item item, ClaimForm form, bool drawer, int status, string error)
    {
        if (drawer)
        {
            return StatusCode(status, new { error });
        }

        ModelState.AddModelError(string.Empty, error);
        var view = View("Create", new ClaimPage(item, form));
        view.StatusCode = status;
        return view;
    }
}
