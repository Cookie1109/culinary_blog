namespace CulinaryBlog.Infrastructure.Email;

public sealed class WelcomeEmailOutbox
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string Recipient { get; set; }

    public required string DisplayName { get; set; }

    public int Attempts { get; set; }

    public DateTimeOffset NextAttemptAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? LastError { get; set; }
}
