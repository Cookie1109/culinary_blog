using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
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

internal sealed class UnitOfWork(AppDbContext dbContext, IRecipeRepository recipes) : IUnitOfWork
{
    public IRecipeRepository Recipes => recipes;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
            when (exception.Entries.Any(entry => entry.Entity is Recipe))
        {
            var recipe = (Recipe)exception.Entries.First(entry => entry.Entity is Recipe).Entity;
            var currentVersion = await dbContext.Recipes.AsNoTracking()
                .Where(item => item.Id == recipe.Id)
                .Select(item => (long?)item.Version)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict, currentVersion);
        }
    }
}
