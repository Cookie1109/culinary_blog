using System.Security.Cryptography;
using System.Text;

namespace CulinaryBlog.Application.Auth;

public static class RefreshTokenSecurity
{
    public static string Create()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
