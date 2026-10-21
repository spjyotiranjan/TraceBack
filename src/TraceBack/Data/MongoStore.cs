using MongoDB.Driver;
using TraceBack.Models;

namespace TraceBack.Data;

public sealed class MongoStore
{
    public IMongoCollection<AppUser> Users { get; }
    public IMongoCollection<Item> Items { get; }
    public IMongoCollection<Claim> Claims { get; }

    public MongoStore(IConfiguration configuration)
    {
        var connection = configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var database = configuration["Mongo:Database"] ?? "TraceBack";
        var db = new MongoClient(connection).GetDatabase(database);
        Users = db.GetCollection<AppUser>("users");
        Items = db.GetCollection<Item>("items");
        Claims = db.GetCollection<Claim>("claims");
        Users.Indexes.CreateOne(new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true }));
        Claims.Indexes.CreateOne(new CreateIndexModel<Claim>(
            Builders<Claim>.IndexKeys.Ascending(x => x.ItemId).Ascending(x => x.UserId),
            new CreateIndexOptions { Unique = true }));
    }
}
