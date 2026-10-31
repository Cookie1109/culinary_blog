#!/bin/sh
set -eu

latest_marker=$(find /backups/postgres -type f -name '*.dump.completed-at' | sort | tail -n 1)
test -n "$latest_marker"
latest_backup=${latest_marker%.completed-at}
sha256sum -c "$latest_backup.sha256"

export PGPASSWORD="$POSTGRES_PASSWORD"
pg_restore \
  --host=postgres-restore \
  --port=5432 \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  --no-owner \
  --no-acl \
  --exit-on-error \
  "$latest_backup"

source_migrations=$(psql --host=postgres --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align \
  --command='select count(*) from "__EFMigrationsHistory";')
restored_migrations=$(psql --host=postgres-restore --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align \
  --command='select count(*) from "__EFMigrationsHistory";')
test "$source_migrations" -gt 0
test "$source_migrations" = "$restored_migrations"

source_recipes=$(psql --host=postgres --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align \
  --command='select count(*) from "Recipes";')
restored_recipes=$(psql --host=postgres-restore --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align \
  --command='select count(*) from "Recipes";')
test "$source_recipes" = "$restored_recipes"

# Stop writers in the isolated source stack before the drill. Concurrent writes
# intentionally fail this comparison rather than silently accepting a partial restore.
table_checksums() {
  psql --host="$1" --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --tuples-only --no-align --set=ON_ERROR_STOP=1 <<'SQL'
SELECT format($query$SELECT %L, count(*), md5(COALESCE(string_agg(row_to_json(t)::text, '' ORDER BY row_to_json(t)::text), '')) FROM %I.%I t;$query$,
  schemaname || '.' || tablename, schemaname, tablename)
FROM pg_tables WHERE schemaname = 'public' ORDER BY tablename
\gexec
SQL
}
source_checksums=$(table_checksums postgres)
restored_checksums=$(table_checksums postgres-restore)
test "$source_checksums" = "$restored_checksums"

schema_dump() {
  pg_dump --host="$1" --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" \
    --schema-only --schema=public --no-owner --no-acl |
    sed '/^--/d; /^\\restrict /d; /^\\unrestrict /d; /^[[:space:]]*$/d'
}
test "$(schema_dump postgres)" = "$(schema_dump postgres-restore)"

printf 'PostgreSQL restore verified: migrations=%s recipes=%s backup=%s\n' \
  "$restored_migrations" "$restored_recipes" "$latest_backup"
