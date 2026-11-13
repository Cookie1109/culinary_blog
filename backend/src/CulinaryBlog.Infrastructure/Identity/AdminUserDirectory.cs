using System.Security.Claims;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Users;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Identity;

internal sealed class AdminUserDirectory(AppDbContext dbContext, IHttpContextAccessor accessor) : IAdminUserDirectory
{
    public Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken)
    {
        var principal = accessor.HttpContext?.User;
        var userId = principal?.FindFirstValue("sub") ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (principal?.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(false);
        }

        return dbContext.Users.AnyAsync(user => user.Id == userId && user.IsActive &&
            dbContext.UserRoles.Any(membership => membership.UserId == user.Id &&
                dbContext.Roles.Any(role => role.Id == membership.RoleId && role.Name == "Admin")), cancellationToken);
    }

    public async Task<PageEnvelope<AdminUserDto>> ListAsync(
        string? search, string? role, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
            query = query.Where(user => EF.Functions.ILike(user.DisplayName, pattern, "\\") ||
                (user.Email != null && EF.Functions.ILike(user.Email, pattern, "\\")));
        }

        if (role is not null)
        {
            query = query.Where(user => dbContext.UserRoles.Any(membership => membership.UserId == user.Id &&
                dbContext.Roles.Any(userRole => userRole.Id == membership.RoleId && userRole.Name == role)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var users = await query.OrderByDescending(user => user.CreatedAt).ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(user => new { user.Id, user.DisplayName, user.Email, user.IsActive, user.CreatedAt })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ids = users.Select(user => user.Id).ToArray();
        var memberships = await (from membership in dbContext.UserRoles.AsNoTracking()
                                 join userRole in dbContext.Roles.AsNoTracking() on membership.RoleId equals userRole.Id
                                 where ids.Contains(membership.UserId)
                                 orderby userRole.Name
                                 select new { membership.UserId, userRole.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var roles = memberships.ToLookup(membership => membership.UserId, membership => membership.Name!);
        var data = users.Select(user => new AdminUserDto(user.Id.ToString(), user.DisplayName, user.Email,
            roles[user.Id].ToArray(), user.IsActive, user.CreatedAt)).ToArray();
        var totalPages = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
        return new PageEnvelope<AdminUserDto>(data,
            new PageMeta(page, pageSize, total, totalPages, page < totalPages, page > 1));
    }
}
