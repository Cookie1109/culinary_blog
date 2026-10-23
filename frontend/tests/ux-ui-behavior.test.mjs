/* eslint-disable react/prop-types -- Test harness components receive fixtures directly. */
import assert from 'node:assert/strict'
import { existsSync, readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import vm from 'node:vm'
import test from 'node:test'
import ts from 'typescript'
import { JSDOM } from 'jsdom'
import React, { act } from 'react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import * as query from '@tanstack/react-query'

const require = createRequire(import.meta.url)
const sourceRoot = fileURLToPath(new URL('../src/', import.meta.url))
const fixture = {
  id: 'recipe-fixture',
  title: 'Công thức thử nghiệm',
  description: 'Mô tả món ăn',
  instructions: '',
  category: { id: 'cat', name: 'Món ăn', slug: 'mon-an' },
  difficulty: 'easy',
  servings: 4,
  prepTime: 15,
  cookTime: 30,
  nutrition: null,
  ingredients: [],
  steps: [],
  images: [],
  version: 1,
  status: 'draft',
  createdAt: '2026-10-01T00:00:00Z',
  author: { displayName: 'Tác giả' },
}

async function setup({ editing = true } = {}) {
  const dom = new JSDOM(
    '<!doctype html><html><body><button id="trigger">Mở</button><div id="root"></div></body></html>',
    { url: 'http://localhost/dashboard/recipes/recipe-fixture/edit' },
  )
  for (const key of [
    'window',
    'document',
    'HTMLElement',
    'Element',
    'Node',
    'Event',
    'MouseEvent',
    'KeyboardEvent',
  ]) {
    globalThis[key] = dom.window[key]
  }
  globalThis.IS_REACT_ACT_ENVIRONMENT = true
  dom.window.scrollTo = () => {}
  const { createRoot } = await import('react-dom/client')
  const root = createRoot(document.getElementById('root'))
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: Infinity }, mutations: { retry: false } },
  })
  let recipe = structuredClone(fixture)
  let user = {
    id: 'user',
    name: 'Tên đã tải từ phiên',
    email: 'fixture@example.test',
    bio: 'Giới thiệu',
    role: 'admin',
  }
  let search = new URLSearchParams()
  const navigations = []
  const requests = []
  let update = async (_id, _version, payload) => ({
    ...recipe,
    ...payload,
    category: recipe.category,
    version: recipe.version + 1,
  })
  const auth = {
    useAuth: () => ({
      user,
      isLoading: false,
      updateProfile: async () => {},
      logout: async () => {
        requests.push(['logout'])
      },
      login: async () => {},
      register: async () => {},
    }),
  }
  const api = new Proxy(
    {
      listCategories: async () => [recipe.category],
      getAdminRecipe: async () => {
        requests.push(['read-admin'])
        return recipe
      },
      getMyRecipe: async () => recipe,
      updateRecipe: async (...args) => {
        recipe = await update(...args)
        return recipe
      },
      createRecipe: async (payload) => {
        recipe = { ...recipe, ...payload }
        return recipe
      },
      listMyRecipes: async (...args) => {
        requests.push(['mine', ...args])
        return { data: [recipe], meta: { total: 20, totalPages: 2 } }
      },
      listAdminRecipes: async (...args) => {
        requests.push(['all', ...args])
        return { data: [recipe], meta: { total: 20, totalPages: 2 } }
      },
    },
    {
      get: (target, key) =>
        target[key] ??
        (async () => {
          throw new Error(`Unexpected API call: ${String(key)}`)
        }),
    },
  )
  const router = {
    Outlet: () => null,
    Link: ({ to, children, ...props }) => React.createElement('a', { href: to, ...props }, children),
    useNavigate: () => (target) => navigations.push(target),
    useParams: () => (editing ? { id: recipe.id } : {}),
    useSearchParams: () => {
      const [, refresh] = React.useState(0)
      return [
        search,
        (next) => {
          search = new URLSearchParams(typeof next === 'function' ? next(search) : next)
          refresh((n) => n + 1)
        },
      ]
    },
  }
  const cache = new Map()
  function load(relative) {
    const filename = path.resolve(sourceRoot, relative)
    if (cache.has(filename)) return cache.get(filename)
    const exports = {}
    cache.set(filename, exports)
    const customRequire = (name) => {
      if (name === 'react-router') return router
      if (name === 'next/navigation') return { usePathname: () => dom.window.location.pathname }
      if (name === '@tanstack/react-query') return query
      if (name === 'next/image')
        return function TestImage({ src, alt }) {
          return React.createElement('img', { src, alt })
        }
      if (name.includes('AuthContext')) return auth
      if (name.endsWith('content-client')) return api
      if (name === 'next/dynamic')
        return () =>
          load('features/culinary/pages/dashboard/RecipeCompositionWizard.tsx').RecipeCompositionWizard
      if (name.startsWith('@/') || name.startsWith('./')) {
        const target = name.startsWith('@/')
          ? name.slice(2)
          : path.relative(sourceRoot, path.resolve(path.dirname(filename), name))
        const extension = existsSync(path.resolve(sourceRoot, target + '.tsx')) ? '.tsx' : '.ts'
        if (name.endsWith('auth-client'))
          return {
            authenticatedBlobUrl: async () => '',
            changePassword: (...args) => api.changePassword(...args),
            forgotPassword: (...args) => api.forgotPassword(...args),
            resetPassword: (...args) => api.resetPassword(...args),
          }
        return load(target + extension)
      }
      return require(name)
    }
    const js = ts.transpileModule(readFileSync(filename, 'utf8'), {
      compilerOptions: {
        target: ts.ScriptTarget.ES2020,
        module: ts.ModuleKind.CommonJS,
        jsx: ts.JsxEmit.ReactJSX,
        esModuleInterop: true,
      },
    }).outputText
    vm.runInNewContext(`(function(require,exports){${js}\n})`, {
      console,
      window: dom.window,
      document: dom.window.document,
      Element: dom.window.Element,
      URL,
      URLSearchParams,
      setTimeout,
      clearTimeout,
      navigator: dom.window.navigator,
    })(customRequire, exports)
    return exports
  }
  const guard = load('components/navigation-guard.tsx')
  let current
  async function render(element) {
    current = element ?? React.cloneElement(current)
    await act(async () => {
      root.render(
        React.createElement(
          QueryClientProvider,
          { client },
          React.createElement(guard.NavigationGuardProvider, null, current),
        ),
      )
      await new Promise((r) => setTimeout(r, 20))
    })
    await act(async () => {
      await new Promise((r) => setTimeout(r, 20))
    })
  }
  return {
    load,
    render,
    requests,
    navigations,
    dom,
    setUser: (next) => {
      user = next
    },
    setSearch: (next) => {
      search = new URLSearchParams(next)
    },
    setUpdate: (next) => {
      update = next
    },
    setApi: (key, next) => {
      api[key] = next
    },
    async close() {
      await act(async () => root.unmount())
      client.clear()
      dom.window.close()
    },
  }
}

