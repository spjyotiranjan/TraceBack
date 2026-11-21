using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TraceBack.Models;
using Xunit;

namespace TraceBack.Tests;

public class SharedControlsTests : IClassFixture<SharedControlsFactory>
{
    private readonly HttpClient _client;

    public SharedControlsTests(SharedControlsFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    }

    [Fact]
    public async Task ServerErrorsIdentifyFieldsWithoutJavaScript()
    {
        const string url = "/Account/Login?returnUrl=%2FItems";
        var form = await _client.GetStringAsync(url);
        var csrf = Regex.Match(form, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(csrf);
        Assert.Contains("data-tb-pending=", form);
        using var response = await _client.PostAsync("/ui-controls-test/invalid-login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrf, ["Email"] = "not-an-email", ["Password"] = ""
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        var email = Regex.Match(page, "<input[^>]*id=\"Email\"[^>]*>").Value;
        Assert.Contains("aria-invalid=\"true\"", email);
        Assert.Contains("aria-describedby=\"Email-validation\"", email);
        Assert.Contains("href=\"#Email\"", page);
        Assert.Contains("field-validation-error", page);
    }

    [Fact]
    public async Task RegistrationRedisplaysMultipleServerErrorsWithoutEchoingPassword()
    {
        var form = await _client.GetStringAsync("/Account/Register");
        var csrf = Regex.Match(form, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(csrf);
        using var response = await _client.PostAsync("/ui-controls-test/invalid-register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrf, ["Name"] = "Q", ["Email"] = "invalid-email", ["Password"] = "short!"
        }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        foreach (var field in new[] { "Name", "Email", "Password" })
        {
            var input = Regex.Match(page, $"<input[^>]*id=\"{field}\"[^>]*>").Value;
            Assert.Contains("aria-invalid=\"true\"", input);
            Assert.Contains($"aria-describedby=\"{field}-validation\"", input);
            Assert.Contains($"href=\"#{field}\"", page);
        }
        Assert.Contains("value=\"Q\"", page);
        Assert.Contains("value=\"invalid-email\"", page);
        Assert.DoesNotContain("short!", page);
        Assert.Contains("action=\"/Account/Register\"", page);
    }

    [Fact]
    public async Task FailedGetOffersAnExplicitContextPreservingGetLink()
    {
        using var response = await _client.GetAsync("/ui-controls-test/failure?q=blue%20bag&page=2");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.Contains("href=\"/ui-controls-test/failure?q=blue%20bag&amp;page=2\"", page);
        Assert.Contains("Reference:", page);
        Assert.Contains("aria-hidden=\"true\"", page);
    }

    [Fact]
    public async Task FailedPostKeepsStatusButDoesNotOfferReplay()
    {
        using var response = await _client.PostAsync("/ui-controls-test/failure?q=blue%20bag", new StringContent("sample"));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var page = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Try again", page);
        Assert.DoesNotContain("data-retry", page);
        Assert.Contains("Go home", page);
    }

    [Fact]
    public async Task RedirectNoticesAreEncodedAndOnlyShownOnce()
    {
        using var redirect = await _client.GetAsync("/ui-controls-test/notices");
        Assert.Equal(HttpStatusCode.Redirect, redirect.StatusCode);
        var page = await _client.GetStringAsync(redirect.Headers.Location);
        Assert.Matches("role=\"status\"[^>]*>[\\s\\S]*?Test saved &lt;sample&gt;", page);
        Assert.Matches("role=\"alert\"[^>]*>[\\s\\S]*?Test failed", page);
        Assert.DoesNotContain("Test saved <sample>", page);
        var next = await _client.GetStringAsync(redirect.Headers.Location);
        Assert.DoesNotContain("Test saved", next);
        Assert.DoesNotContain("Test failed", next);
    }
}

public class SharedControlsFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseStaticWebAssets();
        builder.UseSetting("Jwt:Secret", new string('x', 48));
        builder.ConfigureServices(services => services.AddControllersWithViews().AddApplicationPart(typeof(SharedControlsTestController).Assembly));
    }
}

public class SharedControlsBrowserTests
{
    [SharedControlsBrowserFact]
    public Task SharedControlsInChromium() => RunBrowserAsync("SHARED_CONTROLS_BROWSER_SCRIPT");

    [SharedControlsBrowserFact("AUTH_LAYOUT_BROWSER_SCRIPT")]
    public Task AuthLayoutInChromium() => RunBrowserAsync("AUTH_LAYOUT_BROWSER_SCRIPT");

    private static async Task RunBrowserAsync(string scriptVariable)
    {
        var script = Environment.GetEnvironmentVariable(scriptVariable)!;
        using var factory = new SharedControlsFactory();
        factory.UseKestrel(0);
        using var client = factory.CreateClient();
        var start = new ProcessStartInfo("node") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(script);
        start.Environment["TRACEBACK_URL"] = client.BaseAddress!.GetLeftPart(UriPartial.Authority);
        using var browser = Process.Start(start)!;
        var output = browser.StandardOutput.ReadToEndAsync();
        var errors = browser.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await browser.WaitForExitAsync(timeout.Token);
        }
        finally
        {
            if (!browser.HasExited) browser.Kill(entireProcessTree: true);
        }
        var log = (await output) + (await errors);
        Console.WriteLine(log);
        Assert.True(browser.ExitCode == 0, log);
    }
}

public sealed class SharedControlsBrowserFactAttribute : FactAttribute
{
    public SharedControlsBrowserFactAttribute(string scriptVariable = "SHARED_CONTROLS_BROWSER_SCRIPT")
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(scriptVariable)))
            Skip = $"Set {scriptVariable} to run Chromium checks.";
    }
}

[Route("ui-controls-test")]
public class SharedControlsTestController : Controller
{
    // Exercises the production Razor view and model binding without resolving the account database.
    [HttpPost("invalid-login")]
    [ValidateAntiForgeryToken]
    public IActionResult InvalidLogin(LoginForm form) => View("~/Views/Account/Login.cshtml", form);

    [HttpPost("invalid-register")]
    [ValidateAntiForgeryToken]
    public IActionResult InvalidRegister(RegisterForm form) => View("~/Views/Account/Register.cshtml", form);

    [AcceptVerbs("GET", "POST", Route = "failure")]
    public IActionResult Failure() => throw new InvalidOperationException("Test recovery rendering");

    [HttpGet("button/{tone}")]
    public IActionResult Button(ButtonTone tone, bool disabled = false, bool iconOnly = false) =>
        PartialView("~/Views/Shared/_Button.cshtml", new UiButton("Fixture action", tone,
            Icon: "search", IconOnly: iconOnly, Disabled: disabled));

    [HttpGet("empty")]
    public IActionResult EmptyReports() => PartialView("~/Views/Shared/_RecoveryContent.cshtml", new RecoveryContent(
        "No fixture reports", "Change the filters to see more results.", RecoveryArt.Reports,
        Primary: new UiButton("Clear filters", Href: "/Account/Login?returnUrl=%2FItems")));

    [HttpGet("notices")]
    public IActionResult Notices()
    {
        TempData["Success"] = "Test saved <sample>";
        TempData["Error"] = "Test failed";
        return RedirectToAction("Login", "Account");
    }
}
