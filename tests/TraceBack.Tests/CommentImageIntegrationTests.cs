using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SkiaSharp;
using TraceBack.Data;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class CommentImageIntegrationTests
{
    [Fact]
    public async Task Api_image_lifecycle_uses_isolated_mongo_and_private_r2()
    {
        var mongoUri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI");
        var accountId = Environment.GetEnvironmentVariable("TRACEBACK_TEST_R2_ACCOUNT_ID");
        var bucket = Environment.GetEnvironmentVariable("TRACEBACK_TEST_R2_BUCKET");
        var accessKey = Environment.GetEnvironmentVariable("TRACEBACK_TEST_R2_ACCESS_KEY_ID");
        var secret = Environment.GetEnvironmentVariable("TRACEBACK_TEST_R2_SECRET_ACCESS_KEY");
        if (new[] { mongoUri, accountId, bucket, accessKey, secret }.Any(string.IsNullOrWhiteSpace))
            return;

        var databaseName = $"tb_pat60_{Guid.NewGuid():N}"[..33];
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mongo:ConnectionString"] = mongoUri,
                    ["Mongo:Database"] = databaseName,
                    ["R2:AccountId"] = accountId,
                    ["R2:Bucket"] = bucket,
                    ["R2:AccessKeyId"] = accessKey,
                    ["R2:SecretAccessKey"] = secret
                })));
        var uploadedKeys = new List<string>();
        try
        {
            var store = factory.Services.GetRequiredService<MongoStore>();
            var storage = factory.Services.GetRequiredService<IImageStorage>();
            var failedDeletion = new ImageCleanup(store, new FailingDeleteStorage(),
                NullLogger<ImageCleanup>.Instance);
            const string retryKey = "pat60/delete-retry-probe";
            await failedDeletion.DeleteOrQueueAsync(retryKey);
            Assert.Equal(1, await store.PendingImageDeletes.CountDocumentsAsync(x => x.Key == retryKey));
            await store.PendingImageDeletes.DeleteOneAsync(x => x.Key == retryKey);
            var item = new Item { PostedByUserId = "owner", Title = "Test report" };
            await store.Items.InsertOneAsync(item);
            using var owner = ApiClient(factory, "owner");
            using var viewer = ApiClient(factory, "viewer");
            var png = Png();
            var route = $"/api/items/{item.Id}/comments";

            using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
            { AllowAutoRedirect = false });
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.GetAsync($"{route}/{item.Id}/image")).StatusCode);

            using var disguised = Form(png, "image/jpeg", "bag");
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync(route, disguised)).StatusCode);
            Assert.Equal(0, await store.Comments.CountDocumentsAsync(_ => true));

            using var twoImages = Form(png, "image/png", "bag");
            twoImages.Add(new ByteArrayContent(png), "imageFile", "second.png");
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync(route, twoImages)).StatusCode);
            using var unexpectedFile = new MultipartFormDataContent
            {
                { new StringContent("hello"), "body" },
                { new ByteArrayContent(png), "otherFile", "extra.png" }
            };
            Assert.Equal(HttpStatusCode.BadRequest,
                (await owner.PostAsync(route, unexpectedFile)).StatusCode);
            Assert.Equal(0, await store.Comments.CountDocumentsAsync(_ => true));

            using var oversized = Form(new byte[R2ImageStorage.MaxImageBytes + 1], "image/png", "bag");
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge,
                (await owner.PostAsync(route, oversized)).StatusCode);
            Assert.Equal(0, await store.Comments.CountDocumentsAsync(_ => true));

            using var imageOnly = Form(png, "image/png", "Red bag");
            using var created = await owner.PostAsync(route, imageOnly);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            using var createdJson = await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync());
            var commentId = createdJson.RootElement.GetProperty("id").GetString()!;
            Assert.Equal(JsonValueKind.Null, createdJson.RootElement.GetProperty("body").ValueKind);
            Assert.Equal("Red bag", createdJson.RootElement.GetProperty("imageAlt").GetString());
            Assert.Equal($"{route}/{commentId}/image",
                createdJson.RootElement.GetProperty("imageUrl").GetString());
            var firstKey = (await store.Comments.Find(x => x.Id == commentId).FirstAsync()).ImageKey!;
            uploadedKeys.Add(firstKey);
            Assert.Equal(png, (await storage.ReadAsync(firstKey))!.Bytes);

            using var viewed = await viewer.GetAsync($"{route}/{commentId}/image");
            Assert.Equal(HttpStatusCode.OK, viewed.StatusCode);
            Assert.Equal(png, await viewed.Content.ReadAsByteArrayAsync());
            Assert.True(viewed.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", viewed.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal(HttpStatusCode.Forbidden,
                (await viewer.DeleteAsync($"{route}/{commentId}/image")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest,
                (await owner.DeleteAsync($"{route}/{commentId}/image")).StatusCode);

            using var extraReplacement = Form(png, "image/png", "New view");
            extraReplacement.Add(new ByteArrayContent(png), "otherFile", "extra.png");
            using var extraRequest = new HttpRequestMessage(HttpMethod.Put, $"{route}/{commentId}/image")
            { Content = extraReplacement };
            Assert.Equal(HttpStatusCode.BadRequest,
                (await owner.SendAsync(extraRequest)).StatusCode);
            Assert.Equal(firstKey, (await store.Comments.Find(x => x.Id == commentId).FirstAsync()).ImageKey);

            using var replacement = Form(png, "image/png", "New view");
            using var replaceRequest = new HttpRequestMessage(HttpMethod.Put, $"{route}/{commentId}/image")
            { Content = replacement };
            Assert.Equal(HttpStatusCode.NoContent,
                (await owner.SendAsync(replaceRequest)).StatusCode);
            var secondKey = (await store.Comments.Find(x => x.Id == commentId).FirstAsync()).ImageKey!;
            uploadedKeys.Add(secondKey);
            Assert.NotEqual(firstKey, secondKey);
            Assert.Null(await storage.ReadAsync(firstKey));

            using var textAndImage = Form(png, "image/png", "Blue bag", "Found here");
            using var secondCreated = await owner.PostAsync(route, textAndImage);
            Assert.Equal(HttpStatusCode.Created, secondCreated.StatusCode);
            using var secondJson = await JsonDocument.ParseAsync(await secondCreated.Content.ReadAsStreamAsync());
            var textCommentId = secondJson.RootElement.GetProperty("id").GetString()!;
            var textKey = (await store.Comments.Find(x => x.Id == textCommentId).FirstAsync()).ImageKey!;
            uploadedKeys.Add(textKey);
            Assert.Equal(HttpStatusCode.NoContent,
                (await owner.DeleteAsync($"{route}/{textCommentId}/image")).StatusCode);
            Assert.Null(await storage.ReadAsync(textKey));

            using var cookie = CookieClient(factory, "owner");
            var antiForgeryToken = await AntiForgeryTokenAsync(cookie);
            var cookieRoute = $"/Items/{item.Id}/Comments";
            var countBeforeInvalid = await store.Comments.CountDocumentsAsync(_ => true);
            using var extraCookieImage = Form(png, "image/png", "Red bag");
            extraCookieImage.Add(new ByteArrayContent(png), "otherFile", "extra.png");
            extraCookieImage.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
            Assert.Equal(HttpStatusCode.BadRequest,
                (await cookie.PostAsync(cookieRoute, extraCookieImage)).StatusCode);
            Assert.Equal(countBeforeInvalid, await store.Comments.CountDocumentsAsync(_ => true));

            using var cookieImage = Form(png, "image/png", "Cookie view");
            cookieImage.Add(new StringContent(antiForgeryToken), "__RequestVerificationToken");
            using var cookieCreated = await cookie.PostAsync(cookieRoute, cookieImage);
            Assert.Equal(HttpStatusCode.Created, cookieCreated.StatusCode);
            using var cookieJson = await JsonDocument.ParseAsync(await cookieCreated.Content.ReadAsStreamAsync());
            var cookieCommentId = cookieJson.RootElement.GetProperty("id").GetString()!;
            var cookieImageUrl = cookieJson.RootElement.GetProperty("imageUrl").GetString()!;
            Assert.Equal($"{cookieRoute}/{cookieCommentId}/Image", cookieImageUrl);
            var cookieKey = (await store.Comments.Find(x => x.Id == cookieCommentId).FirstAsync()).ImageKey!;
            uploadedKeys.Add(cookieKey);
            Assert.Equal(HttpStatusCode.OK, (await cookie.GetAsync(cookieImageUrl)).StatusCode);
            using var cookieDelete = new FormUrlEncodedContent(
                new Dictionary<string, string> { ["__RequestVerificationToken"] = antiForgeryToken });
            Assert.Equal(HttpStatusCode.NoContent,
                (await cookie.PostAsync($"{cookieRoute}/{cookieCommentId}/Delete", cookieDelete)).StatusCode);
            Assert.Null(await storage.ReadAsync(cookieKey));

            Assert.Equal(HttpStatusCode.NoContent,
                (await owner.DeleteAsync($"{route}/{commentId}")).StatusCode);
            Assert.Null(await storage.ReadAsync(secondKey));
            Assert.Equal(HttpStatusCode.NotFound,
                (await viewer.GetAsync($"{route}/{commentId}/image")).StatusCode);

            using var finalImage = Form(png, "image/png", "Final image");
            using var finalCreated = await owner.PostAsync(route, finalImage);
            Assert.Equal(HttpStatusCode.Created, finalCreated.StatusCode);
            using var finalJson = await JsonDocument.ParseAsync(await finalCreated.Content.ReadAsStreamAsync());
            var finalId = finalJson.RootElement.GetProperty("id").GetString()!;
            var finalKey = (await store.Comments.Find(x => x.Id == finalId).FirstAsync()).ImageKey!;
            uploadedKeys.Add(finalKey);
            Assert.Equal(HttpStatusCode.NoContent,
                (await owner.DeleteAsync($"/api/items/{item.Id}")).StatusCode);
            Assert.Equal(0, await store.Comments.CountDocumentsAsync(x => x.ItemId == item.Id));
            Assert.Null(await storage.ReadAsync(finalKey));
            Assert.Equal(0, await store.PendingImageDeletes.CountDocumentsAsync(_ => true));

            var queued = Enumerable.Range(0, 101).Select(index => new PendingImageDelete
            {
                Key = $"pat60/stalled-{index:D3}",
                CreatedAt = DateTime.UtcNow.AddMilliseconds(index)
            }).ToList();
            await store.PendingImageDeletes.InsertManyAsync(queued);
            var stubbornStorage = new StubbornDeleteStorage(queued[^1].Key);
            await ImageCleanupWorker.ProcessBatchAsync(store, stubbornStorage,
                NullLogger<ImageCleanupWorker>.Instance);
            Assert.Empty(stubbornStorage.DeletedKeys);
            Assert.Equal(100, await store.PendingImageDeletes.CountDocumentsAsync(x => x.NextAttemptAt != null));
            await ImageCleanupWorker.ProcessBatchAsync(store, stubbornStorage,
                NullLogger<ImageCleanupWorker>.Instance);
            Assert.Equal([queued[^1].Key], stubbornStorage.DeletedKeys);
            Assert.Equal(100, await store.PendingImageDeletes.CountDocumentsAsync(_ => true));
        }
        finally
        {
            var storage = factory.Services.GetRequiredService<IImageStorage>();
            foreach (var key in uploadedKeys)
                await storage.DeleteAsync(key);
            await new MongoClient(mongoUri).DropDatabaseAsync(databaseName);
        }
    }

    private static MultipartFormDataContent Form(byte[] bytes, string type, string alt, string? body = null)
    {
        var content = new MultipartFormDataContent();
        var image = new ByteArrayContent(bytes);
        image.Headers.ContentType = new MediaTypeHeaderValue(type);
        content.Add(image, "imageFile", "sample.png");
        content.Add(new StringContent(alt), "imageAlt");
        if (body is not null) content.Add(new StringContent(body), "body");
        return content;
    }

    private static byte[] Png()
    {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static HttpClient ApiClient(WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var settings = factory.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(issuer: settings.Issuer, audience: "TraceBack.Api",
            claims: [new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId),
                new System.Security.Claims.Claim(ClaimTypes.Name, "Test User")],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static HttpClient CookieClient(WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var identity = new ClaimsIdentity(
            [new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId),
                new System.Security.Claims.Claim(ClaimTypes.Name, "Test User")],
            CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity),
            CookieAuthenticationDefaults.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        return client;
    }

    private static async Task<string> AntiForgeryTokenAsync(HttpClient client)
    {
        using var editor = await client.GetAsync("/Items/Create");
        Assert.Equal(HttpStatusCode.OK, editor.StatusCode);
        var markup = await editor.Content.ReadAsStringAsync();
        var token = WebUtility.HtmlDecode(Regex.Match(markup,
            "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var cookie = editor.Headers.GetValues("Set-Cookie").First(value =>
            value.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        client.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        return token;
    }

    private sealed class FailingDeleteStorage : IImageStorage
    {
        public Task<string> UploadAsync(string itemId, ImageUpload image,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<StoredImage?> ReadAsync(string key,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(string key,
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("R2 unavailable");
    }

    private sealed class StubbornDeleteStorage(string successfulKey) : IImageStorage
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> UploadAsync(string itemId, ImageUpload image,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<StoredImage?> ReadAsync(string key,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            if (key != successfulKey) throw new InvalidOperationException("R2 unavailable");
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
