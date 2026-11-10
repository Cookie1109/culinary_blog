import assert from 'node:assert/strict'
import test from 'node:test'
import AxeBuilder from '@axe-core/playwright'
import { chromium, devices, expect } from '@playwright/test'

const baseURL = process.env.CULINARY_E2E_BASE_URL
assert.match(baseURL ?? '', /^http:\/\/127\.0\.0\.1:\d+$/, 'Use an existing isolated loopback app')

for (const [device, settings] of [
  ['desktop', { viewport: { width: 1440, height: 900 } }],
  ['mobile', devices['Pixel 7']],
]) {
  test(
    `delayed search and clear preserve the results position on ${device}`,
    { timeout: 180_000 },
    async () => {
      const browser = await chromium.launch({ headless: true })
      try {
        const context = await browser.newContext(settings)
        const page = await context.newPage()
        await page.goto(`${baseURL}/recipes`, { waitUntil: 'networkidle', timeout: 90_000 })
        await page.route('**/*', async (route) => {
          if (route.request().url().includes('_rsc='))
            await new Promise((resolve) => {
              setTimeout(resolve, 1200)
            })
          await route.continue()
        })
        const results = page.locator('#recipes-filters').locator('..')
        const top = () => results.evaluate((node) => node.getBoundingClientRect().top + window.scrollY)
        const initial = await top()
        await page.locator('#recipes-search').fill('phở')
        await page.locator('#recipes-search').press('Enter')
        await expect(page.getByRole('button', { name: 'Bỏ từ khóa', exact: true })).toBeVisible({
          timeout: 30_000,
        })
        await expect(page.getByRole('status').filter({ hasText: 'Đang cập nhật công thức' })).toHaveCount(0)
        assert.ok(
          Math.abs((await top()) - initial) <= 2,
          'Search filter controls must not push the results down',
        )
        assert.ok(
          await page
            .locator('footer')
            .last()
            .evaluate((node) => node.getBoundingClientRect().top >= window.innerHeight),
          'A shorter async result set must not pull the footer into the visible results viewport',
        )
        const accessibility = await new AxeBuilder({ page })
          .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
          .analyze()
        assert.deepEqual(
          accessibility.violations.map((violation) => violation.id),
          [],
        )
        await page.getByRole('button', { name: 'Xóa tìm kiếm và bộ lọc', exact: true }).click()
        await expect(page.getByRole('button', { name: 'Bỏ từ khóa', exact: true })).toHaveCount(0, {
          timeout: 30_000,
        })
        assert.ok(Math.abs((await top()) - initial) <= 2, 'Clearing filters must not pull the results up')
        assert.equal(
          await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth),
          false,
        )
      } finally {
        await browser.close()
      }
    },
  )
}
