import assert from 'node:assert/strict'
import test from 'node:test'
import { summarizeWebVitals } from '../scripts/summarize-web-vitals.mjs'

const window = { build: 'reviewed-build', from: '2026-10-31T00:00:00Z', to: '2026-11-01T00:00:00Z' }
const sample = (id, value, extra = {}) => ({
  event: 'web-vital',
  name: 'INP',
  id,
  value,
  route: '/recipes',
  device: 'desktop',
  navigationType: 'navigate',
  build: window.build,
  receivedAt: '2026-10-31T12:00:00Z',
  ...extra,
})

test('p75 deduplicates updated metric IDs, filters build/window, and separates route/device cohorts', () => {
  const report = summarizeWebVitals(
    [
      sample('one', 10),
      sample('two', 20),
      sample('three', 30),
      sample('four', 400),
      sample('four', 40, { receivedAt: '2026-10-31T12:01:00Z' }),
      sample('wrong-build', 999, { build: 'another-build' }),
      sample('outside-window', 999, { receivedAt: '2026-10-30T12:00:00Z' }),
      sample('mobile', 240, { device: 'mobile' }),
      sample('route', 300, { route: '/search' }),
    ],
    window,
  )
  assert.equal(report.distinctMetrics, 6)
  assert.equal(report.excluded, 2)
  assert.deepEqual(
    report.groups.find((g) => g.route === '/recipes' && g.device === 'desktop'),
    {
      route: '/recipes',
      device: 'desktop',
      name: 'INP',
      samples: 4,
      p75: 30,
      threshold: 200,
      withinThreshold: true,
    },
  )
  assert.equal(report.groups.find((g) => g.device === 'mobile').withinThreshold, false)
  assert.deepEqual(report.missingMetrics, ['LCP', 'CLS'])
  assert.match(report.acceptance, /Pending/)
})

test('aggregation refuses invalid samples and absent deployment/window metadata', () => {
  assert.throws(() => summarizeWebVitals([sample('one', -1)], window), /Invalid/)
  assert.throws(() => summarizeWebVitals([sample('one', 20, { url: 'private' })], window), /Invalid/)
  assert.throws(() => summarizeWebVitals([], { ...window, build: '' }), /required/)
  assert.throws(() => summarizeWebVitals([], { ...window, to: window.from }), /required/)
})
