using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(T entity);
}

public interface IRecipeRepository : IRepository<Recipe>
{
    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    IRecipeRepository Recipes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
