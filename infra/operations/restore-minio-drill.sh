#!/bin/sh
set -eu

set -- /backups/minio/????????T??????Z
test -d "$1"
for snapshot; do latest_backup=$snapshot; done
test -f "$latest_backup.completed-at"

mc alias set restored http://minio-restore:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null
mc mb --ignore-existing "restored/$MINIO_BUCKET" >/dev/null
mc anonymous set none "restored/$MINIO_BUCKET" >/dev/null
mc version enable "restored/$MINIO_BUCKET" >/dev/null
mc mirror --overwrite --preserve "$latest_backup" "restored/$MINIO_BUCKET"

source_stats=$(mc du --json "$latest_backup")
source_count=${source_stats#*\"objects\":}
source_count=${source_count%%,*}
restored_stats=$(mc du --json "restored/$MINIO_BUCKET")
restored_count=${restored_stats#*\"objects\":}
restored_count=${restored_count%%,*}
test "$source_count" = "$restored_count"
case "$(mc version info "restored/$MINIO_BUCKET")" in
  *enabled*) ;;
  *) echo "Restored bucket versioning is not enabled" >&2; exit 1 ;;
esac

printf 'MinIO restore verified: objects=%s backup=%s\n' "$restored_count" "$latest_backup"