function button(label, container = document) {
  const result = [...container.querySelectorAll('button')].find((el) => el.textContent.trim() === label)
  assert.ok(result, `Missing button: ${label}`)
  return result
}

test('password change rejects weak or mismatched passwords and preserves the form on server rejection', async () => {
  const h = await setup()
  try {
    const { ApiProblem } = h.load('lib/api/problem-details.ts')
    h.setApi('changePassword', async (...args) => {
      h.requests.push(['change', ...args])
      throw new ApiProblem({ title: 'Invalid password', status: 400, code: 'AUTH_CURRENT_PASSWORD_INVALID' })
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/auth/PasswordForm.tsx').PasswordForm, {
        mode: 'change',
      }),
    )
    await input(document.getElementById('change-current-password'), 'Old#Password1')
    await input(document.getElementById('change-new-password'), 'weak')
    await input(document.getElementById('change-confirm-password'), 'weak')
    await click(button('Đổi mật khẩu'))
    assert.match(document.querySelector('[role="alert"]').textContent, /8 đến 128/)
    assert.equal(h.requests.length, 0)
    await input(document.getElementById('change-new-password'), 'New#Password2')
    await click(button('Đổi mật khẩu'))
    assert.match(document.querySelector('[role="alert"]').textContent, /không khớp/)
    assert.equal(h.requests.length, 0)
    await input(document.getElementById('change-confirm-password'), 'New#Password2')
    await click(button('Đổi mật khẩu'))
    assert.deepEqual(h.requests, [['change', 'Old#Password1', 'New#Password2']])
    assert.match(document.querySelector('[role="alert"]').textContent, /hiện tại không chính xác/)
    assert.equal(document.getElementById('change-new-password').value, 'New#Password2')
    assert.equal(h.navigations.length, 0)
  } finally {
    await h.close()
  }
})

