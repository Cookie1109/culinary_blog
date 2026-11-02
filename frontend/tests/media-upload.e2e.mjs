import assert from 'node:assert/strict'
import test from 'node:test'
import path from 'node:path'
import { expect as playwrightExpect } from '@playwright/test'
import { withIsolatedApp, password } from './helpers/isolated-app.mjs'

const expect = playwrightExpect.configure({ timeout: 30_000 })
const fixtures = {
  png: 'iVBORw0KGgoAAAANSUhEUgAAACAAAAAQAQMAAABNzu8aAAAAIGNIUk0AAHomAACAhAAA+gAAAIDoAAB1MAAA6mAAADqYAAAXcJy6UTwAAAADUExURf8AABniCTcAAAABYktHRACIBR1IAAAAB3RJTUUH6gsCDBIvfO3fMAAAACV0RVh0ZGF0ZTpjcmVhdGUAMjAyNi0xMS0wMlQxMjoxODo0NyswMDowMCWvWXIAAAAldEVYdGRhdGU6bW9kaWZ5ADIwMjYtMTEtMDJUMTI6MTg6NDcrMDA6MDBU8uHOAAAAKHRFWHRkYXRlOnRpbWVzdGFtcAAyMDI2LTExLTAyVDEyOjE4OjQ3KzAwOjAwA+fAEQAAAAxJREFUCNdjYKAuAAAAUAABIhPodQAAAABJRU5ErkJggg==',
  webp: 'UklGRkgAAABXRUJQVlA4IDwAAAAQAwCdASogABAAPpE4l0eloyIhMAgAsBIJQBdl0AAQNgAA/u6mP/4DdfFtMv/3OB/3OB/3OB/HM1mAAAA=',
  avif: 'AAAAHGZ0eXBhdmlmAAAAAG1pZjFhdmlmbWlhZgAAANZtZXRhAAAAAAAAACFoZGxyAAAAAAAAAABwaWN0AAAAAAAAAAAAAAAAAAAAACJpbG9jAAAAAERAAAEAAQAAAAAA+gABAAAAAAAAACkAAAAjaWluZgAAAAAAAQAAABVpbmZlAgAAAAABAABhdjAxAAAAAA5waXRtAAAAAAABAAAAVmlwcnAAAAA4aXBjbwAAAAxhdjFDgQAMAAAAABRpc3BlAAAAAAAAACAAAAAQAAAAEHBpeGkAAAAAAwgICAAAABZpcG1hAAAAAAAAAAEAAQOBAgMAAAAxbWRhdBIACgkYEP/tEBDQaEAyGhlHh4Yhh5555oAAAK6/EEwGqEspogSr79yA',
  jpg: '/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAMCAgICAgMCAgIDAwMDBAYEBAQEBAgGBgUGCQgKCgkICQkKDA8MCgsOCwkJDRENDg8QEBEQCgwSExIQEw8QEBD/2wBDAQMDAwQDBAgEBAgQCwkLEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBD/wAARCAAQACADAREAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAj/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFgEBAQEAAAAAAAAAAAAAAAAAAAcJ/8QAFBEBAAAAAAAAAAAAAAAAAAAAAP/aAAwDAQACEQMRAD8AnRDGqYAAAAAAD//Z',
}

