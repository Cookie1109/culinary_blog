// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the reset password request validator.</summary>
public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ResetPasswordRequestValidator"/> class.</summary>
    public ResetPasswordRequestValidator()
    {
        this.RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        this.RuleFor(request => request.Token).NotEmpty().MaximumLength(4096);
        this.RuleFor(request => request.NewPassword).NotEmpty().SetValidator(new NewPasswordValidator());
    }
}
