// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the recipe step configuration.</summary>
internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    /// <summary>Configures the recipe step configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("RecipeSteps", table =>
        {
            table.HasCheckConstraint("CK_RecipeSteps_StepNumber", "\"StepNumber\" > 0");
            table.HasCheckConstraint("CK_RecipeSteps_TimerMinutes", "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasColumnType("text").HasMaxLength(2000).IsRequired();
        builder.Property(item => item.ImageUrl).HasMaxLength(500);
        builder.Property(item => item.Version).HasDefaultValue(1);
        builder.HasOne<Recipe>().WithMany().HasForeignKey(item => item.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.RecipeId, item.StepNumber })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
        builder.HasQueryFilter(item => !item.IsDeleted);
    }
}