test('successful password change locks duplicate submissions and returns to login after logout', async () => {
  const h = await setup()
  let complete
  try {
    h.setApi('changePassword', (...args) => {
      h.requests.push(['change', ...args])
      return new Promise((resolve) => {
        complete = resolve
      })
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/auth/PasswordForm.tsx').PasswordForm, {
        mode: 'change',
      }),
    )
    for (const [id, value] of [
      ['change-current-password', 'Old#Password1'],
      ['change-new-password', 'New#Password2'],
      ['change-confirm-password', 'New#Password2'],
    ]) {
      await input(document.getElementById(id), value)
    }
    await click(button('Đổi mật khẩu'))
    assert.equal(document.querySelector('fieldset').disabled, true)
    await click(button('Đang xử lý…'))
    assert.equal(h.requests.length, 1)
    await act(async () => complete())
    assert.deepEqual(h.requests.at(-1), ['logout'])
    assert.deepEqual(h.navigations, ['/login?passwordChanged=1'])
    assert.match(document.querySelector('[role="status"]').textContent, /đăng nhập lại/)
  } finally {
    await h.close()
  }
})

test('forgot password sends the trimmed email and displays a neutral confirmation with resend', async () => {
  const h = await setup()
  try {
    h.setApi('forgotPassword', async (...args) => h.requests.push(['forgot', ...args]))
    await h.render(
      React.createElement(h.load('features/culinary/pages/auth/PasswordForm.tsx').PasswordForm, {
        mode: 'forgot',
      }),
    )
    await input(document.getElementById('forgot-email'), 'author@example.test')
    await click(button('Gửi liên kết đặt lại mật khẩu'))
    assert.deepEqual(h.requests, [['forgot', 'author@example.test']])
    assert.match(document.querySelector('[role="status"]').textContent, /Nếu email.*30 phút/)
    await click(button('Gửi lại yêu cầu'))
    assert.ok(document.getElementById('forgot-email'))
  } finally {
    await h.close()
  }
})

test('reset password rejects missing links and decodes fragment tokens without leaking them in navigation', async () => {
  const h = await setup()
  try {
    const PasswordForm = h.load('features/culinary/pages/auth/PasswordForm.tsx').PasswordForm
    await h.render(React.createElement(PasswordForm, { mode: 'reset', key: 'missing' }))
    assert.equal(document.querySelector('form'), null)
    assert.match(document.querySelector('[role="alert"]').textContent, /không hợp lệ/)
    window.location.hash = 'email=author%2Btest%40example.test&token=a%2Bb%2Fc%3D'
    h.setApi('resetPassword', async (...args) => h.requests.push(['reset', ...args]))
    await h.render(React.createElement(PasswordForm, { mode: 'reset', key: 'valid' }))
    await input(document.getElementById('reset-new-password'), 'New#Password2')
    await input(document.getElementById('reset-confirm-password'), 'New#Password2')
    await click(button('Đặt lại mật khẩu'))
    assert.deepEqual(h.requests, [
      ['reset', 'author+test@example.test', 'a+b/c=', 'New#Password2'],
      ['logout'],
    ])
    assert.equal(window.location.hash, '')
    assert.match(document.querySelector('[role="status"]').textContent, /đăng nhập lại/)
    assert.equal(document.querySelector('a').getAttribute('href'), '/login')
  } finally {
    await h.close()
  }
})

