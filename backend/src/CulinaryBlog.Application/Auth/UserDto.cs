// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries user dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="DisplayName">The display name supplied to this operation.</param>
/// <param name="AvatarUrl">The avatar url supplied to this operation.</param>
/// <param name="Bio">The bio supplied to this operation.</param>
/// <param name="Roles">The roles supplied to this operation.</param>
/// <param name="EmailConfirmed">The email confirmed supplied to this operation.</param>
/// <param name="IsActive">The is active supplied to this operation.</param>
/// <param name="CreatedAt">The UTC creation time.</param>
public sealed record UserDto(
    string Id,
    string Email,
    string DisplayName,
    string? AvatarUrl,
    string? Bio,
    IReadOnlyCollection<string> Roles,
    bool EmailConfirmed,
    bool IsActive,
    DateTimeOffset CreatedAt);
