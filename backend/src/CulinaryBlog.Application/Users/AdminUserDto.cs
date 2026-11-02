namespace CulinaryBlog.Application.Users;

public sealed record AdminUserDto(
    string Id,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    DateTimeOffset CreatedAt);
