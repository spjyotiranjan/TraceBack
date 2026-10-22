namespace TraceBack.Models;

public sealed record UserResponse(
    string Id,
    string Name,
    string Email,
    string Role,
    DateTime CreatedAt)
{
    public static UserResponse From(AppUser user) =>
        new(user.Id, user.Name, user.Email, user.Role, user.CreatedAt);
}

public sealed record AuthResponse(
    string Token,
    UserResponse User);

public sealed record ItemResponse(
    string Id,
    string Title,
    string Description,
    string Type,
    string Category,
    string Location,
    DateTime DateReported,
    string ContactPreference,
    string? ImageUrl,
    string Status,
    string PostedByUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ItemResponse From(Item item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.Type,
        item.Category,
        item.Location,
        item.DateReported,
        item.ContactPreference,
        item.DisplayImageUrl,
        item.Status,
        item.PostedByUserId,
        item.CreatedAt,
        item.UpdatedAt);
}

public sealed record ClaimResponse(
    string Id,
    string ItemId,
    string UserId,
    string Message,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ClaimResponse From(Claim claim) => new(
        claim.Id,
        claim.ItemId,
        claim.UserId,
        claim.Message,
        claim.Status,
        claim.CreatedAt,
        claim.UpdatedAt);
}
