// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the recipe slug configuration.</summary>
internal sealed class RecipeSlugConfiguration : IEntityTypeConfiguration<RecipeSlug>
{
    /// <summary>Configures the recipe slug configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<RecipeSlug> builder)
    {
        builder.ToTable("RecipeSlugs");
        builder.HasKey(item => item.Slug);
        builder.Property(item => item.Slug).HasMaxLength(220);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}
