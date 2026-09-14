using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RecipeCompositionMedia : Migration
{
    private static readonly string[] MediaOutboxIndexColumns = ["ProcessedAt", "NextAttemptAt"];
    private static readonly string[] ImageOrderIndexColumns = ["RecipeId", "OrderIndex"];
    private static readonly string[] IngredientOrderIndexColumns = ["RecipeId", "OrderIndex"];
    private static readonly string[] StepNumberIndexColumns = ["RecipeId", "StepNumber"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MediaOutbox",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Payload = table.Column<string>(type: "jsonb", nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MediaOutbox", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RecipeImages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                ObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                MediumObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ThumbnailObjectKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                OrderIndex = table.Column<int>(type: "integer", nullable: false),
                ProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeImages", x => x.Id);
                table.CheckConstraint("CK_RecipeImages_OrderIndex", "\"OrderIndex\" >= 0");
                table.ForeignKey(
                    name: "FK_RecipeImages_Recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RecipeIngredients",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                OrderIndex = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                table.CheckConstraint("CK_RecipeIngredients_OrderIndex", "\"OrderIndex\" >= 0");
                table.CheckConstraint("CK_RecipeIngredients_Quantity", "\"Quantity\" IS NULL OR \"Quantity\" > 0");
                table.ForeignKey(
                    name: "FK_RecipeIngredients_Recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RecipeSteps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                StepNumber = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                TimerMinutes = table.Column<int>(type: "integer", nullable: true),
                ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true),
                Version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeSteps", x => x.Id);
                table.CheckConstraint("CK_RecipeSteps_StepNumber", "\"StepNumber\" > 0");
                table.CheckConstraint("CK_RecipeSteps_TimerMinutes", "\"TimerMinutes\" IS NULL OR \"TimerMinutes\" >= 0");
                table.ForeignKey(
                    name: "FK_RecipeSteps_Recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MediaOutbox_ProcessedAt_NextAttemptAt",
            table: "MediaOutbox",
            columns: MediaOutboxIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeImages_ObjectKey",
            table: "RecipeImages",
            column: "ObjectKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeImages_RecipeId",
            table: "RecipeImages",
            column: "RecipeId",
            unique: true,
            filter: "\"IsPrimary\" = TRUE AND \"IsDeleted\" = FALSE");

        migrationBuilder.CreateIndex(
            name: "IX_RecipeImages_RecipeId_OrderIndex",
            table: "RecipeImages",
            columns: ImageOrderIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeIngredients_RecipeId_OrderIndex",
            table: "RecipeIngredients",
            columns: IngredientOrderIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_RecipeSteps_RecipeId_StepNumber",
            table: "RecipeSteps",
            columns: StepNumberIndexColumns,
            unique: true,
            filter: "\"IsDeleted\" = FALSE");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MediaOutbox");

        migrationBuilder.DropTable(
            name: "RecipeImages");

        migrationBuilder.DropTable(
            name: "RecipeIngredients");

        migrationBuilder.DropTable(
            name: "RecipeSteps");
    }
}
