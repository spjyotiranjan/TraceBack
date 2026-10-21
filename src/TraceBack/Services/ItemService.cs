using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public sealed class ItemService(MongoStore store, R2ImageStorage images, ILogger<ItemService> logger)
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
        var item = Apply(new Item { PostedByUserId = userId }, form);
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
        Apply(item, form);

        var result = await store.Items.ReplaceOneAsync(
            x => x.Id == id && x.Status == "Active" && x.UpdatedAt == previousUpdatedAt,
            item);

        return result.MatchedCount > 0 ? item : null;
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

    public async Task<bool> DeleteAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var filter = Builders<Item>.Filter.Eq(x => x.Id, id);
        if (!admin)
        {
            filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        }

        var existing = await store.Items.Find(filter).FirstOrDefaultAsync();
        if (existing is null)
        {
            return false;
        }

        filter &= Builders<Item>.Filter.Eq(x => x.UpdatedAt, existing.UpdatedAt);
        var result = await store.Items.DeleteOneAsync(filter);
        if (result.DeletedCount > 0)
        {
            await store.Claims.DeleteManyAsync(x => x.ItemId == id);
            await DeleteImageBestEffortAsync(existing.ImageKey);
        }

        return result.DeletedCount > 0;
    }

    public async Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin)
    {
        var item = await GetAsync(id);
        if (item is null || item.Status != "Active" || (!admin && item.PostedByUserId != userId))
        {
            return false;
        }

        var newKey = await images.UploadAsync(id, image);
        UpdateResult result;
        try
        {
            result = await store.Items.UpdateOneAsync(
                x => x.Id == id && x.Status == "Active" && x.UpdatedAt == item.UpdatedAt,
                Builders<Item>.Update
                    .Set(x => x.ImageKey, newKey)
                    .Set(x => x.ImageUrl, null)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));
        }
        catch
        {
            await DeleteImageBestEffortAsync(newKey);
            throw;
        }

        if (result.ModifiedCount == 0)
        {
            await DeleteImageBestEffortAsync(newKey);
            return false;
        }

        await DeleteImageBestEffortAsync(item.ImageKey);
        return true;
    }

    public async Task<bool> RemoveImageAsync(string id, string userId, bool admin)
    {
        var item = await GetAsync(id);
        if (item is null || item.Status != "Active" || (!admin && item.PostedByUserId != userId))
        {
            return false;
        }

        var result = await store.Items.UpdateOneAsync(
            x => x.Id == id && x.Status == "Active" && x.UpdatedAt == item.UpdatedAt,
            Builders<Item>.Update
                .Set(x => x.ImageKey, null)
                .Set(x => x.ImageUrl, null)
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        if (result.ModifiedCount == 0)
        {
            return false;
        }

        await DeleteImageBestEffortAsync(item.ImageKey);
        return true;
    }

    public async Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await GetAsync(id);
        return item?.ImageKey is { } key ? await images.ReadAsync(key, cancellationToken) : null;
    }

    private async Task DeleteImageBestEffortAsync(string? key)
    {
        if (key is null)
        {
            return;
        }

        try
        {
            await images.DeleteAsync(key);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Could not delete R2 image {ImageKey}", key);
        }
    }

    private static Item Apply(Item item, ItemForm form)
    {
        item.Title = form.Title.Trim();
        item.Description = form.Description.Trim();
        item.Type = form.Type;
        item.Category = form.Category.Trim();
        item.Location = form.Location.Trim();
        item.DateReported = form.DateReported.Date;
        item.ContactPreference = form.ContactPreference.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        return item;
    }
}
