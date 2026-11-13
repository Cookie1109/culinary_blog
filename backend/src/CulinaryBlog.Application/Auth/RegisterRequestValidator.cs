// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the register request validator.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RegisterRequestValidator"/> class.</summary>
    public RegisterRequestValidator()
    {
        this.RuleFor(request => request.DisplayName).NotEmpty().Length(2, 100);
        this.RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(256);
        this.RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain a non-alphanumeric character.");
    }
}
