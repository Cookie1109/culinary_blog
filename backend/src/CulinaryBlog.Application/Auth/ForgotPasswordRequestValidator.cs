// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the forgot password request validator.</summary>
public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ForgotPasswordRequestValidator"/> class.</summary>
    public ForgotPasswordRequestValidator()
    {
        this.RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
