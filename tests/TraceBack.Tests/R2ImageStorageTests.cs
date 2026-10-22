using Microsoft.AspNetCore.Http;
using SkiaSharp;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class R2ImageStorageTests
{
    [Fact]
    public async Task Upload_uses_detected_type_instead_of_file_name()
    {
        byte[] png = ValidPng();
        var image = await R2ImageStorage.ReadUploadAsync(File(png, "misleading.jpg", "image/png"));

        Assert.Equal("image/png", image.ContentType);
        Assert.Equal("png", image.Extension);
        Assert.Equal(png, image.Bytes);
    }

    [Fact]
    public async Task Upload_rejects_a_declared_type_that_disagrees_with_the_file_signature()
    {
        byte[] png = ValidPng();

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
    public async Task Upload_rejects_a_header_without_image_data()
    {
        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        byte[] jpegHeader = [0xFF, 0xD8, 0xFF];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File(pngHeader, "photo.png", "image/png")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File(jpegHeader, "photo.jpg", "image/jpeg")));
    }

    [Fact]
    public async Task Upload_rejects_truncated_or_corrupt_pixels()
    {
        var png = ValidPng();
        var truncated = png[..(png.Length / 2)];
        var corrupted = (byte[])png.Clone();
        Array.Fill(corrupted, (byte)0, 45, Math.Min(12, corrupted.Length - 57));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File(truncated, "photo.png", "image/png")));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            R2ImageStorage.ReadUploadAsync(File(corrupted, "photo.png", "image/png")));
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

    private static byte[] ValidPng()
    {
        using var bitmap = new SKBitmap(8, 8);
        bitmap.Erase(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
