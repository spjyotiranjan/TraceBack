using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public sealed class AdminBootstrapService(MongoStore store, IConfiguration configuration)
{
    public async Task EnsureAdminAsync()
    {
        var email = configuration["BootstrapAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["BootstrapAdmin:Password"];
        var name = configuration["BootstrapAdmin:Name"]?.Trim();
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12)
            throw new InvalidOperationException("BootstrapAdmin:Email and a password of at least 12 characters must both be configured.");

        var user = await store.Users.Find(x => x.Email == email).FirstOrDefaultAsync();
        var hasher = new PasswordHasher<AppUser>();
        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                Name = string.IsNullOrWhiteSpace(name) ? "Campus Admin" : name,
                Role = "Admin"
            };
            user.PasswordHash = hasher.HashPassword(user, password);
            try
            {
                await store.Users.InsertOneAsync(user);
                return;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                user = await store.Users.Find(x => x.Email == email).FirstAsync();
            }
        }

        // Configuration owns this one account. Reapply the configured password so bootstrap is repeatable.
        user.Role = "Admin";
        user.PasswordHash = hasher.HashPassword(user, password);
        await store.Users.ReplaceOneAsync(x => x.Id == user.Id && x.Email == email, user);
    }
}
