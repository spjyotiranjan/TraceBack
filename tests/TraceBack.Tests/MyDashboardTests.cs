using System.Diagnostics;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;
using MemberClaim = TraceBack.Models.Claim;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [ClaimBrowserFact]
    public async Task Dashboard_renders_member_activity_and_real_write_notices_in_Chromium()
    {
        var mongoUri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        var database = $"tb_dashboard_{Guid.NewGuid():N}";
        var mongo = new MongoClient(mongoUri);
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => ConfigureClaimDatabase(builder, mongoUri, database));
        factory.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        try
        {
            var store = factory.Services.GetRequiredService<MongoStore>();
            var owned = new Item { PostedByUserId = "overview", Type = "Lost", Title = "Blue backpack " + new string('W', 105), Location = "Library entrance" };
            var reportsOnly = new Item { PostedByUserId = "reports", Type = "Lost", Title = "Green notebook" };
            var target = new Item { PostedByUserId = "finder", Type = "Found", Title = "Blue water bottle" };
            var umbrella = new Item { PostedByUserId = "finder", Type = "Found", Title = "Black umbrella with a small stitched repair on the handle and a red fabric sleeve " + new string('W', 40) };
            var keyRing = new Item { PostedByUserId = "finder", Type = "Found", Title = "Silver key ring" };
            await store.Items.InsertManyAsync([owned, reportsOnly, target, umbrella, keyRing]);

            const string privateMessage = "Private ownership proof, not part of the dashboard";
            var day = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            var missing = ObjectId.GenerateNewId().ToString();
            var claims = new List<MemberClaim>();
            foreach (var member in new[] { "overview", "claims" })
            {
                claims.Add(new() { UserId = member, ItemId = umbrella.Id, Status = "Approved", Message = privateMessage, CreatedAt = day });
                claims.Add(new() { UserId = member, ItemId = keyRing.Id, Status = "Rejected", CreatedAt = day.AddDays(-3) });
                claims.Add(new() { UserId = member, ItemId = missing, Status = "Rejected", CreatedAt = day.AddDays(-8) });
            }
            claims.Add(new() { UserId = "overview", ItemId = target.Id, Status = "Pending", CreatedAt = day.AddDays(4) });
            claims.Add(new() { UserId = "finder", ItemId = owned.Id, Status = "Rejected" });
            await store.Claims.InsertManyAsync(claims);

            var options = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme);
            string Ticket(string member) => options.TicketDataFormat.Protect(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([
                    new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, member),
                    new System.Security.Claims.Claim(ClaimTypes.Name, "Dashboard fixture")
                ], CookieAuthenticationDefaults.AuthenticationScheme)), CookieAuthenticationDefaults.AuthenticationScheme));

            DirectoryInfo? root = new(AppContext.BaseDirectory);
            while (root is not null && !File.Exists(Path.Combine(root.FullName, "TraceBack.slnx"))) root = root.Parent;
            Assert.NotNull(root);
            var start = new ProcessStartInfo("node")
            {
                WorkingDirectory = root.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            start.ArgumentList.Add("tests/ui/my-dashboard-check.cjs");
            foreach (var name in start.Environment.Keys.Where(name =>
                new[] { "MONGO", "JWT", "R2", "BOOTSTRAP", "TRACEBACK_TEST_MONGO_URI" }.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToList())
            {
                start.Environment.Remove(name);
            }
            start.Environment["TRACEBACK_URL"] = factory.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            start.Environment["TRACEBACK_DASHBOARD_FIXTURE"] = JsonSerializer.Serialize(new
            {
                name = options.Cookie.Name,
                members = new[] { "overview", "reports", "claims", "empty" }.ToDictionary(member => member, Ticket),
                owned = owned.Id,
                report = reportsOnly.Id,
                target = target.Id,
                umbrella = umbrella.Id,
                title = umbrella.Title,
                date = day.ToLocalTime().ToString("d MMM yyyy"),
                missing,
                privateMessage
            });
            using var browser = Process.Start(start)!;
            var stdout = browser.StandardOutput.ReadToEndAsync();
            var stderr = browser.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            try
            {
                await browser.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                browser.Kill(entireProcessTree: true);
                throw;
            }
            Assert.True(browser.ExitCode == 0, $"{await stdout}\n{await stderr}");
            Console.WriteLine(await stdout);
            Assert.Equal("Pending", (await store.Claims.Find(x => x.UserId == "claims" && x.ItemId == target.Id).SingleAsync()).Status);
            Assert.False(await store.Items.Find(x => x.Id == owned.Id).AnyAsync());
            Assert.False(await store.Claims.Find(x => x.ItemId == owned.Id).AnyAsync());
            Assert.True(await store.Items.Find(x => x.Id == target.Id).AnyAsync());
        }
        finally
        {
            await mongo.DropDatabaseAsync(database);
        }
    }
}