test(
  'Recipe wizard uploads all formats through real MinIO and workers, recovers and protects private images',
  { timeout: 600_000 },
  async () => {
    await withIsolatedApp({ media: true }, async ({ api, baseURL, artifacts, browser, minio, command }) => {
      const email = 'owner@media-e2e.test'
      const registered = await fetch(`${api}/api/v1/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: 'Media Owner', email, password }),
      })
      assert.equal(registered.status, 201)
      const token = (await registered.json()).data.accessToken
      const headers = { Authorization: `Bearer ${token}` }
      const context = await browser.newContext({ baseURL })
      const page = await context.newPage()
      await page.goto('/login')
      await page.locator('#email').fill(email)
      await page.locator('#password').fill(password)
      await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
      await expect.poll(() => new URL(page.url()).pathname).toMatch(/^\/dashboard/)
      await page.goto('/dashboard/recipes/new')
      await page.locator('#title').fill('Công thức kiểm thử upload ảnh')
      await page.locator('#description').fill('Dữ liệu synthetic kiểm thử riêng cho chức năng ảnh.')
      await page.locator('#category').selectOption({ index: 1 })
      const [created] = await Promise.all([
        page.waitForResponse(
          (response) => response.url().endsWith('/api/v1/recipes') && response.status() === 201,
        ),
        page.locator('button[type="submit"]').filter({ hasText: 'Lưu bản nháp' }).click(),
      ])
      const recipeId = (await created.json()).data.id
      await expect(page).toHaveURL(/\/edit/)
      await page
        .getByRole('button', { name: /4\. Hình ảnh/ })
        .first()
        .click()
      const input = page.locator('#recipe-image')
      const gallery = page.locator('section[aria-labelledby="composition-heading"] article')
      const images = []
      for (const [format, base64] of Object.entries(fixtures)) {
        await input.setInputFiles({
          name: `dish.${format}`,
          mimeType: `image/${format === 'jpg' ? 'jpeg' : format}`,
          buffer: Buffer.from(base64, 'base64'),
        })
        await page.getByRole('textbox', { name: 'Mô tả ảnh', exact: true }).fill(`Ảnh ${format}`)
        const uploaded = page.waitForResponse(
          (response) => response.url().endsWith('/images') && response.request().method() === 'POST',
        )
        await page.getByRole('button', { name: 'Tải ảnh lên', exact: true }).click()
        const response = await uploaded
        assert.equal(response.status(), 201)
        images.push({ ...(await response.json()).data, format })
        await expect(input).toHaveValue('')
      }
      await expect(gallery).toHaveCount(4)
      async function detail() {
        return (await (await fetch(`${api}/api/v1/me/recipes/${recipeId}`, { headers })).json()).data
      }
      await expect
        .poll(async () => (await detail()).images.every((image) => image.processingStatus === 'ready'))
        .toBe(true)
      await page.reload()
      await gallery.first().scrollIntoViewIfNeeded()
      await expect
        .poll(() =>
          gallery
            .first()
            .locator('img')
            .evaluate((image) => image.naturalWidth),
        )
        .toBe(300)
      await gallery.nth(1).getByRole('button', { name: 'Đặt làm ảnh chính', exact: true }).click()
      await expect(gallery.nth(1).getByRole('button', { name: 'Ảnh chính', exact: true })).toBeVisible()
      const alt = gallery.nth(1).getByRole('textbox', { name: 'Alt text', exact: true })
      const edited = page.waitForResponse(
        (response) => response.request().method() === 'PATCH' && response.url().includes('/images/'),
      )
      await alt.fill('Ảnh chính đã cập nhật')
      await alt.press('Tab')
      await edited
      await expect
        .poll(async () => (await detail()).images.find((image) => image.id === images[1].id).altText)
        .toBe('Ảnh chính đã cập nhật')

      await input.setInputFiles({
        name: 'retry.avif',
        mimeType: 'image/avif',
        buffer: Buffer.from(fixtures.avif, 'base64'),
      })
      await page.getByRole('textbox', { name: 'Mô tả ảnh', exact: true }).fill('Giữ mô tả khi storage lỗi')
      await command('docker', ['stop', minio.id])
      try {
        const failure = page.waitForResponse(
          (response) => response.url().endsWith('/images') && response.status() === 503,
        )
        await page.getByRole('button', { name: 'Tải ảnh lên', exact: true }).click()
        await failure
        await expect(page.getByRole('alert').filter({ hasText: 'Không thể tải ảnh' })).toBeVisible()
        await expect(page.getByRole('textbox', { name: 'Mô tả ảnh', exact: true })).toHaveValue(
          'Giữ mô tả khi storage lỗi',
        )
        assert.equal(await input.evaluate((element) => element.files[0].name), 'retry.avif')
      } finally {
        await command('docker', ['start', minio.id])
      }
      await expect
        .poll(async () => {
          try {
            return (await fetch(`http://127.0.0.1:${minio.port}/minio/health/ready`)).ok
          } catch {
            return false
          }
        })
        .toBe(true)
      const retried = page.waitForResponse(
        (response) => response.url().endsWith('/images') && response.status() === 201,
      )
      await page.getByRole('button', { name: 'Tải ảnh lên', exact: true }).click()
      await retried
      await expect(input).toHaveValue('')
      await expect(gallery).toHaveCount(5)
      await expect
        .poll(async () => (await detail()).images.every((image) => image.processingStatus === 'ready'))
        .toBe(true)
      page.once('dialog', (dialog) => dialog.accept())
      const removed = page.waitForResponse(
        (response) => response.request().method() === 'DELETE' && response.url().includes('/images/'),
      )
      await gallery.nth(1).getByRole('button', { name: 'Xóa ảnh', exact: true }).click()
      await removed
      await expect(gallery).toHaveCount(4)
      await expect
        .poll(async () => {
          const output = await command('docker', [
            'exec',
            minio.id,
            'mc',
            'ls',
            '--recursive',
            '--json',
            `test/media-test/recipes/${recipeId.replaceAll('-', '')}/${images[1].id.replaceAll('-', '')}/`,
          ])
          return output.trim()
        })
        .toBe('')
      await expect.poll(async () => (await detail()).images.filter((image) => image.isPrimary).length).toBe(1)
      await page.screenshot({ path: path.join(artifacts, 'media-wizard.png'), fullPage: true })

      const first = images[0]
      const publicUrl = `${baseURL}/api/public-media/${first.id}/original`
      assert.equal((await fetch(publicUrl)).status, 404)
      assert.equal((await fetch(`${api}${first.originalUrl}`)).status, 404)
      const objectKey = `recipes/${recipeId.replaceAll('-', '')}/${first.id.replaceAll('-', '')}/original.${first.format}`
      await command('docker', ['exec', minio.id, 'mc', 'stat', `test/media-test/${objectKey}`])
      assert.equal((await fetch(`http://127.0.0.1:${minio.port}/media-test/${objectKey}`)).status, 403)
      async function mutate(suffix, method, body) {
        const version = (await detail()).version
        const response = await fetch(`${api}/api/v1/recipes/${recipeId}${suffix}`, {
          method,
          headers: { ...headers, 'If-Match': `"${version}"`, 'Content-Type': 'application/json' },
          body: body ? JSON.stringify(body) : undefined,
        })
        assert.ok(response.ok, `Media visibility setup failed: ${suffix} ${response.status}`)
      }
      await mutate('/ingredients', 'POST', { name: 'Gạo', quantity: 1, unit: 'g' })
      await mutate('/steps', 'POST', { title: 'Nấu', description: 'Nấu đến khi chín' })
      await mutate('/publish', 'PATCH')
      const primed = await fetch(publicUrl)
      assert.equal(primed.status, 200)
      assert.match(primed.headers.get('cache-control'), /no-store/)
      await mutate('/archive', 'PATCH')
      for (const variant of ['original', 'medium', 'thumbnail']) {
        assert.equal((await fetch(`${baseURL}/api/public-media/${first.id}/${variant}`)).status, 404)
      }
      await mutate('', 'DELETE')
      assert.equal((await fetch(publicUrl)).status, 404)
      assert.equal((await fetch(`${api}${first.originalUrl}`, { headers })).status, 404)
      await context.close()
    })
  },
)
