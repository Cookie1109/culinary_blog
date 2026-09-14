using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

internal sealed class MediaOutboxMessageConfiguration : IEntityTypeConfiguration<MediaOutboxMessage>
{
    public void Configure(EntityTypeBuilder<MediaOutboxMessage> builder)
    {
        builder.ToTable("MediaOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Type).HasMaxLength(100).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt });
    }
}
