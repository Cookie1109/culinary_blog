import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { randomBytes, createHash } from 'node:crypto'
import { createWriteStream, existsSync } from 'node:fs'
import { cp, mkdir, readFile, rm, writeFile } from 'node:fs/promises'
import http from 'node:http'
import net from 'node:net'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { chromium } from '@playwright/test'

const frontend = fileURLToPath(new URL('../../', import.meta.url))
const root = path.resolve(frontend, '..')
const run = randomBytes(6).toString('hex')
const artifacts = path.join(root, 'tmp/feature-e2e', run)
const label = `culinary.feature=${run}`
const network = `culinary-feature-${run}`
export const password = 'Synthetic#UsersE2e123'

export async function withIsolatedApp({ media = false }, exercise) {
  const containers = []
  const processes = []
  let gateway
  let browser
  let ownsNetwork = false
  await mkdir(artifacts, { recursive: true })

  async function command(exe, args, cwd = root, env = {}) {
    const child = spawn(exe, args, {
      cwd,
      windowsHide: true,
      env: { ...process.env, ...env },
      stdio: ['ignore', 'pipe', 'pipe'],
    })
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
    assert.equal(code, 0, `${exe} ${args[0]} failed: ${output.slice(-2000)}`)
    return output.trim()
  }
  async function build(name, exe, args, cwd) {
    const output = await command(exe, args, cwd)
    await writeFile(path.join(artifacts, `${name}.log`), output)
  }
  function start(name, exe, args, cwd, env) {
    const log = createWriteStream(path.join(artifacts, `${name}.log`))
    const child = spawn(exe, args, {
      cwd,
      windowsHide: true,
      env: { ...process.env, ...env },
      stdio: ['ignore', 'pipe', 'pipe'],
    })
    child.stdout.pipe(log, { end: false })
    child.stderr.pipe(log, { end: false })
    const exit = new Promise((resolve) => {
      child.on('close', (code) => {
        log.end()
        resolve(code)
      })
    })
    processes.push({ child, exit })
  }
  async function freePort() {
    const server = net.createServer()
    await new Promise((resolve) => {
      server.listen(0, '127.0.0.1', resolve)
    })
    const port = server.address().port
    await new Promise((resolve) => server.close(resolve))
    return port
  }
  async function ready(name, probe) {
    const deadline = Date.now() + 90_000
    while (Date.now() < deadline) {
      try {
        if (await probe()) return
      } catch {
        /* Wait only for this run's dependencies. */
      }
      await new Promise((resolve) => setTimeout(resolve, 300))
    }
    throw new Error(`${name} readiness failed; logs: ${artifacts}`)
  }
  async function container(image, port, env = {}, args = [], alias = '') {
    const id = await command('docker', [
      'run',
      '-d',
      '--network',
      network,
      '--label',
      label,
      ...(alias ? ['--network-alias', alias] : []),
      '-p',
      `127.0.0.1:${await freePort()}:${port}`,
      ...Object.entries(env).flatMap(([key, value]) => ['-e', `${key}=${value}`]),
      image,
      ...args,
    ])
    containers.push(id)
    const info = JSON.parse(await command('docker', ['inspect', id]))[0]
    return { id, port: info.NetworkSettings.Ports[`${port}/tcp`][0].HostPort }
  }

  try {
    await command('docker', ['info', '--format', '{{.ServerVersion}}'])
    await build(
      'backend-build',
      'dotnet',
      ['build', 'backend/CulinaryBlog.slnx', '-c', 'Release', '--no-restore'],
      root,
    )
    const previousApi = process.env.INTERNAL_API_BASE_URL
    process.env.INTERNAL_API_BASE_URL = 'http://127.0.0.1:1/api/v1'
    try {
      await build('frontend-build', process.execPath, ['node_modules/next/dist/bin/next', 'build'], frontend)
    } finally {
      if (previousApi === undefined) delete process.env.INTERNAL_API_BASE_URL
      else process.env.INTERNAL_API_BASE_URL = previousApi
    }
    const buildId = (await readFile(path.join(frontend, '.next/BUILD_ID'), 'utf8')).trim()
    await command('docker', ['network', 'create', '--label', label, network])
    ownsNetwork = true
    const pg = await container('postgres:16.10-bookworm', 5432, {
      POSTGRES_DB: 'users',
      POSTGRES_USER: 'users',
      POSTGRES_PASSWORD: 'synthetic-users-only',
    })
    const redis = await container('redis:7.4.5-alpine', 6379)
    const connection = `Host=127.0.0.1;Port=${pg.port};Database=users;Username=users;Password=synthetic-users-only`
    let minio
    if (media) {
      minio = await container(
        'minio/minio:RELEASE.2025-09-07T16-13-09Z@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e',
        9000,
        { MINIO_ROOT_USER: 'media-storage', MINIO_ROOT_PASSWORD: 'synthetic-media-only' },
        ['server', '/data'],
      )
      await ready('MinIO', async () => (await fetch(`http://127.0.0.1:${minio.port}/minio/health/ready`)).ok)
      await command('docker', [
        'exec',
        minio.id,
        'mc',
        'alias',
        'set',
        'test',
        'http://127.0.0.1:9000',
        'media-storage',
        'synthetic-media-only',
      ])
      await command('docker', ['exec', minio.id, 'mc', 'mb', 'test/media-test'])
    }
    if (!media) {
      await ready('PostgreSQL', async () => {
        await command('docker', ['exec', pg.id, 'pg_isready', '-U', 'users', '-d', 'users'])
        return true
      })
      await command('dotnet', ['tool', 'restore'], path.join(root, 'backend'))
      await command(
        'dotnet',
        [
          'ef',
          'database',
          'update',
          '--project',
          'src/CulinaryBlog.Infrastructure',
          '--startup-project',
          'src/CulinaryBlog.Api',
          '--configuration',
          'Release',
          '--no-build',
          '--connection',
          connection,
        ],
        path.join(root, 'backend'),
        {
          ASPNETCORE_ENVIRONMENT: 'Testing',
          BackgroundJobs__Enabled: 'false',
          ConnectionStrings__Database: connection,
          ConnectionStrings__Redis: `127.0.0.1:${redis.port},abortConnect=false`,
        },
      )
      await command('docker', [
        'exec',
        pg.id,
        'psql',
        '-U',
        'users',
        '-d',
        'users',
        '-v',
        'ON_ERROR_STOP=1',
        '-c',
        `INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName") VALUES
        ('00000000-0000-0000-0000-000000000001', 'Author', 'AUTHOR'),
        ('00000000-0000-0000-0000-000000000002', 'Admin', 'ADMIN')`,
      ])
    }
    const api = `http://127.0.0.1:${await freePort()}`
    const web = `http://127.0.0.1:${await freePort()}`
    gateway = http.createServer((request, response) => {
      const upstream = http.request(
        new URL(request.url, request.url.startsWith('/api/v1/') ? api : web),
        { method: request.method, headers: request.headers },
        (result) => {
          response.writeHead(result.statusCode, result.headers)
          result.pipe(response)
        },
      )
      upstream.on('error', () => {
        if (!response.headersSent) response.writeHead(502)
        response.end()
      })
      request.pipe(upstream)
    })
    await new Promise((resolve) => gateway.listen(0, '127.0.0.1', resolve))
    const baseURL = `http://127.0.0.1:${gateway.address().port}`
    const revalidationSecret = randomBytes(32).toString('hex')
    const apiCopy = path.join(artifacts, 'api')
    await cp(path.join(root, 'backend/src/CulinaryBlog.Api/bin/Release/net10.0'), apiCopy, {
      recursive: true,
    })
    start('api', 'dotnet', ['CulinaryBlog.Api.dll'], apiCopy, {
      ASPNETCORE_ENVIRONMENT: 'Testing',
      ASPNETCORE_URLS: api,
      ConnectionStrings__Database: connection,
      ConnectionStrings__Redis: `127.0.0.1:${redis.port},abortConnect=false`,
      ObjectStorage__Endpoint: media ? `http://127.0.0.1:${minio.port}` : 'http://127.0.0.1:1',
      ObjectStorage__AccessKey: 'media-storage',
      ObjectStorage__SecretKey: 'synthetic-media-only',
      ObjectStorage__BucketName: 'media-test',
      Database__ApplyMigrationsOnStartup: String(media),
      BackgroundJobs__Enabled: String(media),
      Email__Enabled: 'false',
      AdminSeed__Enabled: String(media),
      AdminSeed__Email: 'admin@users-e2e.test',
      AdminSeed__Password: password,
      Jwt__SigningKey: randomBytes(32).toString('hex'),
      DataProtection__KeyPath: path.join(artifacts, 'keys'),
      RateLimiting__AuthPermitLimit: '1000',
      RateLimiting__UploadPermitLimit: '1000',
      RateLimiting__GlobalPermitLimit: '10000',
      Seq__ServerUrl: '',
      Telemetry__OtlpEndpoint: '',
      Sitemap__PublicBaseUrl: baseURL,
      Sitemap__RevalidationEndpoint: `${web}/api/revalidate`,
      Sitemap__RevalidationSecret: revalidationSecret,
    })
    await ready('API', async () => (await fetch(`${api}/health/ready`)).ok)
    const standalone = path.join(artifacts, 'standalone')
    await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
    await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), {
      recursive: true,
    })
    if (existsSync(path.join(frontend, 'public'))) {
      await cp(path.join(frontend, 'public'), path.join(standalone, 'public'), { recursive: true })
    }
    assert.equal((await readFile(path.join(standalone, '.next/BUILD_ID'), 'utf8')).trim(), buildId)
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
      REVALIDATION_SECRET: revalidationSecret,
      AUTH_GOOGLE_ID: '',
      AUTH_GOOGLE_SECRET: '',
    })
    await ready('Next', async () => (await fetch(`${web}/login`)).ok)
    await writeFile(
      path.join(artifacts, 'openapi.json'),
      JSON.stringify(await (await fetch(`${api}/openapi/v1.json`)).json()),
    )
    browser = await chromium.launch()
    await exercise({ api, baseURL, artifacts, browser, command, pg, minio })
    await writeFile(
      path.join(artifacts, 'run.json'),
      JSON.stringify(
        {
          run,
          buildId,
          node: process.version,
          apiSha256: createHash('sha256')
            .update(await readFile(path.join(apiCopy, 'CulinaryBlog.Api.dll')))
            .digest('hex'),
        },
        null,
        2,
      ),
    )
  } finally {
    if (browser) await browser.close()
    if (gateway) {
      gateway.closeAllConnections()
      await new Promise((resolve) => gateway.close(resolve))
    }
    for (const { child, exit } of processes.reverse()) {
      if (child.exitCode === null && !child.signalCode) {
        if (process.platform === 'win32') await command('taskkill', ['/PID', String(child.pid), '/T', '/F'])
        else child.kill('SIGTERM')
        await exit
      }
    }
    for (const id of containers.reverse()) {
      const info = JSON.parse(await command('docker', ['inspect', id]))[0]
      assert.equal(info.Config.Labels['culinary.feature'], run)
      await command('docker', ['rm', '-f', '-v', id])
    }
    if (ownsNetwork) await command('docker', ['network', 'rm', network])
    for (const name of ['api', 'standalone', 'keys']) {
      const target = path.resolve(artifacts, name)
      assert.ok(target.startsWith(path.resolve(artifacts) + path.sep))
      await rm(target, { recursive: true, force: true })
    }
    await writeFile(
      path.join(artifacts, 'cleanup.json'),
      JSON.stringify({ run, containersRemoved: containers.length, networkRemoved: ownsNetwork }),
    )
    process.stdout.write(`Evidence: ${artifacts}\n`)
  }
}
