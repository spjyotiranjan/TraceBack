using System.ComponentModel.DataAnnotations;

namespace TraceBack.Models;

public sealed class RegisterForm
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginForm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed class ItemForm : IValidatableObject
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression("Lost|Found")]
    public string Type { get; set; } = "Lost";

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Location { get; set; } = string.Empty;

    public MapLocationInput? MapLocation { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateReported { get; set; } = DateTime.Today;

    [Required]
    [StringLength(120)]
    public string ContactPreference { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            yield return new ValidationResult("Title is required.", [nameof(Title)]);
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            yield return new ValidationResult("Description is required.", [nameof(Description)]);
        }

        if (string.IsNullOrWhiteSpace(Category))
        {
            yield return new ValidationResult("Category is required.", [nameof(Category)]);
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            yield return new ValidationResult("Location is required.", [nameof(Location)]);
        }

        if (!MapLocationRules.TryNormalize(MapLocation, out _, out var locationError))
        {
            yield return new ValidationResult(locationError, [nameof(MapLocation)]);
        }

        if (string.IsNullOrWhiteSpace(ContactPreference))
        {
            yield return new ValidationResult("Contact preference is required.", [nameof(ContactPreference)]);
        }

        if (DateReported.Date > DateTime.UtcNow.Date)
        {
            yield return new ValidationResult("Report date cannot be in the future.", [nameof(DateReported)]);
        }
    }
}

public sealed class HttpsUrlAttribute : ValidationAttribute
{
    public HttpsUrlAttribute() : base("Image URL must be a valid HTTPS URL (https://).")
    {
    }

    public override bool IsValid(object? value)
    {
        if (value is null || value is string { Length: 0 })
        {
            return true;
        }

        return value is string url &&
            Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            uri.IsWellFormedOriginalString();
    }
}

public sealed class ClaimForm : IValidatableObject
{
    [Required]
    [StringLength(1000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Message) || Message.Trim().Length < 10)
        {
            yield return new ValidationResult("Claim message must contain at least 10 characters.", [nameof(Message)]);
        }
    }
}

public sealed class ItemSearch
{
    public string? Query { get; set; }
    public string? Type { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
}
