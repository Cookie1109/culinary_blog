using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRecipeSearchRepository
{
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
        CancellationToken cancellationToken);

    Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
