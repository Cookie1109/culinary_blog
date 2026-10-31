// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CulinaryBlog.Infrastructure.Persistence.Migrations;

/// <summary>Applies the SharedRateLimitFallback schema change without changing its migration identity.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20261025053000_SharedRateLimitFallback")]
public sealed class SharedRateLimitFallback : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "SharedRateLimitState" (
                "Peer" text PRIMARY KEY,
                "GlobalWindow" bigint NOT NULL DEFAULT -1,
                "GlobalCount" integer NOT NULL DEFAULT 0,
                "UploadWindow" bigint NOT NULL DEFAULT -1,
                "UploadCount" integer NOT NULL DEFAULT 0,
                "AuthEvents" bigint[] NOT NULL DEFAULT '{}',
                "ExpiresAtMilliseconds" bigint NOT NULL DEFAULT 0);
            CREATE INDEX "IX_SharedRateLimitState_ExpiresAt" ON "SharedRateLimitState" ("ExpiresAtMilliseconds");
            CREATE FUNCTION "ReserveRateLimit"(p_ip text, p_policy text, p_global integer, p_endpoint integer)
            RETURNS TABLE ("Allowed" boolean, "Limit" integer, "Remaining" integer,
                "Reset" bigint, "RetryAfter" integer, "NowMilliseconds" bigint)
            LANGUAGE plpgsql AS $$
            DECLARE
                r "SharedRateLimitState"%ROWTYPE;
                v_now bigint;
                v_window bigint;
                v_reset bigint;
                v_endpoint_reset bigint;
                v_count integer;
                v_limit integer := p_global;
                v_remaining integer;
                v_endpoint_remaining integer;
                v_allowed boolean;
            BEGIN
                INSERT INTO "SharedRateLimitState" ("Peer") VALUES (p_ip) ON CONFLICT DO NOTHING;
                SELECT * INTO r FROM "SharedRateLimitState" s WHERE s."Peer" = p_ip FOR UPDATE;
                v_now := floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint;
                v_window := v_now / 60000;
                v_reset := (v_window + 1) * 60000;
                r."GlobalCount" := CASE WHEN r."GlobalWindow" = v_window THEN least(r."GlobalCount" + 1, p_global + 1) ELSE 1 END;
                r."GlobalWindow" := v_window;
                v_remaining := greatest(0, p_global - r."GlobalCount");
                v_allowed := r."GlobalCount" <= p_global;
                r."AuthEvents" := ARRAY(SELECT e FROM unnest(r."AuthEvents") e WHERE e > v_now - 60000 ORDER BY e);
                IF p_policy <> '' AND v_allowed THEN
                    v_endpoint_reset := v_reset;
                    IF p_policy = 'auth' THEN
                        IF cardinality(r."AuthEvents") < p_endpoint THEN
                            r."AuthEvents" := array_append(r."AuthEvents", v_now);
                        ELSE
                            v_allowed := false;
                        END IF;
                        v_count := cardinality(r."AuthEvents");
                        v_endpoint_reset := r."AuthEvents"[1] + 60000;
                    ELSE
                        r."UploadCount" := CASE WHEN r."UploadWindow" = v_window THEN least(r."UploadCount" + 1, p_endpoint + 1) ELSE 1 END;
                        r."UploadWindow" := v_window;
                        v_count := r."UploadCount";
                        v_allowed := v_count <= p_endpoint;
                    END IF;
                    v_endpoint_remaining := greatest(0, p_endpoint - v_count);
                    IF v_endpoint_remaining <= v_remaining THEN
                        v_limit := p_endpoint;
                        v_remaining := v_endpoint_remaining;
                        v_reset := v_endpoint_reset;
                    END IF;
                END IF;
                UPDATE "SharedRateLimitState" s SET
                    "GlobalWindow" = r."GlobalWindow", "GlobalCount" = r."GlobalCount",
                    "UploadWindow" = r."UploadWindow", "UploadCount" = r."UploadCount",
                    "AuthEvents" = r."AuthEvents", "ExpiresAtMilliseconds" = v_now + 120000
                    WHERE s."Peer" = p_ip;
                RETURN QUERY SELECT v_allowed, v_limit, v_remaining,
                    ceil(v_reset / 1000.0)::bigint,
                    greatest(1, ceil((v_reset - v_now) / 1000.0)::integer), v_now;
            END;
            $$;
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP FUNCTION "ReserveRateLimit"(text, text, integer, integer);
        DROP TABLE "SharedRateLimitState";
        """);
}
