using CulinaryBlog.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

internal sealed class WelcomeEmailOutboxConfiguration : IEntityTypeConfiguration<WelcomeEmailOutbox>
{
    public void Configure(EntityTypeBuilder<WelcomeEmailOutbox> builder)
    {
        builder.ToTable("WelcomeEmailOutbox");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(message => message.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(1000);
        builder.HasIndex(message => new { message.SentAt, message.NextAttemptAt });
    }
}
