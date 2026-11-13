import assert from 'node:assert/strict'
import path from 'node:path'
import test from 'node:test'
import { expect as playwrightExpect } from '@playwright/test'
import { withIsolatedApp, password } from './helpers/isolated-app.mjs'

const expect = playwrightExpect.configure({ timeout: 30_000 })
const eventual = playwrightExpect.configure({ timeout: 60_000 })
const png =
  'iVBORw0KGgoAAAANSUhEUgAAACAAAAAQAQMAAABNzu8aAAAAIGNIUk0AAHomAACAhAAA+gAAAIDoAAB1MAAA6mAAADqYAAAXcJy6UTwAAAADUExURf8AABniCTcAAAABYktHRACIBR1IAAAAB3RJTUUH6gsCDBIvfO3fMAAAACV0RVh0ZGF0ZTpjcmVhdGUAMjAyNi0xMS0wMlQxMjoxODo0NyswMDowMCWvWXIAAAAldEVYdGRhdGU6bW9kaWZ5ADIwMjYtMTEtMDJUMTI6MTg6NDcrMDA6MDBU8uHOAAAAKHRFWHRkYXRlOnRpbWVzdGFtcAAyMDI2LTExLTAyVDEyOjE4OjQ3KzAwOjAwA+fAEQAAAAxJREFUCNdjYKAuAAAAUAABIhPodQAAAABJRU5ErkJggg=='

