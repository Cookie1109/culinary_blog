// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries category dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Slug">The slug supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="ImageUrl">The image url supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
/// <param name="RecipeCount">The recipe count supplied to this operation.</param>
public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    int OrderIndex,
    int RecipeCount);
