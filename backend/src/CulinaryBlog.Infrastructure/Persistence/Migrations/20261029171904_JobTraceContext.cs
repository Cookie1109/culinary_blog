// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class JobTraceContext : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            table: "WelcomeEmailOutbox",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TraceParent",
            table: "WelcomeEmailOutbox",
            type: "character varying(55)",
            maxLength: 55,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CorrelationId",
            table: "MediaOutbox",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "TraceParent",
            table: "MediaOutbox",
            type: "character varying(55)",
            maxLength: 55,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "CorrelationId",
            table: "WelcomeEmailOutbox");

        migrationBuilder.DropColumn(
            name: "TraceParent",
            table: "WelcomeEmailOutbox");

        migrationBuilder.DropColumn(
            name: "CorrelationId",
            table: "MediaOutbox");

        migrationBuilder.DropColumn(
            name: "TraceParent",
            table: "MediaOutbox");
    }
}
