// Culinary Blog. Authorship is recorded in the repository history.

using MediatR;

namespace CulinaryBlog.Application.Media;

/// <summary>Defines the sitemap location contract.</summary>
public interface ISitemapLocation
{
    /// <summary>Gets the object key.</summary>
    string ObjectKey { get; }
}
