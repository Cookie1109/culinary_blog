import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('P8-12: browser refresh tokens stay in sessionStorage and out of Auth.js JWT state', async () => {
  const store = await readFile(new URL('../src/lib/api/session-token-store.ts', import.meta.url), 'utf8')
  const auth = await readFile(new URL('../src/auth.ts', import.meta.url), 'utf8')
  const types = await readFile(new URL('../src/types/next-auth.d.ts', import.meta.url), 'utf8')

  assert.match(store, /window\.sessionStorage\.setItem\(REFRESH_TOKEN_KEY, session\.refreshToken\)/)
  assert.doesNotMatch(store, /localStorage/)
  assert.match(types, /BackendAccessSession/)
  assert.doesNotMatch(types, /backend\?: BackendSession/)
  assert.match(auth, /withoutRefreshToken\(backendSession\)/)
  assert.doesNotMatch(auth, /token\.backend\.refreshToken/)
})

test('P8-12: application and production edge define the required security headers and TLS floor', async () => {
  const nextConfig = await readFile(new URL('../next.config.mjs', import.meta.url), 'utf8')
  const nginx = await readFile(new URL('../../infra/nginx/nginx.production.conf', import.meta.url), 'utf8')

  for (const header of [
    'Content-Security-Policy',
    'Referrer-Policy',
    'X-Content-Type-Options',
    'X-Frame-Options',
    'Permissions-Policy',
    'Cross-Origin-Opener-Policy',
  ]) {
    assert.match(nextConfig, new RegExp(header))
  }
  assert.match(nextConfig, /poweredByHeader: false/)
  assert.match(nginx, /ssl_protocols TLSv1\.2 TLSv1\.3/)
  assert.match(nginx, /Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"/)
  assert.match(nginx, /return 308 https:\/\/\$host\$request_uri/)
  assert.match(nginx, /proxy_hide_header Content-Security-Policy/)
})

test('P8-11/P8-14/P8-15: CI gates source, image, license, secret, dependency, and ZAP findings', async () => {
  const ci = await readFile(new URL('../../.github/workflows/ci.yml', import.meta.url), 'utf8')
  const [dynamic, release] = await Promise.all([
    readFile(new URL('../../.github/workflows/security-dynamic.yml', import.meta.url), 'utf8'),
    readFile(new URL('../../.github/workflows/release-images.yml', import.meta.url), 'utf8'),
  ])

  assert.match(ci, /gitleaks\/gitleaks:v8\.30\.1/)
  assert.doesNotMatch(ci, /ignore-unfixed: true/)
  assert.match(ci, /scanners: vuln,secret,misconfig/)
  assert.match(ci, /scanners: license/)
  assert.match(ci, /TRIVY_IGNORED_LICENSES: LGPL-3\.0-or-later/)
  assert.match(ci, /Apache-2\.0 AND LGPL-3\.0-or-later AND MIT/)
  assert.match(ci, /Apache-2\.0 AND LGPL-3\.0-or-later/)
  assert.doesNotMatch(ci, /^\s+ignored-licenses:/m)
  assert.match(ci, /image-ref: culinary-blog-api:local/)
  assert.match(ci, /image-ref: culinary-blog-web:local/)
  assert.match(ci, /dependency-review-action/)
  assert.match(dynamic, /zap-baseline\.py/)
  assert.match(dynamic, /target_url must use HTTPS/)
  assert.match(dynamic, /api\/v1\/release/)
  assert.match(dynamic, /apiImageDigest/)
  assert.match(dynamic, /evidence\.json/)
  assert.match(dynamic, /artifact-digest/)
  assert.match(dynamic, /manual triage is required before release sign-off/)
  assert.match(release, /docker\/build-push-action@v6/)
  assert.match(release, /tags: \$\{\{ steps\.image\.outputs\.name \}\}:\$\{\{ github\.sha \}\}/)
  assert.match(release, /actions\/attest-build-provenance@v3/)
  assert.match(release, /subject-digest: \$\{\{ steps\.build\.outputs\.digest \}\}/)
})
