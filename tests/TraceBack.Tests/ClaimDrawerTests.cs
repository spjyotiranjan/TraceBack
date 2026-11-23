using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [ClaimBrowserFact]
    public async Task Claim_drawer_and_direct_fallback_pass_the_browser_matrix()
    {
        var reports = new FakeItemService();
        reports.Item.Type = "Found";
        reports.Item.Title = "Blue water bottle";
        reports.ServeValidImage = true;
        reports.AdditionalItems.Add(new Item { Type = "Found", Title = new string('W', 120), PostedByUserId = "finder" });
        var mongoUri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        var database = $"tb_claim_browser_{Guid.NewGuid():N}";
        var mongo = new MongoClient(mongoUri);
        try
        {
            await mongo.GetDatabase(database).GetCollection<Item>("items").InsertManyAsync([reports.Item, .. reports.AdditionalItems]);
            await RunReportBrowserAsync(reports, "tests/ui/claim-drawer-check.cjs", builder => ConfigureClaimDatabase(builder, mongoUri, database));
        }
        finally
        {
            await mongo.DropDatabaseAsync(database);
        }
    }

    [Fact]
    public async Task Claim_posts_keep_authorization_validation_and_dashboard_handoff()
    {
        var mongoUri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI");
        if (string.IsNullOrWhiteSpace(mongoUri)) return;
        var database = $"tb_claim_{Guid.NewGuid():N}";
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => ConfigureClaimDatabase(builder, mongoUri, database));
        var mongo = new MongoClient(mongoUri);
        try
        {
            using var guest = CreateClient(factory);
            using var member = CreateClient(factory);
            AddCookie(member, factory);
            var store = factory.Services.GetRequiredService<MongoStore>();
            var report = new Item { Type = "Found", Title = "Library bottle", PostedByUserId = "other-user" };
            await store.Items.InsertOneAsync(report);
            var route = $"/Claims/Create?itemId={report.Id}";
            const string draft = "  There is a small dent near the base.  ";

            using var guestGet = await guest.GetAsync(route);
            Assert.Equal("/Account/Login", guestGet.Headers.Location?.AbsolutePath);
            using var guestPost = await guest.PostAsync(route, new FormUrlEncodedContent([]));
            Assert.Equal("/Account/Login", guestPost.Headers.Location?.AbsolutePath);
            var markup = await member.GetStringAsync(route);
            var token = Regex.Match(markup, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.NotEmpty(token);

            async Task<HttpResponseMessage> Post(string text, bool drawer = false, bool withToken = true)
            {
                var fields = new Dictionary<string, string> { ["Message"] = text };
                if (withToken) fields["__RequestVerificationToken"] = token;
                using var request = new HttpRequestMessage(HttpMethod.Post, route) { Content = new FormUrlEncodedContent(fields) };
                if (drawer) request.Headers.Add("X-TraceBack-Claim", "drawer");
                return await member.SendAsync(request);
            }

            using var unprotected = await Post(draft, drawer: true, withToken: false);
            Assert.Equal(HttpStatusCode.BadRequest, unprotected.StatusCode);
            const string tooShort = "  Blue      ";
            using var invalid = await Post(tooShort);
            Assert.Contains($"{tooShort}</textarea>", await invalid.Content.ReadAsStringAsync());
            using var invalidDrawer = await Post(tooShort, drawer: true);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidDrawer.StatusCode);
            using var tooLong = await Post(new string('x', 1001), drawer: true);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLong.StatusCode);
            Assert.Equal(0, await store.Claims.CountDocumentsAsync(_ => true));

            var reports = Builders<Item>.Update;
            await store.Items.UpdateOneAsync(x => x.Id == report.Id, reports.Set(x => x.PostedByUserId, "test-user"));
            using var ownGet = await member.GetAsync(route);
            Assert.Equal("/Account/Denied", ownGet.Headers.Location?.AbsolutePath);
            using var ownPost = await Post(draft);
            Assert.Equal("/Account/Denied", ownPost.Headers.Location?.AbsolutePath);
            using var ownDrawer = await Post(draft, drawer: true);
            Assert.Equal(HttpStatusCode.Forbidden, ownDrawer.StatusCode);
            await store.Items.UpdateOneAsync(x => x.Id == report.Id, reports.Set(x => x.PostedByUserId, "other-user").Set(x => x.Type, "Lost"));
            using var lost = await member.GetAsync(route);
            Assert.Equal(HttpStatusCode.NotFound, lost.StatusCode);
            await store.Items.UpdateOneAsync(x => x.Id == report.Id, reports.Set(x => x.Type, "Found").Set(x => x.Status, "Resolved"));
            using var stale = await Post(draft, drawer: true);
            Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);
            await store.Items.UpdateOneAsync(x => x.Id == report.Id, reports.Set(x => x.Status, "Active"));

            // Simulate a real persistence rejection confined to the disposable claims collection.
            var db = mongo.GetDatabase(database);
            async Task ValidateClaims(BsonDocument validator) => await db.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                ["collMod"] = "claims",
                ["validator"] = validator
            });
            await ValidateClaims(new BsonDocument("testOnlyRequiredField", new BsonDocument("$exists", true)));
            using var failed = await Post(draft);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
            Assert.Contains($"{draft}</textarea>", await failed.Content.ReadAsStringAsync());
            using var failedDrawer = await Post(draft, drawer: true);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failedDrawer.StatusCode);
            await ValidateClaims(new BsonDocument());

            using var sent = await Post(draft, drawer: true);
            Assert.Equal(HttpStatusCode.OK, sent.StatusCode);
            Assert.Equal("/Dashboard", (await sent.Content.ReadFromJsonAsync<ClaimRedirect>())?.RedirectUrl);
            Assert.Contains("Your claim was submitted.", await member.GetStringAsync("/Dashboard"));
            Assert.Equal(draft.Trim(), (await store.Claims.Find(_ => true).SingleAsync()).Message);
            using var duplicate = await Post(draft);
            Assert.Contains($"{draft}</textarea>", await duplicate.Content.ReadAsStringAsync());
            using var duplicateDrawer = await Post(draft, drawer: true);
            Assert.Equal(HttpStatusCode.Conflict, duplicateDrawer.StatusCode);
            Assert.Equal(1, await store.Claims.CountDocumentsAsync(_ => true));

            await store.Claims.DeleteManyAsync(_ => true);
            using var direct = await Post(draft);
            Assert.Equal("/Dashboard", direct.Headers.Location?.OriginalString);
            Assert.Contains("Your claim was submitted.", await member.GetStringAsync("/Dashboard"));
            await store.Items.DeleteOneAsync(x => x.Id == report.Id);
            using var missing = await Post(draft);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }
        finally
        {
            await mongo.DropDatabaseAsync(database);
        }
    }

    private sealed record ClaimRedirect(string RedirectUrl);

    private static void ConfigureClaimDatabase(IWebHostBuilder builder, string mongoUri, string database) =>
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = mongoUri,
                ["Mongo:Database"] = database,
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = ""
            }));

    private sealed class ClaimBrowserFactAttribute : FactAttribute
    {
        public ClaimBrowserFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("TRACEBACK_BROWSER_TESTS") != "1"
                || string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")))
            {
                Skip = "Set TRACEBACK_BROWSER_TESTS=1 and TRACEBACK_TEST_MONGO_URI and make Playwright/Chromium available.";
            }
        }
    }
}
