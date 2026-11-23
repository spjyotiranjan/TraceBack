using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed partial class MapLocationIntegrationTests
{
    [Fact]
    public async Task Create_multipart_failure_rolls_back_before_retry_and_edit_keeps_current_photo_on_invalid_replacement()
    {
        var mongo = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI");
        if (string.IsNullOrWhiteSpace(mongo)) return;
        var database = $"tb_editor_{Guid.NewGuid():N}";
        var storage = new EditorImageStorage();
        var failRollback = false;
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = mongo,
                ["Mongo:Database"] = database,
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = ""
            }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IImageStorage>();
                services.AddSingleton<IImageStorage>(storage);
                services.RemoveAll<IItemImageRepository>();
                services.AddSingleton<IItemImageRepository>(provider => new EditorImageRepository(
                    new MongoItemImageRepository(provider.GetRequiredService<MongoStore>(), provider.GetRequiredService<IImageCleanup>()),
                    () => failRollback));
            });
        });
        var runBrowser = Environment.GetEnvironmentVariable("TRACEBACK_EDITOR_BROWSER") == "1";
        try
        {
            using var guest = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            Assert.Equal(HttpStatusCode.Redirect, (await guest.GetAsync("/Items/Create")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await guest.PostAsync("/Items/Create", Multipart(""))).StatusCode);
            using var owner = MvcClient(factory, "owner");
            var token = await AntiForgeryTokenAsync(owner);
            var reports = factory.Services.GetRequiredService<MongoStore>().Items;
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync("/Items/Create", Multipart(""))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.PostAsync("/Items/Create", Multipart(token, includeDate: false))).StatusCode);
            Assert.Equal(0, await reports.CountDocumentsAsync(_ => true));

            storage.FailUpload = true;
            using var failed = await owner.PostAsync("/Items/Create", Multipart(token));
            Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
            var failedMarkup = await failed.Content.ReadAsStringAsync();
            Assert.Contains("Could not store the image.", failedMarkup);
            Assert.Contains("The previous file was not carried over.", failedMarkup);
            Assert.Equal(0, await reports.CountDocumentsAsync(_ => true));

            storage.FailUpload = false;
            using var success = await owner.PostAsync("/Items/Create", Multipart(token));
            Assert.Equal(HttpStatusCode.Redirect, success.StatusCode);
            var stored = await reports.Find(_ => true).SingleAsync();
            Assert.EndsWith($"/Items/Details/{stored.Id}", success.Headers.Location!.OriginalString);
            using var details = await owner.GetAsync(success.Headers.Location);
            Assert.Contains("Your report is live.", await details.Content.ReadAsStringAsync());
            using var image = await owner.GetAsync($"/Items/Image/{stored.Id}");
            Assert.Equal("image/gif", image.Content.Headers.ContentType?.MediaType);
            Assert.Equal(EditorGif, await image.Content.ReadAsByteArrayAsync());
            Assert.Single(storage.Media);

            var editPath = $"/Items/Edit/{stored.Id}";
            using var guestEdit = await guest.PostAsync(editPath, Multipart("", file: false));
            Assert.Equal(HttpStatusCode.Redirect, guestEdit.StatusCode);
            Assert.Equal("/Account/Login", guestEdit.Headers.Location?.AbsolutePath);
            using var invalid = await owner.PostAsync(editPath, Multipart(token, validImage: false));
            Assert.Equal(HttpStatusCode.OK, invalid.StatusCode);
            var markup = await invalid.Content.ReadAsStringAsync();
            Assert.Contains("Current report image", markup);
            Assert.Contains("The previous file was not carried over.", markup);
            using var conflict = await owner.PostAsync(editPath, Multipart(token, remove: true));
            Assert.Contains("not both", await conflict.Content.ReadAsStringAsync());
            Assert.Equal(stored.ImageKey, (await reports.Find(_ => true).SingleAsync()).ImageKey);
            Assert.Single(storage.Media);

            // A successful save in another tab must invalidate the older draft, including its photo mutation.
            var revision = await EditRevisionAsync(owner, editPath);
            using var newer = await owner.PostAsync(editPath, Multipart(token, title: "Newer edit", file: false, revision: revision));
            Assert.Equal(HttpStatusCode.Redirect, newer.StatusCode);
            using var stale = await owner.PostAsync(editPath, Multipart(token, title: "Stale edit", remove: true, file: false, revision: revision));
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            var recovery = await stale.Content.ReadAsStringAsync();
            Assert.Contains($"href=\"{editPath}\"", recovery);
            Assert.Contains("Reload report", recovery);
            var afterStale = await reports.Find(x => x.Id == stored.Id).SingleAsync();
            Assert.Equal("Newer edit", afterStale.Title);
            Assert.Equal(stored.ImageKey, afterStale.ImageKey);
            Assert.Single(storage.Media);

            using var other = MvcClient(factory, "other");
            var otherToken = await AntiForgeryTokenAsync(other);
            using var forbidden = await other.PostAsync(editPath, Multipart(otherToken, file: false, revision: revision));
            Assert.Equal("/Account/Denied", forbidden.Headers.Location?.AbsolutePath);
            using var unverified = await owner.PostAsync(editPath, Multipart("", file: false));
            Assert.Equal(HttpStatusCode.BadRequest, unverified.StatusCode);
            using var missing = await owner.PostAsync($"/Items/Edit/{MongoDB.Bson.ObjectId.GenerateNewId()}", Multipart(token, file: false));
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

            storage.FailUpload = true;
            var nextRevision = await EditRevisionAsync(owner, editPath);
            using var partial = await owner.PostAsync(editPath, Multipart(token, title: "Details saved without new photo", revision: nextRevision));
            Assert.Equal(HttpStatusCode.Redirect, partial.StatusCode);
            using var partialDetails = await owner.GetAsync(partial.Headers.Location);
            var partialMarkup = await partialDetails.Content.ReadAsStringAsync();
            Assert.Contains("Details saved without new photo", partialMarkup);
            Assert.Contains("Report details were saved, but the image could not be changed.", partialMarkup);
            Assert.Equal(stored.ImageKey, (await reports.Find(x => x.Id == stored.Id).SingleAsync()).ImageKey);
            Assert.Single(storage.Media);
            storage.FailUpload = false;

            var revisionBeforePhoto = await EditRevisionAsync(owner, editPath);
            using var replaced = await owner.PostAsync(editPath, Multipart(token, revision: revisionBeforePhoto));
            Assert.Equal(HttpStatusCode.Redirect, replaced.StatusCode);
            var replacement = (await reports.Find(x => x.Id == stored.Id).SingleAsync()).ImageKey;
            Assert.NotNull(replacement);
            Assert.NotEqual(stored.ImageKey, replacement);
            Assert.DoesNotContain(stored.ImageKey!, storage.Media.Keys);
            Assert.NotEqual(revisionBeforePhoto, await EditRevisionAsync(owner, editPath));
            using var removed = await owner.PostAsync(editPath, Multipart(token, remove: true, file: false, revision: await EditRevisionAsync(owner, editPath)));
            Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode);
            Assert.Null((await reports.Find(x => x.Id == stored.Id).SingleAsync()).ImageKey);
            Assert.Empty(storage.Media);
            using var noPhoto = await owner.GetAsync(editPath);
            Assert.Contains("No photo selected", await noPhoto.Content.ReadAsStringAsync());

            await reports.UpdateOneAsync(x => x.Id == stored.Id, Builders<Item>.Update.Set(x => x.Status, "Resolved"));
            using var inactiveGet = await owner.GetAsync(editPath);
            using var inactivePost = await owner.PostAsync(editPath, Multipart(token, file: false, revision: revision));
            Assert.Equal(HttpStatusCode.BadRequest, inactiveGet.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, inactivePost.StatusCode);

            failRollback = storage.FailUpload = true;
            using var notRolledBack = await owner.PostAsync("/Items/Create", Multipart(token));
            Assert.Equal(HttpStatusCode.Redirect, notRolledBack.StatusCode);
            using var retainedDetails = await owner.GetAsync(notRolledBack.Headers.Location);
            Assert.Contains("Open Edit report to try the photo again.", await retainedDetails.Content.ReadAsStringAsync());
            Assert.Equal(2, await reports.CountDocumentsAsync(_ => true));
            failRollback = storage.FailUpload = false;
            if (runBrowser)
            {
                using var browserFactory = factory.WithWebHostBuilder(_ => { });
                browserFactory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
                using var browserHost = browserFactory.CreateClient();
                await RunEditorBrowserAsync(browserFactory, "report-editor-check.cjs");
                await RunEditorBrowserAsync(browserFactory, "report-map-check.cjs");
            }
        }
        finally { await new MongoClient(mongo).DropDatabaseAsync(database); }
    }

    private static async Task RunEditorBrowserAsync(WebApplicationFactory<Program> factory, string script)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TraceBack.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = root.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add($"tests/ui/{script}");
        start.Environment["TRACEBACK_URL"] = factory.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        start.Environment["EDITOR_STORED_PHOTO"] = "1";
        using var browser = Process.Start(start)!;
        var stdout = browser.StandardOutput.ReadToEndAsync();
        var stderr = browser.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try { await browser.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            browser.Kill(entireProcessTree: true);
            throw;
        }
        var output = $"{await stdout}\n{await stderr}";
        Assert.True(browser.ExitCode == 0, output);
        Console.WriteLine(output);
    }

    private static readonly byte[] EditorGif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    private static async Task<string> EditRevisionAsync(HttpClient client, string editPath)
    {
        using var editor = await client.GetAsync(editPath);
        Assert.Equal(HttpStatusCode.OK, editor.StatusCode);
        var markup = await editor.Content.ReadAsStringAsync();
        var revision = System.Text.RegularExpressions.Regex.Match(markup, "name=\"EditRevision\" value=\"([0-9]+)\"").Groups[1].Value;
        Assert.NotEmpty(revision);
        return revision;
    }

    private static MultipartFormDataContent Multipart(string token, bool validImage = true, bool includeDate = true,
        bool remove = false, bool file = true, string title = "Editor multipart test", string? revision = null)
    {
        var body = new MultipartFormDataContent();
        foreach (var (key, value) in new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Type"] = "Lost",
            ["Title"] = title,
            ["Category"] = "Keys",
            ["Description"] = "A key on a ring",
            ["Location"] = "Library",
            ["ContactPreference"] = "Campus desk",
            ["MapLocation.Latitude"] = "20.5",
            ["MapLocation.Longitude"] = "85.5"
        }) body.Add(new StringContent(value), key);
        if (includeDate) body.Add(new StringContent("2024-01-01"), "DateReported");
        if (remove) body.Add(new StringContent("true"), "RemoveImage");
        if (revision is not null) body.Add(new StringContent(revision), "EditRevision");
        if (file)
        {
            var image = new ByteArrayContent(validImage ? EditorGif : "not a photo"u8.ToArray());
            image.Headers.ContentType = new MediaTypeHeaderValue("image/gif");
            body.Add(image, "ImageFile", "photo.gif");
        }
        return body;
    }

    private sealed class EditorImageRepository(IItemImageRepository inner, Func<bool> failRollback) : IItemImageRepository
    {
        public Task<Item?> GetAsync(string id) => inner.GetAsync(id);
        public Task<bool> DeleteItemAsync(Item item, string userId, bool admin) =>
            failRollback() ? Task.FromResult(false) : inner.DeleteItemAsync(item, userId, admin);
        public Task DeleteClaimsAsync(string itemId) => inner.DeleteClaimsAsync(itemId);
        public Task DeleteCommentsAsync(string itemId) => inner.DeleteCommentsAsync(itemId);
        public Task<bool> ChangeImageAsync(Item item, string? key, string userId, bool admin) => inner.ChangeImageAsync(item, key, userId, admin);
    }

    private sealed class EditorImageStorage : IImageStorage
    {
        public bool FailUpload { get; set; }
        public ConcurrentDictionary<string, StoredImage> Media { get; } = new();

        public Task<string> UploadAsync(string itemId, ImageUpload image, CancellationToken cancellationToken = default)
        {
            if (FailUpload) throw new IOException("Controlled image storage failure");
            var key = $"items/{itemId}/{Guid.NewGuid():N}.{image.Extension}";
            Media[key] = new StoredImage(image.Bytes, image.ContentType);
            return Task.FromResult(key);
        }

        public Task<StoredImage?> ReadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(Media.GetValueOrDefault(key));

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            Media.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
