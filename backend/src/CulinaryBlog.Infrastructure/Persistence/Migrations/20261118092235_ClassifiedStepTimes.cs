using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ClassifiedStepTimes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Recipes_PrepTime",
            table: "Recipes");

        migrationBuilder.AddColumn<string>(
            name: "StepType",
            table: "RecipeSteps",
            type: "character varying(4)",
            maxLength: 4,
            nullable: false,
            defaultValue: "cook");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Recipes_PrepTime",
            table: "Recipes",
            sql: "\"PrepTime\" >= 0");

        migrationBuilder.Sql("""
            UPDATE "Recipes" r SET "PrepTime" = 0,
                "CookTime" = LEAST(2147483647, CEIL(COALESCE((
                    SELECT SUM(s."TimerMinutes") FROM "RecipeSteps" s
                    WHERE s."RecipeId" = r."Id" AND NOT s."IsDeleted"
                ), 0)))::integer,
                "Version" = r."Version" + 1;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Recipes_PrepTime",
            table: "Recipes");

        migrationBuilder.DropColumn(
            name: "StepType",
            table: "RecipeSteps");

        migrationBuilder.Sql("UPDATE \"Recipes\" SET \"PrepTime\" = 1 WHERE \"PrepTime\" = 0;");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Recipes_PrepTime",
            table: "Recipes",
            sql: "\"PrepTime\" > 0");
    }
}
