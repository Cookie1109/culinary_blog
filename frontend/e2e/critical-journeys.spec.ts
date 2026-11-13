import { test, expect, type Page } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

const password = 'Synthetic#G05User123'
async function register(page: Page, name: string) {
  const email = `${name.toLowerCase().replace(/\W/g, '-')}-${Date.now()}@g05.test`
  await page.goto('/auth/register')
  await page.locator('#name').fill(name)
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('#confirm').fill(password)
  await page.getByRole('button', { name: 'Đăng ký tài khoản', exact: true }).click()
  await expect.poll(() => new URL(page.url()).pathname).toBe('/')
  return email
}
async function login(page: Page, email: string, secret = password) {
  await page.goto('/auth/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(secret)
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
  await expect.poll(() => new URL(page.url()).pathname).toBe('/')
}

async function logout(page: Page) {
  await page.goto('/dashboard')
  const signedOut = page.waitForResponse(
    (response) => response.url().includes('/api/auth/signout') && response.ok(),
  )
  await page
    .getByRole('button', { name: /Đăng xuất/ })
    .first()
    .click()
  await signedOut
  await expect
    .poll(async () => {
      const session = await (await page.request.get('/api/auth/session')).json()
      return Boolean(session?.backendUser)
    })
    .toBe(false)
  await expect.poll(() => new URL(page.url()).pathname).toMatch(/^\/(?:$|(?:auth\/)?login$)/)
}

async function createDraft(page: Page, title: string) {
  await page.goto('/dashboard/recipes/new')
  await page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
  await expect(page.locator('#recipe-title-error')).toBeVisible()
  await page.locator('#title').fill(title)
  await page.locator('#description').fill('Công thức synthetic dùng riêng cho kiểm thử browser.')
  await page.locator('#category').selectOption({ index: 1 })
  await page.locator('#prepTime').fill('10')
  await page.locator('#cookTime').fill('20')
  await page.locator('#servings').fill('2')
  const created = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/recipes') &&
      response.request().method() === 'POST' &&
      response.status() === 201,
  )
  await page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
  const recipe = (await (await created).json()).data
  await expect(page).toHaveURL(/recipes\/.*\/edit/)
  await page.getByRole('textbox', { name: 'Tên nguyên liệu', exact: true }).fill('Bánh phở')
  const ingredient = page.waitForResponse(
    (response) => response.url().endsWith('/ingredients') && response.status() === 201,
  )
  await page.getByRole('button', { name: 'Thêm nguyên liệu', exact: true }).click()
  await ingredient
  await page
    .getByRole('button', { name: /3\. Các bước/ })
    .first()
    .click()
  await page.getByRole('textbox', { name: 'Tiêu đề bước', exact: true }).fill('Nấu phở')
  await page.getByRole('textbox', { name: 'Mô tả bước', exact: true }).fill('Nấu nước dùng và thêm bánh phở.')
  const step = page.waitForResponse(
    (response) => response.url().endsWith('/steps') && response.status() === 201,
  )
  await page.getByRole('button', { name: 'Thêm bước', exact: true }).click()
  await step
  await page
    .getByRole('button', { name: /4\. Hình ảnh/ })
    .first()
    .click()
  const image = page.waitForResponse(
    (response) => response.url().endsWith('/images') && response.status() === 201,
  )
  await page.locator('#recipe-image').setInputFiles('e2e/fixtures/recipe.png')
  await image
  await expect(page.getByText('Thư viện ảnh của công thức (1)')).toBeVisible()
  await page.getByRole('button', { name: /5\. Xem trước & Xuất bản/ }).click()
  await expect(page.getByRole('button', { name: 'Xuất bản', exact: true })).toBeEnabled()
  await expect(page).toHaveURL(/step=preview/)
  return recipe
}

