using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Home_recent_recovery_keeps_search_and_independent_browse_available(bool failure)
    {
        var reports = new FakeItemService { FailSearch = failure, EmptySearch = !failure };
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        using var response = await client.GetAsync("/Home/Index?Query=stale");
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(failure ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("home-recent-status", html);
        Assert.Contains(failure ? "Recent reports could not load" : "No active reports yet", html);
        Assert.DoesNotContain(failure ? "No active reports yet" : "Recent reports could not load", html);
        Assert.Contains("Browse all reports", html);
        Assert.Contains("<form method=\"get\"", SearchForm(html));
        Assert.Equal(failure, html.Contains("<span>Retry</span>", StringComparison.Ordinal));
        Assert.Equal(failure, html.Contains("href=\"/Home/Index?Query=stale\"", StringComparison.Ordinal));
        Assert.DoesNotContain("data-record-id", html);
        if (failure)
        {
            reports.FailSearch = false;
            using var retry = await client.GetAsync("/Home/Index?Query=stale");
            Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
            Assert.Contains($"href=\"/Items/Details/{reports.Item.Id}\"", await retry.Content.ReadAsStringAsync());
        }
    }

    [ReportBrowserFact]
    public async Task Home_landing_matches_role_and_responsive_shell_in_Chromium()
    {
        var reports = new FakeItemService { ServeValidImage = true };
        reports.AdditionalItems.AddRange(Enumerable.Range(1, 6).Select(i => new Item
        {
            Title = $"Report {i}",
            Type = "Found",
            Status = "Active",
            Location = "Library"
        }));
        using var factory = CreateHomeBrowserHost(reports);
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        using var client = CreateClient(factory);
        AddCookie(client, factory);
        using var empty = CreateHomeBrowserHost(new FakeItemService { EmptySearch = true });
        using var failed = CreateHomeBrowserHost(new FakeItemService { FailSearch = true });
        empty.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        failed.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        using var emptyClient = CreateClient(empty);
        using var failedClient = CreateClient(failed);
        AddCookie(emptyClient, empty);
        AddCookie(failedClient, failed);
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "TraceBack.slnx")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var info = new ProcessStartInfo("node")
        {
            WorkingDirectory = root.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add("tests/ui/home-landing-check.cjs");
        foreach (var name in info.Environment.Keys.Where(name =>
            new[] { "MONGO", "JWT", "R2", "BOOTSTRAP" }.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToList())
        {
            info.Environment.Remove(name);
        }
        SetFixture("", factory, client);
        SetFixture("_EMPTY", empty, emptyClient);
        SetFixture("_FAILED", failed, failedClient);
        info.Environment["TRACEBACK_REPORT_ID"] = reports.Item.Id;
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
        var result = $"{await stdout}\n{await stderr}";
        Assert.True(browser.ExitCode == 0, result);
        output.WriteLine(result);

        void SetFixture(string suffix, WebApplicationFactory<Program> host, HttpClient fixture)
        {
            info.Environment[$"TRACEBACK_URL{suffix}"] = host.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            info.Environment[$"TRACEBACK_TEST_COOKIE{suffix}"] = fixture.DefaultRequestHeaders.GetValues("Cookie").Single();
        }
    }

    private static WebApplicationFactory<Program> CreateHomeBrowserHost(FakeItemService reports) =>
        CreateFactory(reports).WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType == typeof(ImageCleanupWorker)));
            // The report-card journey also loads the Details discussion.
            services.RemoveAll<ICommentService>();
            services.AddSingleton<ICommentService>(new FakeCommentService());
        }));
}
