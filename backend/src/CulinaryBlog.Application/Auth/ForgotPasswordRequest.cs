// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries forgot password request data.</summary>
/// <param name="Email">The email supplied to this operation.</param>
public sealed record ForgotPasswordRequest(string Email);
