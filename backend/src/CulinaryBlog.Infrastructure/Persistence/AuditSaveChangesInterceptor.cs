using System.Security.Claims;
using CulinaryBlog.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CulinaryBlog.Infrastructure.Persistence;

internal sealed class AuditSaveChangesInterceptor(
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
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
