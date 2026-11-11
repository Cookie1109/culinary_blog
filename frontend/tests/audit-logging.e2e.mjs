import assert from 'node:assert/strict'
import { writeFile } from 'node:fs/promises'
import path from 'node:path'
import test from 'node:test'
import { expect as playwrightExpect } from '@playwright/test'
import { withIsolatedApp, password } from './helpers/isolated-app.mjs'

const expect = playwrightExpect.configure({ timeout: 30_000 })
const png =
  'iVBORw0KGgoAAAANSUhEUgAAACAAAAAQAQMAAABNzu8aAAAAIGNIUk0AAHomAACAhAAA+gAAAIDoAAB1MAAA6mAAADqYAAAXcJy6UTwAAAADUExURf8AABniCTcAAAABYktHRACIBR1IAAAAB3RJTUUH6gsCDBIvfO3fMAAAACV0RVh0ZGF0ZTpjcmVhdGUAMjAyNi0xMS0wMlQxMjoxODo0NyswMDowMCWvWXIAAAAldEVYdGRhdGU6bW9kaWZ5ADIwMjYtMTEtMDJUMTI6MTg6NDcrMDA6MDBU8uHOAAAAKHRFWHRkYXRlOnRpbWVzdGFtcAAyMDI2LTExLTAyVDEyOjE4OjQ3KzAwOjAwA+fAEQAAAAxJREFUCNdjYKAuAAAAUAABIhPodQAAAABJRU5ErkJggg=='

