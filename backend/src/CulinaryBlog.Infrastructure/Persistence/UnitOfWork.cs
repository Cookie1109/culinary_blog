// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the unit of work.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="recipes">The recipes supplied to this operation.</param>
/// <param name="timeProvider">Provides the current time for cleanup messages.</param>
internal sealed class UnitOfWork(AppDbContext dbContext, IRecipeRepository recipes, TimeProvider timeProvider) : IUnitOfWork
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
            await this.QueueDeletedRecipeImagesAsync(cancellationToken).ConfigureAwait(false);
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

    private async Task QueueDeletedRecipeImagesAsync(CancellationToken cancellationToken)
    {
        var recipeIds = dbContext.ChangeTracker.Entries<Recipe>()
            .Where(entry => entry.State == EntityState.Modified && entry.Entity.IsDeleted &&
                !entry.Property(recipe => recipe.IsDeleted).OriginalValue)
            .Select(entry => entry.Entity.Id).ToArray();
        if (recipeIds.Length == 0)
        {
            return;
        }

        var images = await dbContext.RecipeImages.IgnoreQueryFilters()
            .Where(image => recipeIds.Contains(image.RecipeId) && !image.IsDeleted)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var image in images)
        {
            image.Delete();
        }

        var keys = images.SelectMany(image => new[] { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey })
            .OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
        if (keys.Length == 0)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        dbContext.MediaOutbox.Add(new MediaOutboxMessage
        {
            TraceParent = Activity.Current?.Id,
            CorrelationId = Activity.Current?.GetBaggageItem("correlation.id"),
            Id = Guid.NewGuid(),
            Type = MediaOutboxTypes.DeleteObjects,
            Payload = JsonSerializer.Serialize(new DeleteObjectsPayload(keys)),
            CreatedAt = now,
            NextAttemptAt = now,
        });
    }
}
