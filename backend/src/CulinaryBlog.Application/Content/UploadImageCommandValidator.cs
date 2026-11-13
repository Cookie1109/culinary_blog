// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the upload image command validator.</summary>
internal sealed class UploadImageCommandValidator : AbstractValidator<UploadImageCommand>
{
    private static readonly string[] ContentTypes = ["image/jpeg", "image/png", "image/webp", "image/avif"];

    /// <summary>Initializes a new instance of the <see cref="UploadImageCommandValidator"/> class.</summary>
    public UploadImageCommandValidator()
    {
        this.RuleFor(request => request).Custom((request, _) =>
        {
            if (request.Length is <= 0 or > 5 * 1024 * 1024)
            {
                throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size must be between 1 byte and 5 MB.", ContentProblemKind.BadRequest);
            }

            if (!ContentTypes.Contains(request.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                throw new ContentProblemException("FILE_MIME_INVALID", "Only JPEG, PNG, WebP and AVIF images are accepted.", ContentProblemKind.BadRequest);
            }
        });
        this.RuleFor(request => request.Content).NotNull();
        this.RuleFor(request => request.AltText).MaximumLength(200);
    }
}
