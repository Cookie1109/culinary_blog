// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Carries change password request data.</summary>
/// <param name="CurrentPassword">The existing password used to authorize the change.</param>
/// <param name="NewPassword">The replacement password that must satisfy the password policy.</param>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
