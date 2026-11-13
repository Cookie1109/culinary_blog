// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the recipe ingredient configuration.</summary>
internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    /// <summary>Configures the recipe ingredient configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients", table =>
        {
            table.HasCheckConstraint("CK_RecipeIngredients_Quantity", "\"Quantity\" IS NULL OR \"Quantity\" > 0");
            table.HasCheckConstraint("CK_RecipeIngredients_OrderIndex", "\"OrderIndex\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Quantity).HasPrecision(10, 3);
        builder.Property(item => item.Unit).HasMaxLength(50);
        builder.Property(item => item.Notes).HasMaxLength(500);
        builder.Property(item => item.OrderIndex).HasDefaultValue(0);
        builder.Property(item => item.Version).HasDefaultValue(1);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.RecipeId, item.OrderIndex });
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
