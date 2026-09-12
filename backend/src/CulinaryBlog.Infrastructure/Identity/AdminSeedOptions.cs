using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Identity;

public sealed class AdminSeedOptions : IValidatableObject
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }

    [MaxLength(100)]
    public string DisplayName { get; init; } = "Administrator";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(
                "The Email field is required when admin seeding is enabled.",
                [nameof(Email)]);
        }
        else if (!new EmailAddressAttribute().IsValid(Email))
        {
            yield return new ValidationResult(
                "The Email field is not a valid e-mail address.",
                [nameof(Email)]);
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult(
                "The Password field is required when admin seeding is enabled.",
                [nameof(Password)]);
        }
    }
}
