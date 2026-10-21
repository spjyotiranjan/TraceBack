using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace TraceBack.Services;

public sealed record ImageUpload(byte[] Bytes, string ContentType, string Extension);
public sealed record StoredImage(byte[] Bytes, string ContentType);

public sealed class R2ImageStorage : IDisposable
{
    public const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly IAmazonS3? client;
    private readonly string? bucket;

    public R2ImageStorage(IConfiguration configuration)
    {
        var accountId = configuration["R2:AccountId"];
        bucket = configuration["R2:Bucket"];
        var accessKeyId = configuration["R2:AccessKeyId"];
        var secretAccessKey = configuration["R2:SecretAccessKey"];

        var settings = new[] { accountId, bucket, accessKeyId, secretAccessKey };
        if (settings.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        if (settings.Any(string.IsNullOrWhiteSpace) ||
            !System.Text.RegularExpressions.Regex.IsMatch(accountId!, "^[a-fA-F0-9]{32}$"))
        {
            throw new InvalidOperationException("Set all R2 settings with a valid Cloudflare account ID.");
        }

        client = new AmazonS3Client(new BasicAWSCredentials(accessKeyId, secretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = $"https://{accountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            });
    }

    public static async Task<ImageUpload> ReadUploadAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length is < 1 or > MaxImageBytes)
        {
            throw new ArgumentException("Choose an image smaller than 5 MB.");
        }

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length is < 1 or > MaxImageBytes)
        {
            throw new ArgumentException("Choose an image smaller than 5 MB.");
        }

        var bytes = buffer.ToArray();
        var kind = DetectImage(bytes);
        if (kind is null || !string.Equals(file.ContentType, kind.Value.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Choose a JPEG, PNG, GIF, or WebP image with a matching file type.");
        }

        return new ImageUpload(bytes, kind.Value.ContentType, kind.Value.Extension);
    }

    public async Task<string> UploadAsync(string itemId, ImageUpload image, CancellationToken cancellationToken = default)
    {
        var storage = RequireClient();
        var key = $"items/{itemId}/{Guid.NewGuid():N}.{image.Extension}";
        await using var stream = new MemoryStream(image.Bytes, writable: false);
        await storage.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream,
            ContentType = image.ContentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
            UseChunkEncoding = false
        }, cancellationToken);
        return key;
    }

    public async Task<StoredImage?> ReadAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await RequireClient().GetObjectAsync(bucket, key, cancellationToken);
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int count;
            while ((count = await response.ResponseStream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + count > MaxImageBytes)
                {
                    return null;
                }
                await buffer.WriteAsync(chunk.AsMemory(0, count), cancellationToken);
            }

            var bytes = buffer.ToArray();
            var kind = DetectImage(bytes);
            return kind is null ? null : new StoredImage(bytes, kind.Value.ContentType);
        }
        catch (AmazonS3Exception error) when (error.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        RequireClient().DeleteObjectAsync(bucket, key, cancellationToken);

    public void Dispose() => client?.Dispose();

    private IAmazonS3 RequireClient() => client ?? throw new InvalidOperationException("R2 image storage is not configured.");

    private static (string ContentType, string Extension)? DetectImage(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xD8, 0xFF })) return ("image/jpeg", "jpg");
        if (bytes.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ("image/png", "png");
        if (bytes.AsSpan().StartsWith("GIF87a"u8) || bytes.AsSpan().StartsWith("GIF89a"u8)) return ("image/gif", "gif");
        if (bytes.Length >= 12 && bytes.AsSpan().StartsWith("RIFF"u8) && bytes.AsSpan(8).StartsWith("WEBP"u8)) return ("image/webp", "webp");
        return null;
    }
}