test(
  'real recipe lifecycle preserves drafts, enforces ownership and versions, refreshes public pages and cleans deleted media',
  { timeout: 600_000 },
  async () => {
    await withIsolatedApp(
      { media: true },
      async ({ api, baseURL, artifacts, browser, pg, minio, command }) => {
        async function register(email) {
          const response = await fetch(`${api}/api/v1/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ displayName: email.split('@')[0], email, password }),
          })
          assert.equal(response.status, 201)
          return (await response.json()).data.accessToken
        }
        async function login(email) {
          const context = await browser.newContext({ baseURL })
          const page = await context.newPage()
          await page.goto('/login')
          const emailInput = page.locator('#email:visible')
          await expect(emailInput).toHaveCount(1)
          await emailInput.fill(email)
          await page.locator('#password:visible').fill(password)
          await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
          await expect.poll(() => new URL(page.url()).pathname).toBe('/')
          return { context, page }
        }
        const email = 'owner@recipe-e2e.test'
        const registration = await browser.newContext({ baseURL })
        const registerPage = await registration.newPage()
        await registerPage.goto('/register')
        await registerPage.locator('#name').fill('Recipe lifecycle author')
        await registerPage.locator('#email').fill(email)
        await registerPage.locator('#password').fill(password)
        await registerPage.locator('#confirm').fill(password)
        const registered = registerPage.waitForResponse(
          (response) =>
            new URL(response.url()).pathname.endsWith('/auth/register') &&
            response.request().method() === 'POST',
        )
        await registerPage.getByRole('button', { name: 'Đăng ký tài khoản', exact: true }).click()
        assert.equal((await registered).status(), 201)
        await expect.poll(() => new URL(registerPage.url()).pathname).toBe('/')
        assert.equal((await registration.request.get('/dashboard')).headers()['set-cookie'], undefined)
        assert.equal((await registration.request.get('/api/auth/session')).headers()['set-cookie'], undefined)
        await registerPage.goto('/dashboard')
        await registerPage.getByRole('button', { name: 'Đăng xuất', exact: true }).click()
        await expect
          .poll(async () =>
            Boolean((await (await registration.request.get('/api/auth/session')).json())?.backendUser),
          )
          .toBe(false)
        await registerPage.goto('/dashboard')
        await expect(registerPage).toHaveURL(/\/login/)
        await expect(registerPage.getByRole('main')).toBeVisible()
        await registration.close()

        const otherToken = await register('other@recipe-e2e.test')
        const { context, page } = await login(email)
        const token = (await (await context.request.get('/api/auth/session')).json()).accessToken
        assert.ok(token)
        const title = 'Step3 lifecycle synthetic recipe'
        await page.goto('/dashboard/recipes/new')
        await page.locator('#title').fill('abc')
        await page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
        await expect(page.locator('#title')).toHaveAttribute('aria-invalid', 'true')
        await page.locator('#title').fill(title)
        await page.locator('#description').fill('Draft description preserved across login and reload.')
        await page.locator('#category').selectOption({ index: 1 })
        const created = page.waitForResponse(
          (r) => r.request().method() === 'POST' && r.url().endsWith('/api/v1/recipes'),
        )
        await page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
        const createResponse = await created
        assert.equal(createResponse.status(), 201)
        const { id } = (await createResponse.json()).data
        await expect(page).toHaveURL(/\/edit/)
        const editUrl = `/dashboard/recipes/${id}/edit`
        async function detail() {
          const response = await fetch(`${api}/api/v1/me/recipes/${id}`, {
            headers: { Authorization: `Bearer ${token}` },
          })
          assert.equal(response.status, 200)
          return (await response.json()).data
        }
        async function direct(suffix, method, body, version, accessToken = token) {
          return fetch(`${api}/api/v1/recipes/${id}${suffix}`, {
            method,
            headers: {
              Authorization: `Bearer ${accessToken}`,
              'If-Match': `"${version ?? (await detail()).version}"`,
              'Content-Type': 'application/json',
            },
            body: body ? JSON.stringify(body) : undefined,
          })
        }
        async function stage(name) {
          await page
            .getByRole('button', { name: new RegExp(name) })
            .first()
            .click()
        }
        async function write(label, method, suffix, status) {
          const response = page.waitForResponse(
            (r) => r.request().method() === method && r.url().endsWith(suffix),
          )
          await page.getByRole('button', { name: label, exact: true }).click()
          assert.equal((await response).status(), status)
          await expect.poll(async () => (await detail()).version).toBeGreaterThan(1)
          if (suffix.includes('/ingredients'))
            await expect(page.getByRole('textbox', { name: 'Tên nguyên liệu', exact: true })).toHaveValue('')
          if (suffix.includes('/steps'))
            await expect(page.getByRole('textbox', { name: 'Tiêu đề bước', exact: true })).toHaveValue('')
        }
        await expect(page.getByRole('button', { name: 'Xuất bản', exact: true })).toBeDisabled()
        assert.equal((await direct('/publish', 'PATCH')).status, 422)
        await page.getByRole('button', { name: 'Thêm nguyên liệu', exact: true }).click()
        await expect(page.getByRole('textbox', { name: 'Tên nguyên liệu', exact: true })).toHaveAttribute(
          'aria-invalid',
          'true',
        )
        await page.getByRole('textbox', { name: 'Tên nguyên liệu', exact: true }).fill('Rice')
        await page.getByRole('spinbutton', { name: 'Số lượng', exact: true }).fill('200')
        await write('Thêm nguyên liệu', 'POST', '/ingredients', 201)
        await expect(page.locator('article').filter({ hasText: 'Rice' })).toBeVisible()
        await page
          .locator('article')
          .filter({ hasText: 'Rice' })
          .getByRole('button', { name: 'Sửa', exact: true })
          .click()
        await page.getByRole('textbox', { name: 'Tên nguyên liệu', exact: true }).fill('Brown rice')
        await write('Lưu nguyên liệu', 'PUT', `/ingredients/${(await detail()).ingredients[0].id}`, 200)
        await stage('3\\. Các bước')
        await page.getByRole('textbox', { name: 'Tiêu đề bước', exact: true }).fill('Cook rice')
        await page.getByRole('textbox', { name: 'Mô tả bước', exact: true }).fill('Cook until tender.')
        await write('Thêm bước', 'POST', '/steps', 201)
        await expect(page.locator('ol li').filter({ hasText: 'Cook rice' })).toHaveCount(1)
        await page
          .locator('ol li')
          .filter({ hasText: 'Cook rice' })
          .getByRole('button', { name: 'Sửa', exact: true })
          .click()
        await page.getByRole('textbox', { name: 'Mô tả bước', exact: true }).fill('Cook slowly until tender.')
        await write('Lưu bước', 'PUT', `/steps/${(await detail()).steps[0].id}`, 200)
        await stage('4\\. Hình ảnh')
        const uploaded = page.waitForResponse(
          (response) => response.request().method() === 'POST' && response.url().endsWith('/images'),
        )
        await page
          .locator('#recipe-image')
          .setInputFiles({ name: 'dish.png', mimeType: 'image/png', buffer: Buffer.from(png, 'base64') })
        assert.equal((await uploaded).status(), 201)
        await expect.poll(async () => (await detail()).images[0]?.processingStatus).toBe('ready')
        const saved = await detail()
        // A new authenticated browser session proves persistence beyond client memory.
        const second = await login(email)
        await second.page.goto(editUrl)
        await expect(second.page.locator('#title')).toHaveValue(title)
        assert.equal(saved.ingredients[0].name, 'Brown rice')
        assert.equal(saved.steps[0].description, 'Cook slowly until tender.')
        await page.reload()
        await expect(page.locator('section[aria-labelledby="composition-heading"] article')).toHaveCount(1)
        const other = await login('other@recipe-e2e.test')
        await other.page.goto(editUrl)
        await expect(
          other.page.getByRole('alert').filter({ hasText: 'Không thể tải công thức' }),
        ).toContainText('Không thể tải công thức')
        const payload = ({
          title: recipeTitle,
          description,
          category,
          prepTime,
          cookTime,
          servings,
          difficulty,
          nutrition,
        }) => ({
          title: recipeTitle,
          description,
          categoryId: category.id,
          prepTime,
          cookTime,
          servings,
          difficulty,
          nutrition,
        })
        assert.equal((await direct('', 'PUT', payload(saved), saved.version, otherToken)).status, 403)
        const admin = await login('admin@users-e2e.test')
        await admin.page.goto(`${editUrl}?from=system`)
        await admin.page.locator('#description').fill('Edited by administrator.')
        const adminSaved = admin.page.waitForResponse(
          (r) => r.request().method() === 'PUT' && r.url().endsWith(`/recipes/${id}`),
        )
        await admin.page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
        assert.equal((await adminSaved).status(), 200)
        assert.equal((await detail()).description, 'Edited by administrator.')
        // Second owner session still has the old ETag: retain input, then explicitly reload.
        await second.page.locator('#title').fill('Local conflict input')
        const conflict = second.page.waitForResponse(
          (r) => r.status() === 409 && r.request().method() === 'PUT',
        )
        await second.page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click()
        await conflict
        await expect(second.page.getByRole('dialog')).toBeVisible()
        await expect(second.page.locator('#title')).toHaveValue('Local conflict input')
        await second.page.getByRole('button', { name: 'Tải lại phiên bản mới nhất', exact: true }).click()
        await expect(second.page.locator('#description')).toHaveValue('Edited by administrator.')
        await expect(second.page.getByRole('dialog')).toHaveCount(0)
        assert.equal(
          (await direct('', 'PUT', { ...payload(saved), title: 'Should not persist', servings: 0 })).status,
          422,
        )
        assert.equal((await detail()).title, title)
        // Reopen owner editor on current data for real lifecycle actions.
        await page.goto(editUrl)
        await expect(page.locator('#description')).toHaveValue('Edited by administrator.')
        const current = await detail()
        const paths = [
          '/',
          '/recipes',
          `/categories/${current.category.slug}`,
          `/recipes?q=${encodeURIComponent(title)}`,
          `/recipes/${current.slug}`,
        ]
        const guest = await browser.newContext({ baseURL })
        const publicPage = await guest.newPage()
        // Prime actual production caches before first publication.
        for (const route of paths) await publicPage.goto(route)
        async function visiblePublic(visible) {
          for (const route of paths) {
            await eventual
              .poll(
                async () => {
                  await publicPage.goto(route)
                  if (route === `/recipes/${current.slug}`)
                    return publicPage
                      .getByRole('heading', { name: title, exact: true })
                      .count()
                      .then((n) => n > 0)
                  return publicPage
                    .locator(`main a[href="/recipes/${current.slug}"]`)
                    .count()
                    .then((n) => n > 0)
                },
                { message: `${route} published=${visible}` },
              )
              .toBe(visible)
          }
          const image = await fetch(`${baseURL}/api/public-media/${current.images[0].id}/original`)
          assert.equal(image.status, visible ? 200 : 404)
        }
        async function lifecycle(label, suffix) {
          await page.getByRole('button', { name: label, exact: true }).first().click()
          const dialog = page.getByRole('dialog')
          await expect(dialog).toBeVisible()
          const response = page.waitForResponse(
            (r) =>
              r.url().endsWith(`/recipes/${id}${suffix}`) &&
              ['PATCH', 'DELETE'].includes(r.request().method()),
          )
          await dialog.getByRole('button', { name: 'Xác nhận', exact: true }).click()
          const result = await response
          assert.equal(result.status(), suffix ? 200 : 204)
          await expect(dialog).toHaveCount(0)
        }
        await lifecycle('Xuất bản', '/publish')
        await visiblePublic(true)
        await publicPage.goto(paths[3])
        await expect(publicPage.getByRole('group', { name: 'Bộ lọc đang áp dụng' })).toBeVisible()
        const keyword = publicPage.getByRole('button', { name: 'Bỏ từ khóa', exact: true })
        await expect(keyword).toBeVisible()
        await expect(keyword).toHaveText(`Từ khóa: ${title}`)
        const publishedVersion = (await detail()).version
        assert.equal((await direct('/publish', 'PATCH')).status, 409)
        assert.equal((await detail()).version, publishedVersion)
        await lifecycle('Gỡ', '/unpublish')
        assert.equal((await detail()).status, 'draft')
        await visiblePublic(false)
        await lifecycle('Lưu trữ', '/archive')
        assert.equal((await detail()).status, 'archived')
        assert.equal((await direct('/publish', 'PATCH')).status, 409)
        await lifecycle('Khôi phục', '/unarchive')
        assert.equal((await detail()).status, 'draft')
        await lifecycle('Xuất bản', '/publish')
        await visiblePublic(true)
        await lifecycle('Lưu trữ', '/archive')
        await visiblePublic(false)
        await lifecycle('Khôi phục', '/unarchive')
        await lifecycle('Xuất bản', '/publish')
        await visiblePublic(true)
        await page.goto('/dashboard/recipes')
        await expect(page.locator('table').getByText(title, { exact: true })).toBeVisible()
        await page.setViewportSize({ width: 390, height: 844 })
        await expect(
          page.locator('article').filter({ hasText: title }).getByRole('link', { name: 'Sửa', exact: true }),
        ).toBeVisible()
        await page.screenshot({ path: path.join(artifacts, 'recipe-list-mobile.png'), fullPage: true })
        await page.setViewportSize({ width: 1280, height: 900 })
        await page.goto(editUrl)
        await expect(page.locator('#title')).toHaveValue(title)
        await lifecycle('Xóa công thức', '')
        await expect(page).toHaveURL(/\/dashboard\/recipes$/)
        await expect(page.getByText(title, { exact: true })).toHaveCount(0)
        assert.equal(
          (await fetch(`${api}/api/v1/me/recipes/${id}`, { headers: { Authorization: `Bearer ${token}` } }))
            .status,
          404,
        )
        assert.equal(
          (
            await fetch(`${api}/api/v1/media/${current.images[0].id}/original`, {
              headers: { Authorization: `Bearer ${token}` },
            })
          ).status,
          404,
        )
        const sql = `SELECT "IsDeleted", "DeletedAt" IS NOT NULL FROM "Recipes" WHERE "Id"='${id}'; SELECT "IsDeleted" FROM "RecipeImages" WHERE "RecipeId"='${id}'; SELECT COUNT(*) FROM "MediaOutbox" WHERE "Type"='objects.delete' AND "Payload"::text LIKE '%${id.replaceAll('-', '')}%';`
        const rows = await command('docker', [
          'exec',
          pg.id,
          'psql',
          '-U',
          'users',
          '-d',
          'users',
          '-tA',
          '-c',
          sql,
        ])
        assert.match(rows, /t\|t\s+t\s+1/)
        await eventual
          .poll(async () =>
            command('docker', [
              'exec',
              minio.id,
              'mc',
              'ls',
              '--recursive',
              '--json',
              `test/media-test/recipes/${id.replaceAll('-', '')}/`,
            ]),
          )
          .toBe('')
        await visiblePublic(false)
        await Promise.all([
          context.close(),
          second.context.close(),
          admin.context.close(),
          other.context.close(),
          guest.close(),
        ])
      },
    )
  },
)
