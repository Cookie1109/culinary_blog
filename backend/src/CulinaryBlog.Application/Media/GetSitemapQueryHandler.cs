// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Media;

/// <summary>Represents the get sitemap query handler.</summary>
/// <param name="storage">The storage supplied to this operation.</param>
/// <param name="location">The location supplied to this operation.</param>
internal sealed class GetSitemapQueryHandler(IFileStorageService storage, ISitemapLocation location) : IRequestHandler<GetSitemapQuery, Stream?>
{
    /// <summary>Handles the get sitemap query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<Stream?> Handle(GetSitemapQuery request, CancellationToken cancellationToken) =>
        await storage.ExistsAsync(location.ObjectKey, cancellationToken).ConfigureAwait(false)
            ? await storage.OpenReadAsync(location.ObjectKey, cancellationToken).ConfigureAwait(false)
            : null;
}
