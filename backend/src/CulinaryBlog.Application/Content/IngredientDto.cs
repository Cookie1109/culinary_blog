// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries ingredient dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Quantity">The quantity supplied to this operation.</param>
/// <param name="Unit">The unit supplied to this operation.</param>
/// <param name="Notes">The notes supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
public sealed record IngredientDto(
    Guid Id,
    string Name,
    decimal? Quantity,
    string? Unit,
    string? Notes,
    int OrderIndex);
