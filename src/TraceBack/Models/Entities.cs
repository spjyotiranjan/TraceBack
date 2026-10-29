using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TraceBack.Models;

public sealed class AppUser
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Item
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Type { get; set; } = "Lost";

    public string Category { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    [BsonElement("mapLocation")]
    [BsonIgnoreIfNull]
    public MapLocation? MapLocation { get; set; }

    public DateTime DateReported { get; set; } = DateTime.UtcNow.Date;

    public string ContactPreference { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string? ImageKey { get; set; }

    [BsonIgnore]
    public string? DisplayImageUrl => ImageKey is not null ? $"/Items/Image/{Id}" : ImageUrl;

    public string Status { get; set; } = "Active";

    public string? ApprovedClaimId { get; set; }

    public string PostedByUserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class MapLocation
{
    [BsonElement("latitude")]
    public double Latitude { get; set; }

    [BsonElement("longitude")]
    public double Longitude { get; set; }
}

public sealed class Claim
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ItemId { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ReportComment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string ItemId { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? Body { get; set; }
    public string? ImageKey { get; set; }
    public string? ImageAlt { get; set; }
    public string? ImageContentType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }
}

public sealed class PendingImageDelete
{
    [BsonId]
    public string Key { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NextAttemptAt { get; set; }
    public int FailureCount { get; set; }
}
