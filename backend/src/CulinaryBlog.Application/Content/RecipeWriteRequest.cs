// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries recipe write request data.</summary>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="CategoryId">The category id supplied to this operation.</param>
/// <param name="PrepTime">The preparation time in minutes.</param>
/// <param name="CookTime">The cooking time in minutes.</param>
/// <param name="Servings">The servings supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="Instructions">The instructions supplied to this operation.</param>
/// <param name="Nutrition">The nutrition supplied to this operation.</param>
public record RecipeWriteRequest(
    string Title,
    string Description,
    Guid CategoryId,
    int PrepTime,
    int CookTime,
    int Servings,
    RecipeDifficulty Difficulty,
    string? Instructions = null,
    NutritionDto? Nutrition = null);
