// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the media outbox message configuration.</summary>
internal sealed class MediaOutboxMessageConfiguration : IEntityTypeConfiguration<MediaOutboxMessage>
{
    /// <summary>Configures the media outbox message configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<MediaOutboxMessage> builder)
    {
        builder.ToTable("MediaOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.CorrelationId).HasMaxLength(128);
        builder.Property(message => message.TraceParent).HasMaxLength(55);
        builder.Property(message => message.Type).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt });
    }
}
