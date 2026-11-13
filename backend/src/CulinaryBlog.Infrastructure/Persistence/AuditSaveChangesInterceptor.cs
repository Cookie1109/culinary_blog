// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the audit save changes interceptor.</summary>
/// <param name="httpContextAccessor">The http context accessor supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
internal sealed class AuditSaveChangesInterceptor(
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <summary>Performs the saving changes operation.</summary>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <returns>The saving changes result.</returns>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        this.ApplyAudit(eventData.Context);
        return result;
    }

    /// <summary>Performs the saving changes operation.</summary>
    /// <param name="eventData">The event data supplied to this operation.</param>
    /// <param name="result">The result supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        this.ApplyAudit(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void ApplyAudit(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
            ?? "system";

        foreach (var entry in dbContext.ChangeTracker.Entries<MediaOutboxMessage>().Where(entry => entry.State == EntityState.Added))
        {
            MediaJobLogContext.Capture(entry.Entity, httpContextAccessor.HttpContext, actor);
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
                entry.Property(entity => entity.CreatedBy).CurrentValue = actor;
                entry.Property(entity => entity.Version).CurrentValue = 1;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedAt).CurrentValue = now;
                entry.Property(entity => entity.UpdatedBy).CurrentValue = actor;
                if (!entry.Property(entity => entity.Version).IsModified)
                {
                    entry.Property(entity => entity.Version).CurrentValue++;
                }

                if (entry.Property(entity => entity.IsDeleted).CurrentValue &&
                    entry.Property(entity => entity.DeletedAt).CurrentValue is null)
                {
                    entry.Property(entity => entity.DeletedAt).CurrentValue = now;
                    entry.Property(entity => entity.DeletedBy).CurrentValue = actor;
                }
            }
        }
    }
}
