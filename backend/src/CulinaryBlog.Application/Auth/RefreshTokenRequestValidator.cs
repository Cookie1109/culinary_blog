// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the refresh token request validator.</summary>
public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RefreshTokenRequestValidator"/> class.</summary>
    public RefreshTokenRequestValidator()
    {
        this.RuleFor(request => request.RefreshToken).NotEmpty().MaximumLength(256);
    }
}
