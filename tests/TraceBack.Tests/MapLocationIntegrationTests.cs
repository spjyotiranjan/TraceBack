using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class MapLocationIntegrationTests
{
    [Fact]
    public async Task Mvc_and_api_create_edit_and_reload_against_isolated_mongo()
    {
        var mongoUri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI");
        if (string.IsNullOrWhiteSpace(mongoUri)) return;

        var databaseName = $"tb_pat56_{Guid.NewGuid():N}"[..33];
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mongo:ConnectionString"] = mongoUri,
                    ["Mongo:Database"] = databaseName
                })));
        try
        {
            var store = factory.Services.GetRequiredService<MongoStore>();
            using var ownerApi = ApiClient(factory, "owner");
            var form = Form();
            using var missing = await ownerApi.PostAsJsonAsync("/api/items", form);
            Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);

            form.MapLocation = new MapLocationInput { Latitude = 20.296058 };
            using var partialApi = await ownerApi.PostAsJsonAsync("/api/items", form);
            Assert.Equal(HttpStatusCode.BadRequest, partialApi.StatusCode);

            form.MapLocation.Longitude = 181;
            using var invalidApi = await ownerApi.PostAsJsonAsync("/api/items", form);
            Assert.Equal(HttpStatusCode.BadRequest, invalidApi.StatusCode);

            form.MapLocation = new MapLocationInput { Latitude = 20.2960584, Longitude = 85.824539 };
            using var overPrecision = await ownerApi.PostAsJsonAsync("/api/items", form);
            Assert.Equal(HttpStatusCode.BadRequest, overPrecision.StatusCode);

            form.MapLocation.Latitude = 20.296058;
            using var created = await ownerApi.PostAsJsonAsync("/api/items", form);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await JsonDocument.ParseAsync(await created.Content.ReadAsStreamAsync()))
                .RootElement.GetProperty("id").GetString()!;
            Assert.Equal(20.296058, (await store.Items.Find(x => x.Id == id).FirstAsync()).MapLocation?.Latitude);

            using var otherApi = ApiClient(factory, "other-user");
            using var forbiddenEdit = await otherApi.PutAsJsonAsync($"/api/items/{id}", form);
            Assert.Equal(HttpStatusCode.NotFound, forbiddenEdit.StatusCode);

            form.MapLocation = new MapLocationInput();
            form.Location = "Library east entrance";
            using var retained = await ownerApi.PutAsJsonAsync($"/api/items/{id}", form);
            Assert.Equal(HttpStatusCode.OK, retained.StatusCode);
            using var reloaded = await ownerApi.GetAsync($"/api/items/{id}");
            Assert.Contains("20.296058", await reloaded.Content.ReadAsStringAsync());
            Assert.Equal("Library east entrance", (await store.Items.Find(x => x.Id == id).FirstAsync()).Location);

            form.Type = "Found";
            using var conflict = await ownerApi.PutAsJsonAsync($"/api/items/{id}", form);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);

            var legacy = new Item { PostedByUserId = "owner", Location = "Old library", Title = "Legacy report" };
            await store.Items.InsertOneAsync(legacy);
            using var ownerMvc = MvcClient(factory, "owner");
            var token = await AntiForgeryTokenAsync(ownerMvc);
            using var missingMvc = await ownerMvc.PostAsync("/Items/Create", MvcForm(token, null, null));
            Assert.Equal(HttpStatusCode.OK, missingMvc.StatusCode);
            Assert.Contains(MapLocationRules.RequiredMessage, await missingMvc.Content.ReadAsStringAsync());

            using var partialMvc = await ownerMvc.PostAsync("/Items/Create", MvcForm(token, "20.296058", null));
            Assert.Equal(HttpStatusCode.OK, partialMvc.StatusCode);
            Assert.Contains(MapLocationRules.InvalidMessage, await partialMvc.Content.ReadAsStringAsync());

            using var invalidRangeMvc = await ownerMvc.PostAsync("/Items/Create", MvcForm(token, "91", "85.824539"));
            Assert.Equal(HttpStatusCode.OK, invalidRangeMvc.StatusCode);
            Assert.Contains(MapLocationRules.InvalidMessage, await invalidRangeMvc.Content.ReadAsStringAsync());

            using var invalidMvc = await ownerMvc.PostAsync("/Items/Create", MvcForm(token, "20.2960584", "85.824539"));
            Assert.Equal(HttpStatusCode.OK, invalidMvc.StatusCode);
            Assert.Contains("at most six decimal places", await invalidMvc.Content.ReadAsStringAsync());

            using var mvcCreated = await ownerMvc.PostAsync("/Items/Create", MvcForm(token, "20.296058", "85.824539"));
            Assert.Equal(HttpStatusCode.Redirect, mvcCreated.StatusCode);
            var mvcId = mvcCreated.Headers.Location!.ToString().Split('/').Last();
            Assert.Equal(20.296058, (await store.Items.Find(x => x.Id == mvcId).FirstAsync()).MapLocation?.Latitude);
            using var mvcDetail = await ownerMvc.GetAsync($"/Items/Details/{mvcId}");
            Assert.Contains("20.296058", await mvcDetail.Content.ReadAsStringAsync());

            using var mappedEditor = await ownerMvc.GetAsync($"/Items/Edit/{mvcId}");
            var mappedToken = ExtractToken(await mappedEditor.Content.ReadAsStringAsync());
            using var mappedEdit = await ownerMvc.PostAsync($"/Items/Edit/{mvcId}", MvcForm(mappedToken, null, null));
            Assert.Equal(HttpStatusCode.Redirect, mappedEdit.StatusCode);
            Assert.Equal(20.296058, (await store.Items.Find(x => x.Id == mvcId).FirstAsync()).MapLocation?.Latitude);

            using var legacyEditor = await ownerMvc.GetAsync($"/Items/Edit/{legacy.Id}");
            Assert.Equal(HttpStatusCode.OK, legacyEditor.StatusCode);
            var legacyToken = ExtractToken(await legacyEditor.Content.ReadAsStringAsync());
            using var legacyEdit = await ownerMvc.PostAsync($"/Items/Edit/{legacy.Id}", MvcForm(legacyToken, null, null));
            Assert.Equal(HttpStatusCode.Redirect, legacyEdit.StatusCode);
            var legacyReloaded = await store.Items.Find(x => x.Id == legacy.Id).FirstAsync();
            Assert.Null(legacyReloaded.MapLocation);
            Assert.Equal("Library", legacyReloaded.Location);

            using var otherMvc = MvcClient(factory, "other-user");
            using var otherEditor = await otherMvc.GetAsync($"/Items/Edit/{mvcId}");
            Assert.Equal(HttpStatusCode.Redirect, otherEditor.StatusCode);
            Assert.Equal("/Account/Denied", otherEditor.Headers.Location?.AbsolutePath);
        }
        finally
        {
            await new MongoClient(mongoUri).DropDatabaseAsync(databaseName);
        }
    }

    private static ItemForm Form() => new()
    {
        Title = "Found keys",
        Description = "A set of keys",
        Type = "Lost",
        Category = "Keys",
        Location = "Library",
        ContactPreference = "Help desk",
        DateReported = DateTime.UtcNow.Date
    };

    private static HttpClient ApiClient(WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var settings = factory.Services.GetRequiredService<JwtSettings>();
        var token = new JwtSecurityToken(issuer: settings.Issuer, audience: "TraceBack.Api",
            claims: Principal(userId).Claims, expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }

    private static HttpClient MvcClient(WebApplicationFactory<Program> factory, string userId)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        var ticket = new AuthenticationTicket(Principal(userId), CookieAuthenticationDefaults.AuthenticationScheme);
        client.DefaultRequestHeaders.Add("Cookie", $"{options.Cookie.Name}={options.TicketDataFormat.Protect(ticket)}");
        return client;
    }

    private static ClaimsPrincipal Principal(string userId) => new(new ClaimsIdentity(
        [new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, userId),
         new System.Security.Claims.Claim(ClaimTypes.Name, "Test User")],
        CookieAuthenticationDefaults.AuthenticationScheme));

    private static async Task<string> AntiForgeryTokenAsync(HttpClient client)
    {
        using var editor = await client.GetAsync("/Items/Create");
        Assert.Equal(HttpStatusCode.OK, editor.StatusCode);
        var token = ExtractToken(await editor.Content.ReadAsStringAsync());
        var antiCookie = editor.Headers.GetValues("Set-Cookie").First(value => value.StartsWith(".AspNetCore.Antiforgery", StringComparison.Ordinal));
        client.DefaultRequestHeaders.Add("Cookie", antiCookie.Split(';')[0]);
        return token;
    }

    private static string ExtractToken(string markup) => WebUtility.HtmlDecode(
        Regex.Match(markup, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);

    private static FormUrlEncodedContent MvcForm(string token, string? latitude, string? longitude)
    {
        var fields = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Title"] = "Found keys",
            ["Description"] = "A set of keys",
            ["Type"] = "Lost",
            ["Category"] = "Keys",
            ["Location"] = "Library",
            ["ContactPreference"] = "Help desk",
            ["DateReported"] = DateTime.UtcNow.Date.ToString("yyyy-MM-dd")
        };
        if (latitude is not null) fields["MapLocation.Latitude"] = latitude;
        if (longitude is not null) fields["MapLocation.Longitude"] = longitude;
        return new FormUrlEncodedContent(fields);
    }
}
