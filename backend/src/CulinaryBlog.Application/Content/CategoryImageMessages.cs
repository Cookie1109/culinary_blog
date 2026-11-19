// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries the stored category image location.</summary>
/// <param name="Url">The root-relative public URL of the stored image.</param>
public sealed record CategoryImageDto(string Url);

/// <summary>Carries upload category image command data.</summary>
/// <param name="IsAdmin">Whether the caller claims the Admin role.</param>
/// <param name="Content">The uploaded image stream.</param>
/// <param name="Length">The declared image length in bytes.</param>
/// <param name="ContentType">The declared image MIME type.</param>
public sealed record UploadCategoryImageCommand(bool IsAdmin, Stream Content, long Length, string ContentType)
    : IRequest<CategoryImageDto>;

/// <summary>Carries open category image query data.</summary>
/// <param name="FileName">The stored file name including its extension.</param>
public sealed record OpenCategoryImageQuery(string FileName) : IRequest<MediaFileDto>;

/// <summary>Handles category image uploads.</summary>
/// <param name="service">The content service.</param>
internal sealed class UploadCategoryImageCommandHandler(IContentService service)
    : IRequestHandler<UploadCategoryImageCommand, CategoryImageDto>
{
    /// <summary>Stores the image.</summary>
    /// <param name="request">The upload command.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The stored image location.</returns>
    public Task<CategoryImageDto> Handle(UploadCategoryImageCommand request, CancellationToken cancellationToken) =>
        service.UploadCategoryImageAsync(request.Content, request.Length, request.ContentType, cancellationToken);
}

/// <summary>Handles category image reads.</summary>
/// <param name="service">The content service.</param>
internal sealed class OpenCategoryImageQueryHandler(IContentService service)
    : IRequestHandler<OpenCategoryImageQuery, MediaFileDto>
{
    /// <summary>Opens the image.</summary>
    /// <param name="request">The query.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The image stream and content type.</returns>
    public Task<MediaFileDto> Handle(OpenCategoryImageQuery request, CancellationToken cancellationToken) =>
        service.OpenCategoryImageAsync(request.FileName, cancellationToken);
}
