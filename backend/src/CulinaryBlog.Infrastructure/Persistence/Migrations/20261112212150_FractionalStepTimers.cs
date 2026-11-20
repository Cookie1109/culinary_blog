// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class FractionalStepTimers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<double>(
            name: "TimerMinutes",
            table: "RecipeSteps",
            type: "double precision",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<int>(
            name: "TimerMinutes",
            table: "RecipeSteps",
            type: "integer",
            nullable: true,
            oldClrType: typeof(double),
            oldType: "double precision",
            oldNullable: true);
    }
}
