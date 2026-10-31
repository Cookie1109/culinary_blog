// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries reset password request data.</summary>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="Token">The token supplied to this operation.</param>
/// <param name="NewPassword">The password supplied for this authentication operation.</param>
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
