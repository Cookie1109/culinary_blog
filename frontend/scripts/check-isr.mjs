import { format as formatCliOutput } from 'node:util'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { createWriteStream } from 'node:fs'
import { cp, mkdir, readFile, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { createServer } from 'node:net'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const require = createRequire(import.meta.url)
const frontend = fileURLToPath(new URL('../', import.meta.url))
const repository = path.dirname(frontend.replace(/[\\/]$/, ''))
const artifacts = path.join(repository, process.env.ISR_ARTIFACTS ?? 'tmp/srs-audit/isr')
await mkdir(artifacts, { recursive: true })
const children = []

function start(name, args, environment = {}) {
  const output = createWriteStream(path.join(artifacts, `${name}.log`))
  const child = spawn(process.execPath, args, {
    cwd: frontend,
    env: { ...process.env, ...environment },
    detached: process.platform !== 'win32',
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })
  child.stdout.pipe(output, { end: false })
  child.stderr.pipe(output, { end: false })
  const owned = { child, output, name, error: null }
  child.on('error', (error) => {
    owned.error = error
  })
  owned.exit = new Promise((resolve) => {
    child.on('close', (code) => {
      output.end()
      resolve(code)
    })
  })
  children.push(owned)
  return owned
}

async function waitUntil(check, owned, timeout = 30_000) {
  const deadline = Date.now() + timeout
  while (Date.now() < deadline) {
    if (owned.error || owned.child.exitCode !== null || owned.child.signalCode)
      throw new Error(`${owned.name} stopped before readiness; inspect ${artifacts}`, { cause: owned.error })
    try {
      if (await check()) return
    } catch {
      /* The listener may still be starting. */
    }
    await new Promise((resolve) => {
      setTimeout(resolve, 100)
    })
  }
  throw new Error(`Timed out waiting for ${owned.name}; inspect ${artifacts}`)
}

async function freePort() {
  const server = createServer()
  await new Promise((resolve) => {
    server.listen(0, '127.0.0.1', resolve)
  })
  const { port } = server.address()
  await new Promise((resolve) => {
    server.close(resolve)
  })
  return port
}

async function stop(owned) {
  if (!owned.child.pid || owned.child.exitCode !== null || owned.child.signalCode) return
  if (process.platform === 'win32') {
    // Only stop this runner's process tree, including the Postman launcher child.
    const killer = spawn('taskkill', ['/PID', String(owned.child.pid), '/T', '/F'], {
      windowsHide: true,
      stdio: 'ignore',
    })
    await new Promise((resolve) => {
      killer.on('close', resolve)
    })
  } else {
    try {
      process.kill(-owned.child.pid, 'SIGINT')
    } catch {
      /* Already exited. */
    }
    await Promise.race([
      owned.exit,
      new Promise((resolve) => {
        setTimeout(resolve, 3000)
      }),
    ])
    if (owned.child.exitCode === null && !owned.child.signalCode) {
      try {
        process.kill(-owned.child.pid, 'SIGKILL')
      } catch {
        /* Already exited. */
      }
    }
  }
  await owned.exit
}

const next = require.resolve('next/dist/bin/next')
try {
  if (!process.argv.includes('--skip-build')) {
    const build = start('build', [next, 'build'], { INTERNAL_API_BASE_URL: 'http://127.0.0.1:1/api/v1' })
    assert.equal(await build.exit, 0, `Production build failed; inspect ${artifacts}`)
  }
  const mockPort = await freePort()
  let webPort = await freePort()
  while (webPort === mockPort) webPort = await freePort()
  const api = `http://127.0.0.1:${mockPort}/culinary`
  const web = `http://127.0.0.1:${webPort}`
  // NextURL normalizes loopback IPs to localhost; configure the public canonical origin explicitly.
  const canonicalWeb = `http://localhost:${webPort}`
  const postmanEntry = path.join(path.dirname(require.resolve('postman-cli')), 'bin/postman.js')
  const simulation = start('simulation', [
    postmanEntry,
    'simulation',
    'run',
    path.join(repository, 'postman/simulations/public-isr.sim.yaml'),
    '--port',
    String(mockPort),
    '--output',
    'ndjson',
    '--no-history',
  ])
  await waitUntil(
    async () =>
      /"(?:type|event)"\s*:\s*"listening"/.test(
        await readFile(path.join(artifacts, 'simulation.log'), 'utf8'),
      ),
    simulation,
  )
  async function setRevision(revision) {
    const response = await fetch(`${api}/__fixture/revision`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ revision }),
    })
    assert.equal(response.status, 200)
    assert.equal((await response.json()).revision, revision)
  }
  await setRevision(1)
  const secret = randomBytes(32).toString('hex')
  const standalone = path.join(artifacts, `standalone-${randomBytes(4).toString('hex')}`)
  await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
  await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), { recursive: true })
  const clockOffset = path.join(artifacts, 'clock-offset.txt')
  await writeFile(clockOffset, '0')
  const clockModule = path.join(artifacts, 'clock.cjs')
  await writeFile(
    clockModule,
    `const fs = require('node:fs'); const offset = () => Number(fs.readFileSync(${JSON.stringify(clockOffset)}, 'utf8')); const now = Date.now; Date.now = () => now() + offset(); const perf = globalThis.performance; globalThis.performance = new Proxy(perf, { get(target, key) { if (key === 'now') return () => perf.now() + offset(); const value = Reflect.get(target, key, target); return typeof value === 'function' ? value.bind(target) : value; } });`,
  )
  const authSecret = randomBytes(32).toString('hex')
  const app = start('next', [path.join(standalone, 'server.js')], {
    HOSTNAME: '127.0.0.1',
    PORT: String(webPort),
    NODE_ENV: 'production',
    INTERNAL_API_BASE_URL: `${api}/api/v1`,
    SITE_URL: canonicalWeb,
    AUTH_SECRET: authSecret,
    AUTH_TRUST_HOST: 'true',
    AUTH_URL: web,
    NEXTAUTH_URL: web,
    AUTH_GOOGLE_ID: '',
    AUTH_GOOGLE_SECRET: '',
    REVALIDATION_SECRET: secret,
    NODE_OPTIONS: `--require="${clockModule.replaceAll('\\', '/')}"`,
  })
  const invalidate = (value) =>
    fetch(`${web}/api/revalidate`, {
      method: 'POST',
      headers: value ? { 'x-revalidation-secret': value } : {},
    })
  await waitUntil(async () => (await invalidate(secret)).status === 200, app)
  const evidence = []
  const failures = []
  async function check(name, run) {
    try {
      await run()
      evidence.push({ check: name, result: 'pass' })
    } catch (error) {
      failures.push(new Error(name, { cause: error }))
      evidence.push({ check: name, result: 'fail', error: error.message })
    }
  }
  async function page(route, marker, expectedCache, cookie = false) {
    const response = await fetch(`${web}${route}`, {
      headers: cookie ? { Cookie: 'isr-test=anonymous' } : {},
    })
    const html = await response.text()
    assert.equal(response.status, 200, `GET ${route} failed; inspect ${artifacts}`)
    assert.ok(html.includes(marker), `GET ${route} did not render ${marker}`)
    if (expectedCache) assert.equal(response.headers.get('x-nextjs-cache'), expectedCache, route)
    evidence.push({
      route,
      status: response.status,
      cache: response.headers.get('x-nextjs-cache'),
      cacheControl: response.headers.get('cache-control'),
      marker,
    })
    return { response, html }
  }
  const cachedRoutes = [
    ['/', 'ISR recipe revision 1'],
    ['/categories', 'ISR category revision 1'],
    ['/recipes/isr-recipe', 'ISR recipe revision 1'],
    ['/categories/isr-category', 'ISR category revision 1'],
    ['/categories/isr-category?page=2', 'Trang 2'],
  ]
  await check('historical slug returns one HTTP 301 to current public slug', async () => {
    const response = await fetch(`${web}/recipes/isr-old-slug`, { redirect: 'manual' })
    assert.equal(response.status, 301)
    assert.equal(response.headers.get('location'), `${canonicalWeb}/recipes/isr-recipe`)
  })
  for (const [route, marker] of cachedRoutes) {
    await check(`ISR MISS/HIT ${route}`, async () => {
      const { response } = await page(route, marker, 'MISS')
      const categoryTtl = route.startsWith('/categories/') ? 600 : 3600
      const ttl = route.startsWith('/recipes/') ? 300 : categoryTtl
      assert.match(response.headers.get('cache-control'), new RegExp(`s-maxage=${ttl}\\b`))
      await page(route, marker, 'HIT', true)
    })
  }
  await check('production HTML includes bounded metadata and factual Recipe JSON-LD', async () => {
    const response = await fetch(`${web}/recipes/isr-recipe`)
    const html = await response.text()
    const title = html.match(/<title>([^<]+)<\/title>/)?.[1]
    const description = html.match(/<meta name="description" content="([^"]+)"/)?.[1]
    assert.ok(title?.endsWith(' | Culinary Blog'))
    assert.ok(title.length <= 60)
    assert.ok(description?.length >= 150 && description.length <= 160)
    const json = JSON.parse(
      html.match(/<script type="application\/ld\+json">([^<]+)<\/script>/)?.[1] ?? 'null',
    )
    assert.equal(json['@type'], 'Recipe')
    assert.equal(json.totalTime, 'PT30M')
    assert.equal(json.aggregateRating, undefined)
  })
  await setRevision(2)
  assert.equal((await invalidate()).status, 401)
  assert.equal((await invalidate('wrong-secret')).status, 401)
  await page('/', 'ISR recipe revision 1', 'HIT')
  await page('/categories', 'ISR category revision 1', 'HIT')
  assert.equal((await invalidate(secret)).status, 200)
  for (const [route, marker] of [
    ['/', 'ISR recipe revision 2'],
    ['/categories', 'ISR category revision 2'],
  ]) {
    await waitUntil(
      async () => {
        const response = await fetch(`${web}${route}`)
        return response.ok && (await response.text()).includes(marker)
      },
      app,
      10_000,
    )
    await page(route, marker, 'HIT')
  }
  for (const [route, marker] of cachedRoutes) {
    await check(`mutation callback refresh ${route}`, async () => {
      const expected = marker.replace('revision 1', 'revision 2')
      await waitUntil(async () => (await (await fetch(`${web}${route}`)).text()).includes(expected), app)
      await page(route, expected, 'HIT')
    })
  }
  for (const route of ['/recipes?page=2&sort=title', '/search?q=synthetic&page=2&sort=title']) {
    await check(`SSR query ${route}`, async () => {
      const { response } = await page(route, 'Không có công thức phù hợp.')
      assert.match(response.headers.get('cache-control'), /no-store/)
      assert.equal(response.headers.get('x-nextjs-cache'), null)
    })
  }
  for (const route of [
    '/unknown-isr-route',
    '/categories/isr-category/extra',
    '/categories/missing-category',
    '/recipes/missing-recipe',
    '/render-public/home/index',
    '/render-public',
    '/render-public/categories/index',
    '/render-public/category/isr-category/2',
    '/recipes/isr-recipe/extra',
    '/categories/missing-category?page=2',
  ]) {
    await check(`404 ${route}`, async () => assert.equal((await fetch(`${web}${route}`)).status, 404, route))
  }
  for (const [route, marker] of [
    ['/auth/login', 'Chào mừng trở lại'],
    ['/auth/register', 'Tạo tài khoản'],
  ]) {
    await check(`SRS auth route ${route}`, async () => {
      const response = await fetch(`${web}${route}`, { redirect: 'manual' })
      assert.equal(response.status, 200)
      const html = await response.text()
      assert.ok(html.includes(marker))
      assert.match(html, new RegExp(`<link rel="canonical" href="[^"]*${route}"`))
    })
  }
  for (const route of [
    '/dashboard',
    '/dashboard/categories',
    '/dashboard/recipes/new',
    '/dashboard/recipes/123/edit',
    '/profile',
    '/recipes/new',
    '/recipes/123/edit',
  ]) {
    await check(`anonymous private URL ${route}`, async () => {
      const response = await fetch(`${web}${route}`, { redirect: 'manual' })
      assert.equal(response.status, 307)
      assert.equal(new URL(response.headers.get('location')).pathname, '/auth/login')
      assert.match(response.headers.get('cache-control'), /no-store/)
    })
  }
  await check('private media rejects anonymous without caching', async () => {
    const response = await fetch(`${web}/api/private-media/22222222-2222-4222-8222-222222222222/original`)
    assert.equal(response.status, 401)
    assert.match(response.headers.get('cache-control'), /private, no-store/)
  })
  const { encode } = await import('next-auth/jwt')
  for (const role of ['Author', 'Admin']) {
    const token = await encode({
      secret: authSecret,
      salt: 'authjs.session-token',
      token: {
        sub: '33333333-3333-4333-8333-333333333333',
        backend: {
          accessToken: 'synthetic-http-session',
          expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
          user: {
            id: '33333333-3333-4333-8333-333333333333',
            email: 'isr@example.test',
            displayName: 'ISR Test Author',
            roles: [role],
            isActive: true,
          },
        },
      },
    })
    const headers = { Cookie: `authjs.session-token=${token}` }
    await check(`${role} private routes and login redirect`, async () => {
      const response = await fetch(`${web}/dashboard/categories`, { headers, redirect: 'manual' })
      assert.equal(response.status, role === 'Admin' ? 200 : 403)
      assert.match(response.headers.get('cache-control'), /no-store/)
      const login = await fetch(`${web}/auth/login`, { headers, redirect: 'manual' })
      assert.equal(login.status, 307)
      assert.equal(new URL(login.headers.get('location'), web).pathname, '/dashboard')
      assert.equal((await fetch(`${web}/`, { headers })).status, 200)
    })
  }
  // Advance only the test Next process's Date.now; retain production TTL configuration.
  // HTTP headers and HIT before / STALE after each TTL verify the Full Route Cache clock path.
  await setRevision(3)
  for (const [route, ttl, marker] of [
    ['/recipes/isr-recipe', 300, 'ISR recipe revision 3'],
    ['/categories/isr-category', 600, 'ISR category revision 3'],
    ['/', 3600, 'ISR recipe revision 3'],
    ['/categories', 3600, 'ISR category revision 3'],
  ]) {
    await check(`TTL expiry ${ttl}s ${route}`, async () => {
      await writeFile(clockOffset, String((ttl - 30) * 1000))
      assert.equal((await fetch(`${web}${route}`)).headers.get('x-nextjs-cache'), 'HIT')
      await writeFile(clockOffset, String((ttl + 10) * 1000))
      assert.equal((await fetch(`${web}${route}`)).headers.get('x-nextjs-cache'), 'STALE')
      await waitUntil(async () => (await (await fetch(`${web}${route}`)).text()).includes(marker), app)
      await page(route, marker, 'HIT')
    })
  }
  const log = await readFile(path.join(artifacts, 'simulation.log'), 'utf8')
  assert.match(log, /"(?:type|event)"\s*:\s*"request"/)
  assert.match(log, /api\/v1\/categories/)
  assert.match(log, /api\/v1\/recipes/)
  await writeFile(
    path.join(artifacts, 'evidence.json'),
    JSON.stringify(
      {
        runtime: process.version,
        checks: evidence,
        failures: failures.length,
        clock:
          'Next process Date.now and performance.now controlled; TTLs unchanged; no wall-clock duration claim',
        upstream: 'Local Postman mock from Culinary Blog OpenAPI; no user data',
      },
      null,
      2,
    ),
  )
  if (failures.length)
    throw new AggregateError(failures, `${failures.length} HTTP checks failed; inspect ${artifacts}`)
  process.stdout.write(
    `${formatCliOutput('Production HTTP verified: routes/auth/query/404/no-store, ISR MISS/HIT and TTL 300/600/3600 expiry with a controlled clock, protected callback refresh.')}\n`,
  )
  process.stdout.write(`${formatCliOutput(`Evidence: ${artifacts}`)}\n`)
} finally {
  for (const owned of children.reverse()) await stop(owned)
}