test('expired reset links show recovery guidance and do not log the user out', async () => {
  const h = await setup()
  try {
    window.location.hash = 'email=author%40example.test&token=expired'
    const { ApiProblem } = h.load('lib/api/problem-details.ts')
    h.setApi('resetPassword', async () => {
      throw new ApiProblem({ title: 'Invalid token', status: 400, code: 'AUTH_RESET_TOKEN_INVALID' })
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/auth/PasswordForm.tsx').PasswordForm, {
        mode: 'reset',
      }),
    )
    await input(document.getElementById('reset-new-password'), 'New#Password2')
    await input(document.getElementById('reset-confirm-password'), 'New#Password2')
    await click(button('Đặt lại mật khẩu'))
    assert.match(document.querySelector('[role="alert"]').textContent, /hết hạn.*liên kết mới/)
    assert.ok(document.querySelector('a[href="/forgot-password"]'))
    assert.equal(h.requests.length, 0)
  } finally {
    await h.close()
  }
})
async function click(element) {
  await act(async () => {
    element.click()
    await new Promise((r) => setTimeout(r, 10))
  })
}
async function input(element, value) {
  await act(async () => {
    const prototype =
      element.tagName === 'TEXTAREA'
        ? window.HTMLTextAreaElement.prototype
        : window.HTMLInputElement.prototype
    Object.getOwnPropertyDescriptor(prototype, 'value').set.call(element, value)
    element.dispatchEvent(new window.Event('input', { bubbles: true }))
  })
}
async function openLeaveDialog() {
  await click(button('Công thức của tôi'))
}

test('invalid save-and-leave keeps the editor and describes the error inside the dialog', async () => {
  const h = await setup()
  try {
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    assert.ok(h.requests.some(([operation]) => operation === 'read-admin'))
    await input(document.getElementById('title'), 'abc')
    await openLeaveDialog()
    await click(button('Lưu và chuyển tiếp'))
    assert.equal(h.navigations.length, 0)
    assert.match(document.querySelector('[role="dialog"] [role="alert"]').textContent, /ít nhất 5 ký tự/)
    assert.equal(document.getElementById('title').value, 'abc')
  } finally {
    await h.close()
  }
})

test('save-and-leave waits for success, locks duplicate actions, and retains content on rejection', async () => {
  for (const succeeds of [false, true]) {
    const h = await setup()
    try {
      let resolve,
        reject,
        saves = 0
      h.setUpdate(() => {
        saves++
        return new Promise((ok, fail) => {
          resolve = ok
          reject = fail
        })
      })
      await h.render(
        React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
      )
      await input(document.getElementById('title'), 'Nội dung mới cần lưu')
      await openLeaveDialog()
      await click(button('Lưu và chuyển tiếp'))
      assert.equal(h.navigations.length, 0)
      assert.equal(button('Đang lưu…', document.querySelector('[role="dialog"]')).disabled, true)
      assert.equal(button('Rời đi không lưu').disabled, true)
      await act(async () => {
        if (succeeds) resolve({ ...fixture, title: 'Nội dung mới cần lưu', version: 2 })
        else reject(new Error('Offline'))
        await new Promise((r) => setTimeout(r, 30))
      })
      assert.equal(saves, 1)
      assert.equal(h.navigations.length, succeeds ? 1 : 0)
      if (!succeeds) {
        assert.equal(document.getElementById('title').value, 'Nội dung mới cần lưu')
        assert.match(document.querySelector('[role="dialog"] [role="alert"]').textContent, /Không thể lưu/)
      }
    } finally {
      await h.close()
    }
  }
})

test('composition input survives the basic step and sidebar navigation is guarded', async () => {
  const h = await setup()
  try {
    await h.render(
      React.createElement(
        React.Fragment,
        null,
        React.createElement('a', { href: '/dashboard/categories', id: 'sidebar' }, 'Danh mục'),
        React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
      ),
    )
    const stepButton = (starts) =>
      [...document.querySelectorAll('button')].find((el) => el.textContent.trim().startsWith(starts))
    await click(stepButton('2. Nguyên liệu'))
    const name = document.querySelector('input[placeholder="Tên nguyên liệu (bắt buộc)"]')
    assert.ok(name)
    await input(name, 'Nguyên liệu chưa lưu')
    await click(stepButton('1. Thông tin cơ bản'))
    await click(stepButton('2. Nguyên liệu'))
    assert.equal(
      document.querySelector('input[placeholder="Tên nguyên liệu (bắt buộc)"]').value,
      'Nguyên liệu chưa lưu',
    )
    await click(document.getElementById('sidebar'))
    assert.ok(document.querySelector('[role="dialog"]'))
    assert.equal(h.navigations.length, 0)
    assert.ok(![...document.querySelectorAll('button')].some((el) => el.textContent === 'Lưu và chuyển tiếp'))
  } finally {
    await h.close()
  }
})

