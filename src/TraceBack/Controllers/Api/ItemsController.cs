using Microsoft.AspNetCore.Mvc;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Controllers.Api;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class ItemsController(IItemService items, ILogger<ItemsController> logger) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> Search([FromQuery] ItemSearch search)
    {
        if (search.Page < 1 || search.Page > 1000)
        {
            return BadRequest(new { error = "Page must be between 1 and 1000." });
        }

        if (search.Type is not null && search.Type is not ("Lost" or "Found"))
        {
            return BadRequest(new { error = "Type must be Lost or Found." });
        }

        if (search.Status is not null && search.Status is not ("Active" or "Claimed" or "Resolved"))
        {
            return BadRequest(new { error = "Invalid status." });
        }

        if (search.Query?.Length > 120 || search.Category?.Length > 80 || search.Location?.Length > 120)
        {
            return BadRequest(new { error = "Search term is too long." });
        }

        var results = await items.SearchAsync(search);
        return Ok(results.Select(ItemResponse.From));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> Mine()
    {
        var myItems = await items.MineAsync(UserId);
        return Ok(myItems.Select(ItemResponse.From));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemResponse>> Get(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        var item = await items.GetAsync(id);
        return item is null ? NotFound() : Ok(ItemResponse.From(item));
    }

    [HttpPost]
    public async Task<ActionResult<ItemResponse>> Create(ItemForm form)
    {
        if (!ValidForm(form))
        {
            return BadRequest(new { error = "Fields cannot be blank and the report date cannot be in the future." });
        }

        var item = await items.CreateAsync(form, UserId);
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ItemResponse.From(item));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ItemResponse>> Update(string id, ItemForm form)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        if (!ValidForm(form))
        {
            return BadRequest(new { error = "Fields cannot be blank and the report date cannot be in the future." });
        }

        var existing = await items.GetAsync(id);
        if (existing is not null && (IsAdmin || existing.PostedByUserId == UserId) && existing.Type != form.Type)
        {
            return Conflict(new { error = "Item type cannot be changed after posting." });
        }

        var item = await items.UpdateAsync(id, form, UserId, IsAdmin);
        return item is null ? NotFound() : Ok(ItemResponse.From(item));
    }

    [HttpPost("{id}/resolve")]
    public async Task<IActionResult> Resolve(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        var resolved = await items.ResolveAsync(id, UserId, IsAdmin);
        return resolved ? NoContent() : NotFound();
    }

    [HttpPut("{id}/image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ItemResponse>> UploadImage(string id, [FromForm] IFormFile? file)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        if (file is null)
        {
            return BadRequest(new { error = "Choose an image." });
        }

        ImageUpload upload;
        try
        {
            upload = await R2ImageStorage.ReadUploadAsync(file, HttpContext.RequestAborted);
        }
        catch (ArgumentException error)
        {
            return BadRequest(new { error = error.Message });
        }

        try
        {
            if (!await items.SetImageAsync(id, upload, UserId, IsAdmin)) return NotFound();
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Image upload failed for report {ItemId}", id);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Image storage is unavailable." });
        }

        var item = await items.GetAsync(id);
        return item is null ? NotFound() : Ok(ItemResponse.From(item));
    }

    [HttpDelete("{id}/image")]
    public async Task<IActionResult> RemoveImage(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        return await items.RemoveImageAsync(id, UserId, IsAdmin) ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ValidId(id))
        {
            return BadRequest(new { error = "Invalid item ID." });
        }

        var deleted = await items.DeleteAsync(id, UserId, IsAdmin);
        return deleted ? NoContent() : NotFound();
    }

    private static bool ValidForm(ItemForm form) =>
        !string.IsNullOrWhiteSpace(form.Title) &&
        !string.IsNullOrWhiteSpace(form.Description) &&
        !string.IsNullOrWhiteSpace(form.Category) &&
        !string.IsNullOrWhiteSpace(form.Location) &&
        !string.IsNullOrWhiteSpace(form.ContactPreference) &&
        form.DateReported.Date <= DateTime.UtcNow.Date;
}
