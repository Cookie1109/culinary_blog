using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

internal class Repository<T>(AppDbContext dbContext) : IRepository<T> where T : BaseEntity
{
    protected AppDbContext DbContext { get; } = dbContext;

    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        DbContext.Set<T>().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public void Add(T entity) => DbContext.Set<T>().Add(entity);
}

internal sealed class RecipeRepository(AppDbContext dbContext) : Repository<Recipe>(dbContext), IRecipeRepository
{
    public Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken) =>
        DbContext.Recipes.IgnoreQueryFilters().AnyAsync(
            recipe => recipe.Slug == slug && (!excludedId.HasValue || recipe.Id != excludedId.Value),
            cancellationToken);
}

internal sealed class UnitOfWork(AppDbContext dbContext, IRecipeRepository recipes, TimeProvider timeProvider) : IUnitOfWork
{
    public IRecipeRepository Recipes => recipes;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var deletedIds = dbContext.ChangeTracker.Entries<Recipe>()
            .Where(entry => entry.State == EntityState.Modified && entry.Entity.IsDeleted &&
                !entry.Property(recipe => recipe.IsDeleted).OriginalValue)
            .Select(entry => entry.Entity.Id).ToArray();
        if (deletedIds.Length > 0)
        {
            var images = await dbContext.RecipeImages.Where(image => deletedIds.Contains(image.RecipeId))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var image in images) image.Delete();
            var keys = images.SelectMany(image => new[] { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey })
                .OfType<string>().Distinct().ToArray();
            if (keys.Length > 0)
            {
                // Persist cleanup with the soft delete, so a rejected write cannot delete live objects.
                dbContext.MediaOutbox.Add(new MediaOutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = MediaOutboxTypes.DeleteObjects,
                    Payload = JsonSerializer.Serialize(new DeleteObjectsPayload(keys)),
                    CreatedAt = timeProvider.GetUtcNow(),
                    NextAttemptAt = timeProvider.GetUtcNow(),
                });
            }
        }

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
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict, currentVersion);
        }
    }
}
