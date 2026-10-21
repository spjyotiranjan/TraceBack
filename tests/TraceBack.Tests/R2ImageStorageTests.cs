using Microsoft.AspNetCore.Http;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class R2ImageStorageTests
{
    [Fact]
    public async Task Upload_uses_detected_type_instead_of_file_name()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        var image = await R2ImageStorage.ReadUploadAsync(File(png, "misleading.jpg", "image/png"));

        Assert.Equal("image/png", image.ContentType);
        Assert.Equal("png", image.Extension);
        Assert.Equal(png, image.Bytes);
    }

    [Fact]
    public async Task Upload_rejects_a_declared_type_that_disagrees_with_the_file_signature()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File(png, "photo.png", "image/jpeg")));
    }

    [Fact]
    public async Task Upload_rejects_html_disguised_as_an_image()
    {
        var file = File("<script>alert(1)</script>"u8.ToArray(), "photo.png", "image/png");

        await Assert.ThrowsAsync<ArgumentException>(() => R2ImageStorage.ReadUploadAsync(file));
    }

    [Fact]
    public async Task Upload_rejects_files_larger_than_five_megabytes()
    {
        var file = File(new byte[R2ImageStorage.MaxImageBytes + 1], "photo.png", "image/png");

        await Assert.ThrowsAsync<ArgumentException>(() => R2ImageStorage.ReadUploadAsync(file));
    }

    [Fact]
    public async Task Upload_rejects_empty_files()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File([], "photo.png", "image/png")));
    }

    private static IFormFile File(byte[] bytes, string name, string contentType) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
}