test('default category does not mark a new untouched recipe as dirty', async () => {
  const h = await setup({ editing: false })
  try {
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    await openLeaveDialog()
    assert.deepEqual(h.navigations, ['/dashboard/recipes'])
    assert.equal(document.querySelector('[role="dialog"]'), null)
  } finally {
    await h.close()
  }
})

test('profile edit reads the user after an asynchronous session loads', async () => {
  const h = await setup()
  try {
    h.setUser(null)
    await h.render(React.createElement(h.load('features/culinary/pages/Profile.tsx').Profile))
    h.setUser({
      id: 'user',
      name: 'Tên tải sau',
      email: 'fixture@example.test',
      bio: 'Tiểu sử tải sau',
      role: 'admin',
    })
    await h.render()
    await click(button('Cập nhật'))
    assert.equal(document.querySelector('input[aria-label="Họ và tên"]').value, 'Tên tải sau')
    assert.equal(document.querySelector('textarea').value, 'Tiểu sử tải sau')
    assert.equal(document.querySelector('[aria-label="Đổi ảnh đại diện"]'), null)
  } finally {
    await h.close()
  }
})

test('managed search calls the server, URL scope resets, and mobile cards include archive', async () => {
  const h = await setup()
  try {
    h.setSearch('scope=all')
    await h.render(React.createElement(h.load('features/culinary/pages/dashboard/MyRecipes.tsx').MyRecipes))
    await input(document.querySelector('input[type="search"]'), 'Trang hai')
    await h.render()
    assert.ok(h.requests.some((r) => r[0] === 'all' && r.at(-1) === 'Trang hai'))
    h.setSearch('')
    await h.render()
    assert.equal(h.requests.at(-1)[0], 'mine')
    assert.ok(
      [...document.querySelectorAll('.md\\:hidden button')].some((el) => el.textContent.includes('Lưu trữ')),
    )
  } finally {
    await h.close()
  }
})

test('system overview combines administrative totals, recipe management and history without scope tabs', async () => {
  const h = await setup()
  try {
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/SystemOverview.tsx').SystemOverview),
    )
    assert.equal(document.querySelector('h1').textContent, 'Tổng quan hệ thống')
    assert.equal(document.querySelector('h2').textContent, 'Tất cả công thức')
    assert.equal(h.requests.filter((request) => request[0] === 'mine').length, 0)
    assert.equal(h.requests.filter((request) => request[0] === 'all').length, 5)
    assert.equal(document.querySelector('[role="tablist"]'), null)
    assert.equal(document.querySelector('a[href="/dashboard/audit"]').textContent.trim(), 'Lịch sử')
    assert.equal(
      document.querySelector('[aria-label="Thống kê công thức toàn hệ thống"]').textContent.includes('20'),
      true,
    )
  } finally {
    await h.close()
  }
})

test('SRS 5.1: authors can use the dashboard but cannot load administrative overview or audit data', async () => {
  const h = await setup()
  try {
    h.setUser({ id: 'author', name: 'Tác giả', role: 'author' })
    await h.render(React.createElement(h.load('features/culinary/pages/dashboard/Dashboard.tsx').Dashboard))
    assert.equal(document.querySelector('h1').textContent, 'Tổng quan cá nhân')
    assert.equal(h.requests.filter((request) => request[0] === 'mine').length, 4)
    assert.equal(h.requests.filter((request) => request[0] === 'all').length, 0)
    assert.equal(h.navigations.length, 0)
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/SystemOverview.tsx').SystemOverview),
    )
    assert.match(document.querySelector('[role="alert"]').textContent, /Chỉ quản trị viên/)
    assert.equal(h.requests.filter((request) => request[0] === 'all').length, 0)
    h.setApi('listAuditLogs', async () => {
      h.requests.push(['audit'])
      throw new Error('Author must not request system history')
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/AuditLogAdmin.tsx').AuditLogAdmin),
    )
    assert.match(document.querySelector('[role="alert"]').textContent, /Chỉ quản trị viên/)
    assert.equal(h.requests.filter((request) => request[0] === 'all').length, 0)
    assert.equal(h.requests.filter((request) => request[0] === 'audit').length, 0)
  } finally {
    await h.close()
  }
})

