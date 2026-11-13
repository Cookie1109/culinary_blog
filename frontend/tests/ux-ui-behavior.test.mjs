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

test('recipe detail displays the original image when background resize fails', async () => {
  const h = await setup()
  try {
    const { RecipeDetail } = h.load('features/culinary/pages/RecipeDetail.tsx')
    await h.render(
      React.createElement(RecipeDetail, {
        recipe: {
          ...fixture,
          slug: 'failed-resize',
          images: [
            {
              id: 'failed-image',
              originalUrl: '/api/v1/media/failed-image/original',
              mediumUrl: null,
              thumbnailUrl: null,
              isPrimary: true,
              orderIndex: 0,
              processingStatus: 'failed',
              altText: 'Ảnh gốc còn nguyên',
            },
          ],
        },
      }),
    )
    assert.ok(document.querySelector('img[src="/api/public-media/failed-image/original?v=1"]'))
  } finally {
    await h.close()
  }
})

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
    'FormData',
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
  let historyEvents = 0
  dom.window.addEventListener(
    'popstate',
    () => {
      historyEvents += 1
    },
    true,
  )
  let update = async (_id, _version, payload) => ({
    ...recipe,
    ...payload,
    category: recipe.category,
    version: recipe.version + 1,
  })
  const api = new Proxy(
    {
      listCategories: async () => [recipe.category],
      updateProfile: async (values) => {
        requests.push(['profile', values])
        user = { ...user, ...values }
      },
      getAdminRecipe: async () => {
        requests.push(['read-admin'])
        return recipe
      },
      getMyRecipe: async () => recipe,
      updateRecipe: async (...args) => {
        requests.push(['update', ...args])
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
  const auth = {
    useAuth: () => ({
      user,
      isLoading: false,
      updateProfile: async (...args) => api.updateProfile(...args),
      logout: async () => {
        requests.push(['logout'])
      },
      login: async (...args) => api.login(...args),
      register: async (...args) => api.register(...args),
    }),
  }
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
      if (name === 'next/link') return require('next/link')
      if (name === 'next/navigation')
        return {
          usePathname: () => dom.window.location.pathname,
          useSearchParams: () => search,
          useRouter: () => ({
            refresh: () => requests.push(['refresh']),
            push: (href, options) => navigations.push(['push', href, options]),
            replace: (href, options) => navigations.push(['replace', href, options]),
          }),
        }
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
      if (name.startsWith('@/') || name.startsWith('.')) {
        const target = name.startsWith('@/')
          ? name.slice(2)
          : path.relative(sourceRoot, path.resolve(path.dirname(filename), name))
        const extension = existsSync(path.resolve(sourceRoot, `${target}.tsx`)) ? '.tsx' : '.ts'
        if (name.endsWith('auth-client'))
          return {
            authenticatedBlobUrl: async () => '',
            changePassword: (...args) => api.changePassword(...args),
            forgotPassword: (...args) => api.forgotPassword(...args),
            resetPassword: (...args) => api.resetPassword(...args),
          }
        return load(target + extension)
      }
      if (name === 'react') return require('react')
      if (name === 'react/jsx-runtime') return require('react/jsx-runtime')
      if (name === 'react-hook-form') return require('react-hook-form')
      if (name === '@hookform/resolvers/zod') return require('@hookform/resolvers/zod')
      if (name === 'zod') return require('zod')
      if (name === 'zod/mini') return require('zod/mini')
      if (name === 'lucide-react') return require('lucide-react')
      if (name === 'next-auth/react') return require('next-auth/react')
      throw new Error(`Unexpected UI fixture dependency: ${name}`)
    }
    const js = ts.transpileModule(readFileSync(filename, 'utf8'), {
      compilerOptions: {
        target: ts.ScriptTarget.ES2020,
        module: ts.ModuleKind.CommonJS,
        jsx: ts.JsxEmit.ReactJSX,
        esModuleInterop: true,
      },
    }).outputText
    // Share built-in constructors with real dependencies. RHF treats objects from
    // a separate VM realm as class instances and cannot clone their form values.
    vm.runInThisContext(`(function(require,exports){${js}\n})`, { filename })(customRequire, exports)
    return exports
  }
  const guard = load('components/navigation-guard.tsx')
  const { ToastProvider } = load('components/ui/toast.tsx')
  let current
  async function render(element) {
    current = element ?? React.cloneElement(current)
    await act(async () => {
      root.render(
        React.createElement(
          QueryClientProvider,
          { client },
          React.createElement(
            guard.NavigationGuardProvider,
            null,
            React.createElement(ToastProvider, null, current),
          ),
        ),
      )
      await new Promise((r) => {
        setTimeout(r, 20)
      })
    })
    await act(async () => {
      await new Promise((r) => {
        setTimeout(r, 20)
      })
    })
  }
  return {
    load,
    render,
    requests,
    client,
    navigations,
    historyEventCount: () => historyEvents,
    getSearch: () => new URLSearchParams(search),
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

for (const [stage, label, method, item] of [
  ['ingredients', 'Xóa nguyên liệu', 'deleteIngredient', { id: 'child', name: 'Muối', orderIndex: 0 }],
  ['steps', 'Xóa bước', 'deleteStep', { id: 'child', title: 'Nấu', description: 'Nấu chín', stepNumber: 1 }],
  [
    'images',
    'Xóa ảnh',
    'deleteRecipeImage',
    { id: 'child', altText: 'Ảnh thử', orderIndex: 0, processingStatus: 'failed' },
  ],
]) {
  test(`composition ${stage} deletion confirms, blocks duplicate writes and keeps errors recoverable`, async () => {
    const h = await setup()
    let finish
    let reject
    const changed = []
    try {
      h.setApi(method, (...args) => {
        h.requests.push(args)
        return new Promise((resolve, fail) => {
          finish = resolve
          reject = fail
        })
      })
      const { RecipeCompositionWizard } = h.load(
        'features/culinary/pages/dashboard/RecipeCompositionWizard.tsx',
      )
      await h.render(
        React.createElement(RecipeCompositionWizard, {
          recipe: { ...fixture, [stage]: [item] },
          version: 7,
          activeStage: stage,
          onChanged: async (value) => {
            changed.push(value)
          },
        }),
      )
      const openDelete = () => document.querySelector(`[aria-label="${label}"]`)
      await click(openDelete())
      assert.equal(h.requests.length, 0)
      await click(button('Hủy', document.querySelector('[role="dialog"]')))
      assert.equal(document.querySelector('[role="dialog"]'), null)
      await click(openDelete())
      await click(button('Xóa nội dung'))
      assert.deepEqual(h.requests, [[fixture.id, 'child', 7]])
      assert.equal(button('Đang xóa…').disabled, true)
      await click(button('Đang xóa…'))
      await act(async () =>
        document.dispatchEvent(new window.KeyboardEvent('keydown', { key: 'Escape', bubbles: true })),
      )
      assert.ok(document.querySelector('[role="dialog"]'))
      assert.equal(h.requests.length, 1)
      await act(async () => reject(new Error('Synthetic delete failure')))
      assert.equal(changed.length, 0)
      assert.ok(document.querySelector('[role="alert"]'))
      await click(openDelete())
      await click(button('Xóa nội dung'))
      await act(async () => finish(8))
      assert.deepEqual(changed, [8])
      assert.equal(document.querySelector('[role="dialog"]'), null)
    } finally {
      await h.close()
    }
  })
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
    const { PasswordForm } = h.load('features/culinary/pages/auth/PasswordForm.tsx')
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
    await new Promise((r) => {
      setTimeout(r, 10)
    })
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

async function waitUntil(predicate) {
  const deadline = Date.now() + 1500
  while (!predicate() && Date.now() < deadline)
    await new Promise((resolve) => {
      setTimeout(resolve, 10)
    })
  assert.ok(predicate(), 'Timed out waiting for browser state')
}

async function selectOption(element, value) {
  await act(async () => {
    Object.getOwnPropertyDescriptor(window.HTMLSelectElement.prototype, 'value').set.call(element, value)
    element.dispatchEvent(new window.Event('change', { bubbles: true }))
  })
}

test('public search applies numeric filters together, preserves existing filters and resets pagination', async () => {
  const h = await setup()
  try {
    h.setSearch('q=pho&category=mon-an&difficulty=easy&page=3&pageSize=24')
    await h.render(
      React.createElement(h.load('features/culinary/pages/RecipesList.tsx').RecipesList, {
        categories: [{ ...fixture.category, orderIndex: 0, recipeCount: 0 }],
        result: { data: [], meta: { page: 3, pageSize: 24, total: 50, totalPages: 3 } },
      }),
    )
    assert.equal(document.getElementById('recipes-sort-select').value, 'relevance')
    for (const [key, value] of [
      ['maxCookTime', '0'],
      ['minServings', '4'],
      ['minPrepTime', '5'],
      ['maxPrepTime', '15'],
    ])
      await input(document.getElementById(`filter-${key}`), value)
    await click(button('Áp dụng bộ lọc'))
    assert.equal(h.getSearch().get('maxCookTime'), '0')
    assert.equal(h.getSearch().get('minServings'), '4')
    assert.equal(h.getSearch().get('minPrepTime'), '5')
    assert.equal(h.getSearch().get('maxPrepTime'), '15')
    assert.equal(h.getSearch().get('q'), 'pho')
    assert.equal(h.getSearch().get('category'), 'mon-an')
    assert.equal(h.getSearch().get('difficulty'), 'easy')
    assert.equal(h.getSearch().get('pageSize'), '24')
    assert.equal(h.getSearch().has('page'), false)
    await selectOption(document.getElementById('recipes-sort-select'), '-servings')
    assert.equal(h.getSearch().get('sort'), '-servings')
    assert.equal(h.getSearch().get('maxCookTime'), '0')
    await click(document.querySelector('[aria-label="Trang 2"]'))
    assert.equal(h.getSearch().get('page'), '2')
    assert.equal(h.getSearch().get('minPrepTime'), '5')
    await selectOption(document.getElementById('recipes-page-size'), '50')
    assert.equal(h.getSearch().get('pageSize'), '50')
    assert.equal(h.getSearch().has('page'), false)
    await click(button('Xóa tìm kiếm và bộ lọc'))
    assert.equal(h.getSearch().toString(), '')
    assert.equal(document.getElementById('filter-minServings').value, '')
    assert.equal(document.getElementById('recipes-sort-select').value, 'newest')
  } finally {
    await h.close()
  }
})

test('sortBy URL state is shown by the public form and choosing a sort replaces its aliases', async () => {
  const h = await setup()
  try {
    h.setSearch('sortBy=cookTime&sortOrder=desc&maxCookTime=0')
    await h.render(
      React.createElement(h.load('features/culinary/pages/RecipesList.tsx').RecipesList, {
        categories: [],
        result: {
          data: [],
          message: 'Thử từ khóa khác hoặc giảm bộ lọc.',
          meta: { page: 1, pageSize: 12, total: 0, totalPages: 0 },
        },
      }),
    )
    assert.equal(document.getElementById('recipes-sort-select').value, '-cookTime')
    assert.ok(document.body.textContent.includes('Thử từ khóa khác hoặc giảm bộ lọc.'))
    await selectOption(document.getElementById('recipes-sort-select'), 'az')
    assert.equal(h.getSearch().get('sort'), 'az')
    assert.equal(h.getSearch().has('sortBy'), false)
    assert.equal(h.getSearch().has('sortOrder'), false)
    assert.equal(h.getSearch().get('maxCookTime'), '0')
  } finally {
    await h.close()
  }
})

test('public search validates ranges and restores form values from back-forward URL state', async () => {
  const h = await setup()
  try {
    h.setSearch('page=3&minPrepTime=10&maxPrepTime=30')
    await h.render(
      React.createElement(h.load('features/culinary/pages/RecipesList.tsx').RecipesList, {
        categories: [],
        result: { data: [], meta: { page: 3, total: 0, totalPages: 0 } },
      }),
    )
    await input(document.getElementById('filter-minPrepTime'), '40')
    await click(button('Áp dụng bộ lọc'))
    assert.equal(h.getSearch().get('minPrepTime'), '10')
    assert.equal(h.getSearch().get('page'), '3')
    assert.match(document.getElementById('filter-maxPrepTime-error').textContent, /lớn hơn hoặc bằng/)
    h.setSearch('q=bun&minServings=6&sort=-cookTime')
    await h.render()
    assert.equal(document.getElementById('filter-minPrepTime').value, '')
    assert.equal(document.getElementById('filter-minServings').value, '6')
    assert.equal(document.getElementById('recipes-sort-select').value, '-cookTime')
    h.setSearch('q=bun&sort=createdAt')
    await h.render()
    assert.equal(document.getElementById('recipes-sort-select').value, 'oldest')
    await input(document.getElementById('recipes-search'), 'p')
    await click(button('Tìm kiếm'))
    assert.equal(h.getSearch().get('q'), 'bun')
    assert.match(document.getElementById('recipes-search-error').textContent, /ít nhất 2/)
  } finally {
    await h.close()
  }
})

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
      let resolve
      let reject
      let saves = 0
      h.setUpdate(() => {
        saves += 1
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
        await new Promise((r) => {
          setTimeout(r, 30)
        })
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

test('AVIF upload accepts its MIME, reports progress, and retains the file for retry after failure', async () => {
  const h = await setup()
  try {
    let rejectUpload
    let changedVersion
    const attempts = []
    h.setApi('uploadRecipeImage', async (_id, _version, file, _alt, _primary, onProgress) => {
      attempts.push(file.type)
      onProgress(37)
      return new Promise((_resolve, reject) => {
        rejectUpload = reject
      })
    })
    await h.render(
      React.createElement(
        h.load('features/culinary/pages/dashboard/RecipeCompositionWizard.tsx').RecipeCompositionWizard,
        {
          recipe: structuredClone(fixture),
          version: 1,
          activeStage: 'images',
          onChanged: async (version) => {
            changedVersion = version
          },
        },
      ),
    )
    const fileInput = document.getElementById('recipe-image')
    assert.ok(fileInput.accept.includes('image/avif'))
    Object.defineProperty(fileInput, 'files', {
      configurable: true,
      value: [new h.dom.window.File(['synthetic'], 'recipe.avif', { type: 'image/avif' })],
    })
    await act(async () => {
      fileInput.dispatchEvent(new Event('change', { bubbles: true }))
    })
    await click(button('Tải ảnh lên'))
    assert.deepEqual(attempts, ['image/avif'])
    assert.equal(document.querySelector('progress').value, 37)
    assert.ok(button('Đang tải 37%…').disabled)
    await act(async () => {
      rejectUpload(new Error('synthetic failure'))
      await new Promise((resolve) => {
        setTimeout(resolve, 20)
      })
    })
    assert.match(document.querySelector('[role="alert"]').textContent, /Không thể lưu thay đổi/)
    h.setApi('uploadRecipeImage', async (_id, _version, file, _alt, _primary, onProgress) => {
      attempts.push(file.type)
      onProgress(100)
      return { meta: { recipeVersion: 2 } }
    })
    await click(button('Tải ảnh lên'))
    assert.deepEqual(attempts, ['image/avif', 'image/avif'])
    assert.equal(changedVersion, 2)
    assert.equal(document.querySelector('progress'), null)
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

test('profile validates avatar URLs, saves all fields, and supports clearing the avatar', async () => {
  const h = await setup()
  try {
    await h.render(React.createElement(h.load('features/culinary/pages/Profile.tsx').Profile))
    await click(button('Cập nhật'))
    // eslint-disable-next-line no-script-url -- Negative fixture must stay to verify URL rejection.
    await input(document.getElementById('profile-avatar'), 'javascript:alert(1)')
    await click(button('Lưu thay đổi'))
    assert.equal(h.requests.length, 0)
    assert.equal(document.getElementById('profile-avatar').getAttribute('aria-invalid'), 'true')
    assert.match(document.getElementById('profile-avatar-error').textContent, /HTTP hoặc HTTPS/)
    await input(document.querySelector('input[aria-label="Họ và tên"]'), '  Tên đã sửa  ')
    await input(document.getElementById('profile-avatar'), 'https://example.test/avatar.jpg')
    await click(button('Lưu thay đổi'))
    assert.deepEqual(h.requests[0], [
      'profile',
      { name: 'Tên đã sửa', bio: 'Giới thiệu', avatar: 'https://example.test/avatar.jpg' },
    ])
    assert.equal(document.querySelector('img').getAttribute('src'), 'https://example.test/avatar.jpg')
    await click(button('Cập nhật'))
    await input(document.getElementById('profile-avatar'), '')
    await click(button('Lưu thay đổi'))
    assert.equal(h.requests[1][1].avatar, '')
    assert.equal(document.querySelector('img'), null)
  } finally {
    await h.close()
  }
})

test('profile preserves edits and maps server validation to the avatar field', async () => {
  const h = await setup()
  try {
    const { ApiProblem } = h.load('lib/api/problem-details.ts')
    h.setApi('updateProfile', async () => {
      throw new ApiProblem({
        title: 'Validation failed',
        status: 400,
        code: 'VALIDATION_ERROR',
        errors: { AvatarUrl: ['Stack trace: secret storage URL'] },
      })
    })
    await h.render(React.createElement(h.load('features/culinary/pages/Profile.tsx').Profile))
    await click(button('Cập nhật'))
    await input(document.getElementById('profile-avatar'), 'https://example.test/avatar.jpg')
    await click(button('Lưu thay đổi'))
    assert.equal(document.getElementById('profile-avatar').value, 'https://example.test/avatar.jpg')
    assert.match(document.getElementById('profile-avatar-error').textContent, /URL ảnh HTTP hoặc HTTPS/)
    assert.doesNotMatch(document.body.textContent, /Stack trace|secret storage/)
    await click(button('Hủy'))
    await click(button('Cập nhật'))
    assert.equal(document.getElementById('profile-avatar').value, '')
  } finally {
    await h.close()
  }
})

test('recipe server validation keeps local input and marks the title without exposing internal details', async () => {
  const h = await setup()
  try {
    const { ApiProblem } = h.load('lib/api/problem-details.ts')
    h.setUpdate(async () => {
      throw new ApiProblem({
        status: 422,
        title: 'Invalid',
        code: 'VALIDATION_ERROR',
        errors: { Title: ['secret SQL stack trace'] },
      })
    })
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    await input(document.getElementById('title'), 'Nội dung cục bộ cần giữ')
    await click(button('Lưu bản nháp'))
    assert.equal(document.getElementById('title').value, 'Nội dung cục bộ cần giữ')
    assert.equal(document.getElementById('title').getAttribute('aria-invalid'), 'true')
    assert.match(document.getElementById('recipe-title-error').textContent, /5 đến 200/)
    assert.doesNotMatch(document.body.textContent, /secret SQL|stack trace/)
  } finally {
    await h.close()
  }
})

test('ingredient server rejection stays beside the field and retains input for retry', async () => {
  const h = await setup()
  try {
    const { ApiProblem } = h.load('lib/api/problem-details.ts')
    h.setApi('createIngredient', async () => {
      throw new ApiProblem({
        status: 422,
        title: 'Invalid',
        code: 'VALIDATION_ERROR',
        errors: { Name: ['secret SQL stack trace'] },
      })
    })
    await h.render(
      React.createElement(
        h.load('features/culinary/pages/dashboard/RecipeCompositionWizard.tsx').RecipeCompositionWizard,
        { recipe: fixture, version: 1, onChanged: async () => {} },
      ),
    )
    const field = document.querySelector('[aria-label="Tên nguyên liệu"]')
    await input(field, 'Nguyên liệu đang nhập')
    await click(button('Thêm nguyên liệu'))
    assert.equal(field.value, 'Nguyên liệu đang nhập')
    assert.equal(field.getAttribute('aria-invalid'), 'true')
    assert.match(document.getElementById('ingredient-name-error').textContent, /Tên nguyên liệu/)
    assert.equal(button('Thêm nguyên liệu').disabled, false)
    assert.doesNotMatch(document.body.textContent, /secret SQL|stack trace/)
  } finally {
    await h.close()
  }
})

test('registration submits trimmed identity fields while preserving password characters', async () => {
  const h = await setup()
  try {
    h.setApi('register', async (...args) => h.requests.push(['register', ...args]))
    await h.render(React.createElement(h.load('features/culinary/pages/auth/Register.tsx').Register))
    for (const [id, value] of [
      ['name', '  Tác giả mới  '],
      ['email', '  author@example.test  '],
      ['password', ' Strong#Password1 '],
      ['confirm', ' Strong#Password1 '],
    ])
      await input(document.getElementById(id), value)
    await click(button('Đăng ký tài khoản'))
    assert.deepEqual(h.requests, [['register', 'Tác giả mới', 'author@example.test', ' Strong#Password1 ']])
    assert.deepEqual(h.navigations, ['/dashboard'])
  } finally {
    await h.close()
  }
})

test('category edit updates the list optimistically and rolls back without losing form data', async () => {
  const h = await setup()
  let rejectSave
  try {
    const original = { id: 'cat', name: 'Danh mục cũ', slug: 'danh-muc-cu', orderIndex: 0, recipeCount: 0 }
    h.setApi('listCategories', async () => [original])
    h.setApi(
      'updateCategory',
      () =>
        new Promise((_resolve, reject) => {
          rejectSave = reject
        }),
    )
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/CategoryAdmin.tsx').CategoryAdmin),
    )
    await click(document.querySelector('[aria-label="Sửa Danh mục cũ"]'))
    await input(document.querySelector('[role="dialog"] input'), 'Danh mục mới')
    await click(button('Lưu thay đổi'))
    assert.equal(h.client.getQueryData(['categories'])[0].name, 'Danh mục mới')
    await act(async () => {
      const { ApiProblem } = h.load('lib/api/problem-details.ts')
      rejectSave(
        new ApiProblem({
          status: 422,
          title: 'Invalid',
          code: 'VALIDATION_ERROR',
          errors: { Name: ['secret SQL stack trace'] },
        }),
      )
      await new Promise((resolve) => {
        setTimeout(resolve, 20)
      })
    })
    assert.equal(h.client.getQueryData(['categories'])[0].name, 'Danh mục cũ')
    assert.equal(document.querySelector('[role="dialog"] input').value, 'Danh mục mới')
    assert.match(document.getElementById('category-name-error').textContent, /2 đến 50/)
    assert.equal(document.querySelector('[role="dialog"] input').getAttribute('aria-invalid'), 'true')
    assert.doesNotMatch(document.body.textContent, /secret SQL|stack trace/)
  } finally {
    await h.close()
  }
})

test('recipe schema blocks negative quantities and nutrition before calling the API', async () => {
  const h = await setup()
  try {
    await h.render(
      React.createElement(h.load('features/culinary/pages/dashboard/RecipeEditor.tsx').RecipeEditor),
    )
    await input(document.getElementById('servings'), '-1')
    await input(document.getElementById('calories'), '-2')
    await click(button('Lưu bản nháp'))
    assert.match(document.getElementById('recipe-servings-error').textContent, /lớn hơn 0/)
    assert.match(document.getElementById('recipe-nutrition-calories-error').textContent, /không âm/)
    assert.equal(h.requests.filter(([operation]) => operation === 'update').length, 0)
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
      await new Promise((resolve) => {
        setTimeout(resolve, 20)
      })
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
              closed += 1
            },
          },
          React.createElement('input', { id: 'inside' }),
          React.createElement('button', { type: 'button', id: 'last' }, 'Cuối'),
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
    await waitUntil(() => h.historyEventCount() === 2)
    assert.deepEqual(targets, ['/first'])
    assert.equal(window.location.pathname, '/second')
    const unload = new window.Event('beforeunload', { cancelable: true })
    window.dispatchEvent(unload)
    assert.equal(unload.defaultPrevented, true)
    await h.render(React.createElement(Guarded, { dirty: false }))
    window.history.back()
    await waitUntil(() => h.historyEventCount() === 3)
    assert.equal(window.location.pathname, '/first')
    await h.render(React.createElement(Guarded, { dirty: true }))
    window.history.forward()
    await waitUntil(() => h.historyEventCount() === 5)
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
    let resolve
    let count = 0
    h.setApi('archiveRecipe', () => {
      count += 1
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
      await new Promise((r) => {
        setTimeout(r, 20)
      })
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
      nextTimer += 1
      timers.set(nextTimer, callback)
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
    // Gestures bubble through the presentation wrapper without changing layout.
    const gesture = hero.querySelector('[role="presentation"]')
    for (const [type, clientX] of [
      ['mousedown', 200],
      ['mousemove', 20],
      ['mouseup', 20],
    ]) {
      await act(async () => gesture.dispatchEvent(new window.MouseEvent(type, { clientX, bubbles: true })))
    }
    assert.equal(slides[0].hasAttribute('inert'), true)
    assert.equal(slides[1].hasAttribute('inert'), false)
    for (const [type, clientX] of [
      ['touchstart', 20],
      ['touchmove', 200],
      ['touchend', 200],
    ]) {
      const event = new window.Event(type, { bubbles: true })
      Object.defineProperty(event, 'touches', { value: [{ clientX }] })
      await act(async () => gesture.dispatchEvent(event))
    }
    assert.equal(slides[0].hasAttribute('inert'), false)
  } finally {
    await h.close()
  }
})

test('content form schemas reject category HTML and accept long Instructions without an invented text limit', async () => {
  const h = await setup()
  try {
    const { categorySchema, recipeSchema } = h.load('features/culinary/forms/content-schemas.ts')
    assert.equal(categorySchema.shape.name.safeParse('<b>Category</b>').success, false)
    assert.equal(categorySchema.shape.name.safeParse('Plain category').success, true)
    assert.equal(recipeSchema.shape.instructions.safeParse('x'.repeat(10001)).success, true)
  } finally {
    await h.close()
  }
})

test('recipe search normalizes Vietnamese, validates short input, and defaults to relevance', async () => {
  const h = await setup()
  try {
    h.setSearch('q=ga&category=mon-an&difficulty=easy&page=2')
    const { RecipesList } = h.load('features/culinary/pages/RecipesList.tsx')
    const result = {
      data: [{ ...fixture, slug: 'ga', status: 'published', title: 'Gà kho', description: 'Gà và gạo.' }],
      meta: { page: 2, total: 18, totalPages: 2 },
    }
    await h.render(React.createElement(RecipesList, { result, categories: [fixture.category] }))
    assert.equal(document.getElementById('recipes-sort-select').value, 'relevance')
    await input(document.getElementById('recipes-search'), 'a')
    await click(document.querySelector('form[role="search"] button[type="submit"]'))
    assert.match(document.getElementById('recipes-search-error').textContent, /ít nhất 2/)
    assert.equal(h.getSearch().get('q'), 'ga')
    await input(document.getElementById('recipes-search'), '  gà   nướng  ')
    await click(document.querySelector('form[role="search"] button[type="submit"]'))
    assert.equal(h.getSearch().get('q'), 'gà nướng')
    assert.equal(h.getSearch().get('category'), 'mon-an')
    assert.equal(h.getSearch().get('difficulty'), 'easy')
    assert.equal(h.getSearch().has('page'), false)
    assert.equal(h.getSearch().get('sort'), null)
    await click(document.querySelector('[aria-label="Xóa từ khóa"]'))
    assert.equal(h.getSearch().has('q'), false)
    assert.equal(h.getSearch().get('category'), 'mon-an')
    assert.equal(document.activeElement.id, 'recipes-search')
  } finally {
    await h.close()
  }
})

test('recipe search shows plain cards and removes one filter without losing the query', async () => {
  const h = await setup()
  try {
    h.setSearch('q=ga&category=mon-an&difficulty=easy&maxTime=90&page=2')
    const { RecipesList } = h.load('features/culinary/pages/RecipesList.tsx')
    const result = {
      data: [
        {
          ...fixture,
          slug: 'xoi-xeo',
          status: 'published',
          title: 'Xôi xéo',
          description: 'Gạo nếp cùng mỡ gà.',
        },
      ],
      meta: { page: 2, total: 18, totalPages: 2 },
    }
    await h.render(React.createElement(RecipesList, { result, categories: [fixture.category] }))
    assert.equal(document.querySelector('article h2').textContent, 'Xôi xéo')
    assert.match(document.querySelector('article').textContent, /Gạo nếp cùng mỡ gà\./)
    assert.doesNotMatch(
      document.querySelector('article').textContent,
      /Khớp mô tả|Khớp không dấu|Khớp tên và mô tả|Tên món gần đúng/,
    )
    assert.equal(document.querySelectorAll('article mark').length, 0)
    await click(document.querySelector('[aria-label="Bỏ lọc danh mục"]'))
    assert.equal(h.getSearch().has('category'), false)
    assert.equal(h.getSearch().has('page'), false)
    assert.equal(h.getSearch().get('q'), 'ga')
    assert.equal(h.getSearch().get('difficulty'), 'easy')
  } finally {
    await h.close()
  }
})

test('recipe search keeps empty and network error states distinct and retries the same query', async () => {
  const h = await setup()
  try {
    h.setSearch('q=ga&category=mon-an')
    const { RecipesList } = h.load('features/culinary/pages/RecipesList.tsx')
    const result = { data: [], meta: { page: 1, total: 0, totalPages: 0 } }
    await h.render(
      React.createElement(RecipesList, { result, categories: [fixture.category], loadError: true }),
    )
    assert.match(document.querySelector('[role="alert"]').textContent, /Chưa tải được kết quả/)
    assert.doesNotMatch(document.querySelector('section').textContent, /Không có công thức phù hợp/)
    await click(button('Thử lại'))
    assert.deepEqual(h.requests, [['refresh']])
    assert.equal(h.getSearch().get('q'), 'ga')
    await h.render(React.createElement(RecipesList, { result, categories: [fixture.category] }))
    assert.match(document.querySelector('section').textContent, /Không có công thức phù hợp/)
    await click(button('Bỏ bộ lọc'))
    assert.equal(h.getSearch().get('q'), 'ga')
    assert.equal(h.getSearch().has('category'), false)
  } finally {
    await h.close()
  }
})

test('search navigation can preserve browser history and scrolling without changing legacy defaults', async () => {
  const h = await setup()
  try {
    const compat = h.load('lib/router-compat.tsx')
    function Probe() {
      const [, set] = compat.useSearchParams()
      return React.createElement(
        'div',
        null,
        React.createElement(
          'button',
          { type: 'button', onClick: () => set({ q: 'ga' }, { replace: false, scroll: false }) },
          'push-search',
        ),
        React.createElement('button', { type: 'button', onClick: () => set({ q: 'pho' }) }, 'legacy-replace'),
      )
    }
    await h.render(React.createElement(Probe))
    await click(button('push-search'))
    assert.equal(h.navigations[0][0], 'push')
    assert.match(h.navigations[0][1], /\?q=ga$/)
    assert.equal(h.navigations[0][2].scroll, false)
    await click(button('legacy-replace'))
    assert.equal(h.navigations[1][0], 'replace')
    assert.equal(h.navigations[1][2].scroll, true)
  } finally {
    await h.close()
  }
})

test('recipe search blocks Enter during Vietnamese IME composition', async () => {
  const h = await setup()
  try {
    h.setSearch('q=ga')
    const { RecipesList } = h.load('features/culinary/pages/RecipesList.tsx')
    await h.render(
      React.createElement(RecipesList, {
        result: { data: [], meta: { page: 1, total: 0, totalPages: 0 } },
        categories: [],
      }),
    )
    const searchInput = document.getElementById('recipes-search')
    const event = new window.KeyboardEvent('keydown', {
      key: 'Enter',
      isComposing: true,
      bubbles: true,
      cancelable: true,
    })
    await act(async () => searchInput.dispatchEvent(event))
    assert.equal(event.defaultPrevented, true)
    assert.equal(h.getSearch().get('q'), 'ga')
  } finally {
    await h.close()
  }
})

test('recipe search recovers from an out-of-range page without discarding filters', async () => {
  const h = await setup()
  try {
    h.setSearch('q=ga&category=mon-an&page=99')
    const { RecipesList } = h.load('features/culinary/pages/RecipesList.tsx')
    await h.render(
      React.createElement(RecipesList, {
        result: { data: [], meta: { page: 99, total: 18, totalPages: 2 } },
        categories: [fixture.category],
      }),
    )
    assert.match(document.querySelector('section').textContent, /Trang này không còn kết quả/)
    await click(button('Về trang đầu'))
    assert.equal(h.getSearch().get('page'), '1')
    assert.equal(h.getSearch().get('q'), 'ga')
    assert.equal(h.getSearch().get('category'), 'mon-an')
  } finally {
    await h.close()
  }
})

test('category create and edit reject invalid names and accept the 50-character boundary', async () => {
  for (const editing of [false, true]) {
    const h = await setup()
    try {
      const category = {
        ...fixture.category,
        orderIndex: 0,
        recipeCount: 0,
        description: null,
        imageUrl: null,
      }
      h.setApi('listCategories', async () => [category])
      h.setApi('createCategory', async (payload) => {
        h.requests.push(['create', payload])
        return { ...category, ...payload }
      })
      h.setApi('updateCategory', async (id, payload) => {
        h.requests.push(['update', id, payload])
        return { ...category, ...payload }
      })
      await h.render(
        React.createElement(h.load('features/culinary/pages/dashboard/CategoryAdmin.tsx').CategoryAdmin),
      )
      await click(editing ? document.querySelector('[aria-label="Sửa Món ăn"]') : button('Thêm danh mục'))
      const dialog = document.querySelector('[role="dialog"]')
      const field = dialog.querySelector('input')
      assert.equal(field.maxLength, 50)
      for (const name of ['x', 'x'.repeat(51), '<b>Món chay</b>']) {
        await input(field, name)
        await act(async () => {
          dialog
            .querySelector('form')
            .dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
          await new Promise((resolve) => {
            setTimeout(resolve, 20)
          })
        })
        assert.equal(h.requests.length, 0)
        assert.match(dialog.textContent, /2 đến 50|HTML/)
      }
      await input(field, 'x'.repeat(50))
      await act(async () => {
        dialog
          .querySelector('form')
          .dispatchEvent(new window.Event('submit', { bubbles: true, cancelable: true }))
        await new Promise((resolve) => {
          setTimeout(resolve, 20)
        })
      })
      assert.equal(h.requests.length, 1)
      assert.equal(h.requests[0][0], editing ? 'update' : 'create')
      assert.equal(h.requests[0].at(-1).name.length, 50)
      assert.equal(document.querySelector('[role="dialog"]'), null)
    } finally {
      await h.close()
    }
  }
})
