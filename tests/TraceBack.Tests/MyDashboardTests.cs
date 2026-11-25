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
            var day = new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
            string[] variants = ["1440-light", "390-light", "1440-dark", "390-dark"];
            Item[] Reports(string member) =>
            [
                new() { PostedByUserId = member, Type = "Found", Title = "Blue water bottle", Location = "Library entrance",
                    Category = "Accessories", DateReported = day.AddDays(2), CreatedAt = day.AddDays(2), ImageUrl = "/__dashboard-photo/bottle" },
                new() { PostedByUserId = member, Type = "Lost", Title = "Black headphones", Location = "Engineering Building, Room 204",
                    Category = "Electronics", DateReported = day.AddDays(1), CreatedAt = day.AddDays(1), ImageUrl = "/__dashboard-photo/headphones" },
                new() { PostedByUserId = member, Type = "Found", Title = "Silver key ring", Location = "Student Centre reception",
                    Category = "Keys", DateReported = day, CreatedAt = day, ImageUrl = "/__dashboard-photo/keys" }
            ];
            var owned = Reports("overview");
            var reportsOnly = Reports("reports");
            var deleteOwners = variants.ToDictionary(v => v, v => Reports($"delete-{v}"));
            var target = new Item { PostedByUserId = "finder", Type = "Found", Title = "Blue water bottle" };
            const string title = "Black umbrella with a small stitched repair on the handle and a red fabric sleeve";
            var umbrella = new Item { PostedByUserId = "finder", Type = "Found", Title = title };
            var keyRing = new Item { PostedByUserId = "finder", Type = "Found", Title = "Silver key ring" };
            var campusId = new Item { PostedByUserId = "finder", Type = "Found", Title = "Campus ID holder" };
            var longClaim = new Item { PostedByUserId = "finder", Type = "Found", Title = title + " " + new string('W', 40) };
            var longReport = new Item { PostedByUserId = "edge", Status = "Resolved", Title = "Blue backpack " + new string('W', 105), Location = new string('W', 110) };
            var failedPhoto = new Item { PostedByUserId = "edge", Status = "Resolved", Title = "Failed photo", ImageUrl = "/__dashboard-photo/failure" };
            await store.Items.InsertManyAsync([.. owned, .. reportsOnly, .. deleteOwners.Values.SelectMany(x => x), target, umbrella, keyRing, campusId, longClaim, longReport, failedPhoto]);

            const string privateMessage = "Private ownership proof, not part of the dashboard";
            var missing = ObjectId.GenerateNewId().ToString();
            var claims = new List<MemberClaim>();
            foreach (var member in new[] { "overview", "claims" }.Concat(variants.Select(v => $"submit-{v}")))
            {
                claims.Add(new() { UserId = member, ItemId = umbrella.Id, Status = "Approved", Message = privateMessage, CreatedAt = day });
                claims.Add(new() { UserId = member, ItemId = member == "claims" ? keyRing.Id : campusId.Id, Status = "Rejected", CreatedAt = day.AddDays(-3) });
                claims.Add(new() { UserId = member, ItemId = missing, Status = "Rejected", CreatedAt = day.AddDays(-8) });
            }
            claims.Add(new() { UserId = "claims", ItemId = target.Id, Status = "Pending", CreatedAt = day.AddDays(4) });
            claims.Add(new() { UserId = "edge", ItemId = longClaim.Id, Status = "Approved" });
            foreach (var reports in deleteOwners.Values)
                claims.Add(new() { UserId = "finder", ItemId = reports[0].Id, Status = "Rejected" });
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
                members = new[] { "overview", "reports", "claims", "empty", "edge" }.Concat(variants.SelectMany(v => new[] { $"submit-{v}", $"delete-{v}" })).ToDictionary(member => member, Ticket),
                owned = owned.Select(x => x.Id),
                reports = reportsOnly.Select(x => x.Id),
                deletes = deleteOwners.ToDictionary(pair => pair.Key, pair => pair.Value.Select(x => x.Id)),
                longReport = longReport.Id,
                longTarget = longClaim.Id,
                longTitle = longClaim.Title,
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
            foreach (var variant in variants)
            {
                var deleted = deleteOwners[variant][0].Id;
                var submitter = $"submit-{variant}";
                Assert.Equal("Pending", (await store.Claims.Find(x => x.UserId == submitter && x.ItemId == target.Id).SingleAsync()).Status);
                Assert.False(await store.Items.Find(x => x.Id == deleted).AnyAsync());
                Assert.False(await store.Claims.Find(x => x.ItemId == deleted).AnyAsync());
            }
            Assert.True(await store.Items.Find(x => x.Id == target.Id).AnyAsync());
        }
        finally
        {
            await mongo.DropDatabaseAsync(database);
        }
    }
}
