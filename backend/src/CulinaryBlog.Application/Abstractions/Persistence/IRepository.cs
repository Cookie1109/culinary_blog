// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Abstractions.Persistence;

/// <summary>Defines the repository contract.</summary>
/// <typeparam name="T">The  type.</typeparam>
public interface IRepository<T>
    where T : BaseEntity
{
    /// <summary>Gets by id.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds the i repository.</summary>
    /// <param name="entity">The entity supplied to this operation.</param>
    void Add(T entity);
}

/// <summary>Defines the recipe repository contract.</summary>
public interface IRecipeRepository : IRepository<Recipe>
{
    /// <summary>Performs the slug exists operation.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="excludedId">The excluded id supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<bool> SlugExistsAsync(string slug, Guid? excludedId, CancellationToken cancellationToken);
}

/// <summary>Defines the unit of work contract.</summary>
public interface IUnitOfWork
{
    /// <summary>Gets the recipes.</summary>
    IRecipeRepository Recipes { get; }

    /// <summary>Saves changes.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
