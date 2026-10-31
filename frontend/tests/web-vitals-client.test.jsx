import { afterEach, beforeAll, expect, jest, test } from '@jest/globals'
import { render } from '@testing-library/react'

window.history.replaceState({}, '', '/recipes/private-slug')
jest.mock('web-vitals', () => ({ onCLS: jest.fn(), onINP: jest.fn(), onLCP: jest.fn() }))
let sdk
let onINP
let WebVitals
beforeAll(async () => {
  sdk = await import('web-vitals')
  ;({ onINP } = sdk)
  ;({ WebVitals } = await import('../src/components/web-vitals'))
})
const originalBeacon = navigator.sendBeacon
afterEach(() => {
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: originalBeacon })
  jest.clearAllMocks()
  jest.restoreAllMocks()
  window.history.replaceState({}, '', '/recipes/private-slug')
})
const metric = {
  name: 'INP',
  id: 'v4-123',
  value: 24,
  navigationType: 'navigate',
  entries: [{ target: 'private-input' }],
}
const latestCallback = () => onINP.mock.calls.at(-1)[0]
test('beacon masks route parameters and keeps document entry attribution across client navigation', async () => {
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: jest.fn(() => true) })
  const { rerender } = render(<WebVitals />)
  latestCallback()(metric)
  window.history.replaceState({}, '', '/profile')
  rerender(<WebVitals />)
  latestCallback()({ ...metric, name: 'LCP', value: 1000 })
  const first = JSON.parse(navigator.sendBeacon.mock.calls[0][1])
  const second = JSON.parse(navigator.sendBeacon.mock.calls[1][1])
  expect(first).toEqual({
    name: 'INP',
    id: 'v4-123',
    value: 24,
    navigationType: 'navigate',
    route: '/recipes/[slug]',
    device: 'desktop',
  })
  expect(second.route).toBe('/recipes/[slug]')
  expect(navigator.sendBeacon.mock.calls[0][0]).toBe('/api/web-vitals')
  expect(JSON.stringify(first)).not.toContain('private')
})

test('registers only three CWV observers and a full or unavailable beacon queue does not block the visit', () => {
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: jest.fn(() => false) })
  render(<WebVitals />)
  for (const observer of [sdk.onCLS, sdk.onINP, sdk.onLCP]) expect(observer).toHaveBeenCalledTimes(1)
  expect(() => latestCallback()(metric)).not.toThrow()
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: undefined })
  expect(() => latestCallback()(metric)).not.toThrow()
})

test('device cohort is captured at document entry and stays stable during pagehide delivery', async () => {
  const agent = jest.spyOn(navigator, 'userAgent', 'get').mockReturnValue('Android controlled browser')
  Object.defineProperty(navigator, 'sendBeacon', { configurable: true, value: jest.fn(() => true) })
  render(<WebVitals />)
  agent.mockReturnValue('Desktop about:blank browser')
  latestCallback()(metric)
  expect(JSON.parse(navigator.sendBeacon.mock.calls[0][1]).device).toBe('mobile')
})
