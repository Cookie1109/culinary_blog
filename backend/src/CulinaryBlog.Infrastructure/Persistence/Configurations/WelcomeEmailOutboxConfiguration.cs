// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the welcome email outbox configuration.</summary>
internal sealed class WelcomeEmailOutboxConfiguration : IEntityTypeConfiguration<WelcomeEmailOutbox>
{
    /// <summary>Configures the welcome email outbox configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<WelcomeEmailOutbox> builder)
    {
        builder.ToTable("WelcomeEmailOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.CorrelationId).HasMaxLength(128);
        builder.Property(message => message.TraceParent).HasMaxLength(55);
        builder.Property(message => message.UserId).HasMaxLength(450).IsRequired();
        builder.Property(message => message.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(message => message.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(1000);
        builder.Property(message => message.JobId).HasMaxLength(100);
        builder.HasIndex(message => new { message.SentAt, message.NextAttemptAt });
    }
}
