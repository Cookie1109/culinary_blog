// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Application.Auth;

/// <summary>Defines the password reset email sender contract.</summary>
public interface IPasswordResetEmailSender
{
    /// <summary>Gets a value indicating whether is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Performs the send operation.</summary>
    /// <param name="email">The email supplied to this operation.</param>
    /// <param name="token">The token supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task SendAsync(string email, string token, CancellationToken cancellationToken);
}
