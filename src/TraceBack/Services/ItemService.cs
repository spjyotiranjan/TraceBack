using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public interface IItemService
{
    Task<List<Item>> SearchAsync(ItemSearch search);
    Task<Item?> GetAsync(string id);
    Task<List<Item>> MineAsync(string userId);
    Task<Item> CreateAsync(ItemForm form, string userId);
    Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin);
    Task<bool> ResolveAsync(string id, string userId, bool admin);
    Task<bool> DeleteAsync(string id, string userId, bool admin);
    Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin);
    Task<bool> RemoveImageAsync(string id, string userId, bool admin);
    Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class ItemService(MongoStore store, ItemImageService images) : IItemService
{
    public async Task<List<Item>> SearchAsync(ItemSearch search)
    {
        var f = Builders<Item>.Filter;
        var filters = new List<FilterDefinition<Item>>();

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            var pattern = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search.Query.Trim()), "i");
            filters.Add(f.Or(f.Regex(x => x.Title, pattern), f.Regex(x => x.Description, pattern)));
        }

        if (!string.IsNullOrWhiteSpace(search.Type))
        {
            filters.Add(f.Eq(x => x.Type, search.Type));
        }

        if (!string.IsNullOrWhiteSpace(search.Category))
        {
            filters.Add(f.Eq(x => x.Category, search.Category));
        }

        if (!string.IsNullOrWhiteSpace(search.Location))
        {
            var locPattern = new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search.Location.Trim()), "i");
            filters.Add(f.Regex(x => x.Location, locPattern));
        }

        filters.Add(f.Eq(x => x.Status, search.Status ?? "Active"));

        var page = Math.Clamp(search.Page, 1, 1000);
        const int pageSize = 24;

        return await store.Items
            .Find(f.And(filters))
            .SortByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<Item?> GetAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await store.Items.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public Task<List<Item>> MineAsync(string userId) =>
        store.Items.Find(x => x.PostedByUserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();

    public Task<List<Item>> AllAsync() =>
        store.Items.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Limit(100)
            .ToListAsync();

    public Task<long> CountAsync() => store.Items.CountDocumentsAsync(_ => true);

    public async Task<Item> CreateAsync(ItemForm form, string userId)
    {
        var point = ValidateMapLocation(form.MapLocation, required: true);
        var item = Apply(new Item { PostedByUserId = userId }, form, point);
        await store.Items.InsertOneAsync(item);
        return item;
    }

    public async Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        var item = await GetAsync(id);
        if (item is null || item.Status != "Active" || item.Type != form.Type ||
            (!admin && item.PostedByUserId != userId))
        {
            return null;
        }

        var previousUpdatedAt = item.UpdatedAt;
        var point = ValidateMapLocation(form.MapLocation, required: false);
        Apply(item, form, point ?? item.MapLocation);

        var result = await store.Items.ReplaceOneAsync(
            x => x.Id == id && x.Status == "Active" && x.UpdatedAt == previousUpdatedAt,
            item);

        if (result.MatchedCount == 0)
        {
            throw new ItemUpdateConflictException();
        }

        return item;
    }

    public async Task<bool> ResolveAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var filter = Builders<Item>.Filter.Eq(x => x.Id, id) & Builders<Item>.Filter.Eq(x => x.Status, "Active");
        if (!admin)
        {
            filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        }

        var result = await store.Items.UpdateOneAsync(
            filter,
            Builders<Item>.Update
                .Set(x => x.Status, "Resolved")
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        if (result.ModifiedCount == 0)
        {
            return false;
        }

        await store.Claims.UpdateManyAsync(
            x => x.ItemId == id && x.Status == "Pending",
            Builders<Claim>.Update
                .Set(x => x.Status, "Rejected")
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        return true;
    }

    public Task<bool> DeleteAsync(string id, string userId, bool admin) => images.DeleteItemAsync(id, userId, admin);

    public Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin) =>
        images.SetImageAsync(id, image, userId, admin);

    public Task<bool> RemoveImageAsync(string id, string userId, bool admin) =>
        images.RemoveImageAsync(id, userId, admin);

    public Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default) =>
        images.GetImageAsync(id, cancellationToken);

    private static MapLocation? ValidateMapLocation(MapLocationInput? input, bool required)
    {
        if (!MapLocationRules.TryNormalize(input, out var point, out var error))
        {
            throw new ArgumentException(error, nameof(input));
        }

        if (required && point is null)
        {
            throw new ArgumentException(MapLocationRules.RequiredMessage, nameof(input));
        }

        return point;
    }

    private static Item Apply(Item item, ItemForm form, MapLocation? point)
    {
        item.Title = form.Title.Trim();
        item.Description = form.Description.Trim();
        item.Type = form.Type;
        item.Category = form.Category.Trim();
        item.Location = form.Location.Trim();
        item.MapLocation = point;
        item.DateReported = form.DateReported.Date;
        item.ContactPreference = form.ContactPreference.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        return item;
    }
}

public sealed class ItemUpdateConflictException : Exception
{
}
