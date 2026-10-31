// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the repository.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <typeparam name="T">The  type.</typeparam>
internal class Repository<T>(AppDbContext dbContext) : IRepository<T>
    where T : BaseEntity
{
    /// <summary>Gets the db context.</summary>
    protected AppDbContext DbContext { get; } = dbContext;

    /// <summary>Gets by id.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        this.DbContext.Set<T>().SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    /// <summary>Adds the repository.</summary>
    /// <param name="entity">The entity supplied to this operation.</param>
    public void Add(T entity) => this.DbContext.Set<T>().Add(entity);
}
