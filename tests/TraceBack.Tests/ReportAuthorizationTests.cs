using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TraceBack.Controllers;
using TraceBack.Models;
using TraceBack.Services;
using Xunit.Abstractions;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests(ITestOutputHelper output)
{
    private static readonly byte[] ImageBytes = [1, 2, 3, 4];

    [Theory]
    [InlineData("/", "home-guest", 200)]
    [InlineData("/Account/Login", "auth", 200)]
    [InlineData("/Account/Register", "auth", 200)]
    [InlineData("/Account/Denied", "denied", 200)]
    [InlineData("/no-such-route/fixture", "not-found", 404)]
    [InlineData("/Home/Error", "error", 200)]
    [InlineData("/Home/Privacy", "privacy", 200)]
    public async Task Public_views_select_their_decorative_route_art(string path, string variant, int expectedStatus)
    {
        using var factory = CreateFactory(null);
        using var client = CreateClient(factory);
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        using var response = await client.GetAsync(path);
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        AssertRouteArt(await response.Content.ReadAsStringAsync(), variant);
    }

    [Fact]
    public async Task Member_views_select_route_art_including_shared_create_and_edit()
    {
        var reports = new FakeItemService();
        reports.Item.PostedByUserId = "test-user";
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        foreach (var (path, variant, status) in new[]
        {
            ("/", "home-member", 200),
            ("/Items", "browse", 200),
            ($"/Items/Details/{reports.Item.Id}", "details", 200),
            ("/Items/Create", "editor", 200),
            ($"/Items/Edit/{reports.Item.Id}", "editor", 200),
            ($"/Items/Details/{MongoDB.Bson.ObjectId.GenerateNewId()}", "not-found", 404)
        })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(status, (int)response.StatusCode);
            AssertRouteArt(await response.Content.ReadAsStringAsync(), variant);
        }
    }

    private static void AssertRouteArt(string markup, string variant)
    {
        var art = markup.IndexOf("data-route-art=\"", StringComparison.Ordinal);
        Assert.True(art >= 0, "Missing route-art layer.");
        Assert.Contains($"data-route-art=\"{variant}\" aria-hidden=\"true\"", markup);
        Assert.True(art < markup.IndexOf("<main", StringComparison.Ordinal));
    }

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
    public async Task Shared_navigation_exposes_guest_routes_and_marks_the_current_page()
    {
        using var factory = CreateFactory(null);
        using var client = CreateClient(factory);
        var markup = await client.GetStringAsync("/account/login");

        Assert.Matches("<a[^>]*href=\"/Items\"[^>]*>\\s*<span[^>]*>\\s*Browse\\s*</span>\\s*</a>", markup);
        Assert.Matches("<a[^>]*aria-current=\"page\"[^>]*href=\"/Account/Login\"[^>]*>", markup);
        Assert.DoesNotContain("aria-label=\"Member navigation\"", markup);
        Assert.DoesNotContain("/Account/Logout", markup);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Shared_member_navigation_marks_browse_and_gates_admin_actions(bool admin)
    {
        using var factory = CreateFactory(new FakeItemService());
        using var client = CreateClient(factory);
        AddCookie(client, factory, admin);

        var markup = await client.GetStringAsync("/items?Query=backpack");
        Assert.Contains("aria-label=\"Member navigation\"", markup);
        Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(markup,
            "<a[^>]*aria-current=\"page\"[^>]*href=\"/Items\"[^>]*>").Count);
        Assert.Equal(admin, markup.Contains("href=\"/Admin\""));
        Assert.Contains("action=\"/Account/Logout\"", markup);
        Assert.Contains("name=\"__RequestVerificationToken\"", markup);
    }

    [BrowserFact]
    public Task Shared_shell_in_browser() => RunShellBrowserChecks("shell-check.cjs", "theme-check.cjs", "route-art-check.cjs");

    [ReportBrowserFact]
    public Task Route_backgrounds_stay_in_the_viewport_while_content_scrolls()
    {
        var reports = new FakeItemService();
        reports.Item.ImageKey = null;
        reports.Item.PostedByUserId = "test-user";
        reports.Item.MapLocation = new MapLocation { Latitude = 12.34, Longitude = 56.78 };
        reports.AdditionalItems.AddRange(Enumerable.Range(1, 12).Select(index => new Item
        {
            Title = $"Backpack fixture {index}", Description = "Test report", Category = "Bag",
            Location = "Library", PostedByUserId = "test-user"
        }));
        return RunReportBrowserAsync(reports, "tests/ui/fixed-background-check.cjs");
    }

    private async Task RunShellBrowserChecks(params string[] scripts)
    {
        var reports = new FakeItemService();
        reports.Item.ImageKey = null;
        using var factory = CreateFactory(reports);
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        using var client = factory.CreateClient();

        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        foreach (var script in scripts)
        {
            var start = new ProcessStartInfo("node")
            {
                WorkingDirectory = root,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            start.ArgumentList.Add(Path.Combine(root, "tests", "ui", script));
            start.Environment["TRACEBACK_URL"] = factory.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new
            {
                @base = start.Environment["TRACEBACK_URL"],
                member = CreateCookie(factory, false),
                admin = CreateCookie(factory, true)
            }));
            process.StandardInput.Close();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }
            output.WriteLine(await stdout);
            Assert.True(process.ExitCode == 0, await stderr);
        }
    }

    private sealed class BrowserFactAttribute : FactAttribute
    {
        public BrowserFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("TRACEBACK_BROWSER") != "1")
                Skip = "Set TRACEBACK_BROWSER=1 and make Playwright/Chromium available to Node.";
        }
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
        Assert.Contains("Find what matters.", markup);
        Assert.Contains("Reports are available after sign-in.", markup);
        Assert.Contains("Create account", markup);
        Assert.Contains("A little help. A way back.", markup);
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
                Assert.Contains("Search reports", markup);
                Assert.Contains("Browse reports", markup);
                Assert.Contains("Recent reports", markup);
                Assert.DoesNotContain("Reports are available after sign-in.", markup);
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
    public async Task Search_controls_send_native_get_fields_and_only_browse_carries_filters()
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        var home = await client.GetStringAsync("/?Query=stale&Type=Lost&Page=9");
        var homeForm = SearchForm(home);
        Assert.Contains("action=\"/Items\"", homeForm);
        Assert.Contains("method=\"get\"", homeForm);
        Assert.Contains("name=\"Query\"", homeForm);
        Assert.Contains("aria-label=\"Clear search words\"", homeForm);
        Assert.DoesNotContain("stale", homeForm);
        foreach (var name in new[] { "Type", "Status", "Category", "Location", "Page" })
        {
            Assert.DoesNotContain($"name=\"{name}\"", homeForm);
        }

        var browse = await client.GetStringAsync("/Items?Query=bag&Type=Found&Status=Claimed&Category=Gear&Location=Hall&Page=3");
        var browseForm = SearchForm(browse);
        Assert.Contains("aria-label=\"Clear search words\"", browseForm);
        Assert.Contains("value=\"bag\"", browseForm);
        Assert.Contains("value=\"Found\" selected=", browseForm);
        Assert.Contains("value=\"Claimed\" selected=", browseForm);
        Assert.Contains("value=\"Gear\"", browseForm);
        Assert.Contains("value=\"Hall\"", browseForm);
        Assert.DoesNotContain("name=\"Page\"", browseForm);
        Assert.Equal(3, reports.LastSearch?.Page);

        await client.GetStringAsync("/Items?Query=case&Type=Found&Status=Claimed&Category=Gear&Location=Hall");
        var search = Assert.IsType<ItemSearch>(reports.LastSearch);
        Assert.Equal(("case", "Found", "Claimed", "Gear", "Hall", 1),
            (search.Query, search.Type, search.Status, search.Category, search.Location, search.Page));
    }

    private static string SearchForm(string markup) => System.Text.RegularExpressions.Regex
        .Match(markup, "<form\\b[^>]*role=\"search\"[^>]*>.*?</form>", System.Text.RegularExpressions.RegexOptions.Singleline).Value;

    [Theory]
    [InlineData("/", true)]
    [InlineData("/Items", true)]
    [InlineData("/Items", false)]
    public async Task Report_cards_link_to_the_real_item_and_only_request_configured_photos(string path, bool hasPhoto)
    {
        var reports = new FakeItemService();
        reports.Item.Type = "Found";
        reports.Item.Status = "Claimed";
        if (!hasPhoto)
        {
            reports.Item.ImageKey = null;
        }

        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        var markup = await client.GetStringAsync(path);

        var card = System.Text.RegularExpressions.Regex.Match(markup,
            $"<article\\b[^>]*data-record-id=\"{reports.Item.Id}\"[^>]*>.*?</article>",
            System.Text.RegularExpressions.RegexOptions.Singleline).Value;
        Assert.NotEmpty(card);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(card, "<a\\b"));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(markup, $"href=\"/Items/Details/{reports.Item.Id}\""));
        Assert.Contains(reports.Item.Title, card);
        Assert.Contains(">Found</span>", card);
        Assert.Contains(">Claimed</span>", card);

        if (hasPhoto)
        {
            Assert.Contains($"src=\"/Items/Image/{reports.Item.Id}\"", card);
            Assert.Contains("Photo unavailable", card);
            Assert.DoesNotContain("No photo provided", card);
        }
        else
        {
            Assert.Contains("No photo provided", card);
            Assert.DoesNotContain("<img", card);
            Assert.DoesNotContain("Photo unavailable", card);
        }
    }

    [Fact]
    public async Task Signed_in_home_and_browse_show_recoverable_errors_when_report_search_fails()
    {
        var reports = new FakeItemService { FailSearch = true };
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        using var home = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, home.StatusCode);
        var homeMarkup = await home.Content.ReadAsStringAsync();
        Assert.Contains("Recent reports could not load", homeMarkup);
        Assert.Contains("Retry", homeMarkup);
        Assert.DoesNotContain(reports.Item.Title, homeMarkup);

        using var browse = await client.GetAsync("/Items?Query=backpack");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, browse.StatusCode);
        var browseMarkup = await browse.Content.ReadAsStringAsync();
        Assert.Contains("Reports could not load", browseMarkup);
        Assert.Contains("Query=backpack", browseMarkup);
        Assert.DoesNotContain(reports.Item.Title, browseMarkup);
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
        Assert.Contains("Location description: Library", legacyMarkup);
        Assert.Contains("Map location not added to this older report.", legacyMarkup);
        Assert.DoesNotContain("google.com/maps/search", legacyMarkup);

        reports.Item.MapLocation = new MapLocation { Latitude = 20.296058, Longitude = 85.824539 };
        using var mapped = await client.GetAsync($"/Items/Details/{reports.Item.Id}");
        var mappedMarkup = await mapped.Content.ReadAsStringAsync();
        Assert.Contains("Address unavailable for this pin.", mappedMarkup);
        Assert.Contains("Location description: Library", mappedMarkup);
        Assert.Contains("<summary>Show coordinates</summary>", mappedMarkup);
        Assert.DoesNotContain("Coordinates:", mappedMarkup);
        Assert.Contains("20.296058", mappedMarkup);
        Assert.Contains("google.com/maps/search/?api=1&amp;query=20.296058%2C85.824539", mappedMarkup);
        Assert.Contains("rel=\"noopener noreferrer\"", mappedMarkup);
    }


    [ReportBrowserFact]
    public async Task Report_components_pass_the_browser_matrix()
    {
        var reports = new FakeItemService
        {
            SearchResults = search => search.Query switch
            {
                "retry & check" => throw new InvalidOperationException("Fixture search failure"),
                "no matches" => [],
                "inventory & tag" when search.Page == 1 => Enumerable.Range(1, 24).Select(index =>
                    new Item { Title = $"Inventory report {index}", Type = "Found", Status = "Claimed" }).ToList(),
                "inventory & tag" when search.Page == 2 => [new Item { Title = "Final inventory report", Type = "Found", Status = "Claimed" }],
                "inventory & tag" => [],
                _ => null
            }
        };
        reports.Item.Type = "Found";
        reports.Item.Title = "Oversized navy travel backpack with a zippered pocket and a removable reflective name tag attached to the shoulder strap";
        reports.Item.Location = "North Engineering Building second-floor study area near the pedestrian bridge and across from the library reception desk";
        reports.AdditionalItems.AddRange([
            new Item { Title = "Keys", Location = "Hall", Type = "Lost", Status = "Claimed", Category = "Gear" },
            new Item { Title = "Notebook", Location = "Library", Type = "Found", Status = "Resolved", ImageKey = "missing.png" },
            new Item { Title = "Gloves", Location = "Library", Type = "Found", Status = "Active", ImageKey = "invalid.png" }
        ]);
        reports.ServeValidImage = true;

        await RunReportBrowserAsync(reports, "tests/ui/report-components-check.cjs");
    }

    private static async Task RunReportBrowserAsync(FakeItemService reports, string script, Action<Microsoft.AspNetCore.Hosting.IWebHostBuilder>? configure = null)
    {
        // Use the real routes, Razor views and static assets on loopback. The Dashboard
        // model is supplied by a test-only filter so no Mongo/R2 account is required.
        using var baseFactory = CreateFactory(reports, new BrowserDashboardFixture(reports));
        using var configuredFactory = configure is null ? null : baseFactory.WithWebHostBuilder(configure);
        var factory = configuredFactory ?? baseFactory;
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TraceBack.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = root.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add(script);
        foreach (var name in info.Environment.Keys.Where(name =>
            new[] { "MONGO", "JWT", "R2", "BOOTSTRAP" }.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToList())
        {
            info.Environment.Remove(name);
        }
        info.Environment["TRACEBACK_URL"] = factory.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        info.Environment["TRACEBACK_TEST_COOKIE"] = client.DefaultRequestHeaders.GetValues("Cookie").Single();
        info.Environment["TRACEBACK_TEST_ITEM_IDS"] = string.Join(',', new[] { reports.Item }.Concat(reports.AdditionalItems).Select(item => item.Id));
        using var browser = Process.Start(info)!;
        var stdout = browser.StandardOutput.ReadToEndAsync();
        var stderr = browser.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await browser.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            browser.Kill(entireProcessTree: true);
            throw;
        }
        var output = $"{await stdout}\n{await stderr}";
        Assert.True(browser.ExitCode == 0, output);
        Console.WriteLine(output);
    }

    private sealed class ReportBrowserFactAttribute : FactAttribute
    {
        public ReportBrowserFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("TRACEBACK_BROWSER_TESTS") != "1")
            {
                Skip = "Set TRACEBACK_BROWSER_TESTS=1 to run with Playwright and Chromium.";
            }
        }
    }

    private sealed class BrowserDashboardFixture(FakeItemService reports) : IResourceFilter
    {
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            if (context.RouteData.Values["controller"] as string != "Dashboard") return;
            var claims = new[] { "Pending", "Approved", "Rejected" }
                .Select(status => new ClaimRow(new TraceBack.Models.Claim { Status = status }, null)).ToList();
            context.Result = new ViewResult
            {
                ViewName = "Index",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState)
                {
                    Model = new DashboardPage([reports.Item, .. reports.AdditionalItems], claims)
                }
            };
        }

        public void OnResourceExecuted(ResourceExecutedContext context) { }
    }

    private static WebApplicationFactory<Program> CreateFactory(FakeItemService? reports, IResourceFilter? dashboardFixture = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDataProtectionProvider>();
                services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
                if (dashboardFixture is not null)
                {
                    services.Configure<MvcOptions>(options => options.Filters.Add(dashboardFixture));
                    var worker = services.Single(service => service.ServiceType == typeof(IHostedService)
                        && service.ImplementationType == typeof(ImageCleanupWorker));
                    services.Remove(worker);
                    services.RemoveAll<ICommentService>();
                    services.AddSingleton<ICommentService>(new FakeCommentService());
                }
                services.RemoveAll<IItemService>();
                if (reports is not null)
                {
                    services.AddSingleton<IItemService>(reports);
                }
            }));

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static void AddCookie(HttpClient client, WebApplicationFactory<Program> factory, bool admin = false)
    {
        var (name, value) = CreateCookie(factory, admin);
        client.DefaultRequestHeaders.Add("Cookie", $"{name}={value}");
    }

    private static BrowserCookie CreateCookie(WebApplicationFactory<Program> factory, bool admin)
    {
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = Principal();
        if (admin)
            ((ClaimsIdentity)principal.Identity!).AddClaim(new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));
        var ticket = new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
        return new BrowserCookie(options.Cookie.Name!, options.TicketDataFormat.Protect(ticket));
    }

    private sealed record BrowserCookie(string name, string value);

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
        public bool FailSearch { get; set; }
        public bool EmptySearch { get; set; }
        public ItemSearch? LastSearch { get; private set; }
        public List<Item> AdditionalItems { get; } = [];
        public bool ServeValidImage { get; set; }
        public Func<ItemSearch, List<Item>?>? SearchResults { get; init; }

        public Task<List<Item>> SearchAsync(ItemSearch search)
        {
            Interlocked.Increment(ref readCount);
            LastSearch = search;
            if (FailSearch)
            {
                return Task.FromException<List<Item>>(new InvalidOperationException("Fixture search failure"));
            }
            return Task.FromResult(EmptySearch ? [] : SearchResults?.Invoke(search) ?? new List<Item> { Item }.Concat(AdditionalItems).ToList());
        }

        public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(id == Item.Id || AdditionalItems.Exists(item => item.Id == id));

        public Task<Item?> GetAsync(string id)
        {
            Interlocked.Increment(ref readCount);
            return Task.FromResult(id == Item.Id ? Item : AdditionalItems.Find(item => item.Id == id));
        }

        public Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref readCount);
            var fixturePhoto = AdditionalItems.Any(item => item.Id == id && item.ImageKey == "fixture.png");
            var bytes = ServeValidImage && (id == Item.Id || fixturePhoto)
                ? Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAADAAAAAkCAIAAABAJy5dAAAAaklEQVR4nO3OQQ3AIABDUXxgAzPThhHOmOCAgNmYBEhoAytN/vnnhVz6UYXtAoMMMugHoJieYXeDqCYVEM8kBCKZtEAMkxwIblIEYU2iIKDpPFBtL6oZ03AiDYKY1EHrJjxoMYMMMqiSQR9CcNyk4n86hQAAAABJRU5ErkJggg==")
                : ImageBytes;
            var hasImage = id == Item.Id || fixturePhoto || AdditionalItems.Any(item => item.Id == id && item.ImageKey == "invalid.png");
            return Task.FromResult<StoredImage?>(hasImage ? new StoredImage(bytes, "image/png") : null);
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
        public Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin, long? expectedRevision = null) => throw new NotSupportedException();
        public Task<bool> ResolveAsync(string id, string userId, bool admin)
        {
            if (id != Item.Id || Item.Status != "Active" || (!admin && userId != Item.PostedByUserId))
                return Task.FromResult(false);
            Item.Status = "Resolved";
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string id, string userId, bool admin) =>
            Task.FromResult(id == Item.Id && (admin || userId == Item.PostedByUserId));
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
