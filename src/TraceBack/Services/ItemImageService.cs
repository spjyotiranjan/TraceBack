using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public interface IItemImageRepository
{
    Task<Item?> GetAsync(string id);
    Task<bool> DeleteItemAsync(Item item, string userId, bool admin);
    Task DeleteClaimsAsync(string itemId);
    Task DeleteCommentsAsync(string itemId);
    Task<bool> ChangeImageAsync(Item item, string? key, string userId, bool admin);
}

public sealed class MongoItemImageRepository(MongoStore store) : IItemImageRepository
{
    public async Task<Item?> GetAsync(string id) =>
        await store.Items.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<bool> DeleteItemAsync(Item item, string userId, bool admin)
    {
        var filter = Builders<Item>.Filter.Eq(x => x.Id, item.Id) &
            Builders<Item>.Filter.Eq(x => x.UpdatedAt, item.UpdatedAt);
        if (!admin) filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        return (await store.Items.DeleteOneAsync(filter)).DeletedCount > 0;
    }

    public Task DeleteClaimsAsync(string itemId) => store.Claims.DeleteManyAsync(x => x.ItemId == itemId);

    public Task DeleteCommentsAsync(string itemId) => store.Comments.DeleteManyAsync(x => x.ItemId == itemId);

    public async Task<bool> ChangeImageAsync(Item item, string? key, string userId, bool admin)
    {
        var filter = Builders<Item>.Filter.Eq(x => x.Id, item.Id) &
            Builders<Item>.Filter.Eq(x => x.Status, "Active") &
            Builders<Item>.Filter.Eq(x => x.UpdatedAt, item.UpdatedAt);
        if (!admin) filter &= Builders<Item>.Filter.Eq(x => x.PostedByUserId, userId);
        var result = await store.Items.UpdateOneAsync(filter, Builders<Item>.Update
            .Set(x => x.ImageKey, key)
            .Set(x => x.ImageUrl, null)
            .Set(x => x.UpdatedAt, DateTime.UtcNow));
        return result.ModifiedCount > 0;
    }
}

public sealed class ItemImageService(
    IItemImageRepository repository,
    IImageStorage storage,
    ILogger<ItemImageService> logger)
{
    public async Task<bool> DeleteItemAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _)) return false;
        var item = await repository.GetAsync(id);
        if (item is null || (!admin && item.PostedByUserId != userId)) return false;
        if (!await repository.DeleteItemAsync(item, userId, admin)) return false;

        // The item and its image key are gone even when deleting claims fails.
        // Preserve that failure, but still attempt to remove the orphaned object.
        try
        {
            await repository.DeleteCommentsAsync(id);
            await repository.DeleteClaimsAsync(id);
        }
        finally
        {
            await DeleteImageBestEffortAsync(item.ImageKey);
        }
        return true;
    }

    public async Task<bool> SetImageAsync(string id, ImageUpload image, string userId, bool admin)
    {
        var item = await GetEditableAsync(id, userId, admin);
        if (item is null) return false;

        var previousKey = item.ImageKey;
        var newKey = await storage.UploadAsync(id, image);
        bool changed;
        try
        {
            changed = await repository.ChangeImageAsync(item, newKey, userId, admin);
        }
        catch
        {
            await DeleteImageBestEffortAsync(newKey);
            throw;
        }

        if (!changed)
        {
            await DeleteImageBestEffortAsync(newKey);
            return false;
        }
        await DeleteImageBestEffortAsync(previousKey);
        return true;
    }

    public async Task<bool> RemoveImageAsync(string id, string userId, bool admin)
    {
        var item = await GetEditableAsync(id, userId, admin);
        if (item is null) return false;
        var previousKey = item.ImageKey;
        if (!await repository.ChangeImageAsync(item, null, userId, admin)) return false;
        await DeleteImageBestEffortAsync(previousKey);
        return true;
    }

    public async Task<StoredImage?> GetImageAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var item = await repository.GetAsync(id);
        return item?.ImageKey is { } key ? await storage.ReadAsync(key, cancellationToken) : null;
    }

    private async Task<Item?> GetEditableAsync(string id, string userId, bool admin)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var item = await repository.GetAsync(id);
        return item?.Status == "Active" && (admin || item.PostedByUserId == userId) ? item : null;
    }

    private async Task DeleteImageBestEffortAsync(string? key)
    {
        if (key is null) return;
        try
        {
            await storage.DeleteAsync(key);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Could not delete R2 image {ImageKey}", key);
        }
    }
}