test('SRS 2.3/5.1: Admin retains a personal dashboard without fetching other authors data', async () => {
  const h = await setup()
  try {
    await h.render(React.createElement(h.load('features/culinary/pages/dashboard/Dashboard.tsx').Dashboard))
    assert.equal(document.querySelector('h1').textContent, 'Tổng quan cá nhân')
    assert.match(document.body.textContent, /Công thức gần đây/)
    assert.equal(h.requests.filter((request) => request[0] === 'mine').length, 4)
    assert.equal(h.requests.filter((request) => request[0] === 'all').length, 0)
    assert.equal(document.querySelector('a[href="/dashboard/audit"]'), null)
  } finally {
    await h.close()
  }
})

test('sidebar preserves personal dashboard for both roles and gives only Admin a separate system entry', async () => {
  const h = await setup()
  try {
    const { DashboardLayout } = h.load('features/culinary/components/layout/DashboardLayout.tsx')
    h.dom.window.history.replaceState(null, '', '/dashboard')
    await h.render(React.createElement(DashboardLayout))
    assert.equal(document.querySelector('aside a[href="/dashboard"]').textContent.trim(), 'Tổng quan')
    assert.equal(document.querySelector('aside a[aria-current="page"]').getAttribute('href'), '/dashboard')
    assert.ok(document.querySelector('aside a[href="/dashboard/system"]'))
    h.dom.window.history.replaceState(null, '', '/dashboard/system')
    await h.render()
    assert.equal(
      document.querySelector('aside a[aria-current="page"]').getAttribute('href'),
      '/dashboard/system',
    )
    assert.equal(document.querySelector('aside a[href="/dashboard"]').hasAttribute('aria-current'), false)
    h.setUser({ id: 'author', name: 'Tác giả', role: 'author' })
    h.dom.window.history.replaceState(null, '', '/dashboard')
    await h.render()
    assert.ok(document.querySelector('aside a[href="/dashboard"]'))
    assert.equal(document.querySelector('a[href="/dashboard/system"]'), null)
  } finally {
    await h.close()
  }
})

test('dashboard has one create action and system management keeps its own navigation context', async () => {
  const h = await setup()
  try {
    await h.render(React.createElement(h.load('features/culinary/pages/dashboard/Dashboard.tsx').Dashboard))
    assert.equal(document.querySelectorAll('a[href="/dashboard/recipes/new"]').length, 1)
    assert.equal(document.querySelector('a[href="/categories"]'), null)
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/SystemOverview.tsx').SystemOverview),
    )
    assert.equal(document.querySelector('a[href="/dashboard/recipes/new"]'), null)
    assert.equal(document.querySelector('a[href="/dashboard/categories"]'), null)
    assert.ok(document.querySelector('a[href$="/edit?from=system"]'))
    assert.ok(document.querySelector('a[href$="/preview?from=system"]'))
    h.setSearch('from=system')
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    assert.equal(button('Tổng quan hệ thống').textContent.trim(), 'Tổng quan hệ thống')
    assert.ok(document.querySelector('a[href$="/preview?from=system"]'))
    await click(button('2. Nguyên liệu'))
    assert.equal(button('Tổng quan hệ thống').textContent.trim(), 'Tổng quan hệ thống')
    await click(button('Tổng quan hệ thống'))
    assert.equal(h.navigations.at(-1), '/dashboard/system')
  } finally {
    await h.close()
  }
})

test('category list opens one focused form, retains save errors and prevents dismissal while saving', async () => {
  const h = await setup()
  try {
    let rejectSave
    h.setApi(
      'createCategory',
      () =>
        new Promise((_resolve, reject) => {
          rejectSave = reject
        }),
    )
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/CategoryAdmin.tsx').CategoryAdmin),
    )
    assert.equal(document.querySelector('form'), null)
    await click(button('Thêm danh mục'))
    let dialog = document.querySelector('[role="dialog"]')
    await input(dialog.querySelector('input'), 'Danh mục mới')
    await act(async () =>
      dialog
        .querySelector('form')
        .dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true })),
    )
    assert.equal(dialog.querySelector('fieldset').disabled, true)
    await click(button('Hủy', dialog))
    assert.ok(document.querySelector('[role="dialog"]'))
    await act(async () => {
      rejectSave(new Error('Offline'))
      await new Promise((resolve) => setTimeout(resolve, 20))
    })
    dialog = document.querySelector('[role="dialog"]')
    assert.match(dialog.querySelector('[role="alert"]').textContent, /Không thể lưu danh mục/)
    assert.equal(dialog.querySelector('input').value, 'Danh mục mới')
    await click(button('Hủy', dialog))
    assert.equal(document.querySelector('[role="dialog"]'), null)
  } finally {
    await h.close()
  }
})

