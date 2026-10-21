using MongoDB.Bson;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public sealed class ClaimService(MongoStore store)
{
    public async Task<Claim?> CreateAsync(string itemId, string userId, ClaimForm form)
    {
        if (!ObjectId.TryParse(itemId, out _))
        {
            return null;
        }

        var item = await store.Items.Find(x => x.Id == itemId).FirstOrDefaultAsync();
        if (item is null || item.Type != "Found" || item.Status != "Active" || item.PostedByUserId == userId)
        {
            return null;
        }

        var claim = new Claim
        {
            ItemId = itemId,
            UserId = userId,
            Message = form.Message.Trim()
        };

        try
        {
            await store.Claims.InsertOneAsync(claim);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }

        var stillAvailable = await store.Items
            .Find(x => x.Id == itemId && x.Type == "Found" && x.Status == "Active")
            .AnyAsync();

        if (!stillAvailable)
        {
            var removed = await store.Claims.DeleteOneAsync(x => x.Id == claim.Id && x.Status == "Pending");
            if (removed.DeletedCount > 0)
            {
                return null;
            }

            // An admin may have approved this claim while the availability check ran.
            return await store.Claims.Find(x => x.Id == claim.Id && x.Status == "Approved").FirstOrDefaultAsync();
        }

        return claim;
    }

    public Task<List<Claim>> MineAsync(string userId) =>
        store.Claims.Find(x => x.UserId == userId)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task<List<Claim>> AllAsync()
    {
        await ReconcileStaleApprovalsAsync();
        var claims = await store.Claims.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync();

        return claims.OrderBy(x => x.Status switch
        {
            "Pending" => 0,
            "Approving" => 1,
            _ => 2
        }).ThenByDescending(x => x.CreatedAt).ToList();
    }

    public Task<long> CountAsync() => store.Claims.CountDocumentsAsync(_ => true);

    public async Task<bool> DecideAsync(string id, bool approve)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        await ReconcileStaleApprovalsAsync();

        if (!approve)
        {
            var rejected = await store.Claims.UpdateOneAsync(
                x => x.Id == id && x.Status == "Pending",
                Builders<Claim>.Update
                    .Set(x => x.Status, "Rejected")
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            return rejected.ModifiedCount > 0;
        }

        var claim = await store.Claims.Find(x => x.Id == id && x.Status == "Pending").FirstOrDefaultAsync();
        if (claim is null)
        {
            return false;
        }

        var locked = await store.Claims.UpdateOneAsync(
            x => x.Id == id && x.Status == "Pending",
            Builders<Claim>.Update
                .Set(x => x.Status, "Approving")
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        if (locked.ModifiedCount == 0)
        {
            return false;
        }

        var result = await store.Items.UpdateOneAsync(
            x => x.Id == claim.ItemId && x.Type == "Found" && x.Status == "Active",
            Builders<Item>.Update
                .Set(x => x.Status, "Claimed")
                .Set(x => x.ApprovedClaimId, id)
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        if (result.ModifiedCount == 0)
        {
            await store.Claims.UpdateOneAsync(
                x => x.Id == id && x.Status == "Approving",
                Builders<Claim>.Update
                    .Set(x => x.Status, "Rejected")
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            return false;
        }

        // Reject other pending claims on this item
        await store.Claims.UpdateManyAsync(
            x => x.ItemId == claim.ItemId && x.Id != id && x.Status == "Pending",
            Builders<Claim>.Update
                .Set(x => x.Status, "Rejected")
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        // Mark the chosen claim approved
        await store.Claims.UpdateOneAsync(
            x => x.Id == id && x.Status == "Approving",
            Builders<Claim>.Update
                .Set(x => x.Status, "Approved")
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        return true;
    }

    // Standalone MongoDB cannot atomically update an item and its claims. Finish old
    // in-progress decisions whenever an admin lists claims or makes a decision.
    public async Task ReconcileStaleApprovalsAsync()
    {
        var staleBefore = DateTime.UtcNow.AddMinutes(-5);
        var stale = await store.Claims.Find(x => x.Status == "Approving" && x.UpdatedAt < staleBefore)
            .SortBy(x => x.UpdatedAt)
            .Limit(100)
            .ToListAsync();

        foreach (var claim in stale)
        {
            var item = await store.Items.Find(x => x.Id == claim.ItemId).FirstOrDefaultAsync();
            if (item is { Status: "Active", Type: "Found" })
            {
                await store.Claims.UpdateOneAsync(
                    x => x.Id == claim.Id && x.Status == "Approving" && x.UpdatedAt == claim.UpdatedAt,
                    Builders<Claim>.Update
                        .Set(x => x.Status, "Pending")
                        .Set(x => x.UpdatedAt, DateTime.UtcNow));
                continue;
            }

            var recovered = item?.Status == "Claimed" && item.ApprovedClaimId == claim.Id;
            var changed = await store.Claims.UpdateOneAsync(
                x => x.Id == claim.Id && x.Status == "Approving" && x.UpdatedAt == claim.UpdatedAt,
                Builders<Claim>.Update
                    .Set(x => x.Status, recovered ? "Approved" : "Rejected")
                    .Set(x => x.UpdatedAt, DateTime.UtcNow));

            if (recovered && changed.ModifiedCount > 0)
            {
                await store.Claims.UpdateManyAsync(
                    x => x.ItemId == claim.ItemId && x.Status == "Pending",
                    Builders<Claim>.Update
                        .Set(x => x.Status, "Rejected")
                        .Set(x => x.UpdatedAt, DateTime.UtcNow));
            }
        }
    }
}
