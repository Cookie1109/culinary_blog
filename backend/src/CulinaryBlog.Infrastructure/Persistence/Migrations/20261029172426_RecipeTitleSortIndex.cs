// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RecipeTitleSortIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Status_Title_Id",
            table: "Recipes",
            columns: ["Status", "Title", "Id"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Recipes_Status_Title_Id",
            table: "Recipes");
    }
}
