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
            _ => Ok(new CommentPage(result.Value!.Comments.Select(x => x.ForApi()).ToList(), result.Value.NextCursor))
        };
    }

    [HttpPost("/api/items/{itemId}/comments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Create(string itemId, [FromForm] string? body,
        [FromForm] string? parentId, [FromForm] IFormFile? imageFile, [FromForm] string? imageAlt)
    {
        if (!CommentUploadValidation.HasExpectedFiles(Request, imageFile, required: false))
            return BadRequest(new { error = "Provide at most one imageFile." });
        if (imageFile?.Length > R2ImageStorage.MaxImageBytes)
            return StatusCode(413, new { error = "Image exceeds 5 MiB." });
        ImageUpload? image;
        try { image = imageFile is null ? null : await R2ImageStorage.ReadUploadAsync(imageFile); }
        catch (ArgumentException error) { return BadRequest(new { error = error.Message }); }
        var result = await comments.CreateAsync(itemId, parentId, body,
            UserId, User.Identity?.Name ?? string.Empty, image, imageAlt);
        return result.Error switch
        {
            CommentError.InvalidInput => BadRequest(new { error = "Provide 1 to 2000 characters of text or one image with 1 to 200 characters of alt text." }),
            CommentError.NotFound => NotFound(),
            CommentError.Inactive => Conflict(new { error = "This report is no longer active." }),
            CommentError.StorageUnavailable => StatusCode(503, new { error = "Image storage is unavailable." }),
            _ => StatusCode(StatusCodes.Status201Created, result.Value!.ForApi())
        };
    }

    [HttpGet("/api/items/{itemId}/comments/{commentId}/image")]
    public async Task<IActionResult> Image(string itemId, string commentId)
    {
        Response.Headers.XContentTypeOptions = "nosniff";
        var result = await comments.GetImageAsync(itemId, commentId);
        return result.Error switch
        {
            CommentError.StorageUnavailable => StatusCode(503),
            CommentError.None => File(result.Value!.Bytes, result.Value.ContentType),
            _ => NotFound()
        };
    }

    [HttpPut("/api/items/{itemId}/comments/{commentId}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> SetImage(string itemId, string commentId,
        [FromForm] IFormFile imageFile, [FromForm] string? imageAlt)
    {
        if (!CommentUploadValidation.HasExpectedFiles(Request, imageFile, required: true))
            return BadRequest(new { error = "Provide exactly one imageFile." });
        if (imageFile.Length > R2ImageStorage.MaxImageBytes)
            return StatusCode(413, new { error = "Image exceeds 5 MiB." });
        ImageUpload image;
        try { image = await R2ImageStorage.ReadUploadAsync(imageFile); }
        catch (ArgumentException error) { return BadRequest(new { error = error.Message }); }
        return ImageMutationResult(await comments.SetImageAsync(itemId, commentId, image,
            imageAlt, UserId, IsAdmin));
    }

    [HttpDelete("/api/items/{itemId}/comments/{commentId}/image")]
    public async Task<IActionResult> RemoveImage(string itemId, string commentId) =>
        ImageMutationResult(await comments.RemoveImageAsync(itemId, commentId, UserId, IsAdmin));

    private IActionResult ImageMutationResult(CommentError error) => error switch
    {
        CommentError.InvalidInput => BadRequest(new { error = "Image or alt text is invalid, or this image-only comment requires its image." }),
        CommentError.NotFound => NotFound(),
        CommentError.Forbidden => Forbid(),
        CommentError.StorageUnavailable => StatusCode(503, new { error = "Image storage is unavailable." }),
        _ => NoContent()
    };

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
