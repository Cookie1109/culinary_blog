// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

/// <summary>Represents the recipe configuration.</summary>
internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    /// <summary>Configures the recipe configuration.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes", table =>
        {
            table.HasCheckConstraint("CK_Recipes_PrepTime", "\"PrepTime\" > 0");
            table.HasCheckConstraint("CK_Recipes_CookTime", "\"CookTime\" >= 0");
            table.HasCheckConstraint("CK_Recipes_Servings", "\"Servings\" > 0");
            table.HasCheckConstraint("CK_Recipes_Difficulty", "\"Difficulty\" BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_Recipes_Status", "\"Status\" BETWEEN 0 AND 2");
            table.HasCheckConstraint("CK_Recipes_Title", "char_length(btrim(\"Title\")) BETWEEN 5 AND 200");
            table.HasCheckConstraint("CK_Recipes_Description", "char_length(btrim(\"Description\")) BETWEEN 1 AND 2000");
            const string nutritionConstraint = "(\"Nutrition_Calories\" IS NULL OR \"Nutrition_Calories\" >= 0) AND " +
                "(\"Nutrition_Protein\" IS NULL OR \"Nutrition_Protein\" >= 0) AND " +
                "(\"Nutrition_Carbohydrates\" IS NULL OR \"Nutrition_Carbohydrates\" >= 0) AND " +
                "(\"Nutrition_Fat\" IS NULL OR \"Nutrition_Fat\" >= 0) AND " +
                "(\"Nutrition_Fiber\" IS NULL OR \"Nutrition_Fiber\" >= 0) AND " +
                "(\"Nutrition_Sodium\" IS NULL OR \"Nutrition_Sodium\" >= 0)";
            table.HasCheckConstraint("CK_Recipes_Nutrition", nutritionConstraint);
            table.HasCheckConstraint("CK_Recipes_Version", "\"Version\" >= 1");
        });
        builder.HasKey(recipe => recipe.Id);
        builder.Property(recipe => recipe.Title).HasMaxLength(200).IsRequired();
        builder.Property(recipe => recipe.Slug).HasMaxLength(220).IsRequired();
        builder.Property(recipe => recipe.Description).HasColumnType("text").HasMaxLength(2000).IsRequired();
        builder.Property(recipe => recipe.Instructions).HasColumnType("text").IsRequired();
        builder.Property(recipe => recipe.AuthorId).HasMaxLength(450).IsRequired();
        builder.Property(recipe => recipe.Difficulty).HasConversion<short>().HasDefaultValue(RecipeDifficulty.Easy);
        builder.Property(recipe => recipe.Status).HasConversion<short>().HasDefaultValue(RecipeStatus.Draft);
        builder.Property(recipe => recipe.Version).HasDefaultValue(1).IsConcurrencyToken();
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnType("tsvector")
            .IsRequired(false)
            .ValueGeneratedOnAddOrUpdate();
        builder.Property<string>("SearchTitle")
            .HasColumnType("text")
            .ValueGeneratedOnAddOrUpdate();
        builder.OwnsOne(recipe => recipe.Nutrition, nutrition =>
        {
            nutrition.Property(value => value.Calories).HasPrecision(8, 2).HasColumnName("Nutrition_Calories");
            nutrition.Property(value => value.Protein).HasPrecision(8, 2).HasColumnName("Nutrition_Protein");
            nutrition.Property(value => value.Carbohydrates).HasPrecision(8, 2).HasColumnName("Nutrition_Carbohydrates");
            nutrition.Property(value => value.Fat).HasPrecision(8, 2).HasColumnName("Nutrition_Fat");
            nutrition.Property(value => value.Fiber).HasPrecision(8, 2).HasColumnName("Nutrition_Fiber");
            nutrition.Property(value => value.Sodium).HasPrecision(8, 2).HasColumnName("Nutrition_Sodium");
        });
        builder.Navigation(recipe => recipe.Nutrition).IsRequired();
        builder.HasOne<Category>().WithMany().HasForeignKey(recipe => recipe.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(recipe => recipe.AuthorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(recipe => recipe.Slug).IsUnique();
        builder.HasIndex("SearchVector").HasMethod("GIN");
        builder.HasIndex("SearchTitle").HasMethod("GIN").HasOperators("gin_trgm_ops");
        builder.HasIndex(recipe => recipe.Status);
        builder.HasIndex(recipe => recipe.Difficulty);
        builder.HasIndex(recipe => recipe.IsDeleted).HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(recipe => recipe.CategoryId);
        builder.HasIndex(recipe => recipe.AuthorId);
        builder.HasIndex(recipe => recipe.PublishedAt);
        builder.HasIndex(recipe => new { recipe.Status, recipe.PublishedAt });
        builder.HasIndex(recipe => new { recipe.Status, recipe.CreatedAt });
        builder.HasIndex(recipe => new { recipe.Status, recipe.Title, recipe.Id });
        builder.HasIndex(recipe => new { recipe.Status, recipe.CategoryId, recipe.Difficulty, recipe.PublishedAt });
        builder.HasIndex(recipe => new { recipe.Status, recipe.CookTime });
        builder.HasIndex(recipe => new { recipe.Status, recipe.Servings });
        builder.HasIndex(recipe => new { recipe.Status, recipe.PrepTime });
        builder.HasQueryFilter(recipe => !recipe.IsDeleted);
    }
}
