using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;
using Xunit;

namespace TraceBack.Tests;

public sealed class SignInTests
{
    [Theory]
    [InlineData(null, "/Account/Login")]
    [InlineData("https://example.invalid/continue", "/Account/Login")]
    [InlineData("//example.invalid/continue", "/Account/Login")]
    [InlineData("/Items/Details/report?from=claim", "/Account/Login?returnUrl=%2FItems%2FDetails%2Freport%3Ffrom%3Dclaim")]
    public async Task LoginFormOnlyCarriesLocalReturnContext(string? returnUrl, string expectedAction)
    {
        using var factory = new SharedControlsFactory();
        using var client = CreateClient(factory);
        var html = await client.GetStringAsync(LoginUrl(returnUrl));
        Assert.Contains($"action=\"{expectedAction}\"", html);
    }

    [Fact]
    public async Task LoginAndMemberPostsRequireAntiforgeryOrAuthorization()
    {
        using var factory = new SharedControlsFactory();
        using var client = CreateClient(factory);
        using var login = await client.PostAsync(LoginUrl(null), Fields("reader@example.com", "fixture-password"));
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        foreach (var path in new[] { "/Account/Logout", "/Items/Create" })
        {
            using var response = await client.PostAsync(path, Fields(string.Empty, string.Empty));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/Login", response.Headers.Location!.AbsolutePath);
        }
    }

    [SignInMongoFact]
    public async Task LoginPreservesSafeValuesAndReturnsToDetailsOrDashboard()
    {
        var uri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        var database = $"tb_signin_{Guid.NewGuid():N}";
        using var baseFactory = new SharedControlsFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = uri,
                ["Mongo:Database"] = database,
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = ""
            })));
        try
        {
            var user = new AppUser { Name = "Test Reader", Email = "reader@example.com" };
            const string password = "fixture-password";
            user.PasswordHash = new PasswordHasher<AppUser>().HashPassword(user, password);
            var store = factory.Services.GetRequiredService<MongoStore>();
            await store.Users.InsertOneAsync(user);
            var item = new Item { Title = "Fixture water bottle", Type = "Found", Status = "Active", PostedByUserId = user.Id };
            await store.Items.InsertOneAsync(item);
            var details = $"/Items/Details/{item.Id}";
            var localReturn = details + "?from=claim";
            using var client = CreateClient(factory);
            using var challenge = await client.GetAsync(localReturn);
            Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
            var loginLocation = challenge.Headers.Location!;
            Assert.Equal("/Account/Login", loginLocation.AbsolutePath);
            Assert.Equal(localReturn, QueryHelpers.ParseQuery(loginLocation.Query)["ReturnUrl"].ToString());

            var url = LoginUrl(localReturn);
            var token = Token(await client.GetStringAsync(url));
            using var blank = await client.PostAsync(url, Fields(string.Empty, string.Empty, token));
            Assert.Equal(HttpStatusCode.OK, blank.StatusCode);
            var blankHtml = await blank.Content.ReadAsStringAsync();
            Assert.Contains("href=\"#Email\"", blankHtml);
            Assert.Contains("href=\"#Password\"", blankHtml);

            using var malformed = await client.PostAsync(url, Fields("bad-email", "private-invalid-value", token));
            Assert.Equal(HttpStatusCode.OK, malformed.StatusCode);
            var malformedHtml = await malformed.Content.ReadAsStringAsync();
            Assert.Contains("value=\"bad-email\"", malformedHtml);
            Assert.Contains($"action=\"{url}\"", malformedHtml);
            Assert.DoesNotContain("private-invalid-value", malformedHtml);

            foreach (var email in new[] { user.Email, "unknown@example.com" })
            {
                using var failed = await client.PostAsync(url, Fields(email, "private-wrong-value", token));
                Assert.Equal(HttpStatusCode.OK, failed.StatusCode);
                var failedHtml = await failed.Content.ReadAsStringAsync();
                Assert.Contains("The email or password is incorrect.", failedHtml);
                Assert.Contains($"value=\"{email}\"", failedHtml);
                Assert.Contains($"action=\"{url}\"", failedHtml);
                Assert.DoesNotContain("private-wrong-value", failedHtml);
            }

            foreach (var destination in new[] { localReturn, null, "https://example.invalid/continue", "//example.invalid/continue" })
            {
                using var fresh = CreateClient(factory);
                var target = LoginUrl(destination);
                // Send the requested destination directly even if Login's form would omit it.
                using var success = await fresh.PostAsync(target, Fields(user.Email, password, Token(await fresh.GetStringAsync(target))));
                Assert.Equal(HttpStatusCode.Redirect, success.StatusCode);
                var expected = destination == localReturn ? localReturn : "/Dashboard";
                Assert.Equal(expected, success.Headers.Location!.OriginalString);
                var next = await fresh.GetStringAsync(expected);
                Assert.Contains(destination == localReturn ? "Fixture water bottle" : "<h1>My dashboard</h1>", next);
                using var protectedPost = await fresh.PostAsync("/Account/Logout", Fields(string.Empty, string.Empty));
                Assert.Equal(HttpStatusCode.BadRequest, protectedPost.StatusCode);
            }
        }
        finally
        {
            await new MongoClient(uri).DropDatabaseAsync(database);
        }
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) => factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    private static string LoginUrl(string? destination) => destination is null ? "/Account/Login" :
        "/Account/Login?returnUrl=" + Uri.EscapeDataString(destination);

    private static string Token(string html)
    {
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return token;
    }

    private static FormUrlEncodedContent Fields(string email, string password, string? token = null)
    {
        var fields = new Dictionary<string, string> { ["Email"] = email, ["Password"] = password };
        if (token is not null) fields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(fields);
    }

    private sealed class SignInMongoFactAttribute : FactAttribute
    {
        public SignInMongoFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")))
                Skip = "Set TRACEBACK_TEST_MONGO_URI to a test MongoDB instance.";
        }
    }
}
