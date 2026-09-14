#!/usr/bin/env sh
set -eu

docker compose up --detach --build --wait

attempt=0
until [ "$attempt" -ge 90 ]; do
  if curl --fail --silent http://localhost:8080/health >/dev/null \
    && curl --fail --silent --head http://localhost:8080/ \
      | grep -qi '^content-security-policy:'; then
    echo 'Smoke test passed: health and homepage are available with CSP enabled.'
    exit 0
  fi
  attempt=$((attempt + 1))
  sleep 2
done

docker compose ps
docker compose logs --tail 100 api web nginx
exit 1
