// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Media;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Configuration;

/// <summary>Represents the sitemap location.</summary>
/// <param name="options">The options supplied to this operation.</param>
internal sealed class SitemapLocation(IOptions<SitemapOptions> options) : ISitemapLocation
{
    /// <summary>Gets the object key.</summary>
    public string ObjectKey => options.Value.ObjectKey;
}
