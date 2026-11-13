import { format as formatCliOutput } from 'node:util'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { createHash, randomBytes } from 'node:crypto'
import { createWriteStream, existsSync } from 'node:fs'
import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises'
import http from 'node:http'
import net from 'node:net'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const frontend = fileURLToPath(new URL('../', import.meta.url))
const root = path.dirname(frontend.replace(/[\\/]$/, ''))
const run = randomBytes(6).toString('hex')
const artifacts = path.join(root, 'tmp/package-G05/e2e', run)
const containers = []
const processes = []
const network = `culinary-g05-${run}`
let ownsNetwork = false
let gateway
await mkdir(artifacts, { recursive: true })

async function command(exe, args, cwd = root, env = {}) {
  const child = spawn(exe, args, {
    cwd,
    env: { ...process.env, ...env },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  let output = ''
  let stdout = ''
  child.stdout.on('data', (chunk) => {
    output += chunk
    stdout += chunk
  })
  child.stderr.on('data', (chunk) => {
    output += chunk
  })
  const code = await new Promise((resolve, reject) => {
    child.on('error', reject)
    child.on('close', resolve)
  })
  return { code, output, stdout }
}
async function checked(exe, args, cwd, env) {
  const result = await command(exe, args, cwd, env)
  assert.equal(result.code, 0, `${exe} ${args[0]} failed: ${result.output.slice(-2000)}`)
  return result.stdout.trim()
}
async function build(name, exe, args, cwd) {
  const result = await command(exe, args, cwd, {
    INTERNAL_API_BASE_URL: 'http://127.0.0.1:1/api/v1',
    NEXT_PUBLIC_API_BASE_URL: '/api/v1',
  })
  await writeFile(path.join(artifacts, `${name}.log`), result.output)
  assert.equal(result.code, 0, `${name} failed; inspect ${artifacts}`)
}
function start(name, exe, args, cwd, env) {
  const log = createWriteStream(path.join(artifacts, `${name}.log`), { flags: 'a' })
  const child = spawn(exe, args, {
    cwd,
    env: { ...process.env, ...env },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  child.stdout.pipe(log, { end: false })
  child.stderr.pipe(log, { end: false })
  child.on('error', (error) => log.write(String(error)))
  const exit = new Promise((resolve) => {
    child.on('close', (code) => {
      log.end()
      resolve(code)
    })
  })
  processes.push({ child, exit })
  return { child, exit }
}
async function stopProcess({ child, exit }) {
  if (child.pid && child.exitCode === null && !child.signalCode) {
    if (process.platform === 'win32') await checked('taskkill', ['/PID', String(child.pid), '/T', '/F'])
    else child.kill('SIGTERM')
    await exit
  }
}
async function port() {
  const server = net.createServer()
  await new Promise((resolve) => {
    server.listen(0, '127.0.0.1', resolve)
  })
  const value = server.address().port
  await new Promise((resolve) => {
    server.close(resolve)
  })
  return value
}
async function waitFor(name, probe) {
  const deadline = Date.now() + 90_000
  while (Date.now() < deadline) {
    try {
      if (await probe()) return
    } catch {
      /* readiness retries */
    }
    await new Promise((resolve) => {
      setTimeout(resolve, 300)
    })
  }
  throw new Error(`${name} readiness failed; inspect ${artifacts}`)
}
async function container(image, ports, env = {}, args = [], alias = '') {
  const id = await checked('docker', [
    'run',
    '-d',
    '--network',
    network,
    '--label',
    `culinary.g05=${run}`,
    ...(alias ? ['--network-alias', alias] : []),
    ...ports.flatMap((value) => ['-p', `127.0.0.1::${value}`]),
    ...Object.entries(env).flatMap(([key, value]) => ['-e', `${key}=${value}`]),
    image,
    ...args,
  ])
  containers.push(id)
  const info = JSON.parse(await checked('docker', ['inspect', id]))[0]
  return Object.fromEntries(
    ports.map((value) => [value, info.NetworkSettings.Ports[`${value}/tcp`][0].HostPort]),
  )
}

try {
  await checked('docker', ['info', '--format', '{{.ServerVersion}}'])
  await build(
    'backend-build',
    'dotnet',
    ['build', 'CulinaryBlog.slnx', '-c', 'Release', '--no-restore'],
    path.join(root, 'backend'),
  )
  await build('frontend-build', process.execPath, ['node_modules/next/dist/bin/next', 'build'], frontend)
  const buildId = (await readFile(path.join(frontend, '.next/BUILD_ID'), 'utf8')).trim()
  const lockHash = createHash('sha256')
    .update(await readFile(path.join(frontend, 'package-lock.json')))
    .digest('hex')
  await checked('docker', ['network', 'create', '--label', `culinary.g05=${run}`, network])
  ownsNetwork = true
  const pg = await container('postgres:16.10-bookworm', [5432], {
    POSTGRES_DB: 'g05',
    POSTGRES_USER: 'g05',
    POSTGRES_PASSWORD: 'synthetic-g05-only',
  })
  const redis = await container('redis:7.4.5-alpine', [6379])
  const minioImage = 'culinary-blog-minio-test:2025-09-07-v2'
  await checked('docker', [
    'build',
    '--tag',
    minioImage,
    '--file',
    path.join(root, 'infra/testing/minio.Dockerfile'),
    path.join(root, 'infra/testing'),
  ])
  const minio = await container(
    minioImage,
    [9000],
    { MINIO_ROOT_USER: 'g05storage', MINIO_ROOT_PASSWORD: 'synthetic-g05-storage' },
    ['server', '/data'],
    'storage',
  )
  const smtp = await container('mailhog/mailhog:v1.0.1', [1025, 8025])
  await waitFor('MinIO', async () => (await fetch(`http://127.0.0.1:${minio[9000]}/minio/health/ready`)).ok)
  await checked('docker', [
    'run',
    '--rm',
    '--network',
    network,
    '--entrypoint',
    '/bin/sh',
    minioImage,
    '-ec',
    'mc alias set test http://storage:9000 g05storage synthetic-g05-storage >/dev/null && mc mb test/g05',
  ])
  const api = `http://127.0.0.1:${await port()}`
  const web = `http://127.0.0.1:${await port()}`
  let apiProcess
  let startApi
  gateway = http.createServer((request, response) => {
    if (request.method === 'POST' && ['/__g05__/api/down', '/__g05__/api/up'].includes(request.url)) {
      // Test-only controller bound to loopback: stop/restart only this runner's copied API.
      const action = request.url.endsWith('/down')
        ? stopProcess(apiProcess)
        : (async () => {
            apiProcess = startApi()
            await waitFor('restarted API', async () => (await fetch(`${api}/health/ready`)).ok)
          })()
      action.then(
        () => {
          response.writeHead(200)
          response.end()
        },
        () => {
          response.writeHead(500)
          response.end()
        },
      )
      return
    }
    const target = new URL(request.url, request.url.startsWith('/api/v1/') ? api : web)
    const upstream = http.request(target, { method: request.method, headers: request.headers }, (result) => {
      response.writeHead(result.statusCode, result.headers)
      result.pipe(response)
    })
    upstream.on('error', () => {
      if (!response.headersSent && !response.destroyed) {
        response.writeHead(502)
        response.end('Test upstream unavailable')
      } else response.destroy()
    })
    request.pipe(upstream)
  })
  await new Promise((resolve) => {
    gateway.listen(0, '127.0.0.1', resolve)
  })
  const baseURL = `http://127.0.0.1:${gateway.address().port}`
  const secret = randomBytes(32).toString('hex')
  const apiCopy = path.join(artifacts, 'api')
  await cp(path.join(root, 'backend/src/CulinaryBlog.Api/bin/Release/net10.0'), apiCopy, { recursive: true })
  startApi = () =>
    start('api', 'dotnet', ['CulinaryBlog.Api.dll'], apiCopy, {
      ASPNETCORE_ENVIRONMENT: 'Testing',
      ASPNETCORE_URLS: api,
      ConnectionStrings__Database: `Host=127.0.0.1;Port=${pg[5432]};Database=g05;Username=g05;Password=synthetic-g05-only`,
      ConnectionStrings__Redis: `127.0.0.1:${redis[6379]},abortConnect=false`,
      ObjectStorage__Endpoint: `http://127.0.0.1:${minio[9000]}`,
      ObjectStorage__AccessKey: 'g05storage',
      ObjectStorage__SecretKey: 'synthetic-g05-storage',
      ObjectStorage__BucketName: 'g05',
      Database__ApplyMigrationsOnStartup: 'true',
      BackgroundJobs__Enabled: 'true',
      Email__Enabled: 'true',
      Email__Host: '127.0.0.1',
      Email__Port: smtp[1025],
      Email__EnableSsl: 'false',
      AdminSeed__Enabled: 'true',
      AdminSeed__Email: 'admin@g05.test',
      AdminSeed__Password: 'Synthetic#G05Admin123',
      Jwt__SigningKey: randomBytes(32).toString('hex'),
      Jwt__AccessTokenMinutes: '1',
      DataProtection__KeyPath: path.join(artifacts, 'keys'),
      Seq__ServerUrl: '',
      Telemetry__OtlpEndpoint: '',
      Sitemap__PublicBaseUrl: baseURL,
      Sitemap__RevalidationEndpoint: `${web}/api/revalidate`,
      Sitemap__RevalidationSecret: secret,
      RateLimiting__AuthPermitLimit: '100',
      RateLimiting__GlobalPermitLimit: '10000',
      ReverseProxy__TrustForwardedHeaders: 'false',
    })
  apiProcess = startApi()
  await waitFor('API', async () => (await fetch(`${api}/health/ready`)).ok)
  const standalone = path.join(artifacts, 'standalone')
  await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
  assert.equal(
    (await readFile(path.join(standalone, '.next/BUILD_ID'), 'utf8')).trim(),
    buildId,
    'Frontend build changed during artifact copy',
  )
  await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), { recursive: true })
  if (existsSync(path.join(frontend, 'public')))
    await cp(path.join(frontend, 'public'), path.join(standalone, 'public'), { recursive: true })
  start('next', process.execPath, ['server.js'], standalone, {
    HOSTNAME: '127.0.0.1',
    PORT: new URL(web).port,
    NODE_ENV: 'production',
    INTERNAL_API_BASE_URL: `${api}/api/v1`,
    SITE_URL: baseURL,
    AUTH_URL: baseURL,
    NEXTAUTH_URL: baseURL,
    AUTH_TRUST_HOST: 'true',
    AUTH_SECRET: randomBytes(32).toString('hex'),
    AUTH_GOOGLE_ID: '',
    AUTH_GOOGLE_SECRET: '',
    REVALIDATION_SECRET: secret,
  })
  await waitFor('Next', async () => (await fetch(`${web}/auth/login`)).ok)
  const result = await command(
    process.execPath,
    ['node_modules/@playwright/test/cli.js', 'test', ...process.argv.slice(2)],
    frontend,
    {
      E2E_BASE_URL: baseURL,
      E2E_API_URL: api,
      E2E_SMTP_URL: `http://127.0.0.1:${smtp[8025]}`,
      E2E_ARTIFACTS: artifacts,
    },
  )
  await writeFile(path.join(artifacts, 'test.log'), result.output)
  process.stdout.write(`${formatCliOutput(result.output)}\n`)
  await writeFile(
    path.join(artifacts, 'run.json'),
    JSON.stringify(
      {
        run,
        buildId,
        lockHash,
        nodeVersion: process.version,
        apiDllSha256: createHash('sha256')
          .update(await readFile(path.join(apiCopy, 'CulinaryBlog.Api.dll')))
          .digest('hex'),
        code: result.code,
      },
      null,
      2,
    ),
  )
  process.exitCode = result.code
} finally {
  if (gateway) {
    gateway.closeAllConnections()
    await new Promise((resolve) => {
      gateway.close(resolve)
    })
  }
  for (const owned of processes.reverse()) await stopProcess(owned)
  for (const id of containers.reverse()) {
    const info = JSON.parse(await checked('docker', ['inspect', id]))[0]
    assert.equal(info.Config.Labels['culinary.g05'], run)
    await checked('docker', ['rm', '-f', '-v', id])
  }
  if (ownsNetwork) await checked('docker', ['network', 'rm', network])
  for (const name of ['api', 'standalone', 'keys']) {
    const target = path.resolve(artifacts, name)
    assert.ok(target.startsWith(path.resolve(artifacts) + path.sep))
    await rm(target, { recursive: true, force: true })
  }
  await writeFile(
    path.join(artifacts, 'cleanup.json'),
    JSON.stringify({ run, containersRemoved: containers.length, networkRemoved: ownsNetwork }),
  )
  process.stdout.write(`${formatCliOutput(`G05 artifacts: ${artifacts}`)}\n`)
}
