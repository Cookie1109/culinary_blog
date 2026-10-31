// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Carries seed recipe data.</summary>
/// <param name="Slug">The slug supplied to this operation.</param>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="CategorySlug">The category slug supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="PrepTime">The preparation time in minutes.</param>
/// <param name="CookTime">The cooking time in minutes.</param>
/// <param name="Servings">The servings supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="Nutrition">The nutrition supplied to this operation.</param>
/// <param name="Ingredients">The ingredients supplied to this operation.</param>
/// <param name="Steps">The steps supplied to this operation.</param>
internal sealed record SeedRecipe(
    string Slug,
    string Title,
    string CategorySlug,
    string Description,
    int PrepTime,
    int CookTime,
    int Servings,
    RecipeDifficulty Difficulty,
    SeedNutrition Nutrition,
    IReadOnlyList<SeedIngredient> Ingredients,
    IReadOnlyList<SeedStep> Steps);
