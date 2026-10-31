// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the google login request validator.</summary>
public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    /// <summary>Initializes a new instance of the <see cref="GoogleLoginRequestValidator"/> class.</summary>
    public GoogleLoginRequestValidator()
    {
        this.RuleFor(request => request.IdToken).NotEmpty().MaximumLength(8192);
    }
}
