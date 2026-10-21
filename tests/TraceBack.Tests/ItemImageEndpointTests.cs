using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using ApiItemsController = TraceBack.Controllers.Api.ItemsController;

namespace TraceBack.Tests;

public sealed class ItemImageEndpointTests
{
    [Fact]
    public async Task Upload_rejects_an_invalid_image_before_storage_is_called()
    {
        var controller = new ApiItemsController(null!, NullLogger<ApiItemsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        byte[] html = "<script>alert(1)</script>"u8.ToArray();
        var file = new FormFile(new MemoryStream(html), 0, html.Length, "file", "photo.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var response = await controller.UploadImage("507f1f77bcf86cd799439011", file);

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }
}
