using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
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
        Assert.Contains("Sign in to see recent reports", markup);
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
            Assert.Contains(reports.Item.Title, await response.Content.ReadAsStringAsync());
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
        public Task<Item> CreateAsync(ItemForm form, string userId) => throw new NotSupportedException();
        public Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> ResolveAsync(string id, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string id, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin) => throw new NotSupportedException();
        public Task<bool> RemoveImageAsync(string id, string userId, bool admin) => throw new NotSupportedException();
    }
}
