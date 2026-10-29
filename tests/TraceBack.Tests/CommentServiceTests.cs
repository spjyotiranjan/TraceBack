using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class CommentServiceTests
{
    private readonly FakeRepository repository = new();
    private readonly FakeStorage storage = new();
    private readonly FakeCleanup cleanup = new();
    private readonly CommentService service;
    private readonly Item item = new();
    private const string Author = "author";

    public CommentServiceTests()
    {
        service = new CommentService(repository, storage, cleanup);
        repository.Item = item;
    }

    [Fact]
    public async Task Root_reply_and_nested_reply_are_listed_by_parent_with_stable_order()
    {
        var root = (await service.CreateAsync(item.Id, null, " root ", Author, "Alice")).Value!;
        var second = (await service.CreateAsync(item.Id, null, "second", Author, "Alice")).Value!;
        var reply = (await service.CreateAsync(item.Id, root.Id, "reply", Author, "Alice")).Value!;
        var nested = (await service.CreateAsync(item.Id, reply.Id, "nested", Author, "Alice")).Value!;

        var firstPage = (await service.ListAsync(item.Id, null, null, 1)).Value!;
        Assert.Equal(root.Id, Assert.Single(firstPage.Comments).Id);
        Assert.Equal(root.Id, firstPage.NextCursor);
        var lastPage = (await service.ListAsync(item.Id, null, firstPage.NextCursor, 1)).Value!;
        Assert.Equal(second.Id, Assert.Single(lastPage.Comments).Id);
        Assert.Null(lastPage.NextCursor);
        Assert.Equal(reply.Id, Assert.Single((await service.ListAsync(item.Id, root.Id, null, 20)).Value!.Comments).Id);
        Assert.Equal(nested.Id, Assert.Single((await service.ListAsync(item.Id, reply.Id, null, 20)).Value!.Comments).Id);
        Assert.Equal("root", firstPage.Comments[0].Body);
        Assert.Equal("Alice", firstPage.Comments[0].AuthorName);
    }

    [Fact]
    public async Task Mixed_case_ids_use_canonical_report_parent_and_cursor_values()
    {
        var mixedItemId = item.Id.ToUpperInvariant();
        var root = (await service.CreateAsync(mixedItemId, null, "root", Author, "Alice")).Value!;
        var second = (await service.CreateAsync(item.Id, null, "second", Author, "Alice")).Value!;
        var reply = (await service.CreateAsync(mixedItemId, root.Id.ToUpperInvariant(), "reply", Author, "Alice")).Value!;

        Assert.Equal(item.Id, root.ItemId);
        Assert.Equal(item.Id, reply.ItemId);
        Assert.Equal(root.Id, reply.ParentId);
        Assert.Equal([root.Id, second.Id],
            (await service.ListAsync(mixedItemId, null, null, 20)).Value!.Comments.Select(x => x.Id));
        Assert.Equal(second.Id,
            Assert.Single((await service.ListAsync(mixedItemId, null, root.Id.ToUpperInvariant(), 20)).Value!.Comments).Id);
        Assert.Equal(reply.Id,
            Assert.Single((await service.ListAsync(item.Id, root.Id.ToUpperInvariant(), null, 20)).Value!.Comments).Id);
        Assert.Equal(CommentError.None,
            await service.DeleteAsync(mixedItemId, root.Id.ToUpperInvariant(), Author, false));
        Assert.True(Assert.Single((await service.ListAsync(item.Id, null, null, 1)).Value!.Comments).Deleted);
    }

    [Fact]
    public async Task Cross_report_parent_and_cursor_are_rejected()
    {
        var other = new Item();
        repository.OtherItem = other;
        var foreign = (await service.CreateAsync(other.Id, null, "foreign", Author, "Alice")).Value!;

        Assert.Equal(CommentError.NotFound,
            (await service.CreateAsync(item.Id, foreign.Id, "reply", Author, "Alice")).Error);
        Assert.Equal(CommentError.NotFound,
            (await service.ListAsync(item.Id, foreign.Id, null, 20)).Error);
        Assert.Equal(CommentError.InvalidInput,
            (await service.ListAsync(item.Id, null, foreign.Id, 20)).Error);
    }

    [Fact]
    public async Task Invalid_body_cursor_and_page_limit_are_rejected()
    {
        Assert.Equal(CommentError.InvalidInput,
            (await service.CreateAsync(item.Id, null, "   ", Author, "Alice")).Error);
        Assert.Equal(CommentError.InvalidInput,
            (await service.CreateAsync(item.Id, null, new string('a', 2001), Author, "Alice")).Error);
        Assert.Equal(CommentError.InvalidInput,
            (await service.ListAsync(item.Id, null, "not-an-id", 20)).Error);
        Assert.Equal(CommentError.InvalidInput,
            (await service.ListAsync(item.Id, null, null, 51)).Error);
        Assert.Empty(repository.Comments);
    }

    [Fact]
    public async Task Inactive_report_blocks_posting_but_keeps_thread_readable()
    {
        var root = (await service.CreateAsync(item.Id, null, "message", Author, "Alice")).Value!;
        item.Status = "Resolved";
        Assert.Equal(CommentError.Inactive,
            (await service.CreateAsync(item.Id, null, "later", Author, "Alice")).Error);
        Assert.Equal(root.Id, Assert.Single((await service.ListAsync(item.Id, null, null, 20)).Value!.Comments).Id);
    }

    [Fact]
    public async Task Only_author_or_admin_can_soft_delete_and_replies_remain()
    {
        var root = (await service.CreateAsync(item.Id, null, "private text", Author, "Alice")).Value!;
        var reply = (await service.CreateAsync(item.Id, root.Id, "reply", "other", "Bob")).Value!;
        Assert.Equal(CommentError.Forbidden, await service.DeleteAsync(item.Id, root.Id, "other", false));
        Assert.Equal(CommentError.None, await service.DeleteAsync(item.Id, root.Id, "admin", true));
        Assert.Equal(CommentError.None, await service.DeleteAsync(item.Id, root.Id, Author, false));
        var placeholder = Assert.Single((await service.ListAsync(item.Id, null, null, 20)).Value!.Comments);
        Assert.True(placeholder.Deleted);
        Assert.Null(placeholder.Body);
        Assert.Equal(reply.Id, Assert.Single((await service.ListAsync(item.Id, root.Id, null, 20)).Value!.Comments).Id);
    }

    [Fact]
    public async Task Image_only_comment_has_private_route_and_is_removed_on_soft_delete()
    {
        var image = new ImageUpload([1, 2], "image/png", "png");
        var created = (await service.CreateAsync(item.Id, null, null, Author, "Alice", image, "  Red bag ")).Value!;

        Assert.Null(created.Body);
        Assert.Equal("Red bag", created.ImageAlt);
        Assert.Equal($"/Items/{item.Id}/Comments/{created.Id}/Image", created.ImageUrl);
        Assert.Equal($"/api/items/{item.Id}/comments/{created.Id}/image", created.ForApi().ImageUrl);
        Assert.Equal(image.Bytes, (await service.GetImageAsync(item.Id, created.Id)).Value!.Bytes);
        Assert.Equal(CommentError.Forbidden,
            await service.DeleteAsync(item.Id, created.Id, "stranger", false));
        Assert.Equal(CommentError.None, await service.DeleteAsync(item.Id, created.Id, Author, false));
        Assert.Equal(["image-1"], cleanup.DeletedKeys);
        Assert.Null((await service.ListAsync(item.Id, null, null, 20)).Value!.Comments[0].ImageUrl);
        Assert.Equal(CommentError.NotFound, (await service.GetImageAsync(item.Id, created.Id)).Error);
    }

    [Fact]
    public async Task Image_mutations_require_author_and_keep_image_only_posts_valid()
    {
        var image = new ImageUpload([1], "image/png", "png");
        var imageOnly = (await service.CreateAsync(item.Id, null, null, Author, "Alice", image, "bag")).Value!;
        Assert.Equal(CommentError.Forbidden,
            await service.SetImageAsync(item.Id, imageOnly.Id, image, "new bag", "other", false));
        Assert.Equal(CommentError.InvalidInput,
            await service.RemoveImageAsync(item.Id, imageOnly.Id, Author, false));
        Assert.Equal(CommentError.None,
            await service.SetImageAsync(item.Id, imageOnly.Id, image, "new bag", Author, false));
        Assert.Equal(["image-1"], cleanup.DeletedKeys);

        var withText = (await service.CreateAsync(item.Id, null, "hello", Author, "Alice", image, "bag")).Value!;
        Assert.Equal(CommentError.None,
            await service.RemoveImageAsync(item.Id, withText.Id, "admin", true));
        Assert.Null((await service.ListAsync(item.Id, null, null, 20)).Value!.Comments[1].ImageUrl);
        Assert.Equal(["image-1", "image-3"], cleanup.DeletedKeys);
    }

    [Fact]
    public async Task Invalid_alt_storage_failure_and_failed_insert_do_not_publish_image_comments()
    {
        var image = new ImageUpload([1], "image/png", "png");
        Assert.Equal(CommentError.InvalidInput,
            (await service.CreateAsync(item.Id, null, null, Author, "Alice", image, " ")).Error);
        Assert.Equal(CommentError.InvalidInput,
            (await service.CreateAsync(item.Id, null, null, Author, "Alice", image, new string('a', 201))).Error);
        Assert.Empty(repository.Comments);

        storage.UploadError = new InvalidOperationException("R2 unavailable");
        Assert.Equal(CommentError.StorageUnavailable,
            (await service.CreateAsync(item.Id, null, null, Author, "Alice", image, "bag")).Error);
        Assert.Empty(repository.Comments);

        storage.UploadError = null;
        repository.InsertError = new InvalidOperationException("Mongo unavailable");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(item.Id, null, null, Author, "Alice", image, "bag"));
        Assert.Empty(repository.Comments);
        Assert.Equal(["image-1"], cleanup.DeletedKeys);
    }

    private sealed class FakeRepository : ICommentRepository
    {
        public Item? Item { get; set; }
        public Item? OtherItem { get; set; }
        public List<ReportComment> Comments { get; } = [];
        public Exception? InsertError { get; set; }

        public Task<Item?> GetItemAsync(string itemId) =>
            Task.FromResult(new[] { Item, OtherItem }.FirstOrDefault(x => x?.Id == itemId));

        public Task<ReportComment?> GetAsync(string commentId) =>
            Task.FromResult(Comments.FirstOrDefault(x => x.Id == commentId));

        public Task<IReadOnlyList<ReportComment>> ListAsync(
            string itemId, string? parentId, string? afterId, int limit) =>
            Task.FromResult<IReadOnlyList<ReportComment>>(Comments
                .Where(x => x.ItemId == itemId && x.ParentId == parentId &&
                    (afterId is null || string.CompareOrdinal(x.Id, afterId) > 0))
                .OrderBy(x => x.Id).Take(limit).ToList());

        public Task InsertAsync(ReportComment comment)
        {
            if (InsertError is not null) throw InsertError;
            Comments.Add(comment);
            return Task.CompletedTask;
        }

        public Task<ReportComment?> SoftDeleteAsync(string commentId, DateTime deletedAt)
        {
            var comment = Comments.FirstOrDefault(x => x.Id == commentId && x.DeletedAt is null);
            if (comment is null) return Task.FromResult<ReportComment?>(null);
            var old = new ReportComment { ImageKey = comment.ImageKey };
            comment.Body = null;
            comment.DeletedAt = deletedAt;
            comment.ImageKey = null;
            comment.ImageAlt = null;
            return Task.FromResult<ReportComment?>(old);
        }

        public Task<bool> ChangeImageAsync(string commentId, string? expectedKey,
            string? key, string? alt, string? contentType)
        {
            var comment = Comments.FirstOrDefault(x => x.Id == commentId &&
                x.DeletedAt is null && x.ImageKey == expectedKey);
            if (comment is null) return Task.FromResult(false);
            comment.ImageKey = key;
            comment.ImageAlt = alt;
            comment.ImageContentType = contentType;
            return Task.FromResult(true);
        }

        public Task DeleteAsync(string commentId)
        {
            Comments.RemoveAll(x => x.Id == commentId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorage : IImageStorage
    {
        private readonly Dictionary<string, StoredImage> images = [];
        private int nextKey;
        public Exception? UploadError { get; set; }
        public Task<string> UploadAsync(string itemId, ImageUpload image,
            CancellationToken cancellationToken = default)
        {
            if (UploadError is not null) throw UploadError;
            var key = $"image-{++nextKey}";
            images[key] = new StoredImage(image.Bytes, image.ContentType);
            return Task.FromResult(key);
        }
        public Task<StoredImage?> ReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(images.GetValueOrDefault(key));
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeCleanup : IImageCleanup
    {
        public List<string> DeletedKeys { get; } = [];
        public Task DeleteOrQueueAsync(string? key)
        {
            if (key is not null) DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
