using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public enum CommentError { None, InvalidInput, NotFound, Inactive, Forbidden, StorageUnavailable }

public sealed record CommentResult<T>(T? Value, CommentError Error = CommentError.None)
{
    public bool Succeeded => Error == CommentError.None;
}

public sealed record CommentView(
    string Id, string ItemId, string? ParentId, string AuthorId, string AuthorName,
    string? Body, DateTime CreatedAt, bool Deleted, string? ImageAlt = null, string? ImageUrl = null)
{
    public static CommentView From(ReportComment comment) => new(
        comment.Id, comment.ItemId, comment.ParentId, comment.AuthorId,
        comment.AuthorName, comment.DeletedAt is null ? comment.Body : null,
        comment.CreatedAt, comment.DeletedAt is not null,
        comment.DeletedAt is null ? comment.ImageAlt : null,
        comment.DeletedAt is null && comment.ImageKey is not null
            ? $"/Items/{comment.ItemId}/Comments/{comment.Id}/Image" : null);

    public CommentView ForApi() => this with
    {
        ImageUrl = ImageUrl is null ? null : $"/api/items/{ItemId}/comments/{Id}/image"
    };
}

public sealed record CommentPage(IReadOnlyList<CommentView> Comments, string? NextCursor);

public interface ICommentRepository
{
    Task<Item?> GetItemAsync(string itemId);
    Task<ReportComment?> GetAsync(string commentId);
    Task<IReadOnlyList<ReportComment>> ListAsync(string itemId, string? parentId, string? afterId, int limit);
    Task InsertAsync(ReportComment comment);
    Task<ReportComment?> SoftDeleteAsync(string commentId, DateTime deletedAt);
    Task<bool> ChangeImageAsync(string commentId, string? expectedKey, string? key,
        string? alt, string? contentType);
    Task DeleteAsync(string commentId);
}

public sealed class MongoCommentRepository(MongoStore store) : ICommentRepository
{
    public async Task<Item?> GetItemAsync(string itemId) =>
        await store.Items.Find(x => x.Id == itemId).FirstOrDefaultAsync();

    public async Task<ReportComment?> GetAsync(string commentId) =>
        await store.Comments.Find(x => x.Id == commentId).FirstOrDefaultAsync();

    public async Task<IReadOnlyList<ReportComment>> ListAsync(
        string itemId, string? parentId, string? afterId, int limit)
    {
        var filter = Builders<ReportComment>.Filter.Eq(x => x.ItemId, itemId) &
            Builders<ReportComment>.Filter.Eq(x => x.ParentId, parentId);
        if (afterId is not null)
            filter &= Builders<ReportComment>.Filter.Gt(x => x.Id, afterId);
        return await store.Comments.Find(filter).SortBy(x => x.Id).Limit(limit).ToListAsync();
    }

    public Task InsertAsync(ReportComment comment) => store.Comments.InsertOneAsync(comment);

    public async Task<ReportComment?> SoftDeleteAsync(string commentId, DateTime deletedAt) =>
        await store.Comments.FindOneAndUpdateAsync<ReportComment>(
            x => x.Id == commentId && x.DeletedAt == null,
            Builders<ReportComment>.Update.Set(x => x.Body, null).Set(x => x.DeletedAt, deletedAt)
                .Set(x => x.ImageKey, null).Set(x => x.ImageAlt, null)
                .Set(x => x.ImageContentType, null));

    public async Task<bool> ChangeImageAsync(string commentId, string? expectedKey,
        string? key, string? alt, string? contentType) =>
        (await store.Comments.UpdateOneAsync(
            x => x.Id == commentId && x.DeletedAt == null && x.ImageKey == expectedKey,
            Builders<ReportComment>.Update.Set(x => x.ImageKey, key)
                .Set(x => x.ImageAlt, alt).Set(x => x.ImageContentType, contentType)))
        .ModifiedCount > 0;

    public Task DeleteAsync(string commentId) =>
        store.Comments.DeleteOneAsync(x => x.Id == commentId);
}

