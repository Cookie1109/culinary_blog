// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the admin seed options.</summary>
public sealed class AdminSeedOptions : IValidatableObject
{
    /// <summary>The section name.</summary>
    public const string SectionName = "AdminSeed";

    /// <summary>Gets or initializes a value indicating whether enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets or initializes the email.</summary>
    public string? Email { get; init; }

    /// <summary>Gets or initializes the password.</summary>
    public string? Password { get; init; }

    /// <summary>Gets or initializes the display name.</summary>
    [MaxLength(100)]
    public string DisplayName { get; init; } = "Administrator";

    /// <summary>Validates the admin seed options.</summary>
    /// <param name="validationContext">The validation context supplied to this operation.</param>
    /// <returns>The validate result.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!this.Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(this.Email))
        {
            yield return new ValidationResult(
                "The Email field is required when admin seeding is enabled.",
                [nameof(this.Email)]);
        }
        else if (!new EmailAddressAttribute().IsValid(this.Email))
        {
            yield return new ValidationResult(
                "The Email field is not a valid e-mail address.",
                [nameof(this.Email)]);
        }

        if (string.IsNullOrWhiteSpace(this.Password))
        {
            yield return new ValidationResult(
                "The Password field is required when admin seeding is enabled.",
                [nameof(this.Password)]);
        }
    }
}
