// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the login request validator.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initializes a new instance of the <see cref="LoginRequestValidator"/> class.</summary>
    public LoginRequestValidator()
    {
        this.RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        this.RuleFor(request => request.Password).NotEmpty().MaximumLength(128);
    }
}
