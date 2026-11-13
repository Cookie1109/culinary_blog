// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the image metadata request validator.</summary>
internal sealed class ImageMetadataRequestValidator : AbstractValidator<ImageMetadataRequest>
{
    /// <summary>Initializes a new instance of the <see cref="ImageMetadataRequestValidator"/> class.</summary>
    public ImageMetadataRequestValidator()
    {
        this.RuleFor(request => request.AltText).MaximumLength(200);
        this.RuleFor(request => request.OrderIndex).GreaterThanOrEqualTo(0).When(request => request.OrderIndex.HasValue);
    }
}
