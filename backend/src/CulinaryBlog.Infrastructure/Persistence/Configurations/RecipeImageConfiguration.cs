using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

internal sealed class RecipeImageConfiguration : IEntityTypeConfiguration<RecipeImage>
{
    public void Configure(EntityTypeBuilder<RecipeImage> builder)
    {
        builder.ToTable("RecipeImages", table =>
            table.HasCheckConstraint("CK_RecipeImages_OrderIndex", "\"OrderIndex\" >= 0"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.ObjectKey).HasMaxLength(500).IsRequired();
        builder.Property(item => item.MediumObjectKey).HasMaxLength(500);
        builder.Property(item => item.ThumbnailObjectKey).HasMaxLength(500);
        builder.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(item => item.AltText).HasMaxLength(200);
        builder.Property(item => item.ProcessingStatus).HasConversion<int>();
        builder.Property(item => item.Version).HasDefaultValue(1);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => item.ObjectKey).IsUnique();
        builder.HasIndex(item => new { item.RecipeId, item.OrderIndex });
        builder.HasIndex(item => item.RecipeId)
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE AND \"IsDeleted\" = FALSE");
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
