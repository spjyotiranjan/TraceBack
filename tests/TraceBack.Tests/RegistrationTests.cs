using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TraceBack.Data;
using Xunit;

namespace TraceBack.Tests;

public sealed class RegistrationTests
{
    private const string Register = "/Account/Register";

    [Fact]
    public async Task RegisterExplainsTheNameAndPasswordRulesBeforeSubmission()
    {
        using var factory = new SharedControlsFactory();
        using var client = CreateClient(factory);
        var html = WebUtility.HtmlDecode(await client.GetStringAsync(Register));
        Assert.Contains("Full name (2–80 characters)", html);
        Assert.Contains("Password (at least 8 characters)", html);
    }

    [RegistrationMongoFact]
    public async Task InvalidRegistrationPreservesNameAndEmailButNotPassword()
    {
        using var mongo = new RegistrationDatabase();
        using var factory = mongo.CreateFactory();
        using var client = CreateClient(factory);
        var token = Token(await client.GetStringAsync(Register));
        using var response = await client.PostAsync(Register, Fields("A", "bad-email", "short!", token));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        foreach (var field in new[] { "Name", "Email", "Password" })
            Assert.Contains($"href=\"#{field}\"", html);
        Assert.Contains("value=\"A\"", html);
        Assert.Contains("value=\"bad-email\"", html);
        Assert.DoesNotContain("short!", html);
        Assert.Contains("href=\"/Account/Login\"", html);
    }

    [Fact]
    public async Task RegisterRequiresAntiforgeryAndDashboardRequiresAuthentication()
    {
        using var factory = new SharedControlsFactory();
        using var client = CreateClient(factory);
        using var post = await client.PostAsync(Register, Fields("Alex Morgan", "alex@example.com", "fixture-password"));
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);
        using var dashboard = await client.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, dashboard.StatusCode);
        Assert.Equal("/Account/Login", dashboard.Headers.Location!.AbsolutePath);
    }

    [RegistrationMongoFact]
    public async Task RegistrationSignsInThenDuplicateKeepsOnlyNonSecretValues()
    {
        using var mongo = new RegistrationDatabase();
        using var factory = mongo.CreateFactory();
        using var client = CreateClient(factory);
        const string email = "alex@example.com";
        // Exercise the inclusive lower bounds and ignore an unrelated return destination.
        var target = Register + "?returnUrl=https%3A%2F%2Fexample.invalid";
        using var success = await client.PostAsync(target, Fields("Al", email, "fixture8", Token(await client.GetStringAsync(target))));
        Assert.Equal(HttpStatusCode.Redirect, success.StatusCode);
        Assert.Equal("/Dashboard", success.Headers.Location!.OriginalString);
        var dashboard = await client.GetStringAsync("/Dashboard");
        Assert.Contains("<h1>My dashboard</h1>", dashboard);
        Assert.Contains("Your account is ready.", dashboard);
        using var logout = await client.PostAsync("/Account/Logout", Fields("", "", ""));
        Assert.Equal(HttpStatusCode.BadRequest, logout.StatusCode);

        using var fresh = CreateClient(factory);
        using var duplicate = await fresh.PostAsync(Register, Fields("Another Name", "ALEX@example.com", "duplicate-fixture", Token(await fresh.GetStringAsync(Register))));
        Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
        var html = await duplicate.Content.ReadAsStringAsync();
        Assert.Contains("An account with this email already exists.", html);
        Assert.Contains("href=\"#Email\"", html);
        Assert.Contains("value=\"Another Name\"", html);
        Assert.Contains("value=\"ALEX@example.com\"", html);
        Assert.DoesNotContain("duplicate-fixture", html);
        Assert.Contains("href=\"/Account/Login\"", html);
        Assert.Equal(1, await factory.Services.GetRequiredService<MongoStore>().Users.CountDocumentsAsync(user => user.Email == email));
        using var stillGuest = await fresh.GetAsync("/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, stillGuest.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) => factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });

    private static string Token(string html)
    {
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return token;
    }

    private static FormUrlEncodedContent Fields(string name, string email, string password, string? token = null)
    {
        var fields = new Dictionary<string, string> { ["Name"] = name, ["Email"] = email, ["Password"] = password };
        if (token is not null) fields["__RequestVerificationToken"] = token;
        return new FormUrlEncodedContent(fields);
    }

    private sealed class RegistrationMongoFactAttribute : FactAttribute
    {
        public RegistrationMongoFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")))
                Skip = "Set TRACEBACK_TEST_MONGO_URI to a test MongoDB instance.";
        }
    }

    private sealed class RegistrationDatabase : IDisposable
    {
        private readonly string _uri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        private readonly string _database = $"tb_register_{Guid.NewGuid():N}";
        private readonly SharedControlsFactory _factory = new();

        public WebApplicationFactory<Program> CreateFactory() => _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = _uri,
                ["Mongo:Database"] = _database,
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = ""
            })));

        public void Dispose()
        {
            _factory.Dispose();
            new MongoClient(_uri).DropDatabase(_database);
        }
    }
}
