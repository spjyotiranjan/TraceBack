using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public interface IImageCleanup
{
    Task DeleteOrQueueAsync(string? key);
}

public sealed class ImageCleanup(MongoStore store, IImageStorage storage, ILogger<ImageCleanup> logger) : IImageCleanup
{
    public async Task DeleteOrQueueAsync(string? key)
    {
        if (key is null) return;
        try
        {
            await storage.DeleteAsync(key);
        }
        catch (Exception error)
        {
            logger.LogWarning(error, "Image deletion deferred for {ImageKey}", key);
            await store.PendingImageDeletes.ReplaceOneAsync(
                x => x.Key == key, new PendingImageDelete { Key = key },
                new ReplaceOptions { IsUpsert = true });
        }
    }
}

public sealed class ImageCleanupWorker(
    IServiceScopeFactory scopes, ILogger<ImageCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<MongoStore>();
                var storage = scope.ServiceProvider.GetRequiredService<IImageStorage>();
                var pending = await store.PendingImageDeletes.Find(_ => true)
                    .Limit(100).ToListAsync(stoppingToken);
                foreach (var entry in pending)
                {
                    try
                    {
                        await storage.DeleteAsync(entry.Key, stoppingToken);
                        await store.PendingImageDeletes.DeleteOneAsync(x => x.Key == entry.Key, stoppingToken);
                    }
                    catch (Exception error)
                    {
                        logger.LogWarning(error, "Image deletion retry failed for {ImageKey}", entry.Key);
                    }
                }
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(error, "Image cleanup retry cycle failed");
            }
        }
    }
}
