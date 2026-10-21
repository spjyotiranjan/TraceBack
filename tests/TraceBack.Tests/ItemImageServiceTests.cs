using Microsoft.Extensions.Logging.Abstractions;
using TraceBack.Models;
using TraceBack.Services;

namespace TraceBack.Tests;

public sealed class ItemImageServiceTests
{
    private static readonly ImageUpload Image = new([1, 2, 3], "image/png", "png");

    [Fact]
    public async Task Owner_can_upload_and_retrieve_an_image()
    {
        var (service, repository, storage) = Create();

        Assert.True(await service.SetImageAsync(repository.Item!.Id, Image, "owner", false));
        Assert.Equal("key-1", repository.Item.ImageKey);
        Assert.Equal(Image.Bytes, (await service.GetImageAsync(repository.Item.Id))?.Bytes);
        Assert.Equal(Image.ContentType, (await service.GetImageAsync(repository.Item.Id))?.ContentType);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task Nonowner_cannot_change_or_delete_images_or_items()
    {
        var (service, repository, storage) = Create("old-key");

        Assert.False(await service.SetImageAsync(repository.Item!.Id, Image, "stranger", false));
        Assert.False(await service.RemoveImageAsync(repository.Item.Id, "stranger", false));
        Assert.False(await service.DeleteItemAsync(repository.Item.Id, "stranger", false));
        Assert.Equal("old-key", repository.Item.ImageKey);
        Assert.Equal(0, storage.UploadCount);
        Assert.Empty(storage.DeletedKeys);
    }

    [Fact]
    public async Task Admin_can_replace_and_remove_an_owners_image()
    {
        var (service, repository, storage) = Create("old-key");

        Assert.True(await service.SetImageAsync(repository.Item!.Id, Image, "admin", true));
        Assert.Equal("key-1", repository.Item.ImageKey);
        Assert.Contains("old-key", storage.DeletedKeys);
        Assert.True(await service.RemoveImageAsync(repository.Item.Id, "admin", true));
        Assert.Null(repository.Item.ImageKey);
        Assert.Equal(["old-key", "key-1"], storage.DeletedKeys);
    }

    [Fact]
    public async Task Failed_database_change_removes_the_new_object()
    {
        var (service, repository, storage) = Create("old-key");
        repository.ChangeImageResult = false;

        Assert.False(await service.SetImageAsync(repository.Item!.Id, Image, "owner", false));
        Assert.Equal("old-key", repository.Item.ImageKey);
        Assert.Equal(["key-1"], storage.DeletedKeys);

        repository.ChangeImageError = new InvalidOperationException("database unavailable");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetImageAsync(repository.Item.Id, Image, "owner", false));
        Assert.Equal(["key-1", "key-2"], storage.DeletedKeys);
    }

    [Fact]
    public async Task Cleanup_failure_does_not_undo_a_successful_replacement()
    {
        var (service, repository, storage) = Create("old-key");
        storage.FailingDeleteKeys.Add("old-key");

        Assert.True(await service.SetImageAsync(repository.Item!.Id, Image, "owner", false));
        Assert.Equal("key-1", repository.Item.ImageKey);
        Assert.Equal(["old-key"], storage.DeletedKeys);
    }

    [Fact]
    public async Task Claim_delete_failure_still_attempts_image_cleanup()
    {
        var (service, repository, storage) = Create("old-key");
        repository.DeleteClaimsError = new InvalidOperationException("claims unavailable");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteItemAsync(repository.Item!.Id, "owner", false));
        Assert.Null(repository.Item);
        Assert.Equal(["old-key"], storage.DeletedKeys);
    }

    [Fact]
    public async Task Admin_delete_removes_claims_and_attempts_image_cleanup()
    {
        var (service, repository, storage) = Create("old-key");
        storage.FailingDeleteKeys.Add("old-key");

        Assert.True(await service.DeleteItemAsync(repository.Item!.Id, "admin", true));
        Assert.Null(repository.Item);
        Assert.Equal(1, repository.ClaimsDeleted);
        Assert.Equal(["old-key"], storage.DeletedKeys);
    }

    private static (ItemImageService Service, FakeRepository Repository, FakeStorage Storage) Create(string? imageKey = null)
    {
        var repository = new FakeRepository
        {
            Item = new Item { PostedByUserId = "owner", ImageKey = imageKey }
        };
        var storage = new FakeStorage();
        return (new ItemImageService(repository, storage, NullLogger<ItemImageService>.Instance), repository, storage);
    }

    private sealed class FakeRepository : IItemImageRepository
    {
        public Item? Item { get; set; }
        public bool ChangeImageResult { get; set; } = true;
        public Exception? ChangeImageError { get; set; }
        public Exception? DeleteClaimsError { get; set; }
        public int ClaimsDeleted { get; private set; }

        public Task<Item?> GetAsync(string id) => Task.FromResult(Item?.Id == id ? Item : null);

        public Task<bool> DeleteItemAsync(Item item, string userId, bool admin)
        {
            if (Item != item || (!admin && item.PostedByUserId != userId)) return Task.FromResult(false);
            Item = null;
            return Task.FromResult(true);
        }

        public Task DeleteClaimsAsync(string itemId)
        {
            if (DeleteClaimsError is not null) throw DeleteClaimsError;
            ClaimsDeleted++;
            return Task.CompletedTask;
        }

        public Task<bool> ChangeImageAsync(Item item, string? key, string userId, bool admin)
        {
            if (ChangeImageError is not null) throw ChangeImageError;
            if (!ChangeImageResult || Item != item || (!admin && item.PostedByUserId != userId)) return Task.FromResult(false);
            item.ImageKey = key;
            item.ImageUrl = null;
            return Task.FromResult(true);
        }
    }

    private sealed class FakeStorage : IImageStorage
    {
        private readonly Dictionary<string, StoredImage> objects = [];
        public int UploadCount { get; private set; }
        public List<string> DeletedKeys { get; } = [];
        public HashSet<string> FailingDeleteKeys { get; } = [];

        public Task<string> UploadAsync(string itemId, ImageUpload image, CancellationToken cancellationToken = default)
        {
            var key = $"key-{++UploadCount}";
            objects[key] = new StoredImage(image.Bytes, image.ContentType);
            return Task.FromResult(key);
        }

        public Task<StoredImage?> ReadAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(objects.GetValueOrDefault(key));

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            DeletedKeys.Add(key);
            if (FailingDeleteKeys.Contains(key)) throw new InvalidOperationException("storage unavailable");
            objects.Remove(key);
            return Task.CompletedTask;
        }
    }
}
