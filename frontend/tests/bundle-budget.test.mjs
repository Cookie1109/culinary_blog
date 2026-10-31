import assert from 'node:assert/strict'
import { randomBytes } from 'node:crypto'
import test from 'node:test'
import { firstLoadJavaScript } from '../scripts/bundle-budget.mjs'

test('first-load budget includes every ancestor layout and deduplicates shared JS, excluding CSS', () => {
  const files = {
    'shared.js': randomBytes(100),
    'root.js': randomBytes(150),
    'group.js': randomBytes(200),
    'error.js': randomBytes(50),
    'missing.js': randomBytes(60),
    'loading.js': randomBytes(70),
  }
  const pages = {
    '/layout': ['shared.js', 'root.js', 'style.css'],
    '/(private)/layout': ['shared.js', 'group.js'],
    '/(private)/editor/page': ['shared.js'],
    '/error': ['error.js'],
    '/not-found': ['missing.js'],
    '/(private)/loading': ['loading.js'],
    '/global-error': ['error.js'],
    '/api/route': ['server.js'],
  }
  const [result] = firstLoadJavaScript(pages, (name) => files[name])
  assert.equal(result.rawBytes, 630)
  assert.ok(result.gzipBytes > 630)
})

test('a route cannot hide an over-budget layout or pass with missing artifacts', () => {
  const pages = { '/layout': ['large.js'], '/page': ['page.js'] }
  assert.ok(
    firstLoadJavaScript(pages, (name) =>
      name === 'large.js' ? randomBytes(205 * 1024) : Buffer.from('page'),
    )[0].gzipBytes >
      200 * 1024,
  )
  assert.throws(() => firstLoadJavaScript(pages, () => undefined))
  assert.throws(() => firstLoadJavaScript({}, () => Buffer.from('')))
})
