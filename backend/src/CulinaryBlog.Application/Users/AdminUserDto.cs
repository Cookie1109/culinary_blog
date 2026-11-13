// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Users;

/// <summary>Carries admin user dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="DisplayName">The display name supplied to this operation.</param>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="Roles">The roles supplied to this operation.</param>
/// <param name="IsActive">The is active supplied to this operation.</param>
/// <param name="CreatedAt">The UTC creation time.</param>
public sealed record AdminUserDto(
    string Id,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> Roles,
    bool IsActive,
    DateTimeOffset CreatedAt);
