using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public sealed class ItemService(MongoStore store)
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
        if (!string.IsNullOrWhiteSpace(search.Type)) filters.Add(f.Eq(x => x.Type, search.Type));
        if (!string.IsNullOrWhiteSpace(search.Category)) filters.Add(f.Eq(x => x.Category, search.Category));
        if (!string.IsNullOrWhiteSpace(search.Location)) filters.Add(f.Regex(x => x.Location, new BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(search.Location.Trim()), "i")));
        filters.Add(f.Eq(x => x.Status, search.Status ?? "Active"));
        return await store.Items.Find(f.And(filters))
            .SortByDescending(x => x.CreatedAt).Skip((Math.Clamp(search.Page, 1, 1000) - 1) * 24).Limit(24).ToListAsync();
    }

    public async Task<Item?> GetAsync(string id) => ObjectId.TryParse(id, out _)
        ? await store.Items.Find(x => x.Id == id).FirstOrDefaultAsync()
        : null;
    public Task<List<Item>> MineAsync(string userId) => store.Items.Find(x => x.PostedByUserId == userId).SortByDescending(x => x.CreatedAt).ToListAsync();
    public Task<List<Item>> AllAsync() => store.Items.Find(_ => true).SortByDescending(x => x.CreatedAt).Limit(100).ToListAsync();
    public Task<long> CountAsync() => store.Items.CountDocumentsAsync(_ => true);

    public async Task<Item> CreateAsync(ItemForm form, string userId)
    {
        var item = Apply(new Item { PostedByUserId = userId }, form);
        await store.Items.InsertOneAsync(item);
        return item;
    }

    public async Task<Item?> UpdateAsync(string id, ItemForm form, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var item = await GetAsync(id);
        if (item is null || item.Status != "Active" || item.Type != form.Type ||
            (!admin && item.PostedByUserId != userId)) return null;
        var previousUpdatedAt = item.UpdatedAt;
        Apply(item, form);
        var result = await store.Items.ReplaceOneAsync(x => x.Id == id && x.Status == "Active" && x.UpdatedAt == previousUpdatedAt,
            item);
        return result.MatchedCount > 0 ? item : null;
    }

    public async Task<bool> ResolveAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var filter = Builders<Item>.Filter.Eq(x => x.Id, id) & Builders<Item>.Filter.Eq(x => x.Status, "Active");
        if (!admin) filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        var result = await store.Items.UpdateOneAsync(filter,
            Builders<Item>.Update.Set(x => x.Status, "Resolved").Set(x => x.UpdatedAt, DateTime.UtcNow));
        if (result.ModifiedCount == 0) return false;
        await store.Claims.UpdateManyAsync(x => x.ItemId == id && x.Status == "Pending",
            Builders<Claim>.Update.Set(x => x.Status, "Rejected").Set(x => x.UpdatedAt, DateTime.UtcNow));
        return true;
    }

    public async Task<bool> DeleteAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var filter = Builders<Item>.Filter.Eq(x => x.Id, id);
        if (!admin) filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        var result = await store.Items.DeleteOneAsync(filter);
        if (result.DeletedCount > 0) await store.Claims.DeleteManyAsync(x => x.ItemId == id);
        return result.DeletedCount > 0;
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
        item.ImageUrl = form.ImageUrl;
        item.UpdatedAt = DateTime.UtcNow;
        return item;
    }
}
