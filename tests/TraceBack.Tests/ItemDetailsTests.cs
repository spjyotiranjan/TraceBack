using System.Net;
using System.Text.RegularExpressions;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [ReportBrowserFact]
    public async Task Item_details_pass_the_browser_matrix()
    {
        var reports = new FakeItemService { ServeValidImage = true };
        reports.Item.Type = "Found";
        reports.Item.Title = new string('W', 120);
        reports.Item.Description = string.Join('\n', Enumerable.Range(1, 14).Select(line => $"Report description line {line:D2}."));
        reports.Item.MapLocation = new MapLocation { Latitude = 20.296058, Longitude = 85.824539 };
        reports.AdditionalItems.AddRange([
            new Item { Type = "Lost", Title = "Lost keys", Description = "Short description.", PostedByUserId = "test-user", Location = "Studio" },
            new Item { Type = "Found", Status = "Claimed", Title = "Claimed bag", ImageKey = "missing.png" },
            new Item { Type = "Found", Status = "Resolved", Title = "Resolved gloves", ImageKey = "invalid.png", PostedByUserId = "test-user" }
        ]);
        await RunReportBrowserAsync(reports, "tests/ui/item-details-check.cjs");
    }

    [Fact]
    public async Task Owner_confirmation_posts_require_a_token_and_keep_existing_redirects()
    {
        var reports = new FakeItemService();
        reports.Item.PostedByUserId = "test-user";
        using var factory = CreateFactory(reports, new BrowserDashboardFixture(reports));
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        var id = reports.Item.Id;
        var detail = $"/Items/Details/{id}";

        using var unprotected = await client.PostAsync($"/Items/Resolve/{id}", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, unprotected.StatusCode);
        Assert.Equal("Active", reports.Item.Status);

        async Task<HttpResponseMessage> Post(string action, string markup)
        {
            var token = Regex.Match(markup, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
            Assert.NotEmpty(token);
            using var form = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
            return await client.PostAsync($"/Items/{action}/{id}", form);
        }

        var active = await client.GetStringAsync(detail);
        Assert.Contains($"action=\"/Items/Resolve/{id}\"", active);
        using var resolve = await Post("Resolve", active);
        Assert.Equal(HttpStatusCode.Redirect, resolve.StatusCode);
        Assert.Equal(detail, resolve.Headers.Location?.OriginalString);
        var resolved = await client.GetStringAsync(detail);
        Assert.Contains("The report is marked resolved.", resolved);
        Assert.DoesNotContain($"action=\"/Items/Resolve/{id}\"", resolved);
        using var stale = await Post("Resolve", resolved);
        Assert.Equal(HttpStatusCode.NotFound, stale.StatusCode);

        reports.Item.PostedByUserId = "other-user";
        using var forbidden = await Post("Delete", resolved);
        Assert.Equal(HttpStatusCode.Redirect, forbidden.StatusCode);
        Assert.Equal("/Account/Denied", forbidden.Headers.Location?.AbsolutePath);
        reports.Item.PostedByUserId = "test-user";
        using var delete = await Post("Delete", resolved);
        Assert.Equal(HttpStatusCode.Redirect, delete.StatusCode);
        Assert.Equal("/Dashboard", delete.Headers.Location?.OriginalString);
        Assert.Contains("The report and its claims were deleted.", await client.GetStringAsync("/Dashboard"));
    }
}