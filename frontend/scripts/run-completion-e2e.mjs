import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { createHash, randomBytes } from 'node:crypto'
import { mkdir, readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const frontend = fileURLToPath(new URL('../', import.meta.url))
const root = path.resolve(frontend, '..')
const output = path.resolve(
  process.env.CULINARY_COMPLETION_RESULTS ??
    path.join(root, 'tmp/completion-e2e', randomBytes(6).toString('hex')),
)
await mkdir(output, { recursive: true })
const commands = []
async function run(exe, args, cwd, name, env = {}) {
  const started = new Date().toISOString()
  const child = spawn(exe, args, {
    cwd,
    windowsHide: true,
    env: { ...process.env, ...env },
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  let log = ''
  child.stdout.on('data', (chunk) => {
    log += chunk
  })
  child.stderr.on('data', (chunk) => {
    log += chunk
  })
  const code = await new Promise((resolve, reject) => {
    child.on('error', reject)
    child.on('close', resolve)
  })
  await writeFile(path.join(output, `${name}.log`), log)
  commands.push({
    exe,
    args,
    cwd,
    started,
    finished: new Date().toISOString(),
    exitCode: code,
    log: `${name}.log`,
  })
  await writeFile(path.join(output, 'commands.json'), JSON.stringify(commands, null, 2))
  assert.equal(code, 0, `${name} failed; see ${output}: ${log.slice(-2500)}`)
  return log
}
async function sources() {
  const listing = await run(
    'git',
    [
      'ls-files',
      '--cached',
      '--others',
      '--exclude-standard',
      '-z',
      'backend',
      'frontend',
      'scripts',
      '.github',
      '.editorconfig',
    ],
    root,
    'source-inventory',
  )
  const files = {}
  for (const name of [...new Set(listing.split('\0'))]
    .filter(
      (fileName) =>
        /\.(cs|csproj|props|targets|slnx|ts|tsx|js|jsx|mjs|json|yml|yaml|py|config)$/.test(fileName) ||
        fileName === '.editorconfig',
    )
    .sort()) {
    files[name] = createHash('sha256')
      .update((await readFile(path.join(root, name))).toString('utf8').replaceAll('\r\n', '\n'))
      .digest('hex')
  }
  return { sha256: createHash('sha256').update(JSON.stringify(files)).digest('hex'), files }
}
const before = await sources()
await run(
  'dotnet',
  ['build', 'backend/CulinaryBlog.slnx', '-c', 'Release', '--no-restore', '-warnaserror'],
  root,
  'backend-build',
)
await run(process.execPath, ['node_modules/next/dist/bin/next', 'build'], frontend, 'frontend-build', {
  INTERNAL_API_BASE_URL: 'http://127.0.0.1:1/api/v1',
})
const backendBinaries = {}
for (const name of [
  'CulinaryBlog.Api',
  'CulinaryBlog.Application',
  'CulinaryBlog.Domain',
  'CulinaryBlog.Infrastructure',
]) {
  backendBinaries[`${name}.dll`] = createHash('sha256')
    .update(
      await readFile(path.join(root, 'backend/src/CulinaryBlog.Api/bin/Release/net10.0', `${name}.dll`)),
    )
    .digest('hex')
}
const manifest = {
  source: before,
  buildId: (await readFile(path.join(frontend, '.next/BUILD_ID'), 'utf8')).trim(),
  backendBinaries,
  node: process.version,
}
const manifestPath = path.join(output, 'build.json')
await writeFile(manifestPath, JSON.stringify(manifest, null, 2))
const log = await run(
  process.execPath,
  [
    '--test',
    '--test-concurrency=1',
    'tests/admin-users.e2e.mjs',
    'tests/media-upload.e2e.mjs',
    'tests/recipe-lifecycle.e2e.mjs',
    'tests/audit-logging.e2e.mjs',
  ],
  frontend,
  'browser',
  { CULINARY_E2E_BUILD_MANIFEST: manifestPath },
)
assert.equal((await sources()).sha256, before.sha256, 'Source changed during the frozen-build verification')
const evidence = []
for (const match of log.matchAll(/^# Evidence: (.+)$/gm)) {
  const directory = match[1].trim().replaceAll('\\\\', '\\')
  const identity = JSON.parse(await readFile(path.join(directory, 'run.json'), 'utf8'))
  const cleanup = JSON.parse(await readFile(path.join(directory, 'cleanup.json'), 'utf8'))
  assert.equal(identity.buildId, manifest.buildId)
  assert.equal(identity.apiSha256, backendBinaries['CulinaryBlog.Api.dll'])
  assert.ok(cleanup.networkRemoved)
  evidence.push({ directory, identity, cleanup })
}
assert.equal(evidence.length, 4, 'All four real-stack scenarios must report evidence')
await writeFile(
  path.join(output, 'result.json'),
  JSON.stringify(
    { passed: true, sourceSha256: before.sha256, buildId: manifest.buildId, evidence, commands },
    null,
    2,
  ),
)
process.stdout.write(`All four requirements passed on build ${manifest.buildId}; evidence: ${output}\n`)
