import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const rootSource = (path) => readFile(new URL(`../../${path}`, import.meta.url), 'utf8')
const frontendSource = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('P8-05 and P8-06: k6 defines smoke/load/stress profiles and latency percentiles', async () => {
  const script = await rootSource('performance/k6/public-api.js')

  assert.match(script, /smoke:/)
  assert.match(script, /load:/)
  assert.match(script, /stress:/)
  assert.match(script, /vus:\s*100/)
  assert.match(script, /'p\(50\)'/)
  assert.match(script, /'p\(95\)'/)
  assert.match(script, /'p\(99\)'/)
  assert.match(script, /api_warm_duration/)
  assert.match(script, /api_cold_duration/)
})

test('P8-07: k6 verifies application cache hit rate from Prometheus', async () => {
  const script = await rootSource('performance/k6/public-api.js')

  assert.match(script, /cache_hits_total/)
  assert.match(script, /cache_misses_total/)
  assert.match(script, /cache_hit_rate/)
  assert.match(script, /value>=0\.80/)
})

test('P8-08: release Lighthouse run stores artifacts and reuses enforced budgets', async () => {
  const [config, runner, packageJson] = await Promise.all([
    frontendSource('lighthouserc.release.cjs'),
    frontendSource('scripts/run-lighthouse-release.mjs'),
    frontendSource('package.json').then(JSON.parse),
  ])

  assert.match(config, /target:\s*'filesystem'/)
  assert.match(config, /releaseAssertionNames/)
  assert.match(config, /meta-description/)
  assert.match(config, /largest-contentful-paint/)
  assert.match(config, /cumulative-layout-shift/)
  assert.match(config, /total-blocking-time/)
  assert.match(config, /resource-summary:script:size/)
  assert.match(config, /--disable-gpu/)
  assert.match(runner, /numberOfRuns/)
  assert.match(runner, /assertion-summary\.json/)
  assert.match(runner, /resource-summary:script:size/)
  assert.ok(packageJson.scripts['lighthouse:release'])
  assert.ok(packageJson.devDependencies.lighthouse)
  assert.equal(packageJson.devDependencies['@lhci/cli'], undefined)
})

test('P8-09: performance runner records environment and only runs measured gates', async () => {
  const runner = await rootSource('scripts/performance.ps1')

  assert.match(runner, /environment\.json/)
  assert.match(runner, /logicalProcessors/)
  assert.match(runner, /publishedRecipes/)
  assert.match(runner, /k6 run/)
  assert.match(runner, /budget:check/)
  assert.match(runner, /lighthouse:release/)
})
