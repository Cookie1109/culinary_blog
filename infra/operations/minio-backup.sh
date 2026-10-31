#!/bin/sh
set -eu

# Fail before publishing a snapshot if the tool image cannot create its manifest.
command -v find >/dev/null
command -v sha256sum >/dev/null

backup_root=/backups/minio
timestamp=$(date -u +%Y%m%dT%H%M%SZ)
partial_path="$backup_root/$timestamp.partial"
backup_path="$backup_root/$timestamp"

mkdir -p "$backup_root"
mc alias set source "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null
case "$(mc version info "source/$MINIO_BUCKET")" in
  *enabled*) ;;
  *) echo "Bucket versioning is not enabled" >&2; exit 1 ;;
esac

mkdir "$partial_path"
date -u +%s >"$backup_path.started-at"
mc mirror --overwrite --preserve "source/$MINIO_BUCKET" "$partial_path"
(cd "$partial_path" && find . -type f -exec sha256sum {} \;) >"$backup_path.sha256"
mv "$partial_path" "$backup_path"
date -u +%s >"$backup_path.completed-at"

set -- "$backup_root"/????????T??????Z
if test -d "$1"; then
  cutoff_marker="$backup_root/.retention-cutoff"
  touch -d "${BACKUP_RETENTION_DAYS:-30} days ago" "$cutoff_marker"
  for snapshot; do
    if test "$snapshot" -ot "$cutoff_marker"; then
      rm -rf -- "$snapshot" "$snapshot.started-at" "$snapshot.completed-at" "$snapshot.sha256"
    fi
  done
  rm -f "$cutoff_marker"
fi
printf 'MinIO snapshot completed: %s\n' "$backup_path"
