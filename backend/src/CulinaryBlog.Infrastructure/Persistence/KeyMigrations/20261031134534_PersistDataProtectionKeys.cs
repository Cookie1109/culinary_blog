// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.KeyMigrations;

/// <inheritdoc />
public partial class PersistDataProtectionKeys : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DataProtectionKeys",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                FriendlyName = table.Column<string>(type: "text", nullable: true),
                Xml = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $g06$
            BEGIN
                IF EXISTS (SELECT 1 FROM "DataProtectionKeys") THEN
                    RAISE EXCEPTION 'Data Protection keys exist; preserve the keyring before rollback.';
                END IF;
            END $g06$;
            """);
        migrationBuilder.DropTable(
            name: "DataProtectionKeys");
    }
}
