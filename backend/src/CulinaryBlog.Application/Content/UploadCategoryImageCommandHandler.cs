// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Content;

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
