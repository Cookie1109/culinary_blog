// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries ingredient write request data.</summary>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Quantity">The quantity supplied to this operation.</param>
/// <param name="Unit">The unit supplied to this operation.</param>
/// <param name="Notes">The notes supplied to this operation.</param>
/// <param name="OrderIndex">The order index supplied to this operation.</param>
public sealed record IngredientWriteRequest(
    [property: System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 1)] string Name,
    decimal? Quantity = null,
    string? Unit = null,
    string? Notes = null,
    int OrderIndex = 0);
