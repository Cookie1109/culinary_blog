// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Users;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the admin user directory.</summary>
/// <param name="dbContext">The db context supplied to this operation.</param>
/// <param name="accessor">The accessor supplied to this operation.</param>
internal sealed class AdminUserDirectory(AppDbContext dbContext, IHttpContextAccessor accessor) : IAdminUserDirectory
{
    /// <summary>Performs the is current user admin operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken)
    {
        var principal = accessor.HttpContext?.User;
        string? userId = principal?.FindFirstValue("sub") ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(false);
        }

        return dbContext.Users.AnyAsync(
            user => user.Id == userId && user.IsActive &&
                dbContext.UserRoles.Any(membership => membership.UserId == user.Id &&
                    dbContext.Roles.Any(role => role.Id == membership.RoleId && role.Name == "Admin")),
            cancellationToken);
    }

    /// <summary>Lists the admin user directory.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="role">The role supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<PageEnvelope<AdminUserDto>> ListAsync(
        string? search, string? role, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string pattern = $"%{search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
            query = query.Where(user => EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
                (user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")));
        }

        if (role is not null)
        {
            query = query.Where(user => dbContext.UserRoles.Any(membership => membership.UserId == user.Id &&
                dbContext.Roles.Any(userRole => userRole.Id == membership.RoleId && userRole.Name == role)));
        }

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var users = await query.OrderByDescending(user => user.CreatedAt).ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new { user.Id, user.DisplayName, user.Email, user.IsActive, user.CreatedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        string[] ids = [.. users.Select(user => user.Id)];
        var memberships = await (from membership in dbContext.UserRoles.AsNoTracking()
                                 join userRole in dbContext.Roles.AsNoTracking() on membership.RoleId equals userRole.Id
                                 where ids.Contains(membership.UserId)
                                 orderby userRole.Name
                                 select new { membership.UserId, userRole.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var roles = memberships.ToLookup(membership => membership.UserId, membership => membership.Name!);
        var data = users.Select(user => new AdminUserDto(
            user.Id.ToString(),
            user.DisplayName,
            user.Email,
            [.. roles[user.Id]],
            user.IsActive,
            user.CreatedAt)).ToArray();
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
        return new PageEnvelope<AdminUserDto>(
            data,
            new PageMeta(page, pageSize, total, totalPages, page < totalPages, page > 1));
    }
}
