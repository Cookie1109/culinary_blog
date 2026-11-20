import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import test from 'node:test'
import vm from 'node:vm'
import ts from 'typescript'
import React, { act } from 'react'
import { createRoot } from 'react-dom/client'
import { JSDOM } from 'jsdom'

function route(fetch) {
  const exports = {}
  const code = ts.transpileModule(
    readFileSync(new URL('../src/app/api/recipe-events/route.ts', import.meta.url), 'utf8'),
    { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } },
  ).outputText
  vm.runInNewContext(`(function(require,exports){${code}\n})`, {
    fetch,
    Response,
    ReadableStream,
    AbortController,
    TextDecoder,
    TextEncoder,
    process,
    URL,
    setTimeout,
  })(() => ({}), exports)
  return { get: exports.GET }
}

test('SSE proxy preserves split frames, change events and heartbeat comments without buffering', async () => {
  const encoder = new TextEncoder()
  const h = route(
    async () =>
      new Response(
        new ReadableStream({
          start(controller) {
            for (const frame of [
              'event: con',
              'nected\ndata: {}\n\n',
              ': heartbeat\n\n',
              'event: changed\ndata: {}\n\n',
            ])
              controller.enqueue(encoder.encode(frame))
            controller.close()
          },
        }),
      ),
  )
  const response = await h.get(new Request('http://localhost/api/recipe-events'))
  assert.equal(response.headers.get('content-type'), 'text/event-stream')
  assert.match(await response.text(), /event: connected[\s\S]*event: changed/)
})

test('client coalesces changes, refreshes lists, preserves editor drafts, and closes on navigation/unmount', async () => {
  const dom = new JSDOM('<div id="root"></div>', { url: 'http://localhost/recipes' })
  const original = new Map(
    ['window', 'document', 'EventSource', 'IS_REACT_ACT_ENVIRONMENT'].map((key) => [
      key,
      Object.getOwnPropertyDescriptor(globalThis, key),
    ]),
  )
  const streams = []
  class FakeEvents extends dom.window.EventTarget {
    constructor() {
      super()
      streams.push(this)
    }

    close() {
      this.closed = true
    }
  }
  Object.assign(globalThis, {
    window: dom.window,
    document: dom.window.document,
    EventSource: FakeEvents,
    IS_REACT_ACT_ENVIRONMENT: true,
  })
  let refreshes = 0
  const invalidations = []
  const client = { invalidateQueries: async (options) => invalidations.push(options) }
  const router = {
    refresh: () => {
      refreshes += 1
    },
  }
  const exports = {}
  const code = ts.transpileModule(
    readFileSync(new URL('../src/components/recipe-realtime.tsx', import.meta.url), 'utf8'),
    {
      compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
    },
  ).outputText
  vm.runInThisContext(`(function(require,exports){${code}\n})`)((name) => {
    if (name === '@tanstack/react-query') return { useQueryClient: () => client }
    if (name === 'next/navigation')
      return { useRouter: () => router, usePathname: () => dom.window.location.pathname }
    if (name === 'react') return React
    throw new Error(`Unexpected dependency: ${name}`)
  }, exports)
  const root = createRoot(document.getElementById('root'))
  const emit = () => streams.at(-1).dispatchEvent(new dom.window.Event('changed'))
  const settle = () =>
    new Promise((resolve) => {
      setTimeout(resolve, 250)
    })
  try {
    await act(async () => root.render(React.createElement(exports.RecipeRealtime)))
    emit()
    emit()
    emit()
    await settle()
    assert.equal(refreshes, 1)
    assert.equal(invalidations.length, 1)
    assert.equal(invalidations[0].predicate({ queryKey: ['my-recipes', 'count'] }), true)
    assert.equal(invalidations[0].predicate({ queryKey: ['my-recipe', 'dirty-editor'] }), false)
    dom.window.history.replaceState(null, '', '/dashboard/recipes/recipe-id/edit')
    await act(async () => root.render(React.createElement(exports.RecipeRealtime)))
    assert.equal(streams[0].closed, true)
    emit()
    await settle()
    assert.equal(refreshes, 1)
    assert.equal(invalidations.length, 2)
    emit()
    await act(async () => root.unmount())
    await settle()
    assert.equal(streams.at(-1).closed, true)
    assert.equal(invalidations.length, 2)
  } finally {
    dom.window.close()
    for (const [key, descriptor] of original) {
      if (descriptor) Object.defineProperty(globalThis, key, descriptor)
      else delete globalThis[key]
    }
  }
})

test('SSE proxy refuses upstream failures and releases its upstream connection on cancellation', async () => {
  const failed = route(async () => new Response(null, { status: 503 }))
  assert.equal((await failed.get(new Request('http://localhost/api/recipe-events'))).status, 503)
  let upstreamSignal
  const h = route(async (_url, options) => {
    upstreamSignal = options.signal
    return new Response(
      new ReadableStream({
        start(controller) {
          upstreamSignal.addEventListener('abort', () => controller.close())
        },
      }),
    )
  })
  const response = await h.get(new Request('http://localhost/api/recipe-events'))
  await response.body.cancel()
  assert.equal(upstreamSignal.aborted, true)
})

test('public lifecycle views do not reuse Next.js data-cache snapshots after a realtime refresh', async () => {
  const calls = []
  const exports = {}
  const code = ts.transpileModule(
    readFileSync(new URL('../src/lib/api/public-content-server.ts', import.meta.url), 'utf8'),
    { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 } },
  ).outputText
  vm.runInNewContext(`(function(require,exports){${code}\n})`, {
    process,
    URLSearchParams,
    fetch: async (url, options) => {
      calls.push({ url, options })
      return Response.json({ data: [], meta: { total: 0 } })
    },
  })(() => ({}), exports)
  await exports.getPublicCategories(3600)
  await exports.getPublishedRecipes(1, 12, 3600)
  await exports.getPublishedRecipe('test')
  await exports.getPublishedCategory('test', 1)
  assert.equal(calls.length, 4)
  assert.equal(
    calls.every((call) => call.options.cache === 'no-store'),
    true,
  )
})
