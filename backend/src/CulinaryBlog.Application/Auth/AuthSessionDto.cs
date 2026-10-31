// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries auth session dto data.</summary>
/// <param name="AccessToken">The access token supplied to this operation.</param>
/// <param name="RefreshToken">The opaque refresh token supplied by the caller.</param>
/// <param name="ExpiresAt">The UTC expiration time.</param>
/// <param name="User">The user supplied to this operation.</param>
public sealed record AuthSessionDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User);
