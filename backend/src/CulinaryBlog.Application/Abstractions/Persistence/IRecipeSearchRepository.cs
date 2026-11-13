// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

/// <summary>Defines the recipe search repository contract.</summary>
public interface IRecipeSearchRepository
{
    /// <summary>Lists published recipes.</summary>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="maxCookTime">The max cook time supplied to this operation.</param>
    /// <param name="minServings">The min servings supplied to this operation.</param>
    /// <param name="minPrepTime">The min prep time supplied to this operation.</param>
    /// <param name="maxPrepTime">The max prep time supplied to this operation.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="category">The category supplied to this operation.</param>
    /// <param name="maxTime">The max time supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(
        Guid? categoryId,
        RecipeDifficulty? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        int page,
        int pageSize,
        string? category,
        int? maxTime,
        CancellationToken cancellationToken);

    /// <summary>Searches published recipes.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="category">The category supplied to this operation.</param>
    /// <param name="difficulty">The difficulty supplied to this operation.</param>
    /// <param name="maxTime">The max time supplied to this operation.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="categoryId">The category id supplied to this operation.</param>
    /// <param name="maxCookTime">The max cook time supplied to this operation.</param>
    /// <param name="minServings">The min servings supplied to this operation.</param>
    /// <param name="minPrepTime">The min prep time supplied to this operation.</param>
    /// <param name="maxPrepTime">The max prep time supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize,
        Guid? categoryId,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        CancellationToken cancellationToken);
}
