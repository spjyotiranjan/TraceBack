using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

[Authorize(Roles = "Admin")]
public sealed class AdminController(ItemService items, ClaimService claims, AccountService accounts) : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> Stats() => Ok(new { items = await items.CountAsync(), claims = await claims.CountAsync() });

    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> Items() =>
        Ok((await items.AllAsync()).Select(ItemResponse.From));

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> Users() =>
        Ok((await accounts.RecentUsersAsync()).Select(UserResponse.From));

    [HttpGet("claims")]
    public async Task<ActionResult<IEnumerable<ClaimResponse>>> Claims() =>
        Ok((await claims.AllAsync()).Select(ClaimResponse.From));

    [HttpPost("claims/{id}/approve")]
    public Task<IActionResult> Approve(string id) => Decide(id, true);

    [HttpPost("claims/{id}/reject")]
    public Task<IActionResult> Reject(string id) => Decide(id, false);

    [HttpPost("items/{id}/resolve")]
    public async Task<IActionResult> ResolveItem(string id)
    {
        if (!ValidId(id)) return BadRequest(new { error = "Invalid item ID." });
        return await items.ResolveAsync(id, UserId, true) ? NoContent() : NotFound();
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteItem(string id)
    {
        if (!ValidId(id)) return BadRequest(new { error = "Invalid item ID." });
        return await items.DeleteAsync(id, UserId, true) ? NoContent() : NotFound();
    }

    private async Task<IActionResult> Decide(string id, bool approve)
    {
        if (!ValidId(id)) return BadRequest(new { error = "Invalid claim ID." });
        return await claims.DecideAsync(id, approve) ? NoContent() : Conflict(new { error = "Claim is no longer pending or item is unavailable." });
    }
}