test('E2E-01 register, auto-login, profile and captured welcome mail', async ({ page, request }) => {
  await page.goto('/auth/register')
  await page.getByRole('button', { name: 'Đăng ký tài khoản', exact: true }).click()
  await expect(page.locator('#name-error')).toBeVisible()
  const email = await register(page, 'G05 Author')
  await page.goto('/profile')
  await expect(page.getByText(email, { exact: true })).toBeVisible()
  await page.getByRole('button', { name: 'Cập nhật', exact: true }).click()
  await page.getByRole('textbox', { name: 'Họ và tên', exact: true }).fill('G05 Updated Author')
  await page.getByRole('button', { name: 'Lưu thay đổi', exact: true }).click()
  await expect(page.getByText('Cập nhật hồ sơ thành công.').first()).toBeVisible()
  await expect
    .poll(
      async () => {
        const response = await request.get(`${process.env.E2E_SMTP_URL}/api/v2/messages`)
        const data = await response.json()
        return data.items.some((item: { Content: { Headers: { To: string[] } } }) =>
          item.Content.Headers.To.some((value) => value.includes(email)),
        )
      },
      { timeout: 30_000 },
    )
    .toBe(true)
  await logout(page)
  await page.goto('/auth/register')
  await page.locator('#name').fill('Duplicate author')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill(password)
  await page.locator('#confirm').fill(password)
  await page.getByRole('button', { name: 'Đăng ký tài khoản', exact: true }).click()
  await expect(page.locator('#email-error')).toContainText('Email này đã có tài khoản')
})

test('E2E-02 local login, access expiry, rotation, logout and rejection of old refresh', async ({
  page,
  request,
}) => {
  const email = await register(page, 'G05 Session')
  const privateRead = await page.request.get('/dashboard')
  expect(privateRead.status()).toBe(200)
  expect(privateRead.headers()['set-cookie']).toBeUndefined()
  const sessionRead = await page.request.get('/api/auth/session')
  expect(sessionRead.headers()['set-cookie']).toBeUndefined()
  await logout(page)
  await page.goto('/auth/login')
  await page.locator('#email').fill(email)
  await page.locator('#password').fill('Wrong#Password123')
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
  await expect(
    page.getByRole('alert').filter({ hasText: 'Email hoặc mật khẩu không chính xác' }),
  ).toBeVisible()
  await login(page, email)
  const initial = await page.evaluate(() => sessionStorage.getItem('culinary.refresh-token'))
  expect(initial).toBeTruthy()
  const rotated = page.waitForResponse(
    (response) => response.url().endsWith('/auth/refresh') && response.status() === 200,
    { timeout: 100_000 },
  )
  // The disposable backend issues one-minute access tokens; wait through their real expiry.
  await page.waitForTimeout(65_000)
  await page.goto('/profile')
  await rotated
  await expect(page.getByText(email, { exact: true })).toBeVisible()
  const current = await page.evaluate(() => sessionStorage.getItem('culinary.refresh-token'))
  expect(current).toBeTruthy()
  expect(current).not.toBe(initial)
  await logout(page)
  await page.goto('/profile')
  await expect(page).toHaveURL(/auth\/login/)
  const rejected = await request.post(`${process.env.E2E_API_URL}/api/v1/auth/refresh`, {
    data: { refreshToken: current },
  })
  expect(rejected.status()).toBe(401)
})

