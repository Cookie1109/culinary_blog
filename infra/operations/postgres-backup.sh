#!/bin/sh
set -eu

backup_root=/backups/postgres
timestamp=$(date -u +%Y%m%dT%H%M%SZ)
partial_path="$backup_root/$timestamp.dump.partial"
backup_path="$backup_root/$timestamp.dump"

mkdir -p "$backup_root"
export PGPASSWORD="$POSTGRES_PASSWORD"

pg_dump \
  --host="$POSTGRES_HOST" \
  --port="${POSTGRES_PORT:-5432}" \
  --username="$POSTGRES_USER" \
  --dbname="$POSTGRES_DB" \
  --format=custom \
  --compress=9 \
  --no-owner \
  --no-acl \
  --file="$partial_path"

pg_restore --list "$partial_path" >/dev/null
mv "$partial_path" "$backup_path"
sha256sum "$backup_path" >"$backup_path.sha256"
date -u +%s >"$backup_path.completed-at"

find "$backup_root" -type f -mtime "+${BACKUP_RETENTION_DAYS:-30}" -delete
printf 'PostgreSQL backup completed: %s\n' "$backup_path"
