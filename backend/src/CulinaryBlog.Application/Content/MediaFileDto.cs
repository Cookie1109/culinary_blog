// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries media file dto data.</summary>
/// <param name="Content">The content supplied to this operation.</param>
/// <param name="ContentType">The content type supplied to this operation.</param>
public sealed record MediaFileDto(Stream Content, string ContentType);
