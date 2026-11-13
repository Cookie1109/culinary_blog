// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the change password request validator.</summary>
public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ChangePasswordRequestValidator"/> class.</summary>
    public ChangePasswordRequestValidator()
    {
        this.RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(128);
        this.RuleFor(request => request.NewPassword).NotEmpty().SetValidator(new NewPasswordValidator());
    }
}
