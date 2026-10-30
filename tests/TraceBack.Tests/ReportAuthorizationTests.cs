using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class ReportAuthorizationTests
{
    private static readonly byte[] ImageBytes = [1, 2, 3, 4];

    [Fact]
    public async Task Anonymous_requests_cannot_read_report_pages_images_or_api_data()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);

        foreach (var path in new[]
        {
            "/Items", $"/Items/Details/{reports.Item.Id}", $"/Items/Image/{reports.Item.Id}"
        })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location?.AbsolutePath);
        }

        foreach (var path in new[] { "/api/items", $"/api/items/{reports.Item.Id}" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        Assert.Equal(0, reports.ReadCount);
    }

    [Fact]
    public async Task Public_home_and_account_pages_do_not_query_or_display_reports()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(null);
        using var client = CreateClient(factory);

        using var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.True(home.Headers.CacheControl?.NoStore);
        var markup = await home.Content.ReadAsStringAsync();
        Assert.Contains("A better way back for your belongings.", markup);
        Assert.Contains("Reports are available after sign-in.", markup);
        Assert.Contains("Create account", markup);
        Assert.DoesNotContain("Recent reports", markup);
        Assert.DoesNotContain("Browse all", markup);
        Assert.DoesNotContain("Browse lost", markup);
        Assert.DoesNotContain("Report an item", markup);
        Assert.DoesNotContain(reports.Item.Title, markup);
        Assert.DoesNotContain("No active reports yet", markup);

        foreach (var path in new[] { "/Account/Login", "/Account/Register" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        Assert.Equal(0, reports.ReadCount);
    }

    [Fact]
    public async Task Cookie_sign_in_grants_mvc_report_and_image_access_but_not_api_access()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        foreach (var path in new[] { "/", "/Items", $"/Items/Details/{reports.Item.Id}" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            var markup = await response.Content.ReadAsStringAsync();
            Assert.Contains(reports.Item.Title, markup);
            if (path == "/")
            {
                Assert.Contains("Search belongings", markup);
                Assert.Contains("Browse lost", markup);
                Assert.Contains("Recent reports", markup);
                Assert.DoesNotContain("A better way back for your belongings.", markup);
            }
        }

        using var image = await client.GetAsync($"/Items/Image/{reports.Item.Id}");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.True(image.Headers.CacheControl?.NoStore);
        Assert.Equal(ImageBytes, await image.Content.ReadAsByteArrayAsync());

        using var api = await client.GetAsync("/api/items");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        Assert.True(reports.ReadCount >= 4);
    }

    [Fact]
    public async Task Bearer_token_grants_api_search_and_detail_but_not_mvc_access()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddBearerToken(client, factory);

        foreach (var path in new[] { "/api/items", $"/api/items/{reports.Item.Id}" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Contains(reports.Item.Title, await response.Content.ReadAsStringAsync());
        }

        using var mvc = await client.GetAsync("/Items");
        Assert.Equal(HttpStatusCode.Redirect, mvc.StatusCode);
        Assert.Equal("/Account/Login", mvc.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Comment_routes_enforce_cookie_bearer_and_antiforgery_boundaries()
    {
        var comments = new FakeCommentService();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICommentService>();
                services.AddSingleton<ICommentService>(comments);
            }));
        var path = $"/Items/{comments.ItemId}/Comments";
        var apiPath = $"/api/items/{comments.ItemId}/comments";

        using (var anonymous = CreateClient(factory))
        {
            Assert.Equal(HttpStatusCode.Redirect, (await anonymous.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(apiPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect,
                (await anonymous.GetAsync($"{path}/{comments.CommentId}/Image")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await anonymous.GetAsync($"{apiPath}/{comments.CommentId}/image")).StatusCode);
        }

        using (var cookie = CreateClient(factory))
        {
            AddCookie(cookie, factory);
            Assert.Equal(HttpStatusCode.OK, (await cookie.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await cookie.GetAsync(apiPath)).StatusCode);
            using var image = await cookie.GetAsync($"{path}/{comments.CommentId}/Image");
            Assert.Equal(HttpStatusCode.OK, image.StatusCode);
            Assert.True(image.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await cookie.GetAsync($"{apiPath}/{comments.CommentId}/image")).StatusCode);
            using var form = new MultipartFormDataContent { { new StringContent("hello"), "body" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await cookie.PostAsync(path, form)).StatusCode);
            Assert.Equal(0, comments.CreateCount);
        }

        using (var bearer = CreateClient(factory))
        {
            AddBearerToken(bearer, factory);
            Assert.Equal(HttpStatusCode.OK, (await bearer.GetAsync(apiPath)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await bearer.GetAsync(path)).StatusCode);
            using var image = await bearer.GetAsync($"{apiPath}/{comments.CommentId}/image");
            Assert.Equal(HttpStatusCode.OK, image.StatusCode);
            Assert.True(image.Headers.CacheControl?.NoStore);
            Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal(HttpStatusCode.Redirect,
                (await bearer.GetAsync($"{path}/{comments.CommentId}/Image")).StatusCode);
            using var form = new MultipartFormDataContent { { new StringContent("hello"), "body" } };
            Assert.Equal(HttpStatusCode.Created, (await bearer.PostAsync(apiPath, form)).StatusCode);
            Assert.Equal(1, comments.CreateCount);
        }
    }

    [Fact]
    public async Task New_report_requires_a_valid_point_through_bearer_api()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        var form = new ItemForm
        {
            Title = "Found keys",
            Description = "A set of keys",
            Category = "Keys",
            Location = "Library",
            DateReported = DateTime.UtcNow.Date,
            ContactPreference = "Help desk"
        };

        using var anonymous = await client.PostAsJsonAsync("/api/items", form);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        AddBearerToken(client, factory);
        using var missing = await client.PostAsJsonAsync("/api/items", form);
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Contains("location", await missing.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, reports.CreateCount);

        form.MapLocation = new MapLocationInput();
        using var blank = await client.PostAsJsonAsync("/api/items", form);
        Assert.Equal(HttpStatusCode.BadRequest, blank.StatusCode);
        Assert.Contains("location", await blank.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, reports.CreateCount);

        form.MapLocation = new MapLocationInput { Latitude = 91, Longitude = 0 };
        using var invalid = await client.PostAsJsonAsync("/api/items", form);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(0, reports.CreateCount);

        form.MapLocation = new MapLocationInput { Latitude = 20.296058, Longitude = 85.824539 };
        using var created = await client.PostAsJsonAsync("/api/items", form);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(1, reports.CreateCount);
        var body = await created.Content.ReadAsStringAsync();
        Assert.Contains("20.296058", body);
        Assert.Contains("85.824539", body);
    }

    [Fact]
    public async Task Signed_in_detail_shows_safe_map_link_and_legacy_text_without_a_pin()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        using var legacy = await client.GetAsync($"/Items/Details/{reports.Item.Id}");
        var legacyMarkup = await legacy.Content.ReadAsStringAsync();
        Assert.Contains("Library", legacyMarkup);
        Assert.Contains("Map location not available", legacyMarkup);
        Assert.DoesNotContain("google.com/maps/search", legacyMarkup);

        reports.Item.MapLocation = new MapLocation { Latitude = 20.296058, Longitude = 85.824539 };
        using var mapped = await client.GetAsync($"/Items/Details/{reports.Item.Id}");
        var mappedMarkup = await mapped.Content.ReadAsStringAsync();
        Assert.Contains("20.296058", mappedMarkup);
        Assert.Contains("google.com/maps/search/?api=1&amp;query=20.296058%2C85.824539", mappedMarkup);
        Assert.Contains("rel=\"noopener noreferrer\"", mappedMarkup);
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeItemService? reports) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IItemService>();
                if (reports is not null)
                {
                    services.AddSingleton<IItemService>(reports);
                }
            }));

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static void AddCookie(HttpClient client, WebApplicationFactory<Program> factory)
    {
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = Principal();
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        var value = options.TicketDataFormat.Protect(ticket);
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={value}");
    }

    private static void AddBearerToken(HttpClient client, WebApplicationFactory<Program> factory)
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: "TraceBack.Api",
            claims: Principal().Claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }

    private static ClaimsPrincipal Principal() => new(new ClaimsIdentity(
        [new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, "test-user"), new System.Security.Claims.Claim(ClaimTypes.Name, "Test User")],
        CookieAuthenticationDefaults.AuthenticationScheme));

    private sealed class FakeItemService : IItemService
    {
        private int readCount;
        private int createCount;

        public Item Item { get; } = new()
        {
            Title = "Blue backpack fixture",
            Description = "Test report",
            Category = "Bag",
            Location = "Library",
            ContactPreference = "Message me",
            ImageKey = "items/test/image.png",
            PostedByUserId = "other-user"
        };

        public int ReadCount => Volatile.Read(ref readCount);
        public int CreateCount => Volatile.Read(ref createCount);

        public Task<List<Item>> SearchAsync(ItemSearch search)
        {
            Interlocked.Increment(ref readCount);
            return Task.FromResult(new List<Item> { Item });
        }

        public Task<Item?> GetAsync(string id)
        {
            Interlocked.Increment(ref readCount);
            return Task.FromResult(id == Item.Id ? Item : null);
        }

        public Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref readCount);
            return Task.FromResult<StoredImage?>(id == Item.Id ? new StoredImage(ImageBytes, "image/png") : null);
        }

        public Task<List<Item>> MineAsync(string userId) => throw new NotSupportedException();
        public Task<Item> CreateAsync(ItemForm form, string userId)
        {
            Interlocked.Increment(ref createCount);
            Item.Location = form.Location;
            Item.MapLocation = new MapLocation
            {
                Latitude = form.MapLocation!.Latitude!.Value,
                Longitude = form.MapLocation.Longitude!.Value
            };
            return Task.FromResult(Item);
        }
        public Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> ResolveAsync(string id, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> RemoveImageAsync(string id, string userId, bool admin) => throw new NotSupportedException();
    }

    private sealed class FakeCommentService : ICommentService
    {
        public string ItemId { get; } = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        public string CommentId { get; } = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        public int CreateCount { get; private set; }

        public Task<CommentResult<CommentPage>> ListAsync(string itemId, string? parentId, string? cursor, int limit) =>
            Task.FromResult(new CommentResult<CommentPage>(new CommentPage([], null)));

        public Task<CommentResult<CommentView>> CreateAsync(
            string itemId, string? parentId, string? body, string authorId, string authorName,
            ImageUpload? image = null, string? imageAlt = null)
        {
            CreateCount++;
            return Task.FromResult(new CommentResult<CommentView>(new CommentView(
                MongoDB.Bson.ObjectId.GenerateNewId().ToString(), itemId, parentId,
                authorId, authorName, body, DateTime.UtcNow, false)));
        }

        public Task<CommentError> DeleteAsync(string itemId, string commentId, string userId, bool isAdmin) =>
            Task.FromResult(CommentError.None);

        public Task<CommentError> SetImageAsync(string itemId, string commentId, ImageUpload image,
            string? imageAlt, string userId, bool isAdmin) => Task.FromResult(CommentError.None);

        public Task<CommentError> RemoveImageAsync(string itemId, string commentId,
            string userId, bool isAdmin) => Task.FromResult(CommentError.None);

        public Task<CommentResult<StoredImage>> GetImageAsync(string itemId, string commentId) =>
            Task.FromResult(new CommentResult<StoredImage>(new StoredImage(ImageBytes, "image/png")));
    }
}