test(
  'audit history records every real mutation, request and media origin, survives API restart and is Admin-only',
  { timeout: 600_000 },
  async () => {
    await withIsolatedApp(
      { media: true },
      async ({ api, baseURL, artifacts, browser, restartApi, readApiLogs }) => {
        const prefix = 'audit-e2e-' + Date.now()
        const secrets = [
          password,
          'Wrong#PasswordOnly123',
          'synthetic-reset-token-secret',
          'synthetic-refresh-token-secret',
          'synthetic-multipart-secret',
        ]
        const requests = []
        const expected = []
        const started = Date.now()
        function remember(session) {
          secrets.push(session.accessToken, session.refreshToken)
          return session
        }
        const actor = (token) =>
          token ? JSON.parse(Buffer.from(token.split('.')[1], 'base64url')).sub : 'anonymous'
        async function call(route, { method = 'GET', token, body, version, status = 200, event } = {}) {
          const correlation = `${prefix}-${requests.length}`
          const multipart = body instanceof FormData
          const response = await fetch(`${api}/api/v1${route}`, {
            method,
            headers: {
              'X-Correlation-ID': correlation,
              ...(token ? { Authorization: `Bearer ${token}` } : {}),
              ...(body && !multipart ? { 'Content-Type': 'application/json' } : {}),
              ...(version ? { 'If-Match': `"${version}"` } : {}),
            },
            body: body ? (multipart ? body : JSON.stringify(body)) : undefined,
          })
          assert.equal(response.status, status, `${method} ${route}`)
          assert.equal(response.headers.get('x-correlation-id'), correlation)
          requests.push({
            correlation,
            method,
            path: `/api/v1${route.split('?')[0]}`,
            status,
            actor: actor(token),
          })
          if (event) expected.push({ event, correlation })
          return { data: status === 204 ? null : (await response.json()).data, correlation }
        }
        const owner = remember(
          (
            await call('/auth/register', {
              method: 'POST',
              status: 201,
              body: { email: 'owner@audit-e2e.test', displayName: 'Audit Owner', password },
            })
          ).data,
        )
        const other = remember(
          (
            await call('/auth/register', {
              method: 'POST',
              status: 201,
              body: { email: 'other@audit-e2e.test', displayName: 'Other Author', password },
            })
          ).data,
        )
        const admin = remember(
          (await call('/auth/login', { method: 'POST', body: { email: 'admin@users-e2e.test', password } }))
            .data,
        )
        await call('/admin/audit-logs', { status: 401 })
        await call('/admin/audit-logs', { token: owner.accessToken, status: 403 })
        await call('/auth/me', { token: owner.accessToken })
        const categoryId = (await call('/categories')).data[0].id
        const payload = (title) => ({
          title,
          description: 'Persistent audit test',
          categoryId,
          prepTime: 1,
          cookTime: 0,
          servings: 1,
          difficulty: 'easy',
        })
        const created = await call('/recipes', {
          method: 'POST',
          token: owner.accessToken,
          body: payload('Audit recipe original'),
          status: 201,
          event: 'recipe.created',
        })
        const id = created.data.id
        const route = `/recipes/${id}`
        let version = 1
        async function mutation(suffix, method, body, event, status = 200) {
          const result = await call(route + suffix, {
            method,
            body,
            token: owner.accessToken,
            version: version++,
            event,
            status,
          })
          return result
        }
        await mutation('', 'PUT', payload('Audit recipe edit one'), 'recipe.updated')
        await mutation('', 'PUT', payload('Audit recipe edit two'), 'recipe.updated')
        await mutation('/ingredients', 'POST', { name: 'Rice' }, 'recipe.ingredients.created', 201)
        await mutation(
          '/steps',
          'POST',
          { title: 'Cook', description: 'Cook rice' },
          'recipe.steps.created',
          201,
        )
        const imageForm = new FormData()
        imageForm.append(
          'file',
          new Blob([Buffer.from(png, 'base64')], { type: 'image/png' }),
          'synthetic-multipart-secret.png',
        )
        imageForm.append('altText', 'synthetic-multipart-secret')
        const upload = await mutation('/images', 'POST', imageForm, 'recipe.images.created', 201)
        await expect
          .poll(async () =>
            (await readApiLogs()).some(
              (entry) => entry.EventId?.Name === 'ProcessingCompleted' && entry.ImageId === upload.data.id,
            ),
          )
          .toBe(true)
        const resizeLog = (await readApiLogs()).find(
          (entry) => entry.EventId?.Name === 'ProcessingCompleted' && entry.ImageId === upload.data.id,
        )
        assert.equal(resizeLog.CorrelationId, upload.correlation)
        assert.equal(resizeLog.UserId, actor(owner.accessToken))
        assert.ok(resizeLog.OutboxId)
        // Separate the last explicit edit from transitions to catch snapshot-derived phantom edits.
        await new Promise((resolve) => setTimeout(resolve, 1100))
        for (const [suffix, event] of [
          ['publish', 'published'],
          ['unpublish', 'unpublished'],
          ['publish', 'published'],
          ['archive', 'archived'],
          ['unarchive', 'unarchived'],
        ]) {
          await mutation('/' + suffix, 'PATCH', undefined, 'recipe.' + event)
        }
        await call(route, {
          method: 'PUT',
          token: other.accessToken,
          version,
          body: payload('Forbidden update'),
          status: 403,
        })
        await call(route, {
          method: 'PUT',
          token: owner.accessToken,
          version,
          body: payload('abc'),
          status: 422,
        })
        await call(route, { method: 'DELETE', token: owner.accessToken, version: 1, status: 409 })
        await call('/auth/login', {
          method: 'POST',
          body: { email: 'owner@audit-e2e.test', password: secrets[1] },
          status: 401,
        })
        await call('/auth/reset-password', {
          method: 'POST',
          body: { email: 'owner@audit-e2e.test', token: secrets[2], newPassword: password },
          status: 400,
        })
        await call('/auth/refresh', { method: 'POST', body: { refreshToken: secrets[3] }, status: 401 })
        // A rejected multipart body must not become either a successful audit or raw log content.
        const invalidImage = new FormData()
        invalidImage.append(
          'file',
          new Blob(['synthetic-multipart-secret'], { type: 'image/png' }),
          'synthetic-multipart-secret.png',
        )
        await call(route + '/images', {
          method: 'POST',
          token: owner.accessToken,
          version,
          body: invalidImage,
          status: 400,
        })
        const deleted = await mutation('', 'DELETE', undefined, 'recipe.deleted', 204)
        await expect
          .poll(async () =>
            (await readApiLogs()).some(
              (entry) =>
                entry.EventId?.Name === 'DeletionCompleted' && entry.CorrelationId === deleted.correlation,
            ),
          )
          .toBe(true)
        const deletionLog = (await readApiLogs()).find(
          (entry) =>
            entry.EventId?.Name === 'DeletionCompleted' && entry.CorrelationId === deleted.correlation,
        )
        assert.equal(deletionLog.UserId, actor(owner.accessToken))
        assert.ok(deletionLog.OutboxId)
        // Verify the existing Command and Query pipeline on actual HTTP requests.
        await call('/recipes')
        let logs = await readApiLogs()
        assert.ok(
          logs.some(
            (entry) =>
              entry.ApplicationRequest === 'DeleteRecipeCommand' && entry.EventId?.Name === 'HandledRequest',
          ),
        )
        assert.ok(
          logs.some(
            (entry) =>
              entry.ApplicationRequest === 'ListPublishedRecipesQuery' &&
              entry.EventId?.Name === 'HandledRequest',
          ),
        )
        assert.ok(
          logs.some(
            (entry) =>
              entry.ApplicationRequest === 'DeleteRecipeCommand' &&
              entry.EventId?.Name === 'RequestRejected' &&
              entry['@l'] === 'Warning',
          ),
        )
        for (const request of requests) {
          const entry = logs.find(
            (entry) =>
              entry.CorrelationId === request.correlation &&
              entry.SourceContext === 'Serilog.AspNetCore.RequestLoggingMiddleware',
          )
          assert.ok(entry, `Missing request log ${request.correlation}`)
          assert.equal(entry.RequestMethod, request.method)
          assert.equal(entry.RequestPath, request.path)
          assert.equal(entry.StatusCode, request.status)
          assert.equal(entry.UserId, request.actor)
          assert.ok(Number.isFinite(entry.Elapsed) && entry.Elapsed >= 0)
          if (request.status >= 400)
            assert.ok(
              !logs.some((entry) => entry.CorrelationId === request.correlation && entry.AuditEvent),
              'Rejected mutation appeared as successful',
            )
        }
        async function audit(search, pageSize = 100, page = 1) {
          const response = await fetch(
            `${api}/api/v1/admin/audit-logs?search=${encodeURIComponent(search)}&pageSize=${pageSize}&page=${page}`,
            { headers: { Authorization: `Bearer ${admin.accessToken}` } },
          )
          assert.equal(response.status, 200)
          return response.json()
        }
        const history = (await audit(id)).data.filter((entry) => entry.eventName.startsWith('recipe.'))
        assert.equal(history.length, expected.length)
        for (const action of expected) {
          const matches = history.filter((entry) => entry.correlationId === action.correlation)
          assert.equal(matches.length, 1)
          const entry = matches[0]
          assert.equal(entry.eventName, action.event)
          assert.equal(entry.userId, actor(owner.accessToken))
          assert.equal(entry.properties.UserName, 'Audit Owner')
          assert.equal(entry.properties.EntityId, id)
          assert.notEqual(entry.properties.Source, 'snapshot')
          assert.ok(Date.parse(entry.timestamp) >= started && Date.parse(entry.timestamp) <= Date.now())
        }
        assert.equal(history.filter((entry) => entry.eventName === 'recipe.updated').length, 2)
        assert.equal(history.filter((entry) => entry.eventName === 'recipe.published').length, 2)
        const idsBeforeRestart = history.map((entry) => entry.id).sort()
        await restartApi()
        assert.deepEqual(
          (await audit(id)).data
            .filter((entry) => entry.eventName.startsWith('recipe.'))
            .map((entry) => entry.id)
            .sort(),
          idsBeforeRestart,
        )
        async function login(email) {
          const context = await browser.newContext({ baseURL })
          const page = await context.newPage()
          page.on('response', async (response) => {
            if (response.url().endsWith('/api/v1/auth/login') && response.status() === 200)
              remember((await response.json()).data)
          })
          await page.goto('/login')
          const emailInput = page.locator('#email:visible')
          await expect(emailInput).toHaveCount(1)
          await emailInput.fill(email)
          await page.locator('#password:visible').fill(password)
          await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
          await expect.poll(() => new URL(page.url()).pathname).toBe('/')
          return { context, page }
        }
        const browserAdmin = await login('admin@users-e2e.test')
        const page = browserAdmin.page
        await page.goto('/dashboard/audit')
        await expect(page.getByRole('heading', { name: 'Lịch sử hệ thống', exact: true })).toBeVisible()
        const search = page.getByRole('textbox', {
          name: 'Tìm lịch sử theo người thực hiện, nội dung hoặc sự kiện',
          exact: true,
        })
        await page.getByRole('button', { name: 'Thông tin', exact: true }).click()
        await search.fill(id)
        const table = page.locator('table')
        await expect(table.locator('tbody tr')).toHaveCount(expected.length)
        await expect(table.getByText('Cập nhật công thức', { exact: true })).toHaveCount(2)
        await expect(table.getByText('Xuất bản công thức', { exact: true })).toHaveCount(2)
        await expect(table.getByText('Audit Owner', { exact: true })).toHaveCount(expected.length)
        await table.getByRole('button', { name: 'Xem', exact: true }).first().click()
        await expect(page.getByRole('dialog')).toContainText(deleted.correlation)
        await page.getByRole('dialog').getByRole('button', { name: 'Đóng', exact: true }).click()
        await page.getByRole('button', { name: 'Tất cả', exact: true }).click()
        await search.fill(prefix)
        await expect(table.locator('tbody tr')).toHaveCount(15)
        const fullAudit = await audit(prefix)
        assert.ok(fullAudit.meta.total > 15)
        await page.getByRole('button', { name: 'Sau', exact: true }).click()
        await expect(page.getByText(/Trang 2 \/ /)).toBeVisible()
        await search.fill(deleted.correlation)
        await expect(table.locator('tbody tr')).toHaveCount(1)
        await page.getByRole('button', { name: 'Cảnh báo', exact: true }).click()
        await expect(
          page.getByText('Không tìm thấy nhật ký kiểm toán nào phù hợp với bộ lọc hiện tại.', {
            exact: true,
          }),
        ).toBeVisible()
        await search.fill(prefix)
        await expect.poll(async () => table.locator('tbody tr').count()).toBeGreaterThan(0)
        await page.getByRole('button', { name: 'Thông tin', exact: true }).click()
        await search.fill(id)
        await expect(table.locator('tbody tr')).toHaveCount(expected.length)
        await page.screenshot({ path: path.join(artifacts, 'admin-audit-history.png'), fullPage: true })
        const browserAuthor = await login('owner@audit-e2e.test')
        let forbiddenAuditReads = 0
        browserAuthor.page.on('request', (request) => {
          if (request.url().includes('/api/v1/admin/audit-logs')) forbiddenAuditReads++
        })
        await browserAuthor.page.goto('/dashboard/audit')
        await expect.poll(() => new URL(browserAuthor.page.url()).pathname).toBe('/dashboard')
        await expect(
          browserAuthor.page.getByRole('heading', { name: 'Lịch sử hệ thống', exact: true }),
        ).toHaveCount(0)
        assert.equal(forbiddenAuditReads, 0)
        const guest = await browser.newContext({ baseURL })
        const guestPage = await guest.newPage()
        await guestPage.goto('/dashboard/audit')
        await expect.poll(() => new URL(guestPage.url()).pathname).toBe('/login')
        logs = await readApiLogs()
        const serialized = JSON.stringify(logs)
        for (const secret of secrets.filter(Boolean))
          assert.ok(
            !serialized.includes(secret),
            'Credential or multipart content leaked into the rolling logs',
          )
        await writeFile(
          path.join(artifacts, 'audit-evidence.json'),
          JSON.stringify(
            {
              expected,
              history,
              requests,
              resizeLog,
              deletionLog,
              restartPreservedIds: idsBeforeRestart.length,
              privacyMarkersChecked: secrets.filter(Boolean).length,
            },
            null,
            2,
          ),
        )
        await Promise.all([browserAdmin.context.close(), browserAuthor.context.close(), guest.close()])
      },
    )
  },
)
