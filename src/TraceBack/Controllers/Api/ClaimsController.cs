using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

public sealed class ClaimsController(ClaimService claims) : ApiControllerBase
{
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<ClaimResponse>>> Mine() =>
        Ok((await claims.MineAsync(UserId)).Select(ClaimResponse.From));

    [HttpPost("/api/items/{itemId}/claims")]
    public async Task<ActionResult<ClaimResponse>> Create(string itemId, ClaimForm form)
    {
        if (!ValidId(itemId)) return BadRequest(new { error = "Invalid item ID." });
        if (string.IsNullOrWhiteSpace(form.Message) || form.Message.Trim().Length < 10)
            return BadRequest(new { error = "Claim message must contain at least 10 characters." });
        var claim = await claims.CreateAsync(itemId, UserId, form);
        if (claim is null) return Conflict(new { error = "Item unavailable, already claimed, or this claim already exists." });
        return StatusCode(StatusCodes.Status201Created, ClaimResponse.From(claim));
    }
}
