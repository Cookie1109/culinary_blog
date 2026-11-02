import assert from 'node:assert/strict'
import test from 'node:test'
import { writeFile } from 'node:fs/promises'
import path from 'node:path'
import { expect as playwrightExpect } from '@playwright/test'
import { withIsolatedApp, password } from './helpers/isolated-app.mjs'
const expect = playwrightExpect.configure({ timeout: 15_000 })
test(
  'Admin user directory works with the real API, database and authenticated browser',
  { timeout: 600_000 },
  async (t) => {
    await withIsolatedApp({}, async ({ api, baseURL, artifacts, browser, command, pg }) => {
      const adminRegistration = await fetch(`${api}/api/v1/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: 'Directory Admin', email: 'admin@users-e2e.test', password }),
      })
      assert.equal(adminRegistration.status, 201)
      const adminId = (await adminRegistration.json()).data.user.id
      assert.match(adminId, /^[0-9a-f-]{36}$/)
      await command('docker', [
        'exec',
        pg.id,
        'psql',
        '-U',
        'users',
        '-d',
        'users',
        '-v',
        'ON_ERROR_STOP=1',
        '-c',
        `INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES ('${adminId}', '00000000-0000-0000-0000-000000000002')`,
      ])
      for (let index = 0; index < 25; index++) {
        const response = await fetch(`${api}/api/v1/auth/register`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            displayName: `DirectoryBrowser ${String(index).padStart(2, '0')}`,
            email: `browser-${index}@users-e2e.test`,
            password,
          }),
        })
        assert.equal(response.status, 201)
      }
      const context = await browser.newContext({ baseURL })
      const page = await context.newPage()
      async function login(email) {
        await page.goto('/login')
        await page.locator('#email').fill(email)
        await page.locator('#password').fill(password)
        await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
        await expect.poll(() => new URL(page.url()).pathname).toMatch(/^\/dashboard/)
      }

      await t.test(
        'Admin lists users, searches name/email, filters roles, paginates and uses mobile navigation',
        async () => {
          await login('admin@users-e2e.test')
          await page.getByRole('link', { name: 'Danh sách người dùng', exact: true }).first().click()
          await expect(page.getByRole('heading', { name: 'Danh sách người dùng' })).toBeVisible()
          await expect(page.locator('tbody tr')).toHaveCount(20)
          await page.getByRole('button', { name: 'Trang sau', exact: true }).click()
          await expect(page.getByText(/Trang 2 \/ /)).toBeVisible()
          await page.getByLabel('Tìm tên hoặc email').fill('DirectoryBrowser')
          await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
          await expect(page.getByText('Tổng cộng 25 người dùng')).toBeVisible()
          await expect(page.getByText('Trang 1 / 2')).toBeVisible()
          await page.getByLabel('Số dòng mỗi trang').selectOption('50')
          await expect(page.locator('tbody tr')).toHaveCount(25)
          await page.getByLabel('Tìm tên hoặc email').fill('BROWSER-7@USERS-E2E.TEST')
          await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
          await expect(page.locator('tbody tr')).toHaveCount(1)
          await expect(page.locator('tbody')).toContainText('DirectoryBrowser 07')
          await page.getByLabel('Vai trò', { exact: true }).selectOption('Admin')
          await expect(page.getByText('Không tìm thấy người dùng phù hợp.')).toBeVisible()
          await page.getByLabel('Tìm tên hoặc email').fill('')
          await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click()
          await expect(page.locator('tbody tr')).toHaveCount(1)
          await expect(page.locator('tbody')).toContainText('admin@users-e2e.test')
          await page.setViewportSize({ width: 375, height: 812 })
          await expect(page.getByRole('list', { name: 'Tài khoản quản trị viên và tác giả' })).toBeVisible()
          assert.equal(
            await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
            true,
          )
          await page.locator('details > summary').click()
          await expect(
            page
              .getByRole('navigation', { name: 'Điều hướng quản lý trên điện thoại' })
              .getByRole('link', { name: 'Danh sách người dùng' }),
          ).toBeVisible()
          await page.screenshot({ path: path.join(artifacts, 'users-mobile.png') })
        },
      )
      await context.close()
      await t.test('Author and guest cannot access the users screen or API', async () => {
        const guest = await browser.newContext({ baseURL })
        const guestPage = await guest.newPage()
        await guestPage.goto('/dashboard/users')
        await expect(guestPage).toHaveURL(/login/)
        assert.equal((await guest.request.get(`${api}/api/v1/admin/users`)).status(), 401)
        await guest.close()
        const authorContext = await browser.newContext({ baseURL })
        const authorPage = await authorContext.newPage()
        const author = await authorContext.request.post(`${api}/api/v1/auth/login`, {
          data: { email: 'browser-0@users-e2e.test', password },
        })
        const token = (await author.json()).data.accessToken
        assert.equal(
          (
            await authorContext.request.get(`${api}/api/v1/admin/users`, {
              headers: { Authorization: `Bearer ${token}` },
            })
          ).status(),
          403,
        )
        await authorPage.goto('/login')
        await authorPage.locator('#email').fill('browser-0@users-e2e.test')
        await authorPage.locator('#password').fill(password)
        await authorPage.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
        await expect.poll(() => new URL(authorPage.url()).pathname).toMatch(/^\/dashboard/)
        await expect
          .poll(
            async () =>
              (await (await authorContext.request.get('/api/auth/session')).json()).backendUser?.roles,
          )
          .toEqual(['Author'])
        let directoryRequests = 0
        authorPage.on('request', (request) => {
          if (request.url().includes('/api/v1/admin/users')) directoryRequests++
        })
        await authorPage.goto('/dashboard/users')
        try {
          await expect(authorPage.getByRole('alert').filter({ hasText: 'Chỉ quản trị viên' })).toBeVisible()
        } catch (error) {
          await writeFile(
            path.join(artifacts, 'author-state.json'),
            JSON.stringify({ url: authorPage.url(), text: await authorPage.locator('body').innerText() }),
          )
          throw error
        }
        assert.equal(directoryRequests, 0)
        await expect(authorPage.getByRole('link', { name: 'Danh sách người dùng' })).toHaveCount(0)
        await authorContext.close()
      })
    })
  },
)
