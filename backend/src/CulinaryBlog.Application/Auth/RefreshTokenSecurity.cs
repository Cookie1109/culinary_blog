// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Cryptography;
using System.Text;

namespace CulinaryBlog.Application.Auth;

/// <summary>Represents the refresh token security.</summary>
public static class RefreshTokenSecurity
{
    /// <summary>Creates the refresh token security.</summary>
    /// <returns>The create result.</returns>
    public static string Create()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>Performs the hash operation.</summary>
    /// <param name="token">The token supplied to this operation.</param>
    /// <returns>The hash result.</returns>
    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
