import { format as formatCliOutput } from 'node:util'
import { spawnSync } from 'node:child_process'

process.env.ANALYZE = 'true'

process.stdout.write(
  `${formatCliOutput('Starting Next.js production build with bundle analysis enabled (ANALYZE=true)...')}\n`,
)

const build = spawnSync(process.platform === 'win32' ? 'npx.cmd' : 'npx', ['next', 'build'], {
  stdio: 'inherit',
  env: process.env,
})

if (build.status !== 0) {
  console.error(`Build failed with exit code ${build.status}`)
  process.exit(build.status ?? 1)
}

process.stdout.write(`${formatCliOutput('Bundle analysis generated in .next/analyze/')}\n`)
const check = spawnSync('node', ['scripts/check-bundle-budget.mjs'], {
  stdio: 'inherit',
})

process.exit(check.status ?? 0)
