import { createServer } from 'node:http'
import { Readable } from 'node:stream'

const recipe = /^\/recipes\/[a-z0-9]+(?:-[a-z0-9]+)*\/?$/
const media = /^\/api\/public-media\/[a-f0-9-]{36}\/(original|medium|thumbnail)$/i
const publicApi = /^\/api\/v1\/(?:recipes|categories)(?:\/[a-z0-9]+(?:-[a-z0-9]+)*)?\/?$/
const asset =
  /^\/(?:_next\/static|images)\/[a-z0-9_./()[\]-]+\.(?:js|css|woff2?|svg|png|jpe?g|webp|avif|ico)$/i

function allowed(path) {
  return (
    path.length <= 2048 &&
    (recipe.test(path) ||
      publicApi.test(path) ||
      media.test(path) ||
      asset.test(path) ||
      ['/favicon.ico', '/icon.png', '/apple-icon.png', '/opengraph-image'].includes(path))
  )
}

// Fixed loopback upstreams. No auth/write/admin routes or user credentials cross this boundary.
export function createPreviewGateway({ upstream, apiUpstream = () => undefined, publicOrigin }) {
  return createServer(async (request, response) => {
    response.setHeader('Cache-Control', 'no-store')
    if (!['GET', 'HEAD'].includes(request.method)) {
      response.writeHead(405, { Allow: 'GET, HEAD' }).end()
      return
    }
    let url
    try {
      url = new URL(request.url, 'http://127.0.0.1')
      if (/%(?:2f|5c|2e)/i.test(url.pathname)) throw new Error('Encoded path separator')
      url.pathname = decodeURIComponent(url.pathname)
    } catch {
      response.writeHead(400).end()
      return
    }
    if (url.pathname === '/robots.txt') {
      response.setHeader('Content-Type', 'text/plain; charset=utf-8')
      response.end(
        'User-agent: *\nDisallow: /\nAllow: /recipes/\nAllow: /api/v1/recipes\nAllow: /api/v1/categories\nAllow: /api/public-media/\nAllow: /_next/static/\nAllow: /images/\nAllow: /opengraph-image\n',
      )
      return
    }
    if (!allowed(url.pathname)) {
      response.writeHead(404).end()
      return
    }
    const target = publicApi.test(url.pathname) ? apiUpstream() : upstream()
    if (!target) {
      response.writeHead(503, { 'Retry-After': '3' }).end()
      return
    }
    try {
      const destination = new URL(url.pathname + url.search, target)
      if (destination.origin !== new URL(target).origin) throw new Error('Invalid upstream')
      const result = await fetch(destination, {
        method: request.method,
        redirect: 'manual',
        signal: AbortSignal.timeout(30000),
        headers: { Accept: request.headers.accept ?? '*/*' },
      })
      const location = result.headers.get('location')
      if (location) {
        const redirect = new URL(location, target)
        const origin = publicOrigin()
        if (![new URL(target).origin, origin].includes(redirect.origin) || !recipe.test(redirect.pathname)) {
          await result.body?.cancel()
          throw new Error('Blocked redirect')
        }
        response.setHeader(
          'Location',
          origin ? new URL(redirect.pathname + redirect.search, origin).href : redirect.pathname,
        )
      }
      for (const name of [
        'content-type',
        'etag',
        'last-modified',
        'content-security-policy',
        'x-content-type-options',
        'referrer-policy',
      ]) {
        const value = result.headers.get(name)
        if (value) response.setHeader(name, value)
      }
      response.writeHead(result.status)
      if (request.method === 'HEAD' || !result.body) {
        await result.body?.cancel()
        response.end()
      } else {
        const stream = Readable.fromWeb(result.body)
        stream.on('error', () => response.destroy())
        response.on('close', () => stream.destroy())
        stream.pipe(response)
      }
    } catch {
      if (!response.headersSent) response.writeHead(502)
      response.end()
    }
  })
}
