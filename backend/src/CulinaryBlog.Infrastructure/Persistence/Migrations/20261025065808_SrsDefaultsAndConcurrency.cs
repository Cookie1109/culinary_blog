// Culinary Blog. Authorship is recorded in the repository history.

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class SrsDefaultsAndConcurrency : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TEMP TABLE srs_user_triggers ON COMMIT DROP AS SELECT tgname, pg_get_triggerdef(oid) AS definition
            FROM pg_trigger WHERE NOT tgisinternal AND tgrelid = '"AspNetUsers"'::regclass;
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_user_triggers LOOP
                    EXECUTE format('DROP TRIGGER %I ON "AspNetUsers"', item.tgname);
                END LOOP;
            END $migration$;
            """);
        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RefreshTokens",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RefreshTokens",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeSteps",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "RecipeSteps",
            type: "text",
            maxLength: 2000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(2000)",
            oldMaxLength: 2000);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeSteps",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeSteps",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "Recipes",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Recipes",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Recipes",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "RecipeIngredients",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeIngredients",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeIngredients",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeIngredients",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "RecipeImages",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<bool>(
            name: "IsPrimary",
            table: "RecipeImages",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeImages",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeImages",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeImages",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "Categories",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "integer");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "Categories",
            type: "boolean",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Categories",
            type: "text",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(2000)",
            oldMaxLength: 2000,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Categories",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Categories",
            type: "uuid",
            nullable: false,
            defaultValueSql: "gen_random_uuid()",
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AlterColumn<bool>(
            name: "IsActive",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true,
            oldClrType: typeof(bool),
            oldType: "boolean");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()",
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<string>(
            name: "Bio",
            table: "AspNetUsers",
            type: "text",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(2000)",
            oldMaxLength: 2000,
            oldNullable: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "RecipeSteps",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            computedColumnSql: "int8send(\"Version\")",
            stored: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Recipes",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            computedColumnSql: "int8send(\"Version\")",
            stored: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "RecipeIngredients",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            computedColumnSql: "int8send(\"Version\")",
            stored: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "RecipeImages",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            computedColumnSql: "int8send(\"Version\")",
            stored: true);

        migrationBuilder.AddColumn<byte[]>(
            name: "RowVersion",
            table: "Categories",
            type: "bytea",
            rowVersion: true,
            nullable: false,
            computedColumnSql: "int8send(\"Version\")",
            stored: true);

        migrationBuilder.CreateIndex(
            name: "IX_Recipes_Difficulty",
            table: "Recipes",
            column: "Difficulty");

        migrationBuilder.CreateIndex(
            name: "IX_Recipes_IsDeleted",
            table: "Recipes",
            column: "IsDeleted",
            filter: "\"IsDeleted\" = FALSE");

        migrationBuilder.CreateIndex(
            name: "IX_Categories_Name",
            table: "Categories",
            column: "Name",
            unique: true);
        migrationBuilder.Sql("""
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_user_triggers LOOP EXECUTE item.definition; END LOOP;
            END $migration$;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TEMP TABLE srs_user_triggers ON COMMIT DROP AS SELECT tgname, pg_get_triggerdef(oid) AS definition
            FROM pg_trigger WHERE NOT tgisinternal AND tgrelid = '"AspNetUsers"'::regclass;
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_user_triggers LOOP
                    EXECUTE format('DROP TRIGGER %I ON "AspNetUsers"', item.tgname);
                END LOOP;
            END $migration$;
            """);
        migrationBuilder.DropIndex(
            name: "IX_Recipes_Difficulty",
            table: "Recipes");

        migrationBuilder.DropIndex(
            name: "IX_Recipes_IsDeleted",
            table: "Recipes");

        migrationBuilder.DropIndex(
            name: "IX_Categories_Name",
            table: "Categories");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "RecipeSteps");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Recipes");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "RecipeIngredients");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "RecipeImages");

        migrationBuilder.DropColumn(
            name: "RowVersion",
            table: "Categories");

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RefreshTokens",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RefreshTokens",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeSteps",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "RecipeSteps",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text",
            oldMaxLength: 2000);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeSteps",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeSteps",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "Recipes",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Recipes",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Recipes",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "RecipeIngredients",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeIngredients",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeIngredients",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeIngredients",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "RecipeImages",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);

        migrationBuilder.AlterColumn<bool>(
            name: "IsPrimary",
            table: "RecipeImages",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "RecipeImages",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "RecipeImages",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "RecipeImages",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<int>(
            name: "OrderIndex",
            table: "Categories",
            type: "integer",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer",
            oldDefaultValue: 0);

        migrationBuilder.AlterColumn<bool>(
            name: "IsDeleted",
            table: "Categories",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: false);

        migrationBuilder.AlterColumn<string>(
            name: "Description",
            table: "Categories",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldMaxLength: 2000,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Categories",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<Guid>(
            name: "Id",
            table: "Categories",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldDefaultValueSql: "gen_random_uuid()");

        migrationBuilder.AlterColumn<bool>(
            name: "IsActive",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            oldClrType: typeof(bool),
            oldType: "boolean",
            oldDefaultValue: true);

        migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "timestamp with time zone",
            oldDefaultValueSql: "NOW()");

        migrationBuilder.AlterColumn<string>(
            name: "Bio",
            table: "AspNetUsers",
            type: "character varying(2000)",
            maxLength: 2000,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldMaxLength: 2000,
            oldNullable: true);
        migrationBuilder.Sql("""
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_user_triggers LOOP EXECUTE item.definition; END LOOP;
            END $migration$;
            """);
    }
}
