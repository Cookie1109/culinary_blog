// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries recipe create request data.</summary>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="CategoryId">The category id supplied to this operation.</param>
/// <param name="PrepTime">The preparation time in minutes.</param>
/// <param name="CookTime">The cooking time in minutes.</param>
/// <param name="Servings">The servings supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="Instructions">The instructions supplied to this operation.</param>
/// <param name="Nutrition">The nutrition supplied to this operation.</param>
/// <param name="Ingredients">The ingredients supplied to this operation.</param>
/// <param name="Steps">The steps supplied to this operation.</param>
public sealed record RecipeCreateRequest(
    string Title,
    string Description,
    Guid CategoryId,
    int PrepTime,
    int CookTime,
    int Servings,
    RecipeDifficulty Difficulty,
    string? Instructions = null,
    NutritionDto? Nutrition = null,
    IReadOnlyList<IngredientWriteRequest>? Ingredients = null,
    IReadOnlyList<StepWriteRequest>? Steps = null)
    : RecipeWriteRequest(Title, Description, CategoryId, PrepTime, CookTime, Servings, Difficulty, Instructions, Nutrition);
