import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import test from 'node:test'

function inspectConfig(environment) {
  const env = { ...process.env, ...environment }
  if (environment.INTERNAL_API_BASE_URL === null) delete env.INTERNAL_API_BASE_URL
  return spawnSync(
    process.execPath,
    [
      '--input-type=module',
      '-e',
      "const {default:config}=await import('./next.config.mjs'); console.log(JSON.stringify({rewrites:await config.rewrites(),headers:await config.headers()}))",
    ],
    { cwd: new URL('../', import.meta.url), env, encoding: 'utf8' },
  )
}

function apiRewrites(result) {
  assert.equal(result.status, 0, result.stderr)
  const { rewrites } = JSON.parse(result.stdout.trim())
  return (Array.isArray(rewrites) ? rewrites : Object.values(rewrites).flat()).filter(
    (route) => route.source === '/api/v1/:path*',
  )
}

test('Vercel production forwards the same-origin API to the configured HTTPS backend', () => {
  const result = inspectConfig({
    NODE_ENV: 'production',
    VERCEL: '1',
    INTERNAL_API_BASE_URL: 'https://api.example.test/api/v1/',
  })
  assert.deepEqual(apiRewrites(result), [
    { source: '/api/v1/:path*', destination: 'https://api.example.test/api/v1/:path*' },
  ])
  const rules = JSON.parse(result.stdout.trim()).headers
  const policy = rules
    .flatMap((rule) => rule.headers)
    .find((header) => header.key === 'Content-Security-Policy')
  assert.match(policy.value, /connect-src 'self'(?:;|$)/)
})

test('Docker production keeps API forwarding in the existing Nginx layer', () => {
  assert.deepEqual(
    apiRewrites(
      inspectConfig({ NODE_ENV: 'production', VERCEL: '0', INTERNAL_API_BASE_URL: 'http://api:8080/api/v1' }),
    ),
    [],
  )
})

test('development retains the existing local API proxy', () => {
  assert.deepEqual(
    apiRewrites(inspectConfig({ NODE_ENV: 'development', VERCEL: '0', INTERNAL_API_BASE_URL: null })),
    [{ source: '/api/v1/:path*', destination: 'http://localhost:5000/api/v1/:path*' }],
  )
})

test('Vercel fails closed for missing, relative, insecure or credential-bearing destinations', () => {
  for (const endpoint of [
    null,
    '/api/v1',
    'http://api.example.test/api/v1',
    'https://name@api.example.test/api/v1',
    'https://api.example.test/api/v1?x=1',
    'https://api.example.test/api/v1#fragment',
  ]) {
    const result = inspectConfig({ NODE_ENV: 'production', VERCEL: '1', INTERNAL_API_BASE_URL: endpoint })
    assert.notEqual(result.status, 0)
    assert.match(result.stderr, /INTERNAL_API_BASE_URL|Invalid URL/)
  }
})
