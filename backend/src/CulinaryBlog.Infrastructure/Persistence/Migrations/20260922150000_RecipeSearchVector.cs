using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260922150000_RecipeSearchVector")]
public sealed class RecipeSearchVector : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        migrationBuilder.Sql("ALTER TABLE \"Recipes\" ADD COLUMN \"SearchVector\" tsvector;");
        migrationBuilder.Sql("""
            CREATE FUNCTION culinary_recipe_search_vector() RETURNS trigger AS $$
            BEGIN
                NEW."SearchVector" :=
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Title", ''))), 'A') ||
                    setweight(to_tsvector('simple', unaccent(coalesce(NEW."Description", ''))), 'B');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER recipes_search_vector_update
            BEFORE INSERT OR UPDATE OF "Title", "Description" ON "Recipes"
            FOR EACH ROW EXECUTE FUNCTION culinary_recipe_search_vector();
            """);
        migrationBuilder.Sql("UPDATE \"Recipes\" SET \"Title\" = \"Title\";");
        migrationBuilder.Sql("CREATE INDEX \"IX_Recipes_SearchVector\" ON \"Recipes\" USING GIN (\"SearchVector\");");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS recipes_search_vector_update ON \"Recipes\";");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS culinary_recipe_search_vector();");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Recipes_SearchVector\";");
        migrationBuilder.Sql("ALTER TABLE \"Recipes\" DROP COLUMN \"SearchVector\";");
    }
}
