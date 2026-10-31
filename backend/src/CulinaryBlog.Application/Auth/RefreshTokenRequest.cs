// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries refresh token request data.</summary>
/// <param name="RefreshToken">The opaque refresh token supplied by the caller.</param>
public sealed record RefreshTokenRequest(string RefreshToken);
