// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the new password validator.</summary>
internal sealed class NewPasswordValidator : AbstractValidator<string>
{
    /// <summary>Initializes a new instance of the <see cref="NewPasswordValidator"/> class.</summary>
    public NewPasswordValidator()
    {
        this.RuleFor(password => password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[a-z]").Matches("[A-Z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
    }
}
