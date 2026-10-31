// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the recipe repository.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
internal sealed class RecipeRepository(AppDbContext dbContext) : Repository<Recipe>(dbContext), IRecipeRepository
{
    /// <summary>Performs the slug exists operation.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="excludedId">The excluded id supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken) =>
        this.DbContext.Recipes.IgnoreQueryFilters().AnyAsync(
            recipe => recipe.Slug == slug && (!excludedId.HasValue || recipe.Id != excludedId.Value),
            cancellationToken);
}
