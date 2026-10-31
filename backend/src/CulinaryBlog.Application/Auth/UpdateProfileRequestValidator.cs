// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the update profile request validator.</summary>
public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    /// <summary>Initializes a new instance of the <see cref="UpdateProfileRequestValidator"/> class.</summary>
    public UpdateProfileRequestValidator()
    {
        this.RuleFor(request => request.DisplayName).NotEmpty().Length(2, 100).When(request => request.UpdateDisplayName);
        this.RuleFor(request => request.AvatarUrl)
            .MaximumLength(500)
            .Must(value => value is null ||
                (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("AvatarUrl must be an absolute HTTP or HTTPS URL.")
            .When(request => request.UpdateAvatarUrl);
        this.RuleFor(request => request.Bio).MaximumLength(2000).When(request => request.UpdateBio);
        this.RuleFor(request => request)
            .Must(request => request.UpdateDisplayName || request.UpdateAvatarUrl || request.UpdateBio)
            .WithMessage("At least one profile field is required.");
    }
}
