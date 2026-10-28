using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers;

[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[Route("Items/{itemId}/Comments")]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class CommentsController(ICommentService comments) : Controller
{
    [HttpGet]
    public async Task<IActionResult> List(string itemId, string? parentId, string? cursor, int limit = 20)
    {
        var result = await comments.ListAsync(itemId, parentId, cursor, limit);
        return result.Error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Invalid comment query." }),
            CommentError.NotFound => NotFound(),
            _ => Ok(result.Value)
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data", "application/x-www-form-urlencoded")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(string itemId, [FromForm] string? body, [FromForm] string? parentId)
    {
        var result = await comments.CreateAsync(
            itemId, parentId, body,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty);
        return result.Error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Comment text must be 1 to 2000 characters." }),
            CommentError.NotFound => NotFound(),
            CommentError.Inactive => Conflict(new { error = "This report is no longer active." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Value)
        };
    }

    [HttpPost("{commentId}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string itemId, string commentId)
    {
        var error = await comments.DeleteAsync(
            itemId, commentId,
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            User.IsInRole("Admin"));
        return error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Invalid comment ID." }),
            CommentError.NotFound => NotFound(),
            CommentError.Forbidden => Forbid(),
            _ => NoContent()
        };
    }
}
