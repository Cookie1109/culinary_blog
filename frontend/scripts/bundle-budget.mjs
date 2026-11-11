import { gzipSync } from 'node:zlib'

export function firstLoadJavaScript(pages, readChunk) {
  const routes = Object.entries(pages).filter(([route]) => route.endsWith('/page'))
  if (routes.length === 0) throw new Error('No page routes in the production build manifest')
  return routes.map(([route, chunks]) => {
    const inherited = new Set([...chunks, ...(pages['/global-error'] ?? [])])
    const segments = route.split('/').slice(0, -1)
    for (let length = 1; length <= segments.length; length += 1) {
      for (const boundary of ['layout', 'error', 'not-found', 'loading']) {
        for (const chunk of pages[`${segments.slice(0, length).join('/')}/${boundary}`] ?? []) {
          inherited.add(chunk)
        }
      }
    }
    let rawBytes = 0
    let gzipBytes = 0
    for (const chunk of inherited) {
      if (chunk.endsWith('.js')) {
        const content = readChunk(chunk)
        rawBytes += content.length
        gzipBytes += gzipSync(content).length
      }
    }
    return { route, rawBytes, gzipBytes }
  })
}
