import { format as formatCliOutput } from 'node:util'
import assert from 'node:assert/strict'
import { spawn } from 'node:child_process'
import { randomBytes } from 'node:crypto'
import { createWriteStream } from 'node:fs'
import { cp, mkdir, readFile, writeFile } from 'node:fs/promises'
import { createRequire } from 'node:module'
import { createServer } from 'node:net'
import http from 'node:http'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import puppeteer from 'puppeteer-core'

const require = createRequire(import.meta.url)
const frontend = fileURLToPath(new URL('../', import.meta.url))
const repository = path.dirname(frontend.replace(/[\\/]$/, ''))
const artifacts = path.join(repository, 'tmp/package-G02/profile-browser')
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

let browser
let proxy
let page
try {
  if (!process.argv.includes('--skip-build')) {
    const build = start('build', [require.resolve('next/dist/bin/next'), 'build'], {
      INTERNAL_API_BASE_URL: 'http://127.0.0.1:1/api/v1',
    })
    assert.equal(await build.exit, 0)
  }
  const simulationPort = await freePort()
  const webPort = await freePort()
  const edgePort = await freePort()
  const api = `http://127.0.0.1:${simulationPort}/culinary`
  const origin = `http://127.0.0.1:${edgePort}`
  const postmanEntry = path.join(path.dirname(require.resolve('postman-cli')), 'bin/postman.js')
  const simulation = start('simulation', [
    postmanEntry,
    'simulation',
    'run',
    path.join(repository, 'postman/simulations/profile-browser.sim.yaml'),
    '--port',
    String(simulationPort),
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
  const fixture = await fetch(`${api}/api/v1/auth/me`)
  assert.equal(fixture.status, 200, await fixture.text())
  const standalone = path.join(artifacts, `standalone-${randomBytes(4).toString('hex')}`)
  await cp(path.join(frontend, '.next/standalone'), standalone, { recursive: true })
  await cp(path.join(frontend, '.next/static'), path.join(standalone, '.next/static'), { recursive: true })
  const app = start('next', [path.join(standalone, 'server.js')], {
    HOSTNAME: '127.0.0.1',
    PORT: String(webPort),
    NODE_ENV: 'production',
    INTERNAL_API_BASE_URL: `${api}/api/v1`,
    SITE_URL: origin,
    AUTH_SECRET: randomBytes(32).toString('hex'),
    AUTH_TRUST_HOST: 'true',
    AUTH_URL: origin,
    NEXTAUTH_URL: origin,
    AUTH_GOOGLE_ID: '',
    AUTH_GOOGLE_SECRET: '',
  })
  await waitUntil(async () => (await fetch(`http://127.0.0.1:${webPort}/login`)).ok, app)
  // Local routing only: API calls reach the simulation and pages reach the production build.
  proxy = http.createServer((request, response) => {
    const target = request.url.startsWith('/api/v1/')
      ? new URL(api + request.url)
      : new URL(`http://127.0.0.1:${webPort}${request.url}`)
    const upstream = http.request(target, { method: request.method, headers: request.headers }, (result) => {
      response.writeHead(result.statusCode, result.headers)
      result.pipe(response)
    })
    upstream.on('error', () => {
      response.writeHead(502)
      response.end()
    })
    request.pipe(upstream)
  })
  await new Promise((resolve) => {
    proxy.listen(edgePort, '127.0.0.1', resolve)
  })
  const executablePath = process.env.CHROME_PATH ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe'
  browser = await puppeteer.launch({
    executablePath,
    headless: true,
    userDataDir: path.join(artifacts, 'chrome-profile'),
    args: ['--no-first-run', '--no-default-browser-check'],
  })
  page = await browser.newPage()
  await page.setViewport({ width: 1280, height: 960 })
  const avatar = 'https://avatars.example.test/profile.png'
  let avatarRequests = 0
  await page.setRequestInterception(true)
  page.on('request', (request) => {
    if (request.url() === avatar) {
      avatarRequests += 1
      request.respond({
        status: 200,
        contentType: 'image/png',
        body: Buffer.from(
          'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
          'base64',
        ),
      })
    } else request.continue()
  })
  const login = await page.goto(`${origin}/login?returnTo=/profile`, { waitUntil: 'domcontentloaded' })
  const csp = login.headers()['content-security-policy']
  assert.match(csp, /img-src[^;]*https:/)
  // Both Nginx policies must permit the same external avatar schemes.
  for (const config of ['nginx.conf', 'nginx.production.conf']) {
    assert.match(
      await readFile(path.join(repository, 'infra/nginx', config), 'utf8'),
      /img-src 'self' data: blob: https: http:/,
    )
  }
  await page.type('#email', 'profile@example.test')
  await page.type('#password', 'Synthetic#Password1')
  await page.click('button[type="submit"]')
  await page.waitForFunction(
    () =>
      window.location.pathname === '/profile' && document.body.textContent.includes('Profile Browser Author'),
  )
  async function beginEditing() {
    await page.locator('button::-p-text(Cập nhật)').click()
  }
  async function fill(selector, value) {
    if (value) {
      await page.locator(selector).fill(value)
      return
    }
    await page.$eval(selector, (input) => {
      input.focus()
      input.select()
    })
    await page.keyboard.press('Backspace')
  }
  await beginEditing()
  await fill('input[aria-label="Họ và tên"]', 'Browser Updated Author')
  await fill('textarea', 'Biography saved in a real browser')
  // eslint-disable-next-line no-script-url -- Rejected form input, never a navigation target.
  await fill('#profile-avatar', 'javascript:alert(1)')
  await page.click('form button[type="submit"]')
  await page.waitForSelector('#profile-avatar-error')
  const unchanged = await page.evaluate(
    async () => (await (await fetch('/api/auth/session')).json()).backendUser,
  )
  assert.equal(unchanged.displayName, 'Profile Browser Author')
  await fill('#profile-avatar', avatar)
  await page.click('form button[type="submit"]')
  await page.waitForFunction(
    (url) =>
      [...document.images].some((image) => image.src === url && image.complete && image.naturalWidth > 0),
    {},
    avatar,
  )
  const updated = await page.evaluate(async () => (await fetch('/api/auth/session')).json())
  assert.equal(updated.backendUser.displayName, 'Browser Updated Author')
  assert.equal(updated.backendUser.bio, 'Biography saved in a real browser')
  assert.equal(updated.backendUser.avatarUrl, avatar)
  assert.equal(updated.refreshToken, undefined)
  assert.equal(updated.backendUser.refreshToken, undefined)
  assert.equal(await page.$('input[aria-label="Họ và tên"]'), null)
  await page.screenshot({ path: path.join(artifacts, 'avatar-saved.png'), fullPage: true })
  await beginEditing()
  await fill('#profile-avatar', '')
  assert.equal(await page.$eval('#profile-avatar', (input) => input.value), '')
  await page.click('form button[type="submit"]')
  await page.waitForFunction((url) => ![...document.images].some((image) => image.src === url), {}, avatar)
  const cleared = await page.evaluate(
    async () => (await (await fetch('/api/auth/session')).json()).backendUser,
  )
  assert.equal(cleared.avatarUrl, null)
  assert.equal(cleared.displayName, updated.backendUser.displayName)
  assert.equal(cleared.bio, updated.backendUser.bio)
  assert.ok(avatarRequests > 0)
  const evidence = {
    browser: await browser.version(),
    backend: 'Postman HTTP simulation, synthetic profile contract',
    origin,
    csp,
    avatarRequests,
    cases: [
      'local-login',
      'invalid-avatar-keeps-profile',
      'external-avatar-loads',
      'name-bio-session-sync',
      'public-session-no-refresh-token',
      'clear-avatar-keeps-name-bio',
    ],
    result: 'pass',
  }
  await writeFile(path.join(artifacts, 'result.json'), JSON.stringify(evidence, null, 2))
  process.stdout.write(`${formatCliOutput(JSON.stringify(evidence))}\n`)
} catch (error) {
  if (page && !page.isClosed()) {
    await page.screenshot({ path: path.join(artifacts, 'failure.png'), fullPage: true })
    await writeFile(path.join(artifacts, 'failure.html'), await page.content())
  }
  throw error
} finally {
  if (browser) await browser.close()
  if (proxy)
    await new Promise((resolve) => {
      proxy.close(resolve)
    })
  for (const owned of children.reverse()) await stop(owned)
}
