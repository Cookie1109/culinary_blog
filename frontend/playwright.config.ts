import { defineConfig } from '@playwright/test'

if (!process.env.E2E_BASE_URL || !process.env.E2E_ARTIFACTS) {
  throw new Error('Use npm run test:e2e to create the isolated backend and artifact directory.')
}

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120_000,
  expect: { timeout: 15_000 },
  outputDir: `${process.env.E2E_ARTIFACTS}/browser`,
  reporter: [['list'], ['json', { outputFile: `${process.env.E2E_ARTIFACTS}/playwright.json` }]],
  use: {
    baseURL: process.env.E2E_BASE_URL,
    browserName: 'chromium',
    screenshot: 'only-on-failure',
    // Auth tokens must not be written into network traces/storageState artifacts.
    trace: 'off',
  },
})