test('history shows actor, action, object and UTC+7 time with a detail dialog', async () => {
  const h = await setup()
  try {
    h.setApi('listAuditLogs', async () => ({
      data: [
        {
          id: 'log',
          timestamp: '2026-10-21T03:15:00Z',
          level: 'Information',
          eventName: 'recipe.updated',
          message: 'Changed recipe',
          userId: 'admin-id',
          properties: { UserName: 'Nguyễn An', EntityName: 'Phở bò' },
        },
      ],
      meta: { total: 1, totalPages: 1 },
      telemetry: {},
    }))
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/AuditLogAdmin.tsx').AuditLogAdmin),
    )
    const row = document.querySelector('tbody tr')
    assert.match(row.textContent, /10:15:00/)
    assert.match(row.textContent, /Nguyễn An.*Cập nhật công thức.*Phở bò/)
    await click(row.querySelector('button'))
    const dialog = document.querySelector('[role="dialog"]')
    assert.match(dialog.textContent, /Nguyễn An/)
    assert.match(dialog.textContent, /Phở bò/)
    assert.ok(document.querySelector('a[href="/dashboard/system"]'))
  } finally {
    await h.close()
  }
})

test('modal traps focus, uses the latest close handler, keeps focus on rerender, and restores it', async () => {
  const h = await setup()
  try {
    const { Modal } = h.load('components/ui/modal.tsx')
    let closed = 0
    const trigger = document.getElementById('trigger')
    trigger.focus()
    const renderModal = (busy = false) =>
      h.render(
        React.createElement(
          Modal,
          {
            open: true,
            title: 'Kiểm tra',
            busy,
            onClose: () => {
              closed++
            },
          },
          React.createElement('input', { id: 'inside' }),
          React.createElement('button', { id: 'last' }, 'Cuối'),
        ),
      )
    await renderModal()
    const close = document.querySelector('[aria-label="Đóng hộp thoại"]')
    assert.equal(document.activeElement, close)
    document.getElementById('inside').focus()
    await renderModal()
    assert.equal(document.activeElement.id, 'inside')
    document.getElementById('last').focus()
    document.dispatchEvent(
      new window.KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true }),
    )
    assert.equal(document.activeElement, close)
    document.dispatchEvent(
      new window.KeyboardEvent('keydown', { key: 'Tab', shiftKey: true, bubbles: true, cancelable: true }),
    )
    assert.equal(document.activeElement.id, 'last')
    await renderModal(true)
    document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    assert.equal(closed, 0)
    await renderModal()
    document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    assert.equal(closed, 1)
    await h.render(React.createElement(Modal, { open: false, title: 'Kiểm tra', onClose: () => {} }))
    assert.equal(document.activeElement, trigger)
    assert.equal(document.body.style.overflow, '')
  } finally {
    await h.close()
  }
})

test('registration explains password rules and links validation errors to fields', async () => {
  const h = await setup()
  try {
    await h.render(React.createElement(h.load('features/culinary/pages/auth/Register.tsx').Register))
    assert.match(document.getElementById('password-help').textContent, /chữ hoa.*chữ thường.*số.*đặc biệt/)
    await act(async () => {
      document
        .querySelector('form')
        .dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
    })
    assert.equal(document.activeElement.id, 'name')
    for (const id of ['name', 'email', 'password', 'confirm']) {
      const field = document.getElementById(id)
      assert.equal(field.getAttribute('aria-invalid'), 'true')
      assert.ok(
        field
          .getAttribute('aria-describedby')
          .split(' ')
          .every((ref) => document.getElementById(ref)),
      )
    }
    assert.ok(document.querySelector('[role="alert"]'))
  } finally {
    await h.close()
  }
})

