using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [Fact]
    public async Task Access_denied_preserves_the_cookie_forbid_and_public_recovery_contract()
    {
        using var factory = CreateFactory(new FakeItemService());
        using var guest = CreateClient(factory);
        using var member = CreateClient(factory);
        AddCookie(member, factory);

        using var challenge = await guest.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        Assert.Equal("/Account/Login", challenge.Headers.Location?.AbsolutePath);

        using var forbidden = await member.GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        var location = Assert.IsType<Uri>(forbidden.Headers.Location);
        Assert.Equal("/Account/Denied", location.AbsolutePath);
        Assert.Equal("/Admin", Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(location.Query)["ReturnUrl"].ToString());

        using var ajax = new HttpRequestMessage(HttpMethod.Get, "/Admin");
        ajax.Headers.Add("X-Requested-With", "XMLHttpRequest");
        using var ajaxForbidden = await member.SendAsync(ajax);
        Assert.Equal(HttpStatusCode.Forbidden, ajaxForbidden.StatusCode);

        foreach (var (client, target) in new[] { (guest, "/Account/Denied"), (member, location.PathAndQuery) })
        {
            using var denied = await client.GetAsync(target);
            Assert.Equal(HttpStatusCode.OK, denied.StatusCode);
            var markup = await denied.Content.ReadAsStringAsync();
            AssertRouteArt(markup, "denied");
            Assert.Contains("tb-recovery-content--page", markup);
            Assert.Contains("tb-recovery-content__art--access\" aria-hidden=\"true\"", markup);
            Assert.Contains("<h1>Access denied</h1>", markup);
            Assert.Contains("Your account does not have permission to open this page. You can return to the home page and continue with the areas available to you.", markup);
            Assert.Matches("<a[^>]*href=\"/\"[^>]*>\\s*<span>Go home</span>\\s*</a>", markup);
        }
    }

    [BrowserFact]
    public Task Recovery_pages_in_browser() => RunShellBrowserChecks("access-denied-check.cjs", "item-not-found-check.cjs", "privacy-check.cjs");

    [BrowserFact]
    public Task Recovery_and_privacy_match_reference_geometry_in_browser() => RunShellBrowserChecks("recovery-parity.cjs");

    [Fact]
    public async Task Unknown_report_and_global_404_are_distinct_for_guests_and_members()
    {
        using var factory = CreateFactory(new FakeItemService());
        using var guest = CreateClient(factory);
        using var member = CreateClient(factory);
        AddCookie(member, factory);
        foreach (var client in new[] { guest, member })
        {
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
            await AssertMissingReport(client, ObjectId.GenerateNewId().ToString());
            using var other = await client.GetAsync("/no-such-route/recovery-probe");
            Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
            Assert.DoesNotContain("<h1>Item not found</h1>", await other.Content.ReadAsStringAsync());
        }
    }

    [BrowseMongoFact]
    public async Task Deleted_report_returns_404_for_guests_and_members()
    {
        var uri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        var database = $"tb_recovery_{Guid.NewGuid():N}";
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => ConfigureClaimDatabase(builder, uri, database));
        var mongo = new MongoClient(uri);
        try
        {
            using var guest = CreateClient(factory);
            using var member = CreateClient(factory);
            AddCookie(member, factory);
            var store = factory.Services.GetRequiredService<MongoStore>();
            var item = new Item { Title = "Deleted report fixture", Type = "Found", PostedByUserId = "test-user" };
            await store.Items.InsertOneAsync(item);
            var path = $"/Items/Details/{item.Id}";
            using var before = await member.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);
            using var challenge = await guest.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
            Assert.Equal("/Account/Login", challenge.Headers.Location?.AbsolutePath);
            using var ajax = new HttpRequestMessage(HttpMethod.Get, path);
            ajax.Headers.Add("X-Requested-With", "XMLHttpRequest");
            using var ajaxChallenge = await guest.SendAsync(ajax);
            Assert.Equal(HttpStatusCode.Unauthorized, ajaxChallenge.StatusCode);

            using var api = CreateClient(factory);
            AddBearerToken(api, factory);
            using var apiOnly = await api.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, apiOnly.StatusCode);
            Assert.Equal("/Account/Login", apiOnly.Headers.Location?.AbsolutePath);
            using var removal = await api.DeleteAsync($"/api/items/{item.Id}");
            Assert.Equal(HttpStatusCode.NoContent, removal.StatusCode);
            Assert.Equal(0, await store.Items.CountDocumentsAsync(x => x.Id == item.Id));
            await AssertMissingReport(guest, item.Id);
            await AssertMissingReport(member, item.Id);
        }
        finally
        {
            await mongo.DropDatabaseAsync(database);
        }
    }

    private static async Task AssertMissingReport(HttpClient client, string id)
    {
        using var response = await client.GetAsync($"/Items/Details/{id}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var markup = await response.Content.ReadAsStringAsync();
        Assert.Contains("<h1>Item not found</h1>", markup);
        Assert.Contains("tb-recovery-content--page", markup);
        AssertRouteArt(markup, "not-found");
        Assert.Matches("<a[^>]*href=\"/Items\"[^>]*>\\s*<span>Browse items</span>\\s*</a>", markup);
    }
}
