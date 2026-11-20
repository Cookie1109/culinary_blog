// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Content;

/// <summary>Carries the stored category image location.</summary>
/// <param name="Url">The root-relative public URL of the stored image.</param>
public sealed record CategoryImageDto(string Url);
