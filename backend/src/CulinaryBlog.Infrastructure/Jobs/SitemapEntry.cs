// Culinary Blog. Authorship is recorded in the repository history.

using System.Text;
using System.Xml;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Carries sitemap entry data.</summary>
/// <param name="PathAndQuery">The path and query supplied to this operation.</param>
/// <param name="LastModified">The last modified supplied to this operation.</param>
internal sealed record SitemapEntry(string PathAndQuery, DateTimeOffset? LastModified = null);
