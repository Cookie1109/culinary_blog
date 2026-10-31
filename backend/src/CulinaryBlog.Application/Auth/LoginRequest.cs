// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries login request data.</summary>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="Password">The password supplied for this authentication operation.</param>
public sealed record LoginRequest(string Email, string Password);
