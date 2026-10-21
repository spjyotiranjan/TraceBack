using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraceBack.Models;

public sealed class AppUser
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Student";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Item
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "Lost";
    public string Category { get; set; } = "";
    public string Location { get; set; } = "";
    public DateTime DateReported { get; set; } = DateTime.UtcNow.Date;
    public string ContactPreference { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "Active";
    public string? ApprovedClaimId { get; set; }
    public string PostedByUserId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Claim
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ItemId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
