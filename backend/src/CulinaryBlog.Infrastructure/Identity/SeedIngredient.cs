// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries seed ingredient data.</summary>
/// <param name="Name">The name supplied to this operation.</param>
/// <param name="Quantity">The quantity supplied to this operation.</param>
/// <param name="Unit">The unit supplied to this operation.</param>
/// <param name="Notes">The notes supplied to this operation.</param>
internal sealed record SeedIngredient(string Name, decimal Quantity, string Unit, string? Notes);
