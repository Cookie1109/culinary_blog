import assert from 'node:assert/strict'
import path from 'node:path'
import test from 'node:test'
import { expect as playwrightExpect } from '@playwright/test'
import { password, withIsolatedApp } from './helpers/isolated-app.mjs'

const expect = playwrightExpect.configure({ timeout: 30_000 })

test(
  'step list scrolls independently and persists mouse, keyboard and touch reordering',
  { timeout: 600_000 },
  async () => {
    await withIsolatedApp({}, async ({ api, baseURL, browser, command, pg, artifacts }) => {
      const email = 'step-order@example.test'
      const registration = await fetch(`${api}/api/v1/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, displayName: 'Step order test', password }),
      })
      assert.equal(registration.status, 201)
      const token = (await registration.json()).data.accessToken
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
        'INSERT INTO "Categories" ("Name", "NormalizedName", "Slug") VALUES (\'Sortable test\', \'SORTABLE TEST\', \'sortable-test\')',
      ])
      const categories = await (await fetch(`${api}/api/v1/categories`)).json()
      const create = await fetch(`${api}/api/v1/recipes`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
        body: JSON.stringify({
          title: 'Sortable cooking steps',
          description: 'Independent scroll and ordering regression.',
          categoryId: categories.data[0].id,
          prepTime: 10,
          cookTime: 20,
          servings: 2,
          difficulty: 'easy',
          steps: Array.from({ length: 10 }, (_, index) => ({
            title: `Cooking step ${index + 1}`,
            description: index === 8 ? 'q'.repeat(2000) : `Instructions for step ${index + 1}.`,
            timerMinutes: index + 1,
          })),
        }),
      })
      assert.equal(create.status, 201)
      const recipe = (await create.json()).data
      const ids = recipe.steps.map((step) => step.id)
      const detail = async () => {
        const response = await fetch(`${api}/api/v1/me/recipes/${recipe.id}`, {
          headers: { Authorization: `Bearer ${token}` },
        })
        assert.equal(response.status, 200)
        return (await response.json()).data
      }
      const context = await browser.newContext({
        baseURL,
        viewport: { width: 1920, height: 1080 },
        hasTouch: true,
      })
      const page = await context.newPage()
      try {
        await page.goto('/login')
        await page.locator('#email').fill(email)
        await page.locator('#password').fill(password)
        await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
        await expect(page).toHaveURL(`${baseURL}/`)
        await page.goto(`/dashboard/recipes/${recipe.id}/edit?step=steps`)
        const list = page.getByRole('list', { name: 'Danh sách bước', exact: true })
        const rows = list.locator('[data-step-id]')
        const order = () => rows.evaluateAll((items) => items.map((item) => item.dataset.stepId))
        const numbers = () =>
          rows.evaluateAll((items) =>
            items.map((item) => Number(item.querySelector(':scope > span').textContent)),
          )
        await expect(rows).toHaveCount(10)
        await list.scrollIntoViewIfNeeded()
        assert.ok(await list.evaluate((element) => element.scrollHeight > element.clientHeight))
        const pageScroll = await page.evaluate(() => window.scrollY)
        const listBounds = await list.boundingBox()
        await page.mouse.move(listBounds.x + listBounds.width / 2, listBounds.y + listBounds.height / 2)
        await page.mouse.wheel(0, 500)
        await expect.poll(() => list.evaluate((element) => element.scrollTop)).toBeGreaterThan(0)
        assert.equal(await page.evaluate(() => window.scrollY), pageScroll)
        await list.evaluate((element) => {
          element.scrollTo({ top: 0 })
        })

        const firstHandle = list.getByRole('button', { name: 'Kéo để đổi vị trí bước 1', exact: true })
        const handleBounds = await firstHandle.boundingBox()
        const thirdBounds = await rows.nth(2).boundingBox()
        const dropResponse = page.waitForResponse(
          (response) => response.request().method() === 'PUT' && response.url().endsWith(`/steps/${ids[0]}`),
        )
        await page.mouse.move(
          handleBounds.x + handleBounds.width / 2,
          handleBounds.y + handleBounds.height / 2,
        )
        await page.mouse.down()
        await page.mouse.move(handleBounds.x + handleBounds.width / 2, handleBounds.y + 20, { steps: 4 })
        await expect(firstHandle).toHaveAttribute('aria-pressed', 'true')
        await page.mouse.move(thirdBounds.x + thirdBounds.width / 2, thirdBounds.y + thirdBounds.height / 2, {
          steps: 20,
        })
        await page.mouse.up()
        const dropped = await dropResponse
        assert.equal(dropped.status(), 200)
        assert.deepEqual(dropped.request().postDataJSON(), { stepNumber: 3 })
        const mouseOrder = [ids[1], ids[2], ids[0], ...ids.slice(3)]
        await expect.poll(order).toEqual(mouseOrder)
        await expect.poll(numbers).toEqual([1, 2, 3, 4, 5, 6, 7, 8, 9, 10])
        await expect(page.getByText('Đang lưu thứ tự…', { exact: true })).toHaveCount(0)
        assert.deepEqual(
          (await detail()).steps.map((step) => step.id),
          mouseOrder,
        )
        await page.reload()
        await expect.poll(order).toEqual(mouseOrder)

        const keyboardHandle = list.getByRole('button', { name: 'Kéo để đổi vị trí bước 1', exact: true })
        await keyboardHandle.focus()
        await keyboardHandle.press('Space')
        await expect(keyboardHandle).toHaveAttribute('aria-pressed', 'true')
        await expect(page.locator('[data-step-drag-overlay]')).toBeVisible()
        await keyboardHandle.press('ArrowDown')
        await expect(page.getByRole('status').filter({ hasText: 'Vị trí mới là bước 2.' })).toHaveText(
          'Vị trí mới là bước 2.',
        )
        const keyboardResponse = page.waitForResponse(
          (response) => response.request().method() === 'PUT' && response.url().endsWith(`/steps/${ids[1]}`),
        )
        await keyboardHandle.press('Space')
        assert.equal((await keyboardResponse).status(), 200)
        const keyboardOrder = [ids[2], ids[1], ids[0], ...ids.slice(3)]
        await expect.poll(order).toEqual(keyboardOrder)
        await expect(page.getByText('Đang lưu thứ tự…', { exact: true })).toHaveCount(0)
        await expect(page.locator('[data-step-drag-overlay]')).toHaveCount(0)
        const beforeCancel = (await detail()).version
        const cancelHandle = list.getByRole('button', { name: 'Kéo để đổi vị trí bước 1', exact: true })
        await cancelHandle.focus()
        await cancelHandle.press('Space')
        await expect(cancelHandle).toHaveAttribute('aria-pressed', 'true')
        await expect(page.locator('[data-step-drag-overlay]')).toBeVisible()
        await cancelHandle.press('ArrowDown')
        await cancelHandle.press('Escape')
        await expect(cancelHandle).not.toHaveAttribute('aria-pressed', 'true')
        await expect(page.locator('[data-step-drag-overlay]')).toHaveCount(0)
        assert.equal((await detail()).version, beforeCancel)
        await expect.poll(order).toEqual(keyboardOrder)

        await list.evaluate((element) => {
          element.scrollTo({ top: 0 })
        })
        const autoHandle = await cancelHandle.boundingBox()
        const beforeAutoScroll = await page.evaluate(() => window.scrollY)
        const scrollBounds = await list.boundingBox()
        await page.mouse.move(autoHandle.x + autoHandle.width / 2, autoHandle.y + autoHandle.height / 2)
        await page.mouse.down()
        await page.mouse.move(autoHandle.x + autoHandle.width / 2, autoHandle.y + 25, { steps: 4 })
        await expect(cancelHandle).toHaveAttribute('aria-pressed', 'true')
        await page.mouse.move(scrollBounds.x + 50, scrollBounds.y + scrollBounds.height - 8, { steps: 20 })
        await expect.poll(() => list.evaluate((element) => element.scrollTop)).toBeGreaterThan(100)
        await page.keyboard.press('Escape')
        await page.mouse.up()
        assert.equal((await detail()).version, beforeCancel)
        assert.equal(await page.evaluate(() => window.scrollY), beforeAutoScroll)

        await page.setViewportSize({ width: 390, height: 844 })
        await list.scrollIntoViewIfNeeded()
        await list.evaluate((element) => {
          element.scrollTo({ top: 0 })
        })
        assert.ok(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth))
        const touchHandle = list.getByRole('button', { name: 'Kéo để đổi vị trí bước 1', exact: true })
        await touchHandle.scrollIntoViewIfNeeded()
        const touchStart = await touchHandle.boundingBox()
        const touchTarget = await rows.nth(2).boundingBox()
        const session = await context.newCDPSession(page)
        const x = touchStart.x + touchStart.width / 2
        const y = touchStart.y + touchStart.height / 2
        await session.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ x, y, id: 1 }] })
        await session.send('Input.dispatchTouchEvent', {
          type: 'touchMove',
          touchPoints: [{ x, y: y + 12, id: 1 }],
        })
        await expect(touchHandle).toHaveAttribute('aria-pressed', 'true')
        await expect(page.locator('[data-step-drag-overlay]')).toBeVisible()
        await session.send('Input.dispatchTouchEvent', {
          type: 'touchMove',
          touchPoints: [{ x, y: touchTarget.y + touchTarget.height / 2, id: 1 }],
        })
        await expect(page.getByRole('status').filter({ hasText: 'Vị trí mới là bước 3.' })).toHaveText(
          'Vị trí mới là bước 3.',
        )
        const touchResponse = page.waitForResponse(
          (response) => response.request().method() === 'PUT' && response.url().endsWith(`/steps/${ids[2]}`),
        )
        await session.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] })
        assert.equal((await touchResponse).status(), 200)
        const touchOrder = [ids[1], ids[0], ids[2], ...ids.slice(3)]
        await expect.poll(order).toEqual(touchOrder)
        await page.reload()
        await expect.poll(order).toEqual(touchOrder)
        assert.deepEqual(
          (await detail()).steps.map((step) => step.stepNumber),
          [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
        )
      } catch (error) {
        await page.screenshot({ path: path.join(artifacts, 'step-order-failure.png'), fullPage: true })
        throw error
      } finally {
        await context.close()
      }
    })
  },
)
