using Microsoft.Extensions.Configuration;
using TraceBack.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed partial class ReportAuthorizationTests
{
    [Theory]
    [InlineData("/Items", null, null, "Active", null, null)]
    [InlineData("/Items?Type=some&Status=other&Category=%20", null, null, "Active", null, null)]
    [InlineData("/Items?Query=%20bag%20&Type=%20found%20&Status=%20claimed%20&Category=%20Gear%20&Location=%20Hall%20",
        "bag", "Found", "Claimed", "Gear", "Hall")]
    public async Task Browse_normalizes_filters_before_querying_the_store(
        string url, string? query, string? type, string status, string? category, string? location)
    {
        var reports = new FakeItemService();
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        await client.GetStringAsync(url);

        var search = Assert.IsType<ItemSearch>(reports.LastSearch);
        Assert.Equal((query, type, status, category, location),
            (search.Query, search.Type, search.Status, search.Category, search.Location));
    }

    [Theory]
    [InlineData("/Items", "No active reports yet", "Report an item")]
    [InlineData("/Items?Type=Found", "No reports match these filters", "Clear filters")]
    [InlineData("/Items?Type=Found&Query=bag&Page=30", "No reports on this page", "Back to page 1")]
    public async Task Browse_empty_states_offer_contextual_recovery(string url, string heading, string action)
    {
        var reports = new FakeItemService { EmptySearch = true };
        using var factory = CreateFactory(reports);
        using var client = CreateClient(factory);
        AddCookie(client, factory);

        var html = await client.GetStringAsync(url);

        Assert.Contains($">{heading}</h2>", html);
        Assert.Contains($">{action}</span>", html);
        if (url.Contains("Page=30"))
        {
            Assert.Contains("href=\"/Items?Query=bag&amp;Type=Found&amp;Status=Active&amp;Page=1\"", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(">Previous</a>", html);
        }
    }

    [BrowseMongoFact]
    public async Task Browse_filters_reports_in_mongo_and_caps_each_page_at_24()
    {
        var uri = Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")!;
        var databaseName = $"tb_browse_{Guid.NewGuid():N}";
        using var baseFactory = new SharedControlsFactory();
        using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = uri,
                ["Mongo:Database"] = databaseName,
                ["BootstrapAdmin:Email"] = "",
                ["BootstrapAdmin:Password"] = ""
            })));
        var store = factory.Services.GetRequiredService<MongoStore>();
        try
        {
            var rows = Enumerable.Range(1, 25).Select(number => new Item
            {
                Title = $"Fixture report {number:D2}",
                Description = "Inventory & tag",
                Type = "Found",
                Status = "Active",
                Category = "Gear",
                Location = "North & Hall",
                PostedByUserId = "fixture-user",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, number, DateTimeKind.Utc)
            }).ToArray();
            await store.Items.InsertManyAsync(rows.Concat(new[]
            {
                Row("Query mismatch", description: "Another description"),
                Row("Type mismatch", type: "Lost"),
                Row("Category mismatch", category: "Other"),
                Row("Location mismatch", location: "South Hall"),
                Row("Inactive report", status: "Claimed")
            }));
            using var client = CreateClient(factory);
            AddCookie(client, factory);

            const string search = "/Items?Query=%20inventory%20%26%20tag%20&Type=%20found%20&Category=%20Gear%20&Location=%20north%20%26%20hall%20";
            var first = await client.GetStringAsync(search);
            var last = await client.GetStringAsync(search + "&Page=2");
            Assert.Contains("Showing 24 reports on page 1", first);
            Assert.Contains("Showing 1 report on page 2", last);
            Assert.Contains("rel=\"next\"", first);
            Assert.DoesNotContain("rel=\"next\"", last);
            foreach (var row in rows.Skip(1)) Assert.Contains($"data-record-id=\"{row.Id}\"", first);
            Assert.DoesNotContain($"data-record-id=\"{rows[0].Id}\"", first);
            Assert.Contains($"data-record-id=\"{rows[0].Id}\"", last);
            var claimed = await client.GetStringAsync(search + "&Status=claimed");
            Assert.Contains("Showing 1 report on page 1", claimed);
            Assert.Contains("Inactive report", claimed);
        }
        finally
        {
            await new MongoClient(uri).DropDatabaseAsync(databaseName);
        }

        static Item Row(string title, string description = "Inventory & tag", string type = "Found",
            string status = "Active", string category = "Gear", string location = "North & Hall") => new()
            {
                Title = title,
                Description = description,
                Type = type,
                Status = status,
                Category = category,
                Location = location,
                PostedByUserId = "fixture-user",
                CreatedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
            };
    }

    private sealed class BrowseMongoFactAttribute : FactAttribute
    {
        public BrowseMongoFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TRACEBACK_TEST_MONGO_URI")))
                Skip = "Set TRACEBACK_TEST_MONGO_URI to a test MongoDB instance.";
        }
    }
}
