// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <summary>Prevents private recipe writes from invalidating unrelated public responses.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261104121500_PublishedContentCacheGeneration")]
public sealed class PublishedContentCacheGeneration : Migration
{
    private static readonly string[] RecipeTables = ["Recipes", "RecipeIngredients", "RecipeSteps", "RecipeImages"];

    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE FUNCTION "BumpPublishedContentCacheGeneration"() RETURNS trigger LANGUAGE plpgsql AS $$
            DECLARE
                previous_row jsonb;
                current_row jsonb;
                affects_public boolean;
            BEGIN
                IF TG_OP <> 'INSERT' THEN previous_row := to_jsonb(OLD); END IF;
                IF TG_OP <> 'DELETE' THEN current_row := to_jsonb(NEW); END IF;
                IF TG_TABLE_NAME = 'Recipes' THEN
                    affects_public :=
                        coalesce((previous_row->>'Status')::smallint = 1 AND NOT (previous_row->>'IsDeleted')::boolean, false)
                        OR coalesce((current_row->>'Status')::smallint = 1 AND NOT (current_row->>'IsDeleted')::boolean, false);
                ELSE
                    SELECT EXISTS (
                        SELECT 1 FROM "Recipes"
                        WHERE "Id" IN ((previous_row->>'RecipeId')::uuid, (current_row->>'RecipeId')::uuid)
                            AND "Status" = 1 AND NOT "IsDeleted") INTO affects_public;
                END IF;
                IF affects_public THEN
                    UPDATE "PublicCacheState" SET "Generation" = "Generation" + 1 WHERE "Id" = 1;
                END IF;
                RETURN NULL;
            END;
            $$;
            """);
        foreach (var table in RecipeTables)
        {
            migrationBuilder.Sql($"""
                DROP TRIGGER "PublicCacheGenerationChanged" ON "{table}";
                CREATE TRIGGER "PublicCacheGenerationChanged" AFTER INSERT OR UPDATE OR DELETE ON "{table}"
                FOR EACH ROW EXECUTE FUNCTION "BumpPublishedContentCacheGeneration"();
                """);
        }
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in RecipeTables)
        {
            migrationBuilder.Sql($"""
                DROP TRIGGER "PublicCacheGenerationChanged" ON "{table}";
                CREATE TRIGGER "PublicCacheGenerationChanged" AFTER INSERT OR UPDATE OR DELETE ON "{table}"
                FOR EACH STATEMENT EXECUTE FUNCTION "BumpPublicCacheGeneration"();
                """);
        }

        migrationBuilder.Sql("DROP FUNCTION \"BumpPublishedContentCacheGeneration\"();");
    }
}
