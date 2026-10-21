using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using TraceBack.Data;
using TraceBack.Models;

namespace TraceBack.Services;

public sealed record JwtSettings(SecurityKey Key, string Issuer);

public sealed class AccountService(MongoStore store, JwtSettings jwt)
{
    private readonly PasswordHasher<AppUser> _hasher = new();

    public Task<List<AppUser>> RecentUsersAsync() =>
        store.Users.Find(_ => true).SortByDescending(x => x.CreatedAt).Limit(100).ToListAsync();

    public async Task<AppUser?> RegisterAsync(RegisterForm form)
    {
        var email = form.Email.Trim().ToLowerInvariant();
        if (await store.Users.Find(x => x.Email == email).AnyAsync()) return null;
        var user = new AppUser { Name = form.Name.Trim(), Email = email };
        user.PasswordHash = _hasher.HashPassword(user, form.Password);
        try { await store.Users.InsertOneAsync(user); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey) { return null; }
        return user;
    }

    public async Task<AppUser?> LoginAsync(LoginForm form)
    {
        var email = form.Email.Trim().ToLowerInvariant();
        var user = await store.Users.Find(x => x.Email == email).FirstOrDefaultAsync();
        return user is not null &&
            _hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password) != PasswordVerificationResult.Failed
            ? user : null;
    }

    public string CreateToken(AppUser user)
    {
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer, audience: "TraceBack.Api",
            claims: [new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id), new System.Security.Claims.Claim(ClaimTypes.Name, user.Name), new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)],
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(jwt.Key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
