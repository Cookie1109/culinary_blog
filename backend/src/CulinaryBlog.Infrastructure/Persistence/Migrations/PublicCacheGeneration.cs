// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <summary>Applies the PublicCacheGeneration schema change without changing its migration identity.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261025050000_PublicCacheGeneration")]
public sealed class PublicCacheGeneration : Migration
{
    private static readonly string[] ContentTables = ["Categories", "Recipes", "RecipeIngredients", "RecipeSteps", "RecipeImages"];

    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "PublicCacheState" (
                "Id" integer PRIMARY KEY CHECK ("Id" = 1),
                "Generation" bigint NOT NULL DEFAULT 0);
            INSERT INTO "PublicCacheState" ("Id", "Generation") VALUES (1, 0);
            CREATE FUNCTION "BumpPublicCacheGeneration"() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                UPDATE "PublicCacheState" SET "Generation" = "Generation" + 1 WHERE "Id" = 1;
                RETURN NULL;
            END;
            $$;
            """);
        foreach (var table in ContentTables)
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER "PublicCacheGenerationChanged" AFTER INSERT OR UPDATE OR DELETE ON "{table}"
                FOR EACH STATEMENT EXECUTE FUNCTION "BumpPublicCacheGeneration"();
                """);
        }

        migrationBuilder.Sql("""
            CREATE TRIGGER "PublicAuthorCacheGenerationChanged" AFTER UPDATE ON "AspNetUsers"
            FOR EACH ROW WHEN (
                (OLD."DisplayName", OLD."AvatarUrl", OLD."Bio", OLD."Email", OLD."EmailConfirmed", OLD."IsActive")
                IS DISTINCT FROM
                (NEW."DisplayName", NEW."AvatarUrl", NEW."Bio", NEW."Email", NEW."EmailConfirmed", NEW."IsActive"))
            EXECUTE FUNCTION "BumpPublicCacheGeneration"();
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER \"PublicAuthorCacheGenerationChanged\" ON \"AspNetUsers\";");
        foreach (var table in ContentTables)
        {
            migrationBuilder.Sql($"DROP TRIGGER \"PublicCacheGenerationChanged\" ON \"{table}\";");
        }

        migrationBuilder.Sql("DROP FUNCTION \"BumpPublicCacheGeneration\"(); DROP TABLE \"PublicCacheState\";");
    }
}
