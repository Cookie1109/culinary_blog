// Culinary Blog. Authorship is recorded in the repository history.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ContentSlugsAndImageUrls : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MediumUrl",
            table: "RecipeImages",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OriginalUrl",
            table: "RecipeImages",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ThumbnailUrl",
            table: "RecipeImages",
            type: "character varying(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "RecipeSlugs",
            columns: table => new
            {
                Slug = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeSlugs", x => x.Slug);
                table.ForeignKey(
                    name: "FK_RecipeSlugs_Recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RecipeSlugs_RecipeId",
            table: "RecipeSlugs",
            column: "RecipeId");

        migrationBuilder.Sql("""
            INSERT INTO "RecipeSlugs" ("Slug", "RecipeId") SELECT "Slug", "Id" FROM "Recipes";
            UPDATE "RecipeImages" SET
                "OriginalUrl" = '/api/v1/media/' || "Id"::text || '/original',
                "MediumUrl" = CASE WHEN "MediumObjectKey" IS NOT NULL THEN '/api/v1/media/' || "Id"::text || '/medium' END,
                "ThumbnailUrl" = CASE WHEN "ThumbnailObjectKey" IS NOT NULL THEN '/api/v1/media/' || "Id"::text || '/thumbnail' END;
            """);
        migrationBuilder.AlterColumn<string>(
            name: "OriginalUrl",
            table: "RecipeImages",
            type: "character varying(500)",
            maxLength: 500,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(500)",
            oldMaxLength: 500,
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RecipeSlugs");

        migrationBuilder.DropColumn(
            name: "MediumUrl",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "OriginalUrl",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "ThumbnailUrl",
            table: "RecipeImages");
    }
}
