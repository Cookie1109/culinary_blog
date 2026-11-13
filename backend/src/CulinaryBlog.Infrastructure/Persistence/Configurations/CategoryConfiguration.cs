// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the category configuration.</summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    /// <summary>Configures the category configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", table =>
        {
            table.HasCheckConstraint("CK_Categories_Name", "char_length(btrim(\"Name\")) BETWEEN 2 AND 100");
            table.HasCheckConstraint("CK_Categories_OrderIndex", "\"OrderIndex\" >= 0");
        });
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.NormalizedName).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasColumnType("text").HasMaxLength(2000);
        builder.Property(category => category.OrderIndex).HasDefaultValue(0);
        builder.Property(category => category.ImageUrl).HasMaxLength(500);
        builder.Property(category => category.Version).HasDefaultValue(1).IsConcurrencyToken();
        builder.HasIndex(category => category.NormalizedName).IsUnique();
        builder.HasIndex(category => category.Name).IsUnique();
        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => new { category.OrderIndex, category.Name });
        builder.HasQueryFilter(category => !category.IsDeleted);
    }
}
