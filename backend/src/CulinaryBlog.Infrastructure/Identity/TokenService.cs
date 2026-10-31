// Culinary Blog. Authorship is recorded in the repository history.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the token service.</summary>
/// <param name="options">The options supplied to this operation.</param>
/// <param name="timeProvider">The time provider supplied to this operation.</param>
public sealed class TokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JwtOptions options = options.Value;

    /// <summary>Creates refresh token.</summary>
    /// <returns>The create refresh token result.</returns>
    public static string CreateRefreshToken()
    {
        return RefreshTokenSecurity.Create();
    }

    /// <summary>Performs the hash refresh token operation.</summary>
    /// <param name="token">The token supplied to this operation.</param>
    /// <returns>The hash refresh token result.</returns>
    public static string HashRefreshToken(string token)
    {
        return RefreshTokenSecurity.Hash(token);
    }

    /// <summary>Creates access token.</summary>
    /// <param name="user">The user supplied to this operation.</param>
    /// <param name="roles">The roles supplied to this operation.</param>
    /// <returns>The create access token result.</returns>
    public AccessTokenResult CreateAccessToken(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(this.options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.DisplayName),
            new("security_stamp", user.SecurityStamp ?? string.Empty),
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: this.options.Issuer,
            audience: this.options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
