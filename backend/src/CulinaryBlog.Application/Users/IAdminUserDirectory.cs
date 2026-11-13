// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Users;

/// <summary>Defines the admin user directory contract.</summary>
public interface IAdminUserDirectory
{
    /// <summary>Performs the is current user admin operation.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken);

    /// <summary>Lists the i admin user directory.</summary>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="role">The role supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<AdminUserDto>> ListAsync(
        string? search,
        string? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
