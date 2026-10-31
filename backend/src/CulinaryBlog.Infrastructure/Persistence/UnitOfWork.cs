// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the unit of work.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="recipes">The recipes supplied to this operation.</param>
internal sealed class UnitOfWork(AppDbContext dbContext, IRecipeRepository recipes) : IUnitOfWork
{
    /// <summary>Gets the recipes.</summary>
    public IRecipeRepository Recipes => recipes;

    /// <summary>Saves changes.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            long? currentVersion = null;
            var recipeEntry = exception.Entries.FirstOrDefault(entry => entry.Entity is Recipe);
            if (recipeEntry?.Entity is Recipe recipe)
            {
                try
                {
                    currentVersion = await dbContext.Recipes.AsNoTracking()
                        .Where(item => item.Id == recipe.Id)
                        .Select(item => (long?)item.Version)
                        .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    // Fall back to null if the connection or transaction cannot execute further queries
                }
            }

            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT",
                "The recipe was changed by another request.",
                ContentProblemKind.Conflict,
                currentVersion);
        }
    }
}
