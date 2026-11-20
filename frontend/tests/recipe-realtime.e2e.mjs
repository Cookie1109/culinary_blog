// Run explicitly against the local Docker stack. Uses a temporary recipe and deletes it afterwards.
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import puppeteer from 'puppeteer-core'

const base = process.env.REALTIME_TEST_URL ?? 'http://localhost:8080'
const local = Object.fromEntries(
  readFileSync(new URL('../../.env', import.meta.url), 'utf8')
    .split(/\r?\n/)
    .filter((line) => line && !line.startsWith('#') && line.includes('='))
    .map((line) => [
      line.slice(0, line.indexOf('=')),
      line.slice(line.indexOf('=') + 1).replace(/^"|"$/g, ''),
    ]),
)
async function api(path, options = {}) {
  const response = await fetch(`${base}/api/v1${path}`, options)
  assert.ok(response.ok, `API ${options.method ?? 'GET'} ${path}: ${response.status}`)
  return response.status === 204 ? null : response.json()
}
const session = (
  await api('/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      email: process.env.REALTIME_TEST_EMAIL ?? local.ADMIN_EMAIL,
      password: process.env.REALTIME_TEST_PASSWORD ?? local.ADMIN_PASSWORD,
    }),
  })
).data
const headers = { Authorization: `Bearer ${session.accessToken}`, 'Content-Type': 'application/json' }
const category = (await api('/categories')).data[0]
const title = `Realtime verification ${Date.now()}`
let recipe = (
  await api('/recipes', {
    method: 'POST',
    headers,
    body: JSON.stringify({
      title,
      description: 'Temporary realtime verification.',
      categoryId: category.id,
      prepTime: 0,
      cookTime: 0,
      servings: 2,
      difficulty: 'easy',
      ingredients: [{ name: 'Water', quantity: 1, unit: 'cup', orderIndex: 0 }],
      steps: [{ title: 'Boil', description: 'Boil the water.', timerMinutes: 1, stepType: 'cook' }],
    }),
  })
).data
const browser = await puppeteer.launch({
  executablePath: process.env.CHROME_PATH ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe',
  headless: true,
})
const metrics = []
try {
  const actor = await browser.createBrowserContext()
  const guest = await browser.createBrowserContext()
  const owner = await actor.newPage()
  const viewer = await guest.newPage()
  await owner.goto(base, { waitUntil: 'domcontentloaded' })
  await owner.evaluate(
    async (credentials) => {
      const { csrfToken } = await (await fetch('/api/auth/csrf')).json()
      await fetch('/api/auth/callback/credentials', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Auth-Return-Redirect': '1' },
        body: new URLSearchParams({
          csrfToken,
          accessToken: credentials.accessToken,
          expiresAt: credentials.expiresAt,
          callbackUrl: '/dashboard/recipes',
        }),
      })
    },
    { accessToken: session.accessToken, expiresAt: session.expiresAt },
  )
  await owner.goto(`${base}/dashboard/recipes`, { waitUntil: 'domcontentloaded' })
  await owner.waitForFunction((name) => document.body.textContent.includes(name), {}, title)
  await viewer.goto(base, { waitUntil: 'domcontentloaded' })
  await viewer.waitForSelector('#home-hero-title')
  await viewer.evaluate(() => {
    window.realtimeSignals = []
    window.realtimeMonitor = new EventSource('/api/recipe-events')
    for (const type of ['connected', 'changed', 'error'])
      window.realtimeMonitor.addEventListener(type, () => window.realtimeSignals.push(type))
  })
  await viewer.waitForFunction(() => window.realtimeSignals.includes('connected'))
  const visible = (page, expected) =>
    page.waitForFunction(
      (name, present) =>
        [...document.querySelectorAll('article h3 a')].some((item) => item.textContent.includes(name)) ===
        present,
      { timeout: 20_000, polling: 100 },
      title,
      expected,
    )
  await visible(viewer, false)
  const act = async (action, expectedPublic, label) => {
    await viewer.bringToFront()
    const started = Date.now()
    recipe = (
      await api(`/recipes/${recipe.id}/${action}`, {
        method: 'PATCH',
        headers: { ...headers, 'If-Match': `"${recipe.version}"` },
      })
    ).data
    await visible(viewer, expectedPublic)
    await owner.waitForFunction(
      (name, status) =>
        [...document.querySelectorAll('tbody tr')].some(
          (row) => row.textContent.includes(name) && row.textContent.includes(status),
        ),
      { timeout: 20_000, polling: 100 },
      title,
      label,
    )
    metrics.push({ action, milliseconds: Date.now() - started })
  }
  await act('publish', true, 'Đã xuất bản')
  const detail = await guest.newPage()
  await detail.goto(`${base}/recipes/${recipe.slug}`, { waitUntil: 'domcontentloaded' })
  await detail.waitForFunction((name) => document.querySelector('h1')?.textContent.includes(name), {}, title)
  await act('unpublish', false, 'Bản nháp')
  await detail.waitForFunction(
    (name) => !document.querySelector('h1')?.textContent.includes(name),
    { timeout: 20_000, polling: 100 },
    title,
  )
  await act('publish', true, 'Đã xuất bản')
  await act('archive', false, 'Đã lưu trữ')
  await act('unarchive', false, 'Bản nháp')
  // Close this browser's streams, change state while it is away, and reconnect.
  await viewer.goto('about:blank')
  recipe = (
    await api(`/recipes/${recipe.id}/publish`, {
      method: 'PATCH',
      headers: { ...headers, 'If-Match': `"${recipe.version}"` },
    })
  ).data
  await viewer.goto(base, { waitUntil: 'domcontentloaded' })
  await visible(viewer, true)
  process.stdout.write(
    JSON.stringify({
      passed: true,
      twoIndependentBrowserContexts: true,
      detailRemovedWithoutReload: true,
      returningBrowserCaughtUp: true,
      metrics,
    }),
  )
} finally {
  await browser.close()
  await api(`/recipes/${recipe.id}`, {
    method: 'DELETE',
    headers: { ...headers, 'If-Match': `"${recipe.version}"` },
  })
  if (session.refreshToken)
    await api('/auth/logout', {
      method: 'POST',
      headers,
      body: JSON.stringify({ refreshToken: session.refreshToken }),
    })
}
