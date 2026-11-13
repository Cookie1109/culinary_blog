import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import vm from 'node:vm'
import test from 'node:test'
import React, { act } from 'react'
import { JSDOM } from 'jsdom'
import ts from 'typescript'

const require = createRequire(import.meta.url)

async function setup() {
  const dom = new JSDOM('<div id="root"></div>', { url: 'http://localhost/profile' })
  for (const key of ['window', 'document', 'HTMLElement', 'Node', 'Event']) globalThis[key] = dom.window[key]
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  const { createRoot } = await import('react-dom/client')
  const root = createRoot(document.getElementById('root'))
  let refresh
  let token = 'old-refresh'
  let calls = 0
  let release
  let fail = false
  const events = []
  const session = {
    accessToken: 'access',
    accessTokenExpiresAt: new Date(Date.now() + 900_000).toISOString(),
    backendUser: { id: 'owner', displayName: 'Author', roles: ['Author'] },
  }
  const update = async () => session
  const auth = {
    useSession: () => ({ data: session, status: 'authenticated', update }),
    signIn: async () => {
      events.push('sign-in')
      return {}
    },
    signOut: async () => {
      events.push('sign-out')
    },
  }
  const api = {
    rotateBackendSession: async () => {
      calls += 1
      await new Promise((resolve) => {
        release = resolve
      })
      if (fail) throw new Error('expired refresh')
      return {
        refreshToken: 'rotated-refresh',
        accessToken: 'new-access',
        expiresAt: session.accessTokenExpiresAt,
      }
    },
    revokeBackendSession: async (value) => {
      events.push(['revoke', value])
    },
  }
  const store = {
    readRefreshToken: () => token,
    storeRefreshToken: (value) => {
      token = value.refreshToken
    },
    clearRefreshToken: () => {
      token = null
    },
  }
  const exports = {}
  const js = ts.transpileModule(
    readFileSync(new URL('../src/features/culinary/contexts/AuthContext.tsx', import.meta.url), 'utf8'),
    {
      compilerOptions: {
        target: ts.ScriptTarget.ES2020,
        module: ts.ModuleKind.CommonJS,
        jsx: ts.JsxEmit.ReactJSX,
        esModuleInterop: true,
      },
    },
  ).outputText
  vm.runInThisContext(`(function(require,exports){${js}\n})`)((name) => {
    if (name === 'next-auth/react') return auth
    if (name === '@tanstack/react-query')
      return { useQueryClient: () => ({ removeQueries: () => events.push('clear-queries') }) }
    if (name.endsWith('auth-client')) return api
    if (name.endsWith('session-token-store')) return store
    if (name.endsWith('axios'))
      return {
        configureSessionRefresh: (value) => {
          refresh = value
        },
      }
    if (name === 'react') return React
    if (name === 'react/jsx-runtime') return require('react/jsx-runtime')
    throw new Error(`Unexpected AuthProvider dependency: ${name}`)
  }, exports)
  let context
  function Probe() {
    context = exports.useAuth()
    return null
  }
  await act(async () =>
    root.render(React.createElement(exports.AuthProvider, null, React.createElement(Probe))),
  )
  return {
    refresh: () => refresh(),
    context: () => context,
    events,
    calls: () => calls,
    token: () => token,
    release: () => release(),
    fail: () => {
      fail = true
    },
    async close() {
      await act(async () => root.unmount())
      dom.window.close()
    },
  }
}

test('concurrent expired-access retries rotate once and logout revokes the latest token', async () => {
  const app = await setup()
  try {
    const first = app.refresh()
    const second = app.refresh()
    assert.equal(first, second)
    assert.equal(app.calls(), 1)
    const logout = app.context().logout()
    assert.deepEqual(app.events, [])
    app.release()
    await act(async () => {
      await Promise.all([first, second, logout])
    })
    assert.equal(app.calls(), 1)
    assert.deepEqual(app.events, ['sign-in', ['revoke', 'rotated-refresh'], 'sign-out', 'clear-queries'])
    assert.equal(app.token(), null)
  } finally {
    await app.close()
  }
})

test('failed refresh clears the tab and ends the session without an infinite retry', async () => {
  const app = await setup()
  try {
    app.fail()
    const pending = app.refresh()
    app.release()
    assert.equal(await pending, null)
    assert.equal(await app.refresh(), null)
    assert.equal(app.calls(), 1)
    assert.equal(app.token(), null)
    assert.deepEqual(app.events, ['sign-out'])
  } finally {
    await app.close()
  }
})
