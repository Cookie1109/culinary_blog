import { mkdir, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

import { launch } from 'chrome-launcher'
import lighthouse from 'lighthouse'
import desktopConfig from 'lighthouse/core/config/desktop-config.js'

const require = createRequire(import.meta.url)
const config = require('../lighthouserc.release.cjs')
const currentDirectory = path.dirname(fileURLToPath(import.meta.url))
const outputDirectory = path.resolve(currentDirectory, '..', config.ci.upload.outputDir)
const runs = config.ci.collect.numberOfRuns ?? 3
const assertions = config.ci.assert.assertions
const chromeFlags = config.ci.collect.settings.chromeFlags.split(/\s+/).filter(Boolean)

const slugFor = (url) => {
  const parsed = new URL(url)
  return `${parsed.hostname}${parsed.pathname}`.replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '') || 'home'
}

const median = (values) => [...values].sort((left, right) => left - right)[Math.floor(values.length / 2)]

const measuredValue = (report, assertionName) => {
  if (assertionName.startsWith('categories:')) {
    return report.categories[assertionName.slice('categories:'.length)]?.score
  }

  if (assertionName === 'resource-summary:script:size') {
    return report.audits['resource-summary']?.details?.items?.find((item) => item.resourceType === 'script')
      ?.transferSize
  }

  const audit = report.audits[assertionName]
  return audit?.numericValue ?? audit?.score
}

const evaluate = (url, reports) => {
  const results = []
  for (const [name, [severity, expectation]] of Object.entries(assertions)) {
    const values = reports.map((report) => measuredValue(report, name))
    if (values.some((value) => typeof value !== 'number')) {
      results.push({ name, severity: 'error', passed: false, reason: 'metric unavailable' })
      continue
    }

    const value = median(values)
    const passed =
      (expectation.minScore === undefined || value >= expectation.minScore) &&
      (expectation.maxNumericValue === undefined || value <= expectation.maxNumericValue)
    results.push({ name, severity, passed, value, expectation })
  }

  return { url, results }
}

await mkdir(outputDirectory, { recursive: true })
const chrome = await launch({ chromeFlags, handleSIGINT: false })
const summaries = []

try {
  for (const url of config.ci.collect.url) {
    const reports = []
    const slug = slugFor(url)
    for (let run = 1; run <= runs; run += 1) {
      const result = await lighthouse(
        url,
        {
          logLevel: 'error',
          output: ['json', 'html'],
          port: chrome.port,
        },
        config.ci.collect.settings.preset === 'desktop' ? desktopConfig : undefined,
      )
      if (!result) {
        throw new Error(`Lighthouse did not return a report for ${url}`)
      }

      const reportsByFormat = Array.isArray(result.report) ? result.report : [result.report]
      await Promise.all([
        writeFile(path.join(outputDirectory, `${slug}-${run}.json`), reportsByFormat[0]),
        writeFile(path.join(outputDirectory, `${slug}-${run}.html`), reportsByFormat[1]),
      ])
      reports.push(result.lhr)
    }

    summaries.push(evaluate(url, reports))
  }
} finally {
  chrome.kill()
  chrome.process.unref()
}

await writeFile(
  path.join(outputDirectory, 'assertion-summary.json'),
  `${JSON.stringify({ generatedAtUtc: new Date().toISOString(), summaries }, null, 2)}\n`,
)

let failed = false
for (const summary of summaries) {
  for (const assertion of summary.results) {
    const message = `${summary.url} ${assertion.name}: ${assertion.passed ? 'PASS' : 'FAIL'} (${assertion.value ?? assertion.reason})`
    if (!assertion.passed && assertion.severity === 'error') {
      failed = true
      console.error(message)
    } else if (!assertion.passed) {
      console.warn(message)
    } else {
      console.log(message)
    }
  }
}

if (failed) {
  process.exitCode = 1
}
