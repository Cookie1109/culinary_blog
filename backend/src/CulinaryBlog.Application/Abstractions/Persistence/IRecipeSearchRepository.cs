using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRecipeSearchRepository
{
    Task<PageEnvelope<RecipeDto>> SearchPublishedRecipesAsync(
        string? search,
        string? category,
        RecipeDifficulty? difficulty,
        int? maxTime,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
