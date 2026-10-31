import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import vm from 'node:vm'
import test from 'node:test'
import ts from 'typescript'

const require = createRequire(import.meta.url)

function fixture(environment) {
  let schemaLoads = 0
  function load(file) {
    const exports = {}
    const code = ts.transpileModule(readFileSync(new URL(`../src/lib/${file}`, import.meta.url), 'utf8'), {
      compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
    }).outputText
    const customRequire = (name) => {
      if (name === 'zod/mini') return require('zod/mini')
      if (name === './problem-details-schema') {
        schemaLoads += 1
        return load('api/problem-details-schema.ts')
      }
      throw new Error(`Unexpected dependency: ${name}`)
    }
    vm.runInNewContext(`(function(require,exports){${code}\n})`, {
      URL,
      process: { env: { NEXT_PUBLIC_API_BASE_URL: environment } },
    })(customRequire, exports)
    return exports
  }
  return { load, schemaLoads: () => schemaLoads }
}

test('API error schema loads on the error path and preserves validation, defaults and safe fallback', async () => {
  const h = fixture()
  const { readProblemDetails, ApiProblem } = h.load('api/problem-details.ts')
  assert.equal(h.schemaLoads(), 0)
  const valid = await readProblemDetails(
    Response.json(
      { title: 'Invalid input', status: 422, errors: { Title: ['Required'] }, extra: 'strip' },
      { status: 422 },
    ),
  )
  assert.ok(valid instanceof ApiProblem)
  assert.equal(h.schemaLoads(), 1)
  assert.equal(valid.problem.code, 'HTTP_ERROR')
  assert.deepEqual(valid.problem.errors, { Title: ['Required'] })
  assert.equal(Object.hasOwn(valid.problem, 'extra'), false)
  for (const value of [
    { title: 'Bad', status: '500' },
    { title: 'Bad', status: 500, errors: { field: [1] } },
  ]) {
    const invalid = await readProblemDetails(
      Response.json(value, { status: 500, statusText: 'Internal error' }),
    )
    assert.equal(invalid.problem.title, 'Internal error')
    assert.equal(invalid.problem.status, 500)
    assert.equal(invalid.problem.errors, undefined)
  }
})

test('public API URL retains its default, absolute and root-relative validation without loading Zod', () => {
  for (const value of [undefined, '/api/v1', 'https://api.example.test/v1']) {
    const h = fixture(value)
    assert.equal(h.load('env.ts').publicEnvironment.NEXT_PUBLIC_API_BASE_URL, value ?? '/api/v1')
    assert.equal(h.schemaLoads(), 0)
  }
  for (const value of ['', 'not a URL']) {
    assert.throws(() => fixture(value).load('env.ts'), /absolute URL or root-relative path/)
  }
})