public interface ICommentService
{
    Task<CommentResult<CommentPage>> ListAsync(string itemId, string? parentId, string? cursor, int limit);
    Task<CommentResult<CommentView>> CreateAsync(
        string itemId, string? parentId, string? body, string authorId, string authorName,
        ImageUpload? image = null, string? imageAlt = null);
    Task<CommentError> DeleteAsync(string itemId, string commentId, string userId, bool isAdmin);
    Task<CommentError> SetImageAsync(string itemId, string commentId, ImageUpload image,
        string? imageAlt, string userId, bool isAdmin);
    Task<CommentError> RemoveImageAsync(string itemId, string commentId, string userId, bool isAdmin);
    Task<CommentResult<StoredImage>> GetImageAsync(string itemId, string commentId);
}

public sealed class CommentService(ICommentRepository repository, IImageStorage storage,
    IImageCleanup cleanup) : ICommentService
{
    public const int MaxBodyLength = 2000;
    public const int MaxPageSize = 50;
    public const int MaxAltLength = 200;

    public async Task<CommentResult<CommentPage>> ListAsync(
        string itemId, string? parentId, string? cursor, int limit)
    {
        if (!TryCanonicalId(itemId, out itemId) ||
            !TryCanonicalOptionalId(parentId, out parentId) ||
            !TryCanonicalOptionalId(cursor, out cursor) ||
            limit is < 1 or > MaxPageSize)
            return new(null, CommentError.InvalidInput);

        if (await repository.GetItemAsync(itemId) is null)
            return new(null, CommentError.NotFound);

        if (parentId is not null && !await BelongsToItemAsync(parentId, itemId))
            return new(null, CommentError.NotFound);

        if (cursor is not null)
        {
            var anchor = await repository.GetAsync(cursor);
            if (anchor is null || anchor.ItemId != itemId || anchor.ParentId != parentId)
                return new(null, CommentError.InvalidInput);
        }

        var rows = await repository.ListAsync(itemId, parentId, cursor, limit + 1);
        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).Select(CommentView.From).ToList();
        return new(new CommentPage(page, hasMore ? page[^1].Id : null));
    }

    public async Task<CommentResult<CommentView>> CreateAsync(
        string itemId, string? parentId, string? body, string authorId, string authorName,
        ImageUpload? image = null, string? imageAlt = null)
    {
        if (!TryCanonicalId(itemId, out itemId) ||
            !TryCanonicalOptionalId(parentId, out parentId) ||
            (string.IsNullOrWhiteSpace(body) && image is null) ||
            (body?.Trim().Length ?? 0) > MaxBodyLength ||
            !ValidImageFields(image, imageAlt) ||
            string.IsNullOrWhiteSpace(authorId) || string.IsNullOrWhiteSpace(authorName))
            return new(null, CommentError.InvalidInput);

        var item = await repository.GetItemAsync(itemId);
        if (item is null) return new(null, CommentError.NotFound);
        if (item.Status != "Active") return new(null, CommentError.Inactive);
        if (parentId is not null && !await BelongsToItemAsync(parentId, itemId))
            return new(null, CommentError.NotFound);

        var comment = new ReportComment
        {
            ItemId = itemId,
            ParentId = parentId,
            AuthorId = authorId,
            AuthorName = authorName.Trim(),
            Body = string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
            ImageAlt = imageAlt?.Trim(),
            ImageContentType = image?.ContentType
        };
        if (image is not null)
        {
            try { comment.ImageKey = await storage.UploadAsync(itemId, image); }
            catch { return new(null, CommentError.StorageUnavailable); }
        }
        try { await repository.InsertAsync(comment); }
        catch
        {
            await cleanup.DeleteOrQueueAsync(comment.ImageKey);
            throw;
        }

        // Avoid leaving a new orphan when report deletion wins the race.
        if (await repository.GetItemAsync(itemId) is null)
        {
            await repository.DeleteAsync(comment.Id);
            await cleanup.DeleteOrQueueAsync(comment.ImageKey);
            return new(null, CommentError.NotFound);
        }

        return new(CommentView.From(comment));
    }

    public async Task<CommentError> DeleteAsync(string itemId, string commentId, string userId, bool isAdmin)
    {
        if (!TryCanonicalId(itemId, out itemId) ||
            !TryCanonicalId(commentId, out commentId)) return CommentError.InvalidInput;
        if (await repository.GetItemAsync(itemId) is null) return CommentError.NotFound;
        var comment = await repository.GetAsync(commentId);
        if (comment is null || comment.ItemId != itemId) return CommentError.NotFound;
        if (!isAdmin && comment.AuthorId != userId) return CommentError.Forbidden;
        if (comment.DeletedAt is not null) return CommentError.None;
        var deleted = await repository.SoftDeleteAsync(commentId, DateTime.UtcNow);
        await cleanup.DeleteOrQueueAsync(deleted?.ImageKey);
        return CommentError.None;
    }

    public async Task<CommentError> SetImageAsync(string itemId, string commentId,
        ImageUpload image, string? imageAlt, string userId, bool isAdmin)
    {
        if (!TryCanonicalId(itemId, out itemId) || !TryCanonicalId(commentId, out commentId) ||
            !ValidImageFields(image, imageAlt)) return CommentError.InvalidInput;
        var result = await GetEditableAsync(itemId, commentId, userId, isAdmin);
        if (result.Error != CommentError.None) return result.Error;
        var previousKey = result.Value!.ImageKey;
        string key;
        try { key = await storage.UploadAsync(itemId, image); }
        catch { return CommentError.StorageUnavailable; }
        bool changed;
        try
        {
            changed = await repository.ChangeImageAsync(commentId, previousKey,
                key, imageAlt!.Trim(), image.ContentType);
        }
        catch
        {
            await cleanup.DeleteOrQueueAsync(key);
            throw;
        }
        if (!changed)
        {
            await cleanup.DeleteOrQueueAsync(key);
            return CommentError.NotFound;
        }
        await cleanup.DeleteOrQueueAsync(previousKey);
        return CommentError.None;
    }

    public async Task<CommentError> RemoveImageAsync(string itemId, string commentId,
        string userId, bool isAdmin)
    {
        if (!TryCanonicalId(itemId, out itemId) || !TryCanonicalId(commentId, out commentId))
            return CommentError.InvalidInput;
        var result = await GetEditableAsync(itemId, commentId, userId, isAdmin);
        if (result.Error != CommentError.None) return result.Error;
        var comment = result.Value!;
        if (comment.ImageKey is null) return CommentError.None;
        if (string.IsNullOrWhiteSpace(comment.Body)) return CommentError.InvalidInput;
        var previousKey = comment.ImageKey;
        if (!await repository.ChangeImageAsync(commentId, previousKey, null, null, null))
            return CommentError.NotFound;
        await cleanup.DeleteOrQueueAsync(previousKey);
        return CommentError.None;
    }

    public async Task<CommentResult<StoredImage>> GetImageAsync(string itemId, string commentId)
    {
        if (!TryCanonicalId(itemId, out itemId) || !TryCanonicalId(commentId, out commentId))
            return new(null, CommentError.InvalidInput);
        if (await repository.GetItemAsync(itemId) is null) return new(null, CommentError.NotFound);
        var comment = await repository.GetAsync(commentId);
        if (comment?.ItemId != itemId || comment.DeletedAt is not null || comment.ImageKey is null)
            return new(null, CommentError.NotFound);
        try
        {
            var image = await storage.ReadAsync(comment.ImageKey);
            return image is null ? new(null, CommentError.NotFound) : new(image);
        }
        catch { return new(null, CommentError.StorageUnavailable); }
    }

    private async Task<CommentResult<ReportComment>> GetEditableAsync(
        string itemId, string commentId, string userId, bool isAdmin)
    {
        if (await repository.GetItemAsync(itemId) is null) return new(null, CommentError.NotFound);
        var comment = await repository.GetAsync(commentId);
        if (comment?.ItemId != itemId || comment.DeletedAt is not null)
            return new(null, CommentError.NotFound);
        if (!isAdmin && comment.AuthorId != userId) return new(null, CommentError.Forbidden);
        return new(comment);
    }

    private static bool ValidImageFields(ImageUpload? image, string? alt) =>
        image is null ? alt is null :
        !string.IsNullOrWhiteSpace(alt) && alt.Trim().Length <= MaxAltLength;

    private async Task<bool> BelongsToItemAsync(string commentId, string itemId) =>
        (await repository.GetAsync(commentId))?.ItemId == itemId;

    private static bool TryCanonicalId(string? id, out string canonicalId)
    {
        var valid = ObjectId.TryParse(id, out var parsed);
        canonicalId = valid ? parsed.ToString() : string.Empty;
        return valid;
    }

    private static bool TryCanonicalOptionalId(string? id, out string? canonicalId)
    {
        if (id is null)
        {
            canonicalId = null;
            return true;
        }

        var valid = TryCanonicalId(id, out var parsed);
        canonicalId = valid ? parsed : null;
        return valid;
    }
}
