// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries recipe image dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="OriginalUrl">The original url supplied to this operation.</param>
/// <param name="MediumUrl">The medium url supplied to this operation.</param>
/// <param name="ThumbnailUrl">The thumbnail url supplied to this operation.</param>
/// <param name="AltText">The alt text supplied to this operation.</param>
/// <param name="IsPrimary">The is primary supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
/// <param name="ProcessingStatus">The processing status supplied to this operation.</param>
public sealed record RecipeImageDto(
    Guid Id,
    string OriginalUrl,
    string? MediumUrl,
    string? ThumbnailUrl,
    string? AltText,
    bool IsPrimary,
    int OrderIndex,
    string ProcessingStatus);
