import { format as formatCliOutput } from 'node:util'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { createHash, randomBytes } from 'node:crypto'
import { createWriteStream, existsSync } from 'node:fs'
import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises'
import { createServer } from 'node:net'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { createPreviewGateway } from './g04-preview-gateway.mjs'

// Default: loopback preflight, then cleanup. --public is a separate, explicitly approved action.
const frontend = fileURLToPath(new URL('../', import.meta.url))
const root = path.dirname(frontend.replace(/[\\/]$/, ''))
const publish = process.argv.includes('--public')
const runId = randomBytes(5).toString('hex')
const artifacts = path.join(root, 'tmp/g04/preview', runId)
const network = `culinary-g04-preview-${runId}`
const containers = []
const children = []
const delay = (ms) =>
  new Promise((resolve) => {
    setTimeout(resolve, ms)
  })
const hash = (buffer) => createHash('sha256').update(buffer).digest('hex')
let gateway
let ownsNetwork = false
let nextOrigin
let apiOrigin
let siteOrigin
let publicExpiry
let publicTimer
await mkdir(artifacts, { recursive: true })

async function command(executable, args, cwd = root) {
  const child = spawn(executable, args, { cwd, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] })
  let output = ''
  child.stdout.on('data', (chunk) => {
    output += chunk
  })
  child.stderr.on('data', (chunk) => {
    output += chunk
  })
  const code = await new Promise((resolve, reject) => {
    child.on('error', reject)
    child.on('close', resolve)
  })
  assert.equal(code, 0, `${executable} ${args[0]} failed: ${output.slice(-1500)}`)
  return output.trim()
}
function start(name, executable, args, cwd, env = {}) {
  const output = createWriteStream(path.join(artifacts, `${name}.log`))
  const child = spawn(executable, args, {
    cwd,
    env: { ...process.env, ...env },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  child.stdout.pipe(output, { end: false })
  child.stderr.pipe(output, { end: false })
  child.on('error', (error) => output.write(String(error)))
  const owned = {
    child,
    exit: new Promise((resolve) => {
      child.on('close', (code) => {
        output.end()
        resolve(code)
      })
    }),
  }
  children.push(owned)
  return owned
}
async function port() {
  const server = createServer()
  await new Promise((resolve) => {
    server.listen(0, '127.0.0.1', resolve)
  })
  const value = server.address().port
  await new Promise((resolve) => {
    server.close(resolve)
  })
  return value
}
async function waitFor(name, probe, timeout = 90000) {
  const deadline = Date.now() + timeout
  while (Date.now() < deadline) {
    try {
      if (await probe()) return
    } catch {
      /* Readiness is retried until the deadline. */
    }
    await delay(300)
  }
  throw new Error(`Timed out waiting for ${name}; inspect ${artifacts}`)
}
async function container(image, targetPort, args = [], env = {}) {
  const id = await command('docker', [
    'run',
    '--detach',
    '--network',
    network,
    '--label',
    `culinary.g04.preview=${runId}`,
    ...(image.startsWith('minio/minio') ? ['--network-alias', 'storage'] : []),
    '-p',
    `127.0.0.1::${targetPort}`,
    ...Object.entries(env).flatMap(([key, value]) => ['-e', `${key}=${value}`]),
    image,
    ...args,
  ])
  containers.push(id)
  const info = JSON.parse(await command('docker', ['inspect', id]))[0]
  return { id, port: info.NetworkSettings.Ports[`${targetPort}/tcp`][0].HostPort }
}
async function checkedJson(url) {
  const response = await fetch(url, { signal: AbortSignal.timeout(30000) })
  assert.equal(response.status, 200, url)
  return response.json()
}

try {
  const lockHash = hash(await readFile(path.join(frontend, 'package-lock.json')))
  const gate = JSON.parse(await readFile(path.join(root, 'tmp/g04/preview/security-gate.json'), 'utf8'))
  assert.equal(gate.lockHash, lockHash, 'Dependency lock changed after audit')
  assert.equal(gate.vulnerabilities, 0, 'Audit gate must pass before preview')
  const buildId = (await readFile(path.join(frontend, '.next/BUILD_ID'), 'utf8')).trim()
  assert.equal(gate.buildId, buildId, 'Use the verified frontend build')
  gateway = createPreviewGateway({
    upstream: () => nextOrigin,
    apiUpstream: () => apiOrigin,
    publicOrigin: () => siteOrigin,
  })
  await new Promise((resolve) => {
    gateway.listen(0, '127.0.0.1', resolve)
  })
  const localGateway = `http://127.0.0.1:${gateway.address().port}`
  siteOrigin = localGateway
  if (publish) {
    const binary = path.join(root, 'tmp/g04/preview/tools/cloudflared.exe')
    const download = JSON.parse(
      await readFile(path.join(root, 'tmp/g04/preview/tools/cloudflared.json'), 'utf8'),
    )
    assert.equal(hash(await readFile(binary)), download.sha256, 'Tunnel binary hash mismatch')
    const tunnel = start(
      'tunnel',
      binary,
      ['tunnel', '--url', localGateway, '--protocol', 'http2', '--no-autoupdate'],
      root,
    )
    publicExpiry = Date.now() + 20 * 60 * 1000
    publicTimer = setTimeout(() => tunnel.child.kill('SIGTERM'), 20 * 60 * 1000)
    await waitFor('tunnel URL', async () => {
      const log = await readFile(path.join(artifacts, 'tunnel.log'), 'utf8')
      const match = log.match(/https:\/\/[a-z0-9-]+\.trycloudflare\.com/)
      if (!match) return false
      ;[siteOrigin] = match
      return true
    })
  }
  await command('docker', ['network', 'create', '--label', `culinary.g04.preview=${runId}`, network])
  ownsNetwork = true
  const pg = await container('postgres:16.10-bookworm', 5432, [], {
    POSTGRES_DB: 'g04preview',
    POSTGRES_USER: 'g04preview',
    POSTGRES_PASSWORD: 'synthetic-preview-only',
  })
  const redis = await container('redis:7.4.5-alpine', 6379)
  const storage = await container(
    'minio/minio:RELEASE.2025-09-07T16-13-09Z@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e',
    9000,
    ['server', '/data'],
    { MINIO_ROOT_USER: 'g04preview', MINIO_ROOT_PASSWORD: 'synthetic-preview-storage' },
  )
  await waitFor('PostgreSQL', async () =>
    (await command('docker', ['exec', pg.id, 'pg_isready', '-U', 'g04preview'])).includes(
      'accepting connections',
    ),
  )
  await waitFor('MinIO', async () => (await fetch(`http://127.0.0.1:${storage.port}/minio/health/live`)).ok)
  await command('docker', [
    'run',
    '--rm',
    '--network',
    network,
    '--entrypoint',
    '/bin/sh',
    'minio/mc:RELEASE.2025-08-13T08-35-41Z@sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727',
    '-ec',
    'mc alias set preview http://storage:9000 g04preview synthetic-preview-storage >/dev/null && mc mb preview/g04-preview',
  ])
  apiOrigin = `http://127.0.0.1:${await port()}`
  const webOrigin = `http://127.0.0.1:${await port()}`
  const secret = randomBytes(32).toString('hex')
  const apiCopy = path.join(artifacts, 'api')
  await cp(path.join(root, 'backend/src/CulinaryBlog.Api/bin/Release/net10.0'), apiCopy, { recursive: true })
  start('api', 'dotnet', [path.join(apiCopy, 'CulinaryBlog.Api.dll')], apiCopy, {
    ASPNETCORE_ENVIRONMENT: 'Testing',
    ASPNETCORE_URLS: apiOrigin,
    ConnectionStrings__Database: `Host=127.0.0.1;Port=${pg.port};Database=g04preview;Username=g04preview;Password=synthetic-preview-only`,
    ConnectionStrings__Redis: `127.0.0.1:${redis.port},abortConnect=false`,
    ObjectStorage__Endpoint: `http://127.0.0.1:${storage.port}`,
    ObjectStorage__AccessKey: 'g04preview',
    ObjectStorage__SecretKey: 'synthetic-preview-storage',
    ObjectStorage__BucketName: 'g04-preview',
    Database__ApplyMigrationsOnStartup: 'true',
    AdminSeed__Enabled: 'false',
    Email__Enabled: 'false',
    BackgroundJobs__Enabled: 'true',
    Seq__ServerUrl: '',
    Telemetry__OtlpEndpoint: '',
    Jwt__SigningKey: randomBytes(32).toString('hex'),
    DataProtection__KeyPath: path.join(artifacts, 'data-protection'),
    Sitemap__PublicBaseUrl: siteOrigin,
    Sitemap__RevalidationEndpoint: `${webOrigin}/api/revalidate`,
    Sitemap__RevalidationSecret: secret,
    ReverseProxy__TrustForwardedHeaders: 'false',
  })
  await waitFor('seeded API', async () => (await fetch(`${apiOrigin}/health/ready`)).ok)
  const standalone = path.join(artifacts, 'standalone')
  await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
  await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), { recursive: true })
  if (existsSync(path.join(frontend, 'public')))
    await cp(path.join(frontend, 'public'), path.join(standalone, 'public'), { recursive: true })
  start('next', process.execPath, [path.join(standalone, 'server.js')], standalone, {
    HOSTNAME: '127.0.0.1',
    PORT: new URL(webOrigin).port,
    NODE_ENV: 'production',
    INTERNAL_API_BASE_URL: `${apiOrigin}/api/v1`,
    SITE_URL: siteOrigin,
    AUTH_URL: siteOrigin,
    NEXTAUTH_URL: siteOrigin,
    AUTH_TRUST_HOST: 'true',
    AUTH_SECRET: randomBytes(32).toString('hex'),
    AUTH_GOOGLE_ID: '',
    AUTH_GOOGLE_SECRET: '',
    REVALIDATION_SECRET: secret,
  })
  await waitFor('Next', async () => (await fetch(`${webOrigin}/auth/login`)).ok)
  nextOrigin = webOrigin
  const page = await checkedJson(`${apiOrigin}/api/v1/recipes?page=1&pageSize=12`)
  assert.equal(page.meta.total, 50)
  const recipe = (await checkedJson(`${apiOrigin}/api/v1/recipes/${page.data[0].slug}`)).data
  const recipePath = `/recipes/${recipe.slug}`
  const response = await fetch(localGateway + recipePath)
  assert.equal(response.status, 200)
  const html = await response.text()
  const jsonld = html.match(/<script[^>]*type="application\/ld\+json"[^>]*>([\s\S]*?)<\/script>/)
  assert.ok(jsonld, 'Recipe JSON-LD must be server-rendered')
  const data = JSON.parse(jsonld[1])
  assert.equal(data['@type'], 'Recipe')
  assert.equal(data.name, recipe.title)
  assert.equal(data.mainEntityOfPage, siteOrigin + recipePath)
  assert.ok(data.recipeIngredient.length > 0 && data.recipeInstructions.length > 0)
  assert.equal((await checkedJson(`${localGateway}/api/v1/recipes?pageSize=12`)).meta.total, 50)
  assert.ok((await checkedJson(`${localGateway}/api/v1/categories`)).data.length > 0)
  const assets = [
    ...new Set(
      [...html.matchAll(/(?:src|href)="([^"?]+\.(?:js|css))(?:\?[^" ]*)?"/g)].map((match) => match[1]),
    ),
  ]
  assert.ok(assets.length > 0)
  for (const asset of assets) {
    const result = await fetch(new URL(asset, localGateway))
    assert.equal(result.status, 200, asset)
    await result.body?.cancel()
  }
  const image = new URL(data.image[0])
  assert.equal(image.origin, siteOrigin)
  const imageResult = await fetch(localGateway + image.pathname + image.search)
  assert.equal(imageResult.status, 200)
  assert.ok(imageResult.headers.get('content-type').startsWith('image/'))
  await imageResult.body?.cancel()
  for (const route of [
    '/jobs',
    '/auth/login',
    '/api/v1/me/recipes',
    '/api/auth/session',
    '/api/private-media/a/original',
  ]) {
    assert.equal((await fetch(localGateway + route)).status, 404, route)
  }
  assert.equal((await fetch(localGateway + recipePath, { method: 'POST' })).status, 405)
  if (publish) {
    const publicResponse = await fetch(siteOrigin + recipePath)
    await publicResponse.body?.cancel()
    assert.equal(publicResponse.status, 200, 'Public HTTPS recipe must be reachable')
    assert.ok(
      !/\b(?:none|noindex)\b/i.test(publicResponse.headers.get('x-robots-tag') ?? ''),
      'Public provider adds an indexing block; this URL cannot pass Google Rich Results URL acceptance',
    )
  }
  const result = {
    runId,
    public: publish,
    siteOrigin,
    recipeUrl: siteOrigin + recipePath,
    title: recipe.title,
    recipeCount: page.meta.total,
    buildId,
    lockHash,
    jsonld: data,
    imageFetched: true,
    frontendAssetsFetched: assets.length,
    publicReadApiFetched: true,
    privateAndWritePathsBlocked: true,
    scope:
      'Disposable seed-only PG/Redis/MinIO and current G04 API/Next; gateway only reads recipes, public recipe/category API and assets. No user stack.',
    googleRichResults: 'Pending; HTML/JSON-LD preflight is not a Google tool result.',
  }
  await writeFile(path.join(artifacts, 'evidence.json'), JSON.stringify(result, null, 2))
  await writeFile(
    path.join(root, 'tmp/g04/preview/latest.json'),
    JSON.stringify({ artifacts, recipeUrl: result.recipeUrl, public: publish }, null, 2),
  )
  process.stdout.write(
    `${formatCliOutput(`PASS preview preflight: 50 seeded recipes, real API/storage, server Recipe JSON-LD, image200, private/write routes blocked. ${artifacts}`)}\n`,
  )
  if (publish) {
    process.stdout.write(
      `${formatCliOutput(`Temporary recipe URL (expires ${new Date(publicExpiry).toISOString()}): ${result.recipeUrl}`)}\n`,
    )
    await new Promise((resolve) => {
      const timer = setTimeout(resolve, Math.max(0, publicExpiry - Date.now()))
      for (const signal of ['SIGINT', 'SIGTERM'])
        process.once(signal, () => {
          clearTimeout(timer)
          resolve()
        })
    })
  }
} catch (error) {
  await writeFile(path.join(artifacts, 'failure.txt'), error.stack)
  throw error
} finally {
  clearTimeout(publicTimer)
  if (gateway) {
    gateway.closeAllConnections()
    await new Promise((resolve) => {
      gateway.close(resolve)
    })
  }
  for (const owned of children.reverse()) {
    if (owned.child.pid && owned.child.exitCode === null && !owned.child.signalCode) {
      if (process.platform === 'win32')
        await command('taskkill', ['/PID', String(owned.child.pid), '/T', '/F'])
      else owned.child.kill('SIGTERM')
      await Promise.race([owned.exit, delay(5000)])
    }
  }
  for (const id of containers.reverse()) {
    const info = JSON.parse(await command('docker', ['inspect', id]))[0]
    assert.equal(info.Config.Labels['culinary.g04.preview'], runId)
    await command('docker', ['rm', '--force', '--volumes', id])
  }
  if (ownsNetwork) await command('docker', ['network', 'rm', network])
  for (const name of ['api', 'standalone', 'data-protection']) {
    const target = path.resolve(artifacts, name)
    assert.ok(target.startsWith(path.resolve(artifacts) + path.sep))
    await rm(target, { recursive: true, force: true })
  }
}
