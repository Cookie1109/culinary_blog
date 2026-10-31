// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries register request data.</summary>
/// <param name="DisplayName">The display name supplied to this operation.</param>
/// <param name="Email">The email supplied to this operation.</param>
/// <param name="Password">The password supplied for this authentication operation.</param>
public sealed record RegisterRequest(string DisplayName, string Email, string Password);
