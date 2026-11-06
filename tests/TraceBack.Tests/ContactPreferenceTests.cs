using System.ComponentModel.DataAnnotations;
using TraceBack.Models;

namespace TraceBack.Tests;

public sealed class ContactPreferenceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Missing_contact_preference_uses_visible_label(string? contactPreference)
    {
        var form = new ItemForm { ContactPreference = contactPreference! };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), errors, true));
        var error = Assert.Single(errors, error => error.MemberNames.Contains(nameof(ItemForm.ContactPreference)));
        Assert.Equal("The Contact preference field is required.", error.ErrorMessage);
    }

    [Fact]
    public void Contact_length_limit_uses_visible_label()
    {
        var form = new ItemForm { ContactPreference = new string('x', 121) };
        var errors = new List<ValidationResult>();

        Assert.False(Validator.TryValidateObject(form, new ValidationContext(form), errors, true));
        var error = Assert.Single(errors, error => error.MemberNames.Contains(nameof(ItemForm.ContactPreference)));
        Assert.Equal("The field Contact preference must be a string with a maximum length of 120.", error.ErrorMessage);
    }
}
