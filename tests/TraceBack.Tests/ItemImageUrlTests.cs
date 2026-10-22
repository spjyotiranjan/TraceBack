using TraceBack.Models;

namespace TraceBack.Tests;

public sealed class ItemImageUrlTests
{
    [Fact]
    public void Stored_image_uses_application_retrieval_route()
    {
        var item = new Item { ImageKey = "items/abc/image.png", ImageUrl = "https://legacy.example/image.png" };

        Assert.Equal($"/Items/Image/{item.Id}", item.DisplayImageUrl);
        Assert.Equal(item.DisplayImageUrl, ItemResponse.From(item).ImageUrl);
    }

    [Fact]
    public void Legacy_external_image_remains_visible()
    {
        var item = new Item { ImageUrl = "https://legacy.example/image.png" };

        Assert.Equal(item.ImageUrl, item.DisplayImageUrl);
    }
}
