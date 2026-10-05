import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const rootFile = (path) => readFile(new URL(`../../${path}`, import.meta.url), 'utf8')

test('Docker frontend development uses bind-mounted source and drive-local caches without rebuilding', async () => {
  const [compose, gitignore, readme, warmup, nginx, productionNginx, nextConfig] = await Promise.all([
    rootFile('compose.dev.yaml'),
    rootFile('.gitignore'),
    rootFile('README.md'),
    rootFile('frontend/scripts/warm-dev-routes.mjs'),
    rootFile('infra/nginx/nginx.conf'),
    rootFile('infra/nginx/nginx.production.conf'),
    rootFile('frontend/next.config.mjs'),
  ])

  assert.match(compose, /build:\s*!reset null/)
  assert.match(compose, /\.\/frontend:\/workspace\/frontend/)
  assert.match(compose, /\.\/\.docker-dev\/frontend-node-modules:\/workspace\/frontend\/node_modules/)
  assert.match(compose, /\.\/\.docker-dev\/frontend-next:\/workspace\/frontend\/\.next/)
  assert.match(compose, /\.\/\.docker-dev\/npm-cache:\/root\/\.npm/)
  assert.match(compose, /WATCHPACK_POLLING:\s*"true"/)
  assert.match(compose, /node scripts\/warm-dev-routes\.mjs &/)
  assert.match(compose, /max-size:\s*"10m"/)
  assert.match(warmup, /warmRoute\('\/recipes'\)/)
  assert.match(warmup, /warmRoute\('\/categories'\)/)
  assert.match(warmup, /warmRoute\(`\/recipes\/\$\{encodeURIComponent\(slug\)\}`\)/)
  assert.match(nginx, /proxy_set_header Upgrade \$http_upgrade;/)
  assert.match(nginx, /proxy_set_header Connection \$connection_upgrade;/)
  assert.match(nginx, /script-src 'self' 'unsafe-inline' 'unsafe-eval'/)
  assert.match(nginx, /connect-src 'self' ws: wss:/)
  assert.doesNotMatch(productionNginx, /unsafe-eval/)
  assert.match(nextConfig, /process\.env\.NODE_ENV === 'development'/)
  assert.match(gitignore, /\/\.docker-dev\//)
  assert.match(readme, /compose\.dev\.yaml up --detach --no-build --wait/)
})
