import assert from 'node:assert/strict'
import { readFile, mkdir, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { chromium, devices } from '@playwright/test'
import { summarizeWebVitals } from './summarize-web-vitals.mjs'

const [, , base, build, logFile, output] = process.argv
assert.match(base, /^http:\/\/127\.0\.0\.1:\d+$/)
assert.ok(build && logFile && output, 'Expected base, build ID, Next log and output directory.')
await mkdir(output, { recursive: true })
const startedAt = new Date().toISOString()
const browser = await chromium.launch({ headless: true })
const sent = []
const statuses = []
const visits = []
try {
  for (const [device, settings] of [
    ['desktop', { viewport: { width: 1440, height: 900 } }],
    ['mobile', devices['Pixel 7']],
  ]) {
    for (const route of ['/login', '/recipes', '/search']) {
      const context = await browser.newContext(settings)
      context.on('request', (request) => {
        if (request.url() === `${base}/api/web-vitals` && request.method() === 'POST')
          sent.push(request.url())
      })
      context.on('response', (response) => {
        if (response.url() === `${base}/api/web-vitals`) statuses.push(response.status())
      })
      const page = await context.newPage()
      await page.goto(base + route, { waitUntil: 'networkidle' })
      if (route === '/login') {
        await page.locator('#email').fill('invalid')
        await page.locator('#password').fill('x')
        await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click()
      } else {
        const input = page.locator('input[type="search"], input[placeholder*="Tìm"]').first()
        await input.pressSequentially('phở', { delay: 80 })
        await input.press('Enter')
      }
      await page.waitForTimeout(1500)
      await page.goto('about:blank')
      await page.waitForTimeout(500)
      visits.push({ route, device })
      await context.close()
    }
  }
} finally {
  await browser.close()
}
assert.ok(sent.length, 'No real browser beacon; a schema/unit pass is insufficient.')
assert.ok(statuses.length && statuses.every((status) => status === 204), JSON.stringify(statuses))
const records = (await readFile(logFile, 'utf8'))
  .split(/\r?\n/)
  .filter((line) => line.startsWith('{'))
  .map((line) => JSON.parse(line))
  .filter((record) => record.event === 'web-vital' && Date.parse(record.receivedAt) >= Date.parse(startedAt))
assert.ok(records.length && records.every((record) => record.build === build))
for (const record of records) {
  assert.deepEqual(Object.keys(record).sort(), [
    'build',
    'device',
    'event',
    'id',
    'name',
    'navigationType',
    'receivedAt',
    'route',
    'value',
  ])
  assert.ok(['LCP', 'CLS', 'INP'].includes(record.name))
  assert.ok(!JSON.stringify(record).includes('invalid'))
}
for (const device of ['desktop', 'mobile']) {
  for (const name of ['LCP', 'CLS', 'INP'])
    assert.ok(
      records.some((record) => record.device === device && record.name === name),
      `Missing ${name}/${device}`,
    )
}
const finishedAt = new Date().toISOString()
const summary = summarizeWebVitals(records, { build, from: startedAt, to: finishedAt })
await writeFile(
  path.join(output, 'metrics.jsonl'),
  `${records.map((record) => JSON.stringify(record)).join('\n')}\n`,
)
await writeFile(
  path.join(output, 'summary.json'),
  JSON.stringify(
    {
      ...summary,
      visits,
      requestCount: sent.length,
      statuses,
      measurement: 'Local browser pipeline check, six controlled visits; not production field acceptance.',
    },
    null,
    2,
  ),
)
process.stdout.write(
  `Browser CWV pipeline: ${sent.length} metric requests, ${records.length} stored records, desktop/mobile LCP/CLS/INP present.\n`,
)