test('back and forward navigation restore the editor until unsaved changes are resolved', async () => {
  const h = await setup()
  try {
    const targets = []
    const { useUnsavedChanges } = h.load('components/navigation-guard.tsx')
    function Guarded({ dirty }) {
      useUnsavedChanges(dirty, (target) => targets.push(target))
      return React.createElement('p', null, 'Editor')
    }
    await h.render(React.createElement(Guarded, { dirty: true }))
    window.history.pushState({}, '', '/first')
    window.history.pushState({}, '', '/second')
    window.history.back()
    await new Promise((r) => setTimeout(r, 40))
    assert.deepEqual(targets, ['/first'])
    assert.equal(window.location.pathname, '/second')
    const unload = new window.Event('beforeunload', { cancelable: true })
    window.dispatchEvent(unload)
    assert.equal(unload.defaultPrevented, true)
    await h.render(React.createElement(Guarded, { dirty: false }))
    window.history.back()
    await new Promise((r) => setTimeout(r, 30))
    assert.equal(window.location.pathname, '/first')
    await h.render(React.createElement(Guarded, { dirty: true }))
    window.history.forward()
    await new Promise((r) => setTimeout(r, 40))
    assert.equal(targets.at(-1), '/second')
    assert.equal(window.location.pathname, '/first')
  } finally {
    await h.close()
  }
})

test('recipe action failures remain visible inside the confirmation dialog', async () => {
  const h = await setup()
  try {
    h.setApi('archiveRecipe', async () => {
      throw new Error('Offline')
    })
    await h.render(React.createElement(h.load('features/culinary/pages/dashboard/MyRecipes.tsx').MyRecipes))
    await click(button('Lưu trữ'))
    await click(button('Xác nhận'))
    assert.match(document.querySelector('[role="dialog"] [role="alert"]').textContent, /Không thể lưu trữ/)
  } finally {
    await h.close()
  }
})

test('editor action confirmation disables repeated submissions while pending', async () => {
  const h = await setup()
  try {
    let resolve,
      count = 0
    h.setApi('archiveRecipe', () => {
      count++
      return new Promise((ok) => {
        resolve = ok
      })
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    await click(button('Lưu trữ'))
    await click(button('Xác nhận'))
    const confirm = button('Đang xử lý…')
    assert.equal(confirm.disabled, true)
    await click(confirm)
    assert.equal(count, 1)
    await act(async () => {
      resolve({ ...fixture, version: 2, status: 'archived' })
      await new Promise((r) => setTimeout(r, 20))
    })
    assert.equal(document.querySelector('[role="dialog"]'), null)
  } finally {
    await h.close()
  }
})

test('carousel excludes hidden links, pauses on focus and hover, and respects reduced motion', async () => {
  const h = await setup()
  try {
    const timers = new Map()
    const listeners = new Set()
    let nextTimer = 0
    const preference = {
      matches: false,
      addEventListener: (_event, callback) => listeners.add(callback),
      removeEventListener: (_event, callback) => listeners.delete(callback),
    }
    window.matchMedia = () => preference
    window.setInterval = (callback) => {
      timers.set(++nextTimer, callback)
      return nextTimer
    }
    window.clearInterval = (id) => timers.delete(id)
    const { Home } = h.load('features/culinary/pages/Home.tsx')
    await h.render(
      React.createElement(Home, {
        categories: [],
        recipePage: {
          data: [
            { ...fixture, slug: 'one' },
            { ...fixture, id: 'two', slug: 'two', title: 'Món thứ hai' },
          ],
          meta: { page: 1, pageSize: 12, totalPages: 1 },
        },
      }),
    )
    const hero = document.querySelector('[aria-roledescription="carousel"]')
    const slides = [...document.getElementById('latest-recipes-carousel').children]
    assert.equal(timers.size, 1)
    assert.equal(slides[1].hasAttribute('inert'), true)
    assert.equal(slides[1].querySelector('a').tabIndex, -1)
    await act(async () => hero.dispatchEvent(new window.MouseEvent('mouseover', { bubbles: true })))
    assert.equal(timers.size, 0)
    await act(async () =>
      hero.dispatchEvent(new window.MouseEvent('mouseout', { bubbles: true, relatedTarget: document.body })),
    )
    assert.equal(timers.size, 1)
    await act(async () => slides[0].querySelector('a').focus())
    assert.equal(timers.size, 0)
    await act(async () => document.getElementById('trigger').focus())
    assert.equal(timers.size, 1)
    await act(async () => {
      preference.matches = true
      listeners.forEach((update) => update())
    })
    assert.equal(timers.size, 0)
    assert.equal(document.getElementById('latest-recipes-carousel').style.transition, 'none')
  } finally {
    await h.close()
  }
})
