using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261003120000_RecipeDiscoveryIndexes")]
public sealed class RecipeDiscoveryIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.AddColumn<string>(
            name: "SearchTitle",
            table: "Recipes",
            type: "text",
            nullable: true);
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION culinary_recipe_search_vector() RETURNS trigger AS $$
            BEGIN
                NEW."SearchTitle" := lower(unaccent(coalesce(NEW."Title", '')));
                NEW."SearchVector" :=
                    setweight(to_tsvector('simple', NEW."SearchTitle"), 'A') ||
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))), 'B');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);
        migrationBuilder.Sql("UPDATE \"Recipes\" SET \"Title\" = \"Title\";");
        migrationBuilder.Sql(
            "CREATE INDEX \"IX_Recipes_SearchTitle\" ON \"Recipes\" USING GIN (\"SearchTitle\" gin_trgm_ops);");
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_CategoryId_Difficulty_PublishedAt",
            table: "Recipes",
            columns: ["Status", "CategoryId", "Difficulty", "PublishedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_CreatedAt",
            table: "Recipes",
            columns: ["Status", "CreatedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_CookTime",
            table: "Recipes",
            columns: ["Status", "CookTime"]);
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_Servings",
            table: "Recipes",
            columns: ["Status", "Servings"]);
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_PrepTime",
            table: "Recipes",
            columns: ["Status", "PrepTime"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Recipes_SearchTitle", table: "Recipes");
        migrationBuilder.DropIndex(name: "IX_Recipes_Status_CategoryId_Difficulty_PublishedAt", table: "Recipes");
        migrationBuilder.DropIndex(name: "IX_Recipes_Status_CreatedAt", table: "Recipes");
        migrationBuilder.DropIndex(name: "IX_Recipes_Status_CookTime", table: "Recipes");
        migrationBuilder.DropIndex(name: "IX_Recipes_Status_Servings", table: "Recipes");
        migrationBuilder.DropIndex(name: "IX_Recipes_Status_PrepTime", table: "Recipes");
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION culinary_recipe_search_vector() RETURNS trigger AS $$
            BEGIN
                NEW."SearchVector" :=
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))), 'A') ||
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))), 'B');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);
        migrationBuilder.DropColumn(name: "SearchTitle", table: "Recipes");
    }
}
