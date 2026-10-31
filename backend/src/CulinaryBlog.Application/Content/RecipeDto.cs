// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries recipe dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="Title">The title supplied to this operation.</param>
/// <param name="Slug">The slug supplied to this operation.</param>
/// <param name="Description">The description supplied to this operation.</param>
/// <param name="PrepTime">The preparation time in minutes.</param>
/// <param name="CookTime">The cooking time in minutes.</param>
/// <param name="Servings">The servings supplied to this operation.</param>
/// <param name="Difficulty">The difficulty supplied to this operation.</param>
/// <param name="Status">The status supplied to this operation.</param>
/// <param name="PrimaryImageUrl">The primary image url supplied to this operation.</param>
/// <param name="Category">The category supplied to this operation.</param>
/// <param name="Author">The author supplied to this operation.</param>
/// <param name="CreatedAt">The UTC creation time.</param>
/// <param name="PublishedAt">The optional UTC publication time.</param>
/// <param name="Version">The recipe version used to reject concurrent changes.</param>
/// <param name="Instructions">The instructions supplied to this operation.</param>
/// <param name="Nutrition">The nutrition supplied to this operation.</param>
/// <param name="Ingredients">The ingredients supplied to this operation.</param>
/// <param name="Steps">The steps supplied to this operation.</param>
/// <param name="Images">The images supplied to this operation.</param>
/// <param name="RelevanceScore">The relevance score supplied to this operation.</param>
public sealed record RecipeDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    int PrepTime,
    int CookTime,
    int Servings,
    string Difficulty,
    string Status,
    string? PrimaryImageUrl,
    CategoryDto Category,
    RecipeAuthorDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    long Version,
    string Instructions,
    NutritionDto? Nutrition,
    IReadOnlyCollection<IngredientDto> Ingredients,
    IReadOnlyCollection<StepDto> Steps,
    IReadOnlyCollection<RecipeImageDto> Images,
    float? RelevanceScore = null);
