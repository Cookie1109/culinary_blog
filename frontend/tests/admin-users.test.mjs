import assert from 'node:assert/strict'
import test from 'node:test'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import vm from 'node:vm'
import ts from 'typescript'
import { JSDOM } from 'jsdom'
import React, { act } from 'react'
import * as query from '@tanstack/react-query'

const require = createRequire(import.meta.url)
const source = new URL('../src/features/culinary/pages/dashboard/UsersAdmin.tsx', import.meta.url)
const fixture = {
  id: 'fixture-user',
  displayName: 'Người thử nghiệm',
  email: 'fixture@users.test',
  roles: ['Admin', 'Author'],
  isActive: true,
  createdAt: '2026-11-02T00:00:00Z',
}

function load(file, mocks) {
  const module = { exports: {} }
  const code = ts.transpileModule(readFileSync(file, 'utf8'), {
    compilerOptions: {
      module: ts.ModuleKind.CommonJS,
      jsx: ts.JsxEmit.ReactJSX,
      target: ts.ScriptTarget.ES2022,
    },
  }).outputText
  vm.runInNewContext(code, {
    module,
    exports: module.exports,
    require: (name) => mocks[name] ?? require(name),
    Intl,
    Date,
    URLSearchParams,
  })
  return module.exports
}

async function setup(role = 'admin') {
  const dom = new JSDOM('<!doctype html><html><body><div id="root"></div></body></html>', {
    url: 'http://localhost/dashboard/users',
  })
  for (const key of ['window', 'document', 'HTMLElement', 'Element', 'Node', 'Event', 'MouseEvent'])
    globalThis[key] = dom.window[key]
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  const { createRoot } = await import('react-dom/client')
  const root = createRoot(document.getElementById('root'))
  const client = new query.QueryClient({ defaultOptions: { queries: { retry: false } } })
  const calls = []
  let respond = async (filters) => ({
    data: [fixture],
    meta: {
      ...filters,
      total: 25,
      totalPages: 2,
      hasNextPage: filters.page === 1,
      hasPreviousPage: filters.page > 1,
    },
  })
  const { UsersAdmin } = load(source, {
    '@tanstack/react-query': query,
    '../../contexts/AuthContext': { useAuth: () => ({ user: role ? { id: 'admin', role } : null }) },
    '@/lib/api/admin-users-client': {
      listAdminUsers: async (filters) => {
        calls.push(filters)
        return respond(filters)
      },
    },
  })
  async function render() {
    await act(async () => {
      root.render(React.createElement(query.QueryClientProvider, { client }, React.createElement(UsersAdmin)))
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
  }
  async function event(element, name = 'click') {
    await act(async () => {
      element.dispatchEvent(new dom.window.Event(name, { bubbles: true, cancelable: true }))
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
    await act(async () => {
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
  }
  return {
    calls,
    render,
    event,
    respond: (handler) => {
      respond = handler
    },
    change: async (id, value) => {
      const element = document.getElementById(id)
      const prototype =
        element.tagName === 'SELECT'
          ? dom.window.HTMLSelectElement.prototype
          : dom.window.HTMLInputElement.prototype
      Object.getOwnPropertyDescriptor(prototype, 'value').set.call(element, value)
      await event(element, element.tagName === 'SELECT' ? 'change' : 'input')
    },
    close: async () => {
      await act(async () => root.unmount())
      client.clear()
      dom.window.close()
    },
  }
}

test('Admin directory renders roles and filters before pagination; changing filters resets the page', async () => {
  const h = await setup()
  try {
    await h.render()
    assert.match(document.body.textContent, /Người thử nghiệm/)
    assert.match(document.body.textContent, /Quản trị viên, Tác giả/)
    await h.event(
      [...document.querySelectorAll('button')].find((button) => button.textContent === 'Trang sau'),
    )
    assert.equal(h.calls.at(-1).page, 2)
    await h.change('user-role', 'Author')
    assert.equal(h.calls.at(-1).role, 'Author')
    assert.equal(h.calls.at(-1).page, 1)
    await h.change('user-search', '  fixture@users.test  ')
    await h.event(document.querySelector('form'), 'submit')
    assert.equal(h.calls.at(-1).search, 'fixture@users.test')
    await h.change('users-page-size', '50')
    assert.equal(h.calls.at(-1).pageSize, 50)
    assert.equal(h.calls.at(-1).page, 1)
  } finally {
    await h.close()
  }
})

test('Author and guest cannot mount or fetch the administrative directory', async () => {
  for (const role of ['author', null]) {
    const h = await setup(role)
    try {
      await h.render()
      assert.match(document.querySelector('[role="alert"]').textContent, /Chỉ quản trị viên/)
      assert.equal(h.calls.length, 0)
      assert.equal(document.querySelector('table'), null)
    } finally {
      await h.close()
    }
  }
})

test('Directory distinguishes loading, network errors, retry and empty results without rendering server detail', async () => {
  const h = await setup()
  try {
    let reject
    h.respond(
      () =>
        new Promise((_resolve, rejectPromise) => {
          reject = rejectPromise
        }),
    )
    await h.render()
    assert.match(document.querySelector('[role="status"]').textContent, /Đang tải/)
    await act(async () => {
      reject(new Error('private-database-detail'))
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
    assert.match(document.querySelector('[role="alert"]').textContent, /Không thể tải/)
    assert.doesNotMatch(document.body.textContent, /private-database-detail/)
    h.respond(async (filters) => ({
      data: [],
      meta: { ...filters, total: 0, totalPages: 1, hasNextPage: false, hasPreviousPage: false },
    }))
    await h.event([...document.querySelectorAll('button')].find((button) => button.textContent === 'Thử lại'))
    assert.match(document.body.textContent, /Không tìm thấy người dùng phù hợp/)
    assert.equal(document.querySelector('[role="alert"]'), null)
  } finally {
    await h.close()
  }
})

test('Directory client encodes literal search and uses the authenticated no-store request', async () => {
  const calls = []
  const { listAdminUsers } = load(new URL('../src/lib/api/admin-users-client.ts', import.meta.url), {
    './auth-client': {
      authenticatedApiRequest: (...args) => {
        calls.push(args)
      },
    },
  })
  listAdminUsers({ search: '  name+%_@users.test  ', role: 'Author', page: 2, pageSize: 50 })
  const [path, options] = calls[0]
  const url = new URL(path, 'http://localhost')
  assert.equal(url.pathname, '/admin/users')
  assert.equal(url.searchParams.get('search'), 'name+%_@users.test')
  assert.equal(url.searchParams.get('role'), 'Author')
  assert.equal(url.searchParams.get('page'), '2')
  assert.equal(url.searchParams.get('pageSize'), '50')
  assert.equal(options.cache, 'no-store')
})
