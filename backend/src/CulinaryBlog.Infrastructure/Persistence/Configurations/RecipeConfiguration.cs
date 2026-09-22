using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace CulinaryBlog.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
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
            table.HasCheckConstraint(
                "CK_Recipes_Nutrition",
                "(\"Nutrition_Calories\" IS NULL OR \"Nutrition_Calories\" >= 0) AND " +
                "(\"Nutrition_Protein\" IS NULL OR \"Nutrition_Protein\" >= 0) AND " +
                "(\"Nutrition_Carbohydrates\" IS NULL OR \"Nutrition_Carbohydrates\" >= 0) AND " +
                "(\"Nutrition_Fat\" IS NULL OR \"Nutrition_Fat\" >= 0) AND " +
                "(\"Nutrition_Fiber\" IS NULL OR \"Nutrition_Fiber\" >= 0) AND " +
                "(\"Nutrition_Sodium\" IS NULL OR \"Nutrition_Sodium\" >= 0)");
            table.HasCheckConstraint("CK_Recipes_Version", "\"Version\" >= 1");
        });
        builder.HasKey(recipe => recipe.Id);
        builder.Property(recipe => recipe.Title).HasMaxLength(200).IsRequired();
        builder.Property(recipe => recipe.Slug).HasMaxLength(220).IsRequired();
        builder.Property(recipe => recipe.Description).HasMaxLength(2000).IsRequired();
        builder.Property(recipe => recipe.Instructions).HasMaxLength(10000);
        builder.Property(recipe => recipe.Difficulty).HasConversion<int>();
        builder.Property(recipe => recipe.Status).HasConversion<int>();
        builder.Property(recipe => recipe.Version).HasDefaultValue(1).IsConcurrencyToken();
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnType("tsvector")
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
        builder.HasIndex(recipe => recipe.Status);
        builder.HasIndex(recipe => recipe.CategoryId);
        builder.HasIndex(recipe => recipe.AuthorId);
        builder.HasIndex(recipe => recipe.PublishedAt);
        builder.HasIndex(recipe => new { recipe.Status, recipe.PublishedAt });
        builder.HasQueryFilter(recipe => !recipe.IsDeleted);
    }
}
