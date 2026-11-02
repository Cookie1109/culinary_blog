import assert from 'node:assert/strict'
import test from 'node:test'
import { readFileSync } from 'node:fs'
import ts from 'typescript'
import vm from 'node:vm'

test('public media rechecks backend visibility instead of returning cached private bytes', async () => {
  const source = readFileSync(
    new URL('../src/app/api/public-media/[imageId]/[variant]/route.ts', import.meta.url),
    'utf8',
  )
  const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS } }).outputText
  const exports = {}
  let visible = true
  const requests = []
  vm.runInNewContext(`(function(require,exports){${code}\n})`, {
    process: { env: {} },
    Response,
    Set,
    encodeURIComponent,
    fetch: async (_url, options) => {
      requests.push(options)
      return new Response(visible ? 'image' : null, { status: visible ? 200 : 404 })
    },
  })(() => ({ PUBLIC_CONTENT_TAG: 'public-content' }), exports)
  const context = { params: Promise.resolve({ imageId: 'fixture', variant: 'original' }) }
  const first = await exports.GET(null, context)
  assert.equal(first.status, 200)
  assert.match(first.headers.get('cache-control'), /no-store/)
  assert.equal(requests[0].cache, 'no-store')
  visible = false
  assert.equal((await exports.GET(null, context)).status, 404)
  assert.equal(requests.length, 2)
})
