/* eslint-disable @typescript-eslint/no-require-imports */
const baseConfig = require('./lighthouserc.json')

const baseUrl = (process.env.LHCI_BASE_URL || 'http://localhost:8080').replace(/\/$/, '')
const defaultPaths = ['/', '/recipes', '/categories', '/search']
const paths = process.env.LHCI_PATHS_JSON ? JSON.parse(process.env.LHCI_PATHS_JSON) : defaultPaths
const releaseAssertionNames = [
  'categories:performance',
  'largest-contentful-paint',
  'cumulative-layout-shift',
  'total-blocking-time',
  'resource-summary:script:size',
]
const releaseAssertions = {
  ...Object.fromEntries(releaseAssertionNames.map((name) => [name, baseConfig.ci.assert.assertions[name]])),
  'meta-description': ['error', { minScore: 1 }],
}

module.exports = {
  ci: {
    collect: {
      ...baseConfig.ci.collect,
      startServerCommand: undefined,
      url: paths.map((path) => `${baseUrl}${path}`),
      settings: {
        ...baseConfig.ci.collect.settings,
        chromeFlags: '--headless=new --no-sandbox --disable-gpu --disable-dev-shm-usage',
      },
    },
    assert: {
      assertions: releaseAssertions,
    },
    upload: {
      target: 'filesystem',
      outputDir: process.env.LHCI_OUTPUT_DIR || '../artifacts/performance/lighthouse',
    },
  },
}
