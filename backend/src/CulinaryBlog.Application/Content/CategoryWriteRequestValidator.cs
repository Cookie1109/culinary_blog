// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the category write request validator.</summary>
internal sealed class CategoryWriteRequestValidator : AbstractValidator<CategoryWriteRequest>
{
    /// <summary>Initializes a new instance of the <see cref="CategoryWriteRequestValidator"/> class.</summary>
    public CategoryWriteRequestValidator()
    {
        this.RuleFor(request => request.Name).Cascade(CascadeMode.Stop).NotEmpty().MinimumLength(2).MaximumLength(50)
            .Must(name => !name.Contains('<') || name.IndexOf('>', name.IndexOf('<')) < 0)
            .WithMessage("Category name must not contain HTML.");
        this.RuleFor(request => request.Description).MaximumLength(2000);
        this.RuleFor(request => request.ImageUrl)
            .MaximumLength(500)
            .Must(BeSafeUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.ImageUrl))
            .WithMessage("imageUrl must be an absolute HTTP or HTTPS URL.");
        this.RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0);
    }

    private static bool BeSafeUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
