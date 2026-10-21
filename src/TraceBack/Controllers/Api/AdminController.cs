using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

[Authorize(Roles = "Admin")]
public sealed class AdminController(ItemService items, ClaimService claims, AccountService accounts) : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var itemCount = await items.CountAsync();
        var claimCount = await claims.CountAsync();
        return Ok(new { items = itemCount, claims = claimCount });
    }

    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> Items()
    {
        var allItems = await items.AllAsync();
        return Ok(allItems.Select(ItemResponse.From));
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> Users()
    {
        var recentUsers = await accounts.RecentUsersAsync();
        return Ok(recentUsers.Select(UserResponse.From));
    }

    [HttpGet("claims")]
    public async Task<ActionResult<IEnumerable<ClaimResponse>>> Claims()
    {
        var allClaims = await claims.AllAsync();
        return Ok(allClaims.Select(ClaimResponse.From));
    }

    [HttpPost("claims/{id}/approve")]
    public Task<IActionResult> Approve(string id) => Decide(id, approve: true);

    [HttpPost("claims/{id}/reject")]
    public Task<IActionResult> Reject(string id) => Decide(id, approve: false);

    [HttpPost("items/{id}/resolve")]
    public async Task<IActionResult> ResolveItem(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        var resolved = await items.ResolveAsync(id, UserId, admin: true);
        return resolved ? NoContent() : NotFound();
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteItem(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        var deleted = await items.DeleteAsync(id, UserId, admin: true);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult> Decide(string id, bool approve)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid claim ID." });
        }

        var success = await claims.DecideAsync(id, approve);
        return success
            ? NoContent()
            : Conflict(new { error = "Claim is no longer pending or item is unavailable." });
    }
}
