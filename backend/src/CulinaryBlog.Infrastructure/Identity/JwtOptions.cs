// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the jwt options.</summary>
public sealed class JwtOptions
{
    /// <summary>The section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets or initializes the issuer.</summary>
    [Required]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>Gets or initializes the audience.</summary>
    [Required]
    public string Audience { get; init; } = string.Empty;

    /// <summary>Gets or initializes the signing key.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    /// <summary>Gets or initializes the access token minutes.</summary>
    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    /// <summary>Gets or initializes the refresh token days.</summary>
    [Range(1, 30)]
    public int RefreshTokenDays { get; init; } = 7;
}
