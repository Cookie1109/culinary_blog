using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Configuration;

public sealed class SitemapOptions
{
    public const string SectionName = "Sitemap";

    [Required]
    [Url]
    public string PublicBaseUrl { get; init; } = "http://localhost:8080";

    [Required]
    public string ObjectKey { get; init; } = "system/sitemap.xml";

    [Url]
    public string? RevalidationEndpoint { get; init; }

    public string? RevalidationSecret { get; init; }

    [Range(1, 300)]
    public int DebounceSeconds { get; init; } = 5;
}
