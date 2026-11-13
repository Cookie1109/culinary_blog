/** @jest-environment node */
import { afterEach, beforeEach, expect, jest, test } from '@jest/globals'
import { webVitalRoute, vitalRoutes } from '../src/lib/web-vitals'
import { POST } from '../src/app/api/web-vitals/route'

const keys = [
  'WEB_VITALS_ENABLED',
  'WEB_VITALS_BUILD_ID',
  'VERCEL_GIT_COMMIT_SHA',
  'RELEASE_COMMIT_SHA',
  'SITE_URL',
]
const saved = Object.fromEntries(keys.map((key) => [key, process.env[key]]))
let sink
const sample = {
  name: 'INP',
  id: 'v4-123-456',
  value: 32,
  route: '/recipes/[slug]',
  device: 'desktop',
  navigationType: 'navigate',
}
const request = (data = sample, headers = {}) =>
  new Request('https://food.test/api/web-vitals', {
    method: 'POST',
    headers: { origin: 'https://food.test', 'content-type': 'application/json', ...headers },
    body: typeof data === 'string' ? data : JSON.stringify(data),
  })

beforeEach(() => {
  process.env.WEB_VITALS_ENABLED = 'true'
  process.env.WEB_VITALS_BUILD_ID = 'reviewed-build'
  process.env.SITE_URL = 'https://food.test'
  delete process.env.VERCEL_GIT_COMMIT_SHA
  delete process.env.RELEASE_COMMIT_SHA
  sink = jest.spyOn(process.stdout, 'write').mockImplementation(() => true)
})
afterEach(() => {
  for (const key of keys) {
    if (saved[key] === undefined) delete process.env[key]
    else process.env[key] = saved[key]
  }
  jest.restoreAllMocks()
})

test('receiver is opt-in, requires build identity, and records only validated metric fields', async () => {
  delete process.env.WEB_VITALS_ENABLED
  expect((await POST(request())).status).toBe(404)
  process.env.WEB_VITALS_ENABLED = 'true'
  delete process.env.WEB_VITALS_BUILD_ID
  expect((await POST(request())).status).toBe(503)
  process.env.VERCEL_GIT_COMMIT_SHA = 'a'.repeat(40)
  const response = await POST(request())
  expect(response.status).toBe(204)
  expect(response.headers.get('cache-control')).toBe('no-store')
  const record = JSON.parse(sink.mock.calls[0][0])
  expect(record).toEqual({
    ...sample,
    event: 'web-vital',
    build: 'a'.repeat(40),
    receivedAt: expect.any(String),
  })
  expect(Number.isNaN(Date.parse(record.receivedAt))).toBe(false)
})

test('receiver rejects cross-origin, wrong content type and oversized bytes without logging', async () => {
  expect((await POST(request(sample, { origin: 'https://evil.test' }))).status).toBe(403)
  expect((await POST(request(sample, { 'content-type': 'application/xml' }))).status).toBe(415)
  expect((await POST(request(sample, { 'content-length': '2049' }))).status).toBe(413)
  expect((await POST(request('é'.repeat(1100), { 'content-length': '1' }))).status).toBe(413)
  expect(sink).not.toHaveBeenCalled()
})

test.each([
  '{bad json',
  { ...sample, url: 'https://food.test/?token=private' },
  { ...sample, route: '/recipes/private-user-slug' },
  { ...sample, id: 'token?private' },
  { ...sample, name: 'TBT' },
  { ...sample, value: -1 },
  { ...sample, value: null },
  { ...sample, device: 'arbitrary-agent' },
  { ...sample, navigationType: 'unknown' },
])('invalid or potentially identifying sample is not retained: %p', async (data) => {
  expect((await POST(request(data))).status).toBe(400)
  expect(sink).not.toHaveBeenCalled()
})

test('route masking covers every allowed pattern  without exposing unknown paths', () => {
  for (const route of vitalRoutes.filter((value) => value !== 'other'))
    expect(webVitalRoute(route.replace(/\[[^\]]+\]/g, 'private-value'))).toBe(route)
  expect(webVitalRoute('/render-public/home/2')).toBe('other')
  expect(webVitalRoute('/render-public/categories/2')).toBe('other')
  expect(webVitalRoute('/render-public/category/private-slug/2')).toBe('other')
  for (const path of ['/private-user/email@example.test', '/recipes/private/extra', '/dashboard/unknown'])
    expect(webVitalRoute(path)).toBe('other')
})

test('receiver accepts same-origin JSON text from browser sendBeacon and rejects cross-origin plain text', async () => {
  expect((await POST(request(sample, { 'content-type': 'text/plain;charset=UTF-8' }))).status).toBe(204)
  expect(
    (await POST(request(sample, { 'content-type': 'text/plain;charset=UTF-8', origin: 'https://evil.test' })))
      .status,
  ).toBe(403)
  expect(sink).toHaveBeenCalledTimes(1)
})
