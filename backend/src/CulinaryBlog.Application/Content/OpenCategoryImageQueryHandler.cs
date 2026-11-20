// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Content;

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
