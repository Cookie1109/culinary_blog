// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the media outbox message.</summary>
public sealed class MediaOutboxMessage
{
    /// <summary>Gets or sets the originating request correlation identifier.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the originating W3C trace parent.</summary>
    public string? TraceParent { get; set; }

    /// <summary>Gets or sets the resource identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the type.</summary>
    public required string Type { get; set; }

    /// <summary>Gets or sets the payload.</summary>
    public required string Payload { get; set; }

    /// <summary>Gets or sets the attempts.</summary>
    public int Attempts { get; set; }

    /// <summary>Gets or sets the UTC creation time.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the next attempt at.</summary>
    public DateTimeOffset NextAttemptAt { get; set; }

    /// <summary>Gets or sets the processed at.</summary>
    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Gets or sets the last error.</summary>
    public string? LastError { get; set; }
}
