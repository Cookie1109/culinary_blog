import assert from 'node:assert/strict'
import { existsSync, readFileSync } from 'node:fs'
import { readFile } from 'node:fs/promises'
import { join } from 'node:path'
import test from 'node:test'
import { gzipSync } from 'node:zlib'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('P6-22: budget.json defines Core Web Vitals and resource size budgets', async () => {
  const rawBudget = await source('budget.json')
  const budget = JSON.parse(rawBudget)

  assert.ok(Array.isArray(budget) && budget.length > 0, 'budget.json must be a non-empty array')
  const config = budget[0]

  // Check resource sizes
  const scriptBudget = config.resourceSizes.find((item) => item.resourceType === 'script')
  assert.ok(scriptBudget, 'budget.json must specify script budget')
  assert.equal(scriptBudget.budget, 200, 'First-load JS budget must be <= 200 KB')

  // Check Core Web Vitals timings
  const lcp = config.timings.find((item) => item.metric === 'largest-contentful-paint')
  const cls = config.timings.find((item) => item.metric === 'cumulative-layout-shift')
  const inp = config.timings.find((item) => item.metric === 'interaction-to-next-paint')
  const tbt = config.timings.find((item) => item.metric === 'total-blocking-time')

  assert.ok(lcp && lcp.budget <= 2500, 'LCP budget must be <= 2.5s (2500ms)')
  assert.ok(cls && cls.budget <= 0.1, 'CLS budget must be <= 0.1')
  assert.ok(inp && inp.budget <= 200, 'INP budget must be <= 200ms')
  assert.ok(tbt && tbt.budget <= 200, 'TBT budget must be <= 200ms')
})

test('P6-22: lighthouserc.json specifies required public URLs and assertions matching NFR-QA budgets', async () => {
  const rawLhrc = await source('lighthouserc.json')
  const lhrc = JSON.parse(rawLhrc)

  assert.ok(lhrc.ci, 'lighthouserc.json must define ci section')
  const urls = lhrc.ci.collect.url

  // Must include all key public routes
  assert.ok(
    urls.some((url) => url.endsWith('/')),
    'Collect URLs must include homepage',
  )
  assert.ok(
    urls.some((url) => url.endsWith('/recipes')),
    'Collect URLs must include recipes listing',
  )
  assert.ok(
    urls.some((url) => url.includes('/recipes/')),
    'Collect URLs must include recipe detail',
  )
  assert.ok(
    urls.some((url) => url.endsWith('/categories')),
    'Collect URLs must include categories listing',
  )
  assert.ok(
    urls.some((url) => url.includes('/categories/')),
    'Collect URLs must include category detail',
  )
  assert.ok(
    urls.some((url) => url.endsWith('/search')),
    'Collect URLs must include search page',
  )

  // Check assertions
  const assertions = lhrc.ci.assert.assertions
  assert.ok(assertions['largest-contentful-paint'], 'Must assert LCP')
  assert.equal(assertions['largest-contentful-paint'][1].maxNumericValue, 2500)

  assert.ok(assertions['cumulative-layout-shift'], 'Must assert CLS')
  assert.equal(assertions['cumulative-layout-shift'][1].maxNumericValue, 0.1)

  assert.ok(assertions['total-blocking-time'], 'Must assert TBT/INP')
  assert.equal(assertions['total-blocking-time'][1].maxNumericValue, 200)

  assert.ok(assertions['resource-summary:script:size'], 'Must assert script size budget')
  assert.equal(assertions['resource-summary:script:size'][1].maxNumericValue, 204800)
})

test('P6-22 & P6-23: All built routes strictly satisfy the <= 200 KB First Load JS gzip budget', async () => {
  const nextDir = join(process.cwd(), '.next')
  const manifestPath = join(nextDir, 'app-build-manifest.json')

  assert.ok(existsSync(manifestPath), 'Production build manifest (.next/app-build-manifest.json) must exist')
  const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'))
  const pages = manifest.pages || {}

  const MAX_GZIP_BYTES = 200 * 1024

  for (const [route, chunks] of Object.entries(pages)) {
    let totalGzipBytes = 0
    const uniqueChunks = Array.from(new Set(chunks))

    for (const chunk of uniqueChunks) {
      const chunkPath = join(nextDir, chunk)
      if (existsSync(chunkPath)) {
        const content = readFileSync(chunkPath)
        totalGzipBytes += gzipSync(content).length
      }
    }

    assert.ok(
      totalGzipBytes <= MAX_GZIP_BYTES,
      `Route ${route} exceeds 200 KB gzip budget: ${(totalGzipBytes / 1024).toFixed(1)} kB`,
    )
  }
})

test('P6-23: Bundle analyzer and code splitting optimizations are wired in next.config.mjs and package.json', async () => {
  const [config, pkg] = await Promise.all([source('next.config.mjs'), source('package.json')])

  // Bundle analyzer in next.config.mjs
  assert.match(config, /withBundleAnalyzer/)
  assert.match(config, /process\.env\.ANALYZE === 'true'/)
  assert.match(config, /optimizePackageImports:\s*\['lucide-react'\]/)

  // Scripts in package.json
  const packageJson = JSON.parse(pkg)
  assert.ok(packageJson.scripts.analyze, 'package.json must provide "analyze" script')
  assert.ok(packageJson.scripts['budget:check'], 'package.json must provide "budget:check" script')
})