test('E2E-03 create draft using UI wizard and preserve private visibility', async ({ page, request }) => {
  await register(page, 'G05 Cook')
  const recipe = await createDraft(page, 'G05 Phở thử nghiệm')
  await expect(page.getByText('Bản nháp', { exact: true }).first()).toBeVisible()
  const hidden = await request.get(`${process.env.E2E_API_URL}/api/v1/recipes/${recipe.slug}`)
  expect(hidden.status()).toBe(404)
  const publicList = await request.get(`${process.env.E2E_API_URL}/api/v1/recipes?pageSize=50`)
  expect(
    (await publicList.json()).data.some((item: { title: string }) => item.title === 'G05 Phở thử nghiệm'),
  ).toBe(false)
  const editor = page.url()
  const popup = page.waitForEvent('popup')
  await page.getByRole('link', { name: /Xem trước riêng tư/ }).click()
  const preview = await popup
  await expect(preview.locator('h1')).toContainText(recipe.title)
  await expect(preview.locator('meta[name="robots"]')).toHaveAttribute('content', /noindex/)
  await preview.close()
  await page.goto(editor)
  await page.getByRole('button', { name: /1\. Thông tin cơ bản/ }).click()
  await page.locator('#title').fill('G05 Local draft copy')
  // Induce a real competing write while the browser retains an earlier aggregate version.
  const session = await (await page.request.get('/api/auth/session')).json()
  const headers = { Authorization: `Bearer ${session.accessToken}` }
  const current = await request.get(`${process.env.E2E_API_URL}/api/v1/me/recipes/${recipe.id}`, { headers })
  const { version } = (await current.json()).data
  const competing = await request.put(`${process.env.E2E_API_URL}/api/v1/recipes/${recipe.id}`, {
    headers: { ...headers, 'If-Match': `"${version}"` },
    data: { title: 'G05 Other editor version' },
  })
  expect(competing.status()).toBe(200)
  await page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
  await expect(page.getByRole('dialog', { name: 'Xung đột phiên bản dữ liệu' })).toBeVisible()
  await expect(page.getByRole('dialog')).toContainText('G05 Local draft copy')
  await page.getByRole('button', { name: 'Tải lại phiên bản mới nhất', exact: true }).click()
  await expect(page.locator('#title')).toHaveValue('G05 Other editor version')
})

