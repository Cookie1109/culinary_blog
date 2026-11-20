// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries upload category image command data.</summary>
/// <param name="IsAdmin">Whether the caller claims the Admin role.</param>
/// <param name="Content">The uploaded image stream.</param>
/// <param name="Length">The declared image length in bytes.</param>
/// <param name="ContentType">The declared image MIME type.</param>
public sealed record UploadCategoryImageCommand(bool IsAdmin, Stream Content, long Length, string ContentType)
    : IRequest<CategoryImageDto>;
