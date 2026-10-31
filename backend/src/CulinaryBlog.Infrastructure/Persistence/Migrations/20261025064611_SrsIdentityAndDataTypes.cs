// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <summary>Aligns stored identity and content data types with the SRS.</summary>
public partial class SrsIdentityAndDataTypes : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $migration$ BEGIN
                IF EXISTS (SELECT 1 FROM "RefreshTokens" WHERE char_length("CreatedByIp") > 45 OR char_length("RevokedByIp") > 45) THEN
                    RAISE EXCEPTION 'Review legacy IP values longer than 45 characters before migration; no values were truncated';
                END IF;
            END $migration$;
            CREATE TEMP TABLE srs_identity_triggers ON COMMIT DROP AS
            SELECT tgrelid::regclass::text AS table_name, tgname, pg_get_triggerdef(oid) AS definition
            FROM pg_trigger WHERE NOT tgisinternal AND tgrelid IN ('"Recipes"'::regclass, '"AspNetUsers"'::regclass);
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_identity_triggers LOOP
                    EXECUTE format('DROP TRIGGER %I ON %s', item.tgname, item.table_name);
                END LOOP;
            END $migration$;
            CREATE TEMP TABLE srs_identity_fks ON COMMIT DROP AS
            SELECT conrelid::regclass::text AS table_name, conname, pg_get_constraintdef(oid) AS definition
            FROM pg_constraint WHERE contype = 'f' AND confrelid IN ('"AspNetUsers"'::regclass, '"AspNetRoles"'::regclass);
            DO $migration$
            DECLARE item record;
            BEGIN
                FOR item IN SELECT * FROM srs_identity_fks LOOP
                    EXECUTE format('ALTER TABLE %s DROP CONSTRAINT %I', item.table_name, item.conname);
                END LOOP;
            END $migration$;
            ALTER TABLE "AspNetUsers" ALTER COLUMN "Id" TYPE varchar(450) USING "Id"::text;
            ALTER TABLE "AspNetRoles" ALTER COLUMN "Id" TYPE varchar(450) USING "Id"::text;
            ALTER TABLE "AspNetUserClaims" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            ALTER TABLE "AspNetRoleClaims" ALTER COLUMN "RoleId" TYPE varchar(450) USING "RoleId"::text;
            ALTER TABLE "AspNetUserLogins" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            ALTER TABLE "AspNetUserRoles" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            ALTER TABLE "AspNetUserRoles" ALTER COLUMN "RoleId" TYPE varchar(450) USING "RoleId"::text;
            ALTER TABLE "AspNetUserTokens" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            ALTER TABLE "Recipes" ALTER COLUMN "AuthorId" TYPE varchar(450) USING "AuthorId"::text;
            ALTER TABLE "RefreshTokens" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            ALTER TABLE "WelcomeEmailOutbox" ALTER COLUMN "UserId" TYPE varchar(450) USING "UserId"::text;
            DO $migration$
            DECLARE item record;
            BEGIN
                FOR item IN SELECT * FROM srs_identity_fks LOOP
                    EXECUTE format('ALTER TABLE %s ADD CONSTRAINT %I %s', item.table_name, item.conname, item.definition);
                END LOOP;
            END $migration$;
            ALTER TABLE "RefreshTokens" ALTER COLUMN "CreatedByIp" TYPE varchar(45), ALTER COLUMN "RevokedByIp" TYPE varchar(45);
            ALTER TABLE "RefreshTokens" ADD COLUMN IF NOT EXISTS "UserAgentHash" varchar(64);
            UPDATE "RefreshTokens" SET "UserAgentHash" = upper(encode(sha256(convert_to("UserAgent", 'UTF8')), 'hex'))
            WHERE "UserAgent" IS NOT NULL AND "UserAgentHash" IS NULL;
            UPDATE "Recipes" SET "Instructions" = '' WHERE "Instructions" IS NULL;
            ALTER TABLE "Recipes" ALTER COLUMN "Instructions" TYPE text, ALTER COLUMN "Instructions" SET NOT NULL,
                ALTER COLUMN "Description" TYPE text,
                ALTER COLUMN "Difficulty" TYPE smallint USING "Difficulty"::smallint,
                ALTER COLUMN "Difficulty" SET DEFAULT 1,
                ALTER COLUMN "Status" TYPE smallint USING "Status"::smallint,
                ALTER COLUMN "Status" SET DEFAULT 0;
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_identity_triggers LOOP
                    EXECUTE item.definition;
                END LOOP;
            END $migration$;
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $migration$ BEGIN
                IF EXISTS (SELECT 1 FROM "AspNetUsers" WHERE "Id" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetRoles" WHERE "Id" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetUserClaims" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetRoleClaims" WHERE "RoleId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetUserLogins" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetUserRoles" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetUserRoles" WHERE "RoleId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "AspNetUserTokens" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "Recipes" WHERE "AuthorId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "RefreshTokens" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "WelcomeEmailOutbox" WHERE "UserId" !~* '^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$') OR EXISTS (SELECT 1 FROM "Recipes" WHERE char_length("Instructions") > 10000) THEN
                    RAISE EXCEPTION 'Rollback requires UUID-compatible Identity keys and Instructions <=10000; restore a reviewed backup for incompatible new data';
                END IF;
            END $migration$;
            CREATE TEMP TABLE srs_identity_triggers ON COMMIT DROP AS
            SELECT tgrelid::regclass::text AS table_name, tgname, pg_get_triggerdef(oid) AS definition
            FROM pg_trigger WHERE NOT tgisinternal AND tgrelid IN ('"Recipes"'::regclass, '"AspNetUsers"'::regclass);
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_identity_triggers LOOP
                    EXECUTE format('DROP TRIGGER %I ON %s', item.tgname, item.table_name);
                END LOOP;
            END $migration$;
            CREATE TEMP TABLE srs_identity_fks ON COMMIT DROP AS
            SELECT conrelid::regclass::text AS table_name, conname, pg_get_constraintdef(oid) AS definition
            FROM pg_constraint WHERE contype = 'f' AND confrelid IN ('"AspNetUsers"'::regclass, '"AspNetRoles"'::regclass);
            DO $migration$
            DECLARE item record;
            BEGIN
                FOR item IN SELECT * FROM srs_identity_fks LOOP
                    EXECUTE format('ALTER TABLE %s DROP CONSTRAINT %I', item.table_name, item.conname);
                END LOOP;
            END $migration$;
            ALTER TABLE "WelcomeEmailOutbox" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "RefreshTokens" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "Recipes" ALTER COLUMN "AuthorId" TYPE uuid USING "AuthorId"::uuid;
            ALTER TABLE "AspNetUserTokens" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "AspNetUserRoles" ALTER COLUMN "RoleId" TYPE uuid USING "RoleId"::uuid;
            ALTER TABLE "AspNetUserRoles" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "AspNetUserLogins" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "AspNetRoleClaims" ALTER COLUMN "RoleId" TYPE uuid USING "RoleId"::uuid;
            ALTER TABLE "AspNetUserClaims" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;
            ALTER TABLE "AspNetRoles" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
            ALTER TABLE "AspNetUsers" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;
            DO $migration$
            DECLARE item record;
            BEGIN
                FOR item IN SELECT * FROM srs_identity_fks LOOP
                    EXECUTE format('ALTER TABLE %s ADD CONSTRAINT %I %s', item.table_name, item.conname, item.definition);
                END LOOP;
            END $migration$;
            ALTER TABLE "RefreshTokens" ALTER COLUMN "CreatedByIp" TYPE varchar(64), ALTER COLUMN "RevokedByIp" TYPE varchar(64);
            -- Retain UserAgentHash on rollback: new sessions deliberately no longer store raw user-agent.
            ALTER TABLE "Recipes" ALTER COLUMN "Instructions" DROP NOT NULL,
                ALTER COLUMN "Instructions" TYPE varchar(10000), ALTER COLUMN "Description" TYPE varchar(2000),
                ALTER COLUMN "Difficulty" DROP DEFAULT, ALTER COLUMN "Difficulty" TYPE integer,
                ALTER COLUMN "Status" DROP DEFAULT, ALTER COLUMN "Status" TYPE integer;
            DO $migration$ DECLARE item record; BEGIN
                FOR item IN SELECT * FROM srs_identity_triggers LOOP
                    EXECUTE item.definition;
                END LOOP;
            END $migration$;
            """);
    }
}
