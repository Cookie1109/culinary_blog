// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries google login request data.</summary>
/// <param name="IdToken">The id token supplied to this operation.</param>
public sealed record GoogleLoginRequest(string IdToken);
