import { format as formatCliOutput } from 'node:util'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { createWriteStream } from 'node:fs'
import { request as httpRequest } from 'node:http'
import { cp, mkdir, readFile, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { createServer } from 'node:net'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

// Real backend + Hangfire/PostgreSQL/Redis/MinIO + Next production. No provider or user stack.
const frontend = fileURLToPath(new URL('../', import.meta.url))
const root = path.dirname(frontend.replace(/[\\/]$/, ''))
const require = createRequire(import.meta.url)
const runId = randomBytes(5).toString('hex')
const artifacts = path.join(root, process.env.PUBLIC_STACK_ARTIFACTS ?? `tmp/srs-audit/public-stack-${runId}`)
await mkdir(artifacts, { recursive: true })
const processes = []
const containers = []
const evidence = []
const network = `culinary-g03-${runId}`
let ownsNetwork = false
let app
const delay = (ms) =>
  new Promise((resolve) => {
    setTimeout(resolve, ms)
  })
function http(url, options) {
  return new Promise((resolve, reject) => {
    const request = httpRequest(url, options, (response) => {
      const chunks = []
      response.on('data', (chunk) => chunks.push(chunk))
      response.on('error', reject)
      response.on('end', () =>
        resolve(
          new Response(response.statusCode === 204 ? null : Buffer.concat(chunks), {
            status: response.statusCode,
            headers: Object.fromEntries(
              Object.entries(response.headers)
                .filter(([, value]) => value !== undefined)
                .map(([key, value]) => [key, Array.isArray(value) ? value.join(', ') : value]),
            ),
          }),
        ),
      )
    })
    request.on('error', reject)
    request.end(options.body)
  })
}
async function command(executable, args, cwd = root, env = {}) {
  const child = spawn(executable, args, {
    cwd,
    env: { ...process.env, ...env },
    windowsHide: true,
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
  assert.equal(code, 0, `${executable} ${args[0]} failed: ${output.slice(-2000)}`)
  return output.trim()
}
function start(name, executable, args, cwd, env) {
  const output = createWriteStream(path.join(artifacts, `${name}.log`))
  const child = spawn(executable, args, {
    cwd,
    env: { ...process.env, ...env },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  child.stdout.pipe(output, { end: false })
  child.stderr.pipe(output, { end: false })
  const owned = {
    child,
    name,
    exit: new Promise((resolve) => {
      child.on('close', (code) => {
        output.end()
        resolve(code)
      })
    }),
  }
  child.on('error', (error) => {
    owned.error = error
  })
  processes.push(owned)
  return owned
}
async function stop(owned) {
  if (!owned?.child.pid || owned.child.exitCode !== null || owned.child.signalCode) return
  if (process.platform === 'win32') await command('taskkill', ['/PID', String(owned.child.pid), '/T', '/F'])
  else owned.child.kill('SIGTERM')
  await Promise.race([owned.exit, delay(5000)])
}
async function waitFor(name, probe, timeout = 30_000) {
  const deadline = Date.now() + timeout
  let last
  while (Date.now() < deadline) {
    try {
      if (await probe()) return
    } catch (error) {
      last = error
    }
    await delay(250)
  }
  throw new Error(`Timed out: ${name}`, { cause: last })
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
async function container(image, containerPort, args = [], environment = {}) {
  const id = await command('docker', [
    'run',
    '--detach',
    '--label',
    `culinary.g03.run=${runId}`,
    '--network',
    network,
    ...(image.startsWith('minio/minio') ? ['--network-alias', 'storage'] : []),
    '-p',
    `127.0.0.1::${containerPort}`,
    ...Object.entries(environment).flatMap(([key, value]) => ['-e', `${key}=${value}`]),
    image,
    ...args,
  ])
  containers.push(id)
  const info = JSON.parse(await command('docker', ['inspect', id]))[0]
  return { id, port: info.NetworkSettings.Ports[`${containerPort}/tcp`][0].HostPort }
}
async function check(name, action) {
  const started = Date.now()
  await action()
  evidence.push({ name, result: 'pass', seconds: (Date.now() - started) / 1000 })
  await writeFile(
    path.join(artifacts, 'evidence.json'),
    JSON.stringify({ runtime: process.version, runId, evidence }, null, 2),
  )
  process.stdout.write(`${formatCliOutput(`PASS ${name}`)}\n`)
}
try {
  if (!process.argv.includes('--skip-build')) {
    await command(
      'dotnet',
      ['build', 'CulinaryBlog.slnx', '-c', 'Release', '--no-restore'],
      path.join(root, 'backend'),
    )
    await command(process.execPath, [require.resolve('next/dist/bin/next'), 'build'], frontend, {
      INTERNAL_API_BASE_URL: 'http://127.0.0.1:1/api/v1',
    })
  }
  await command('docker', ['network', 'create', '--label', `culinary.g03.run=${runId}`, network])
  ownsNetwork = true
  const pg = await container('postgres:16.10-bookworm', 5432, [], {
    POSTGRES_DB: 'g03',
    POSTGRES_USER: 'g03',
    POSTGRES_PASSWORD: 'synthetic-stack-only',
  })
  const redis = await container('redis:7.4.5-alpine', 6379)
  const storage = await container(
    'minio/minio:RELEASE.2025-09-07T16-13-09Z@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e',
    9000,
    ['server', '/data'],
    { MINIO_ROOT_USER: 'g03synthetic', MINIO_ROOT_PASSWORD: 'synthetic-storage-only' },
  )
  await waitFor('PostgreSQL', async () =>
    (await command('docker', ['exec', pg.id, 'pg_isready', '-U', 'g03'])).includes('accepting connections'),
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
    'mc alias set synthetic http://storage:9000 g03synthetic synthetic-storage-only >/dev/null && mc mb synthetic/g03-media',
  ])
  const apiPorts = [await port(), await port()]
  const webPort = await port()
  const web = `http://127.0.0.1:${webPort}`
  const apis = apiPorts.map((value) => `http://127.0.0.1:${value}`)
  const secret = randomBytes(32).toString('hex')
  const signingKey = randomBytes(32).toString('hex')
  const apiEnvironment = {
    ASPNETCORE_ENVIRONMENT: 'Testing',
    ConnectionStrings__Database: `Host=127.0.0.1;Port=${pg.port};Database=g03;Username=g03;Password=synthetic-stack-only`,
    ConnectionStrings__Redis: `127.0.0.1:${redis.port},abortConnect=false`,
    ObjectStorage__Endpoint: `http://127.0.0.1:${storage.port}`,
    ObjectStorage__AccessKey: 'g03synthetic',
    ObjectStorage__SecretKey: 'synthetic-storage-only',
    ObjectStorage__BucketName: 'g03-media',
    Email__Enabled: 'false',
    Seq__ServerUrl: '',
    Telemetry__OtlpEndpoint: '',
    Jwt__SigningKey: signingKey,
    DataProtection__KeyPath: path.join(artifacts, 'data-protection'),
    Sitemap__PublicBaseUrl: web,
    Sitemap__RevalidationEndpoint: `${web}/api/revalidate`,
    Sitemap__RevalidationSecret: secret,
    Sitemap__DebounceSeconds: '1',
    RateLimiting__GlobalPermitLimit: '10000',
    RateLimiting__AuthPermitLimit: '100',
    RateLimiting__UploadPermitLimit: '100',
    AdminSeed__Email: 'g03-admin@example.test',
    AdminSeed__Password: 'Synthetic-Admin-123!',
    ReverseProxy__TrustForwardedHeaders: 'false',
  }
  const apiDll = path.join(root, 'backend/src/CulinaryBlog.Api/bin/Release/net10.0/CulinaryBlog.Api.dll')
  const apiCwd = path.join(root, 'backend/src/CulinaryBlog.Api')
  start('api-a', 'dotnet', [apiDll], apiCwd, {
    ...apiEnvironment,
    ASPNETCORE_URLS: apis[0],
    Database__ApplyMigrationsOnStartup: 'true',
    BackgroundJobs__Enabled: 'true',
    AdminSeed__Enabled: 'true',
  })
  await waitFor('API A seeded', async () => (await fetch(`${apis[0]}/health/live`)).ok, 90_000)
  start('api-b', 'dotnet', [apiDll], apiCwd, {
    ...apiEnvironment,
    ASPNETCORE_URLS: apis[1],
    Database__ApplyMigrationsOnStartup: 'false',
    BackgroundJobs__Enabled: 'false',
    AdminSeed__Enabled: 'false',
  })
  await waitFor('API B', async () => (await fetch(`${apis[1]}/health/live`)).ok)
  const standalone = path.join(artifacts, 'standalone')
  await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
  await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), { recursive: true })
  const nextEnvironment = {
    HOSTNAME: '127.0.0.1',
    PORT: String(webPort),
    NODE_ENV: 'production',
    INTERNAL_API_BASE_URL: `${apis[0]}/api/v1`,
    SITE_URL: web,
    AUTH_SECRET: randomBytes(32).toString('hex'),
    AUTH_TRUST_HOST: 'true',
    AUTH_URL: web,
    NEXTAUTH_URL: web,
    AUTH_GOOGLE_ID: '',
    AUTH_GOOGLE_SECRET: '',
    REVALIDATION_SECRET: secret,
  }
  const startNext = (name) =>
    start(name, process.execPath, [path.join(standalone, 'server.js')], frontend, nextEnvironment)
  app = startNext('next')
  await waitFor('Next', async () => (await fetch(`${web}/auth/login`)).ok)
  async function api(route, { method = 'GET', body, token, version, instance = 0, expected = 200 } = {}) {
    // A reverse proxy preserves one public Host across API instances; differing lab ports must not split the cache key.
    const headers = {
      Host: 'g03.synthetic.test',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(version !== undefined ? { 'If-Match': `"${version}"` } : {}),
    }
    const response = await http(`${apis[instance]}/api/v1${route}`, {
      method,
      headers: { ...headers, ...(body ? { 'Content-Type': 'application/json' } : {}) },
      ...(body ? { body: JSON.stringify(body) } : {}),
    })
    const text = await response.text()
    assert.equal(response.status, expected, `${method} ${route}: ${text.slice(0, 500)}`)
    return { response, data: text ? JSON.parse(text) : null }
  }
  const credentials = {
    email: `g03-${runId}@example.test`,
    password: 'Synthetic-Author-123!',
    displayName: 'G03 Author',
  }
  const ownerToken = (await api('/auth/register', { method: 'POST', body: credentials, expected: 201 })).data
    .data.accessToken
  const adminToken = (
    await api('/auth/login', {
      method: 'POST',
      body: { email: 'g03-admin@example.test', password: 'Synthetic-Admin-123!' },
    })
  ).data.data.accessToken
  const html = async (route) => {
    const response = await fetch(`${web}${route}`, { redirect: 'manual' })
    return {
      status: response.status,
      body: await response.text(),
      cache: response.headers.get('x-nextjs-cache'),
    }
  }
  async function visible(route, marker, present = true) {
    await waitFor(
      `${route} ${present ? 'contains' : 'omits'} ${marker}`,
      async () => {
        const page = await html(route)
        return page.status === 200 && page.body.includes(marker) === present
      },
      45_000,
    )
  }
  async function missing(route) {
    await waitFor(`404 ${route}`, async () => (await html(route)).status === 404, 45_000)
  }
  let category
  await check('category create/update/delete reaches Next through real scheduler/job/callback', async () => {
    const created = (
      await api('/categories', {
        method: 'POST',
        token: adminToken,
        expected: 201,
        body: { name: `G03 empty ${runId}`, orderIndex: 0 },
      })
    ).data.data
    await visible('/categories', created.name)
    const updated = (
      await api(`/categories/${created.id}`, {
        method: 'PUT',
        token: adminToken,
        body: { name: `G03 renamed ${runId}`, orderIndex: 0 },
      })
    ).data.data
    await visible('/categories', updated.name)
    await api(`/categories/${created.id}`, { method: 'DELETE', token: adminToken, expected: 204 })
    await visible('/categories', updated.name, false)
    category = (
      await api('/categories', {
        method: 'POST',
        token: adminToken,
        expected: 201,
        body: { name: `G03 category ${runId}`, orderIndex: 0 },
      })
    ).data.data
    await visible('/categories', category.name)
  })
  const term = `g03${runId}`
  const recipeBody = {
    title: `G03 recipe ${term}`,
    description: 'G03 original description.',
    categoryId: category.id,
    prepTime: 10,
    cookTime: 20,
    servings: 4,
    difficulty: 'easy',
    instructions: 'Cook thoroughly.',
  }
  let recipe = (await api('/recipes', { method: 'POST', token: ownerToken, expected: 201, body: recipeBody }))
    .data.data
  const owned = async () => (await api(`/me/recipes/${recipe.id}`, { token: ownerToken })).data.data
  const mutate = async (method, suffix, body, expected = 200) => {
    recipe = await owned()
    return api(`/recipes/${recipe.id}${suffix}`, {
      method,
      body,
      expected,
      token: ownerToken,
      version: recipe.version,
    })
  }
  const ingredient = (await mutate('POST', '/ingredients', { name: 'G03 rice', orderIndex: 0 }, 201)).data
    .data
  const step = (await mutate('POST', '/steps', { title: 'G03 cook', description: 'Cook thoroughly.' }, 201))
    .data.data
  const png = Buffer.from(
    'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
    'base64',
  )
  async function upload() {
    recipe = await owned()
    const form = new FormData()
    form.append('file', new Blob([png], { type: 'image/png' }), 'synthetic.png')
    const response = await fetch(`${apis[0]}/api/v1/recipes/${recipe.id}/images`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${ownerToken}`, 'If-Match': `"${recipe.version}"` },
      body: form,
    })
    assert.equal(response.status, 201, await response.clone().text())
    return (await response.json()).data
  }
  const image = await upload()
  await waitFor(
    'real image variants',
    async () => (await owned()).images.find((item) => item.id === image.id)?.processingStatus === 'ready',
    45_000,
  )
  await check('draft/private media is excluded from public cache and pages', async () => {
    await missing(`/recipes/${recipe.slug}`)
    await api(`/media/${image.id}/original`, { expected: 404 })
    const privateMedia = await fetch(`${apis[0]}/api/v1/media/${image.id}/original`, {
      headers: { Authorization: `Bearer ${ownerToken}` },
    })
    assert.equal(privateMedia.status, 200)
    assert.match(privateMedia.headers.get('cache-control'), /private, no-store/)
  })
  await mutate('PATCH', '/publish')
  const routes = [
    '/',
    `/categories/${category.slug}`,
    `/recipes?categoryId=${category.id}`,
    `/search?q=${term}`,
    `/recipes/${recipe.slug}`,
  ]
  await check('publish refreshes home/category/list/search/detail and shared API output', async () => {
    for (const route of routes) await visible(route, recipeBody.title)
    for (const instance of [0, 1]) {
      const result = await api(`/recipes/${recipe.slug}`, { instance })
      assert.equal(result.data.data.status, 'published')
      if (instance === 1) assert.ok(result.response.headers.has('age'))
    }
    assert.equal((await fetch(`${web}/api/public-media/${image.id}/medium`)).status, 200)
  })
  await check(
    'recipe/profile/composition/primary-image/variants updates invalidate both caches and Next',
    async () => {
      await mutate('PUT', '', { ...recipeBody, description: 'G03 updated description.' })
      await visible(`/recipes/${recipe.slug}`, 'G03 updated description.')
      await api('/auth/me', {
        method: 'PATCH',
        token: ownerToken,
        body: {
          displayName: 'G03 updated author',
          avatarUrl: 'https://example.test/g03-author.webp',
          bio: 'G03 updated author biography.',
        },
      })
      await visible(`/recipes/${recipe.slug}`, 'G03 updated author')
      for (const instance of [1, 0]) {
        const { author } = (await api(`/recipes/${recipe.slug}`, { instance })).data.data
        assert.equal(author.displayName, 'G03 updated author')
        assert.equal(author.avatarUrl, 'https://example.test/g03-author.webp')
        assert.equal(author.bio, 'G03 updated author biography.')
      }
      await mutate('PUT', `/ingredients/${ingredient.id}`, { name: 'G03 updated rice', orderIndex: 0 })
      await visible(`/recipes/${recipe.slug}`, 'G03 updated rice')
      await mutate('PUT', `/steps/${step.id}`, { title: 'G03 updated step', description: 'Cook thoroughly.' })
      await visible(`/recipes/${recipe.slug}`, 'G03 updated step')
      const secondImage = await upload()
      await waitFor(
        'second variants',
        async () =>
          (await owned()).images.find((item) => item.id === secondImage.id)?.processingStatus === 'ready',
        45_000,
      )
      await mutate('PATCH', `/images/${secondImage.id}`, {
        altText: 'G03 new primary image',
        isPrimary: true,
      })
      await visible(`/recipes/${recipe.slug}`, 'G03 new primary image')
      await mutate('DELETE', `/images/${image.id}`, undefined, 204)
      await missing(`/api/public-media/${image.id}/original`)
    },
  )
  await check(
    'Next unavailable does not roll back committed write; real Hangfire retry refreshes after restart',
    async () => {
      await stop(app)
      await mutate('PUT', '', { ...recipeBody, description: 'G03 retry committed description.' })
      assert.equal((await owned()).description, 'G03 retry committed description.')
      await waitFor(
        'failed callback scheduled for retry',
        async () =>
          /PublicContentRefreshJob.*RunAsync/.test(
            await readFile(path.join(artifacts, 'api-a.log'), 'utf8'),
          ) &&
          /retry attempt|Retry attempt|will be retried/.test(
            await readFile(path.join(artifacts, 'api-a.log'), 'utf8'),
          ),
        30_000,
      )
      app = startNext('next-restarted')
      await waitFor('Next restarted', async () => (await fetch(`${web}/auth/login`)).ok)
      await waitFor(
        'retried callback refresh',
        async () => (await html(`/recipes/${recipe.slug}`)).body.includes('G03 retry committed description.'),
        100_000,
      )
    },
  )
  await check(
    'unpublish/archive/delete remove content and media from all public views across instances',
    async () => {
      const assertHidden = async () => {
        await missing(`/recipes/${recipe.slug}`)
        for (const route of routes.slice(0, -1)) await visible(route, recipeBody.title, false)
        for (const instance of [0, 1]) await api(`/recipes/${recipe.slug}`, { instance, expected: 404 })
        const current = await owned()
        for (const item of current.images) await missing(`/api/public-media/${item.id}/medium`)
      }
      await mutate('PATCH', '/unpublish')
      await assertHidden()
      await mutate('PATCH', '/publish')
      await visible(`/recipes/${recipe.slug}`, recipeBody.title)
      await mutate('PATCH', '/archive')
      await assertHidden()
      await mutate('PATCH', '/unarchive')
      await assertHidden()
      await mutate('PATCH', '/publish')
      await visible(`/recipes/${recipe.slug}`, recipeBody.title)
      await mutate('DELETE', '', undefined, 204)
      await missing(`/recipes/${recipe.slug}`)
      for (const route of routes.slice(0, -1)) await visible(route, recipeBody.title, false)
      for (const instance of [0, 1]) await api(`/recipes/${recipe.slug}`, { instance, expected: 404 })
    },
  )
  const log = await readFile(path.join(artifacts, 'api-a.log'), 'utf8')
  assert.ok(log.includes('Sitemap generated'))
  process.stdout.write(`${formatCliOutput(`Real stack evidence: ${artifacts}`)}\n`)
} catch (error) {
  await writeFile(path.join(artifacts, 'failure.txt'), `${error.stack}\n${error.cause?.stack ?? ''}`)
  throw error
} finally {
  for (const owned of processes.reverse()) await stop(owned)
  for (const id of containers.reverse()) {
    const info = JSON.parse(await command('docker', ['inspect', id]))[0]
    assert.equal(info.Config.Labels['culinary.g03.run'], runId)
    await command('docker', ['rm', '--force', '--volumes', id])
  }
  if (ownsNetwork) await command('docker', ['network', 'rm', network])
}