test('E2E-04 publish and unpublish invalidate all public views', async ({ page, browser }) => {
  await register(page, 'G05 Publisher')
  const title = 'G05 Phở xuất bản'
  const recipe = await createDraft(page, title)
  const editor = page.url()
  await page.getByRole('button', { name: 'Xuất bản', exact: true }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
  await expect(page.getByText('Đã xuất bản', { exact: true }).first()).toBeVisible()
  const guest = await browser.newContext()
  try {
    const publicPage = await guest.newPage()
    for (const route of [
      '/',
      '/recipes',
      `/categories/${recipe.category.slug}`,
      `/search?q=${encodeURIComponent(title)}`,
      `/recipes/${recipe.slug}`,
    ]) {
      await expect
        .poll(
          async () => {
            await publicPage.goto(route)
            return (await publicPage.locator('article').allTextContents()).join(' ')
          },
          { timeout: 30_000 },
        )
        .toContain(title)
    }
    const outsider = await browser.newContext()
    const admin = await browser.newContext()
    try {
      const otherPage = await outsider.newPage()
      await register(otherPage, 'G05 Other Author')
      await otherPage.goto(editor)
      await expect(
        otherPage.getByRole('alert').filter({ hasText: 'Không thể tải công thức này' }),
      ).toBeVisible()
      const adminPage = await admin.newPage()
      await login(adminPage, 'admin@g05.test', 'Synthetic#G05Admin123')
      await adminPage.goto(`/dashboard/recipes/${recipe.id}/edit?from=system`)
      await adminPage.locator('#description').fill('Công thức đã được Admin kiểm thử cập nhật.')
      const saved = adminPage.waitForResponse(
        (response) =>
          response.url().endsWith(`/recipes/${recipe.id}`) &&
          response.request().method() === 'PUT' &&
          response.status() === 200,
      )
      await adminPage.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
      await saved
    } finally {
      await outsider.close()
      await admin.close()
    }
    await page.goto(editor)
    await page.getByRole('button', { name: 'Gỡ', exact: true }).click()
    await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
    await expect(page.getByText('Bản nháp', { exact: true }).first()).toBeVisible()
    for (const route of [
      '/',
      '/recipes',
      `/categories/${recipe.category.slug}`,
      `/search?q=${encodeURIComponent(title)}`,
    ]) {
      await expect
        .poll(
          async () => {
            await publicPage.goto(route)
            return (await publicPage.locator('article').allTextContents()).join(' ')
          },
          { timeout: 30_000 },
        )
        .not.toContain(title)
    }
    expect((await publicPage.goto(`/recipes/${recipe.slug}`))?.status()).toBe(404)
    await page.getByRole('button', { name: 'Xuất bản', exact: true }).click()
    await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
    await expect(page.getByText('Đã xuất bản', { exact: true }).first()).toBeVisible()
    await page.getByRole('button', { name: 'Lưu trữ', exact: true }).click()
    await page.getByRole('dialog').getByRole('button', { name: 'Xác nhận', exact: true }).click()
    await expect(page.getByText('Đã lưu trữ', { exact: true }).first()).toBeVisible()
    await expect
      .poll(async () => {
        const response = await publicPage.goto(`/recipes/${recipe.slug}`)
        return response?.status()
      })
      .toBe(404)
  } finally {
    await guest.close()
  }
})

test('E2E-05 Vietnamese search, filters, paging, detail and URL history', async ({ page, request }) => {
  // Search fixture setup only; the journey below uses the browser for every search action.
  const registration = await request.post(`${process.env.E2E_API_URL}/api/v1/auth/register`, {
    data: { displayName: 'Search fixture', email: `search-${Date.now()}@g05.test`, password },
  })
  const session = (await registration.json()).data
  const categories = await request.get(`${process.env.E2E_API_URL}/api/v1/categories`)
  const category = (await categories.json()).data[0]
  for (const title of ['G05 Phở bò', 'G05 Phở gà', 'G05 Phở chay']) {
    const response = await request.post(`${process.env.E2E_API_URL}/api/v1/recipes`, {
      headers: { Authorization: `Bearer ${session.accessToken}` },
      data: {
        title,
        description: 'Search fixture recipe.',
        categoryId: category.id,
        prepTime: 5,
        cookTime: 10,
        servings: 2,
        difficulty: 'easy',
        ingredients: [{ name: 'Bánh phở' }],
        steps: [{ title: 'Nấu', description: 'Nấu phở.' }],
      },
    })
    expect(response.status()).toBe(201)
    const recipe = (await response.json()).data
    const published = await request.patch(`${process.env.E2E_API_URL}/api/v1/recipes/${recipe.id}/publish`, {
      headers: { Authorization: `Bearer ${session.accessToken}`, 'If-Match': `"${recipe.version}"` },
    })
    expect(published.status()).toBe(200)
  }
  await page.goto('/search?q=pho')
  await expect(page.locator('article').first()).toContainText(/phở/i)
  await page.locator('#recipes-search').fill('phở')
  await page.route(
    '**/search?*',
    async (route) => {
      await new Promise((resolve) => {
        setTimeout(resolve, 1000)
      })
      await route.continue()
    },
    { times: 1 },
  )
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await expect(page.locator('[aria-busy="true"]')).toBeVisible()
  await expect.poll(() => new URL(page.url()).searchParams.get('q')).toBe('phở')
  await expect(page.locator('main')).toContainText(/phở/i)
  await page.locator('#recipes-sort-select').selectOption('az')
  await expect(page).toHaveURL(/sort=az/)
  await page.goto(`/search?q=pho&pageSize=1&category=${category.slug}&difficulty=easy`)
  await page.getByRole('button', { name: 'Trang 2', exact: true }).click()
  await expect(page).toHaveURL(/page=2/)
  await page.goBack()
  await expect(page).not.toHaveURL(/page=2/)
  await page.goForward()
  await expect(page).toHaveURL(/page=2/)
  await page.locator('article a').first().click()
  await expect(page).toHaveURL(/\/recipes\//)
  await expect(page.locator('h1')).toContainText(/phở/i)
  await page.goto('/search')
  await page.locator('#recipes-search').fill('zzzzg05nomatch')
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await expect(page.locator('main')).toContainText(/không tìm thấy|không có/i)
  await page.locator('#recipes-search').fill('p')
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
  await expect(page.locator('[role="alert"]').first()).toBeVisible()
  expect((await request.post('/__g05__/api/down')).status()).toBe(200)
  try {
    await page.goto('/search?q=pho')
    await expect(page.getByRole('alert').filter({ hasText: 'Chưa tải được kết quả' })).toBeVisible()
    await expect(page.locator('body')).not.toContainText(/stack trace|NpgsqlException/)
  } finally {
    expect((await request.post('/__g05__/api/up')).status()).toBe(200)
  }
  await page.getByRole('button', { name: 'Thử lại', exact: true }).click()
  await expect(page.locator('article').first()).toContainText(/phở/i)
})

test('responsive public, auth, profile and dashboard have no overflow or axe AA violations', async ({
  page,
  browser,
  request,
}, testInfo) => {
  await register(page, 'G05 Responsive')
  const publicRecipes = await request.get(`${process.env.E2E_API_URL}/api/v1/recipes?pageSize=50`)
  const recipe = (await publicRecipes.json()).data.find(
    (item: { primaryImageUrl: string | null }) => item.primaryImageUrl,
  )
  const guest = await browser.newContext()
  const guestPage = await guest.newPage()
  const findings: unknown[] = []
  try {
    for (const width of [320, 767, 768, 1199, 1200, 1440]) {
      await page.setViewportSize({ width, height: 900 })
      await guestPage.setViewportSize({ width, height: 900 })
      for (const route of [
        '/',
        '/recipes',
        '/search?q=pho',
        `/recipes/${recipe.slug}`,
        `/categories/${recipe.category.slug}`,
        '/auth/login',
        '/auth/register',
        '/forgot-password',
        '/profile',
        '/dashboard',
        '/dashboard/recipes',
        '/dashboard/recipes/new',
      ]) {
        const target = route.startsWith('/dashboard') || route === '/profile' ? page : guestPage
        await target.goto(route)
        await expect(target.locator('main').first()).toBeVisible()
        if (route === '/dashboard/recipes/new') await expect(target.locator('#category')).toBeEnabled()
        await target.evaluate(async () => {
          const transitions = document.getAnimations().filter((animation) => {
            const element = (animation.effect as KeyframeEffect | null)?.target
            return element instanceof Element && ['INPUT', 'SELECT', 'TEXTAREA'].includes(element.tagName)
          })
          await Promise.all(transitions.map((animation) => animation.finished.catch(() => {})))
        })
        const overflow = await target.evaluate(() => document.documentElement.scrollWidth > window.innerWidth)
        const controlContrast = await target.evaluate(() => {
          const canvas = document.createElement('canvas')
          canvas.width = 1
          canvas.height = 1
          const context = canvas.getContext('2d')!
          const rgb = (color: string) => {
            context.clearRect(0, 0, 1, 1)
            context.fillStyle = color
            context.fillRect(0, 0, 1, 1)
            return [...context.getImageData(0, 0, 1, 1).data]
          }
          const background = (element: Element): number[] => {
            const color = rgb(getComputedStyle(element).backgroundColor)
            if (color[3] === 255) return color
            const beneath = element.parentElement ? background(element.parentElement) : [255, 255, 255, 255]
            return color.map((value, i) =>
              i === 3 ? 255 : (value * color[3]) / 255 + beneath[i] * (1 - color[3] / 255),
            )
          }
          const luminance = (color: number[]) =>
            color
              .slice(0, 3)
              .map((value) => {
                const channel = value / 255
                return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4
              })
              .reduce((sum, value, i) => sum + value * [0.2126, 0.7152, 0.0722][i], 0)
          return [...document.querySelectorAll<HTMLInputElement>('input, select, textarea')]
            .filter(
              (element) =>
                !element.disabled &&
                element.getBoundingClientRect().width > 0 &&
                !['hidden', 'checkbox', 'radio', 'range'].includes(element.type) &&
                Number.parseFloat(getComputedStyle(element).borderTopWidth) > 0,
            )
            .map((element) => {
              const border = luminance(rgb(getComputedStyle(element).borderTopColor))
              const adjacent = luminance(background(element.parentElement!))
              return {
                control: element.id || element.name || element.tagName,
                ratio: (Math.max(border, adjacent) + 0.05) / (Math.min(border, adjacent) + 0.05),
              }
            })
        })
        await testInfo.attach(`ui-contrast-${width}-${route.replace(/\W/g, '_')}`, {
          body: JSON.stringify(controlContrast),
          contentType: 'application/json',
        })
        const result = await new AxeBuilder({ page: target })
          .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
          .analyze()
        await testInfo.attach(`axe-${width}-${route.replace(/\W/g, '_')}`, {
          body: JSON.stringify(result.violations),
          contentType: 'application/json',
        })
        if (overflow || result.violations.length)
          findings.push({ width, route, overflow, violations: result.violations })
        if (controlContrast.some((item) => item.ratio < 3)) findings.push({ width, route, controlContrast })
        await target.screenshot({ path: testInfo.outputPath(`${width}-${route.replace(/\W/g, '_')}.png`) })
      }
    }
  } finally {
    await guest.close()
  }
  expect(findings).toEqual([])
})

test('mobile touch menu keeps keyboard focus, closes with Escape and restores focus', async ({
  browser,
}, testInfo) => {
  const context = await browser.newContext({
    viewport: { width: 390, height: 844 },
    isMobile: true,
    hasTouch: true,
  })
  try {
    const page = await context.newPage()
    await page.goto('/')
    const trigger = page.getByRole('button', { name: 'Mở menu điều hướng', exact: true })
    await trigger.tap()
    const dialog = page.getByRole('dialog', { name: 'Menu điều hướng di động', exact: true })
    await expect(dialog).toBeVisible()
    const containsFocus = () => dialog.evaluate((element) => element.contains(document.activeElement))
    await expect.poll(containsFocus).toBe(true)
    await page.keyboard.press('Shift+Tab')
    expect(await containsFocus()).toBe(true)
    for (let i = 0; i < 14; i += 1) {
      await page.keyboard.press('Tab')
      expect(await containsFocus()).toBe(true)
    }
    const axe = await new AxeBuilder({ page })
      .include('#mobile-nav-drawer')
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze()
    expect(axe.violations).toEqual([])
    await page.screenshot({ path: testInfo.outputPath('mobile-menu.png') })
    await page.keyboard.press('Escape')
    await expect(dialog).toBeHidden()
    await expect(trigger).toBeFocused()
    await page.keyboard.press('Enter')
    await expect(dialog).toBeVisible()
    await page.keyboard.press('Escape')
    await register(page, 'G05 Touch')
    await page.goto('/dashboard')
    await page.locator('summary').tap()
    await expect(page.getByRole('navigation', { name: 'Điều hướng quản lý trên điện thoại' })).toBeVisible()
    const dashboardMenu = await new AxeBuilder({ page })
      .include('details')
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze()
    expect(dashboardMenu.violations).toEqual([])
    const userMenu = page.getByRole('button', { name: 'Menu người dùng: G05 Touch', exact: true })
    await userMenu.tap()
    await expect(page.getByRole('menu', { name: 'Menu tài khoản', exact: true })).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(page.getByRole('menu', { name: 'Menu tài khoản', exact: true })).toBeHidden()
    await expect(userMenu).toBeFocused()
  } finally {
    await context.close()
  }
})
