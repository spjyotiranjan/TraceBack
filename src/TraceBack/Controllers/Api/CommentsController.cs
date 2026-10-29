using Microsoft.AspNetCore.Mvc;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class CommentsController(ICommentService comments) : ApiControllerBase
{
    [HttpGet("/api/items/{itemId}/comments")]
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

    [HttpPost("/api/items/{itemId}/comments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(string itemId, [FromForm] string? body, [FromForm] string? parentId)
    {
        var result = await comments.CreateAsync(
            itemId, parentId, body, UserId, User.Identity?.Name ?? string.Empty);
        return result.Error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Comment text must be 1 to 2000 characters." }),
            CommentError.NotFound => NotFound(),
            CommentError.Inactive => Conflict(new { error = "This report is no longer active." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Value)
        };
    }

    [HttpDelete("/api/items/{itemId}/comments/{commentId}")]
    public async Task<IActionResult> Delete(string itemId, string commentId)
    {
        var error = await comments.DeleteAsync(itemId, commentId, UserId, IsAdmin);
        return error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Invalid comment ID." }),
            CommentError.NotFound => NotFound(),
            CommentError.Forbidden => Forbid(),
            _ => NoContent()
        };
    }
}
