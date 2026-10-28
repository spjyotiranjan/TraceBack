using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public enum CommentError { None, InvalidInput, NotFound, Inactive, Forbidden }

public sealed record CommentResult<T>(T? Value, CommentError Error = CommentError.None)
{
    public bool Succeeded => Error == CommentError.None;
}

public sealed record CommentView(
    string Id, string ItemId, string? ParentId, string AuthorId, string AuthorName,
    string? Body, DateTime CreatedAt, bool Deleted)
{
    public static CommentView From(ReportComment comment) => new(
        comment.Id, comment.ItemId, comment.ParentId, comment.AuthorId,
        comment.AuthorName, comment.DeletedAt is null ? comment.Body : null,
        comment.CreatedAt, comment.DeletedAt is not null);
}

public sealed record CommentPage(IReadOnlyList<CommentView> Comments, string? NextCursor);

public interface ICommentRepository
{
    Task<Item?> GetItemAsync(string itemId);
    Task<ReportComment?> GetAsync(string commentId);
    Task<IReadOnlyList<ReportComment>> ListAsync(string itemId, string? parentId, string? afterId, int limit);
    Task InsertAsync(ReportComment comment);
    Task<bool> SoftDeleteAsync(string commentId, DateTime deletedAt);
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

    public async Task<bool> SoftDeleteAsync(string commentId, DateTime deletedAt) =>
        (await store.Comments.UpdateOneAsync(
            x => x.Id == commentId && x.DeletedAt == null,
            Builders<ReportComment>.Update.Set(x => x.Body, null).Set(x => x.DeletedAt, deletedAt)))
        .ModifiedCount > 0;

    public Task DeleteAsync(string commentId) =>
        store.Comments.DeleteOneAsync(x => x.Id == commentId);
}

public interface ICommentService
{
    Task<CommentResult<CommentPage>> ListAsync(string itemId, string? parentId, string? cursor, int limit);
    Task<CommentResult<CommentView>> CreateAsync(
        string itemId, string? parentId, string? body, string authorId, string authorName);
    Task<CommentError> DeleteAsync(string itemId, string commentId, string userId, bool isAdmin);
}

public sealed class CommentService(ICommentRepository repository) : ICommentService
{
    public const int MaxBodyLength = 2000;
    public const int MaxPageSize = 50;

    public async Task<CommentResult<CommentPage>> ListAsync(
        string itemId, string? parentId, string? cursor, int limit)
    {
        if (!ValidId(itemId) || !ValidOptionalId(parentId) || !ValidOptionalId(cursor) ||
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
        string itemId, string? parentId, string? body, string authorId, string authorName)
    {
        if (!ValidId(itemId) || !ValidOptionalId(parentId) ||
            string.IsNullOrWhiteSpace(body) || body.Trim().Length > MaxBodyLength ||
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
            Body = body.Trim()
        };
        await repository.InsertAsync(comment);

        // Avoid leaving a new orphan when report deletion wins the race.
        if (await repository.GetItemAsync(itemId) is null)
        {
            await repository.DeleteAsync(comment.Id);
            return new(null, CommentError.NotFound);
        }

        return new(CommentView.From(comment));
    }

    public async Task<CommentError> DeleteAsync(string itemId, string commentId, string userId, bool isAdmin)
    {
        if (!ValidId(itemId) || !ValidId(commentId)) return CommentError.InvalidInput;
        if (await repository.GetItemAsync(itemId) is null) return CommentError.NotFound;
        var comment = await repository.GetAsync(commentId);
        if (comment is null || comment.ItemId != itemId) return CommentError.NotFound;
        if (!isAdmin && comment.AuthorId != userId) return CommentError.Forbidden;
        if (comment.DeletedAt is not null) return CommentError.None;
        await repository.SoftDeleteAsync(commentId, DateTime.UtcNow);
        return CommentError.None;
    }

    private async Task<bool> BelongsToItemAsync(string commentId, string itemId) =>
        (await repository.GetAsync(commentId))?.ItemId == itemId;

    private static bool ValidId(string? id) => ObjectId.TryParse(id, out _);
    private static bool ValidOptionalId(string? id) => id is null || ValidId(id);
}
