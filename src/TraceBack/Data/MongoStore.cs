using MongoDB.Driver;
using TraceBack.Models;

namespace TraceBack.Data;

/// <summary>
/// MongoDB data context providing access to database collections and index initializations.
/// </summary>
public sealed class MongoStore
{
    public IMongoCollection<AppUser> Users { get; }
    public IMongoCollection<Item> Items { get; }
    public IMongoCollection<Claim> Claims { get; }
    public IMongoCollection<ReportComment> Comments { get; }
    public IMongoCollection<PendingImageDelete> PendingImageDeletes { get; }

    public MongoStore(IConfiguration configuration)
    {
        var connectionString = configuration["Mongo:ConnectionString"]
            ?? configuration["Mongo__ConnectionString"]
            ?? configuration["MONGO_CONNECTION_STRING"]
            ?? configuration["MONGODB_URI"]
            ?? "mongodb://localhost:27017";

        var databaseName = configuration["Mongo:Database"]
            ?? configuration["Mongo__Database"]
            ?? configuration["MONGO_DATABASE"]
            ?? "TraceBack";

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        Users = database.GetCollection<AppUser>("users");
        Items = database.GetCollection<Item>("items");
        Claims = database.GetCollection<Claim>("claims");
        Comments = database.GetCollection<ReportComment>("comments");
        PendingImageDeletes = database.GetCollection<PendingImageDelete>("pendingImageDeletes");

        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        // Unique index on AppUser email
        Users.Indexes.CreateOne(new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(user => user.Email),
            new CreateIndexOptions { Unique = true }));

        // Compound unique index on Claim (ItemId + UserId) preventing duplicate claims per user on the same item
        Claims.Indexes.CreateOne(new CreateIndexModel<Claim>(
            Builders<Claim>.IndexKeys.Ascending(claim => claim.ItemId).Ascending(claim => claim.UserId),
            new CreateIndexOptions { Unique = true }));

        Comments.Indexes.CreateOne(new CreateIndexModel<ReportComment>(
            Builders<ReportComment>.IndexKeys.Ascending(comment => comment.ItemId)
                .Ascending(comment => comment.ParentId).Ascending(comment => comment.Id)));
    }
}
