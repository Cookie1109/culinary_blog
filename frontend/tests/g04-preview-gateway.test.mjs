import assert from 'node:assert/strict'
import { createServer } from 'node:http'
import test from 'node:test'
import { createPreviewGateway } from '../scripts/g04-preview-gateway.mjs'

test('preview gateway exposes recipe/assets and rejects writes, private paths, traversal and credential forwarding', async () => {
  const received = []
  const upstream = createServer((request, response) => {
    received.push({ path: request.url, headers: request.headers })
    response.setHeader('Content-Type', 'application/json')
    response.end(JSON.stringify({ path: request.url }))
  })
  await new Promise((resolve) => {
    upstream.listen(0, '127.0.0.1', resolve)
  })
  const target = `http://127.0.0.1:${upstream.address().port}`
  const gateway = createPreviewGateway({
    upstream: () => target,
    apiUpstream: () => target,
    publicOrigin: () => 'https://preview.example',
  })
  await new Promise((resolve) => {
    gateway.listen(0, '127.0.0.1', resolve)
  })
  const base = `http://127.0.0.1:${gateway.address().port}`
  try {
    const result = await fetch(`${base}/recipes/pho-bo?v=1`, {
      headers: {
        Cookie: 'session=private',
        Authorization: 'Bearer private',
        'X-Middleware-Subrequest': 'middleware',
      },
    })
    assert.equal(result.status, 200)
    assert.deepEqual(await result.json(), { path: '/recipes/pho-bo?v=1' })
    assert.equal(received[0].headers.cookie, undefined)
    assert.equal(received[0].headers.authorization, undefined)
    assert.equal(received[0].headers['x-middleware-subrequest'], undefined)
    for (const path of [
      '/_next/static/chunks/app/(public)/recipes/%5Bslug%5D/page-123.js',
      '/api/public-media/11111111-1111-4111-8111-111111111111/original',
      '/opengraph-image',
      '/api/v1/recipes?page=1&pageSize=12',
      '/api/v1/recipes/pho-bo',
      '/api/v1/categories',
    ]) {
      const asset = await fetch(base + path)
      assert.equal(asset.status, 200)
      await asset.body?.cancel()
    }
    const count = received.length
    for (const path of [
      '/auth/login',
      '/api/auth/session',
      '/api/v1/me/recipes',
      '/api/private-media/x/original',
      '/jobs',
      '/openapi/v1.json',
      '/.env',
      '/_next/static/%2f..%2f.env',
    ]) {
      const blocked = await fetch(base + path)
      assert.ok([400, 404].includes(blocked.status), path)
      await blocked.body?.cancel()
    }
    for (const method of ['POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS']) {
      assert.equal((await fetch(`${base}/recipes/pho-bo`, { method })).status, 405)
    }
    assert.equal(received.length, count)
    const robots = await (await fetch(`${base}/robots.txt`)).text()
    assert.ok(robots.includes('Disallow: /\nAllow: /recipes/'))
  } finally {
    gateway.closeAllConnections()
    upstream.closeAllConnections()
    await Promise.all([
      new Promise((resolve) => {
        gateway.close(resolve)
      }),
      new Promise((resolve) => {
        upstream.close(resolve)
      }),
    ])
  }
})

test('preview gateway rewrites trusted recipe redirects and blocks redirects outside its recipe scope', async () => {
  const upstream = createServer((request, response) => {
    response
      .writeHead(301, {
        Location:
          request.url === '/recipes/old-slug'
            ? '/recipes/current-slug'
            : 'https://outside.example/auth/login',
      })
      .end()
  })
  await new Promise((resolve) => {
    upstream.listen(0, '127.0.0.1', resolve)
  })
  const gateway = createPreviewGateway({
    upstream: () => `http://127.0.0.1:${upstream.address().port}`,
    publicOrigin: () => 'https://preview.example',
  })
  await new Promise((resolve) => {
    gateway.listen(0, '127.0.0.1', resolve)
  })
  const base = `http://127.0.0.1:${gateway.address().port}`
  try {
    const redirect = await fetch(`${base}/recipes/old-slug`, { redirect: 'manual' })
    assert.equal(redirect.status, 301)
    assert.equal(redirect.headers.get('location'), 'https://preview.example/recipes/current-slug')
    assert.equal((await fetch(`${base}/recipes/escape`, { redirect: 'manual' })).status, 502)
  } finally {
    gateway.closeAllConnections()
    upstream.closeAllConnections()
    await Promise.all([
      new Promise((resolve) => {
        gateway.close(resolve)
      }),
      new Promise((resolve) => {
        upstream.close(resolve)
      }),
    ])
  }
})
