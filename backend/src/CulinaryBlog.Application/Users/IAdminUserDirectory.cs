using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Users;

public interface IAdminUserDirectory
{
    Task<bool> IsCurrentUserAdminAsync(CancellationToken cancellationToken);

    Task<PageEnvelope<AdminUserDto>> ListAsync(
        string? search,
        string? role,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
