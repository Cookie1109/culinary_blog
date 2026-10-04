#!/bin/sh
set -eu

latest_backup=$(find /backups/postgres -type f -name '*.dump' | sort | tail -n 1)
test -n "$latest_backup"
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

printf 'PostgreSQL restore verified: migrations=%s recipes=%s backup=%s\n' \
  "$restored_migrations" "$restored_recipes" "$latest_backup"
