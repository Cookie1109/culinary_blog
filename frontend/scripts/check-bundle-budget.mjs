import { format as formatCliOutput } from 'node:util'
import { readFileSync, existsSync } from 'node:fs'
import { join } from 'node:path'
import { firstLoadJavaScript } from './bundle-budget.mjs'

const NEXT_DIR = join(process.cwd(), '.next')
const MANIFEST_PATH = join(NEXT_DIR, 'app-build-manifest.json')
const MAX_FIRST_LOAD_GZIP_BYTES = 200 * 1024 // 200 KB gzip budget per NFR-QA P6-22

if (!existsSync(MANIFEST_PATH)) {
  console.error(`Error: app-build-manifest.json not found at ${MANIFEST_PATH}. Run 'npm run build' first.`)
  process.exit(1)
}

const manifest = JSON.parse(readFileSync(MANIFEST_PATH, 'utf8'))
const pages = manifest.pages || {}

process.stdout.write(`${formatCliOutput('='.repeat(80))}\n`)
process.stdout.write(`${formatCliOutput(' CULINARY BLOG - BUNDLE BUDGET AUDIT (NFR-QA P6-22)')}\n`)
process.stdout.write(
  `${formatCliOutput(` Budget Threshold: First-load JS gzip <= ${MAX_FIRST_LOAD_GZIP_BYTES / 1024} KB`)}\n`,
)
process.stdout.write(`${formatCliOutput('='.repeat(80))}\n`)

let hasFailure = false
const report = []

for (const { route, rawBytes: totalRawBytes, gzipBytes: totalGzipBytes } of firstLoadJavaScript(
  pages,
  (chunk) => readFileSync(join(NEXT_DIR, chunk)),
)) {
  const rawKb = (totalRawBytes / 1024).toFixed(1)
  const gzipKb = (totalGzipBytes / 1024).toFixed(1)
  const passed = totalGzipBytes <= MAX_FIRST_LOAD_GZIP_BYTES

  if (!passed) {
    hasFailure = true
  }

  report.push({
    route,
    rawKb: Number.parseFloat(rawKb),
    gzipKb: Number.parseFloat(gzipKb),
    status: passed ? 'PASS' : 'FAIL',
  })
}

// Print formatted table
process.stdout.write(
  `${formatCliOutput('Route'.padEnd(46) + 'Raw JS (kB)'.padStart(14) + 'Gzip (kB)'.padStart(12) + 'Status'.padStart(8))}\n`,
)
process.stdout.write(`${formatCliOutput('-'.repeat(80))}\n`)

for (const item of report) {
  const statusFormatted = item.status === 'PASS' ? '✓ PASS' : '✗ FAIL'
  process.stdout.write(
    `${formatCliOutput(
      item.route.padEnd(46) +
        `${item.rawKb} kB`.padStart(14) +
        `${item.gzipKb} kB`.padStart(12) +
        statusFormatted.padStart(8),
    )}\n`,
  )
}

process.stdout.write(`${formatCliOutput('='.repeat(80))}\n`)
if (hasFailure) {
  console.error('❌ Bundle budget check FAILED: One or more routes exceeded the 200 KB gzip budget!')
  process.exit(1)
} else {
  process.stdout.write(
    `${formatCliOutput('✅ Bundle budget check PASSED: All routes are strictly within the 200 KB gzip budget!')}\n`,
  )
}
