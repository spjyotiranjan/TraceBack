using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class CommentServiceTests
{
    private readonly FakeRepository repository = new();
    private readonly CommentService service;
    private readonly Item item = new();
    private const string Author = "author";

    public CommentServiceTests()
    {
        service = new CommentService(repository);
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

    private sealed class FakeRepository : ICommentRepository
    {
        public Item? Item { get; set; }
        public Item? OtherItem { get; set; }
        public List<ReportComment> Comments { get; } = [];

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
            Comments.Add(comment);
            return Task.CompletedTask;
        }

        public Task<bool> SoftDeleteAsync(string commentId, DateTime deletedAt)
        {
            var comment = Comments.FirstOrDefault(x => x.Id == commentId && x.DeletedAt is null);
            if (comment is null) return Task.FromResult(false);
            comment.Body = null;
            comment.DeletedAt = deletedAt;
            return Task.FromResult(true);
        }

        public Task DeleteAsync(string commentId)
        {
            Comments.RemoveAll(x => x.Id == commentId);
            return Task.CompletedTask;
        }
    }
}
