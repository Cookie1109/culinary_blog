// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Configuration;

/// <summary>Represents the sitemap options.</summary>
public sealed class SitemapOptions
{
    /// <summary>The section name.</summary>
    public const string SectionName = "Sitemap";

    /// <summary>Gets or initializes the public base url.</summary>
    [Required]
    [Url]
    public string PublicBaseUrl { get; init; } = "http://localhost:8080";

    /// <summary>Gets or initializes the object key.</summary>
    [Required]
    public string ObjectKey { get; init; } = "system/sitemap.xml";

    /// <summary>Gets or initializes the revalidation endpoint.</summary>
    [Url]
    public string? RevalidationEndpoint { get; init; }

    /// <summary>Gets or initializes the revalidation secret.</summary>
    public string? RevalidationSecret { get; init; }

    /// <summary>Gets or initializes the debounce seconds.</summary>
    [Range(1, 300)]
    public int DebounceSeconds { get; init; } = 5;
}
