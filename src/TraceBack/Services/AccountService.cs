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
        store.Users.Find(_ => true)
            .SortByDescending(user => user.CreatedAt)
            .Limit(100)
            .ToListAsync();

    public async Task<AppUser?> RegisterAsync(RegisterForm form)
    {
        var email = form.Email.Trim().ToLowerInvariant();

        var existingUser = await store.Users.Find(user => user.Email == email).AnyAsync();
        if (existingUser)
        {
            return null;
        }

        var user = new AppUser
        {
            Name = form.Name.Trim(),
            Email = email
        };

        user.PasswordHash = _hasher.HashPassword(user, form.Password);

        try
        {
            await store.Users.InsertOneAsync(user);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }

        return user;
    }

    public async Task<AppUser?> LoginAsync(LoginForm form)
    {
        var email = form.Email.Trim().ToLowerInvariant();
        var user = await store.Users.Find(u => u.Email == email).FirstOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, form.Password);
        return verificationResult != PasswordVerificationResult.Failed ? user : null;
    }

    public string CreateToken(AppUser user)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, user.Id),
            new System.Security.Claims.Claim(ClaimTypes.Name, user.Name),
            new System.Security.Claims.Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: "TraceBack.Api",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: new SigningCredentials(jwt.Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
