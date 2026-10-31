// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries nutrition dto data.</summary>
/// <param name="Calories">The calories supplied to this operation.</param>
/// <param name="Protein">The protein supplied to this operation.</param>
/// <param name="Carbohydrates">The carbohydrates supplied to this operation.</param>
/// <param name="Fat">The fat supplied to this operation.</param>
/// <param name="Fiber">The fiber supplied to this operation.</param>
/// <param name="Sodium">The sodium supplied to this operation.</param>
public sealed record NutritionDto(
    decimal? Calories = null,
    decimal? Protein = null,
    decimal? Carbohydrates = null,
    decimal? Fat = null,
    decimal? Fiber = null,
    decimal? Sodium = null);
