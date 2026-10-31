#!/bin/sh
set -eu

# Anchor every run to 03:00 UTC, including after container restarts and retries.
now=$(date -u +%s)
next=$((now / 86400 * 86400 + 10800))
if test "$next" -lt "$now"; then next=$((next + 86400)); fi

while true; do
  now=$(date -u +%s)
  delay=$((next - now))
  if test "$delay" -gt 0; then sleep "$delay"; fi
  until /bin/sh "$1"; do
    sleep "${BACKUP_RETRY_SECONDS:-300}"
  done
  now=$(date -u +%s)
  next=$((next + 86400))
  while test "$next" -le "$now"; do next=$((next + 86400)); done
done
