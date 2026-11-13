// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the refresh token.</summary>
public sealed class RefreshToken
{
    /// <summary>Gets or sets the resource identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the user.</summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the token hash.</summary>
    public required string TokenHash { get; set; }

    /// <summary>Gets or sets the family id.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>Gets or sets the UTC expiration time.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the created by ip.</summary>
    public string? CreatedByIp { get; set; }

    /// <summary>Gets or sets the user agent.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Gets or sets the user agent hash.</summary>
    public string? UserAgentHash { get; set; }

    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets the revoked by ip.</summary>
    public string? RevokedByIp { get; set; }

    /// <summary>Gets or sets the replaced by token hash.</summary>
    public string? ReplacedByTokenHash { get; set; }
}
