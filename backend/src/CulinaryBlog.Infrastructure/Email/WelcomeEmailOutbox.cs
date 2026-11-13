// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Infrastructure.Email;

/// <summary>Represents the welcome email outbox.</summary>
public sealed class WelcomeEmailOutbox
{
    /// <summary>Gets or sets the originating request correlation identifier.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the originating W3C trace parent.</summary>
    public string? TraceParent { get; set; }

    /// <summary>Gets or sets the resource identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user id.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the recipient.</summary>
    public required string Recipient { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public required string DisplayName { get; set; }

    /// <summary>Gets or sets the attempts.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets the next attempt at.</summary>
    public DateTimeOffset NextAttemptAt { get; set; }

    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the sent at.</summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>Gets or sets the last error.</summary>
    public string? LastError { get; set; }

    /// <summary>Gets or sets the job id.</summary>
    public string? JobId { get; set; }
}
