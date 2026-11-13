import { parseWebVital } from '@/lib/web-vitals'

export async function POST(request: Request) {
  const reply = (status: number) => new Response(null, { status, headers: { 'Cache-Control': 'no-store' } })
  if (process.env.WEB_VITALS_ENABLED !== 'true') return reply(404)
  const build =
    process.env.WEB_VITALS_BUILD_ID ?? process.env.VERCEL_GIT_COMMIT_SHA ?? process.env.RELEASE_COMMIT_SHA
  if (!build || !/^[a-zA-Z0-9._-]{1,128}$/.test(build)) return reply(503)
  if (request.headers.get('origin') !== new URL(process.env.SITE_URL ?? request.url).origin) return reply(403)
  if (!['application/json', 'text/plain'].includes(request.headers.get('content-type')?.split(';')[0] ?? ''))
    return reply(415)
  if (Number(request.headers.get('content-length')) > 2048) return reply(413)
  const reader = request.body?.getReader()
  if (!reader) return reply(400)
  const chunks: Uint8Array[] = []
  let size = 0
  for (;;) {
    // eslint-disable-next-line no-await-in-loop -- Read sequentially to enforce the byte limit before buffering.
    const { value, done } = await reader.read()
    if (done) break
    size += value.byteLength
    if (size > 2048) {
      // eslint-disable-next-line no-await-in-loop -- Cancel this reader before returning the oversized response.
      await reader.cancel()
      return reply(413)
    }
    chunks.push(value)
  }
  let payload: unknown
  try {
    payload = JSON.parse(Buffer.concat(chunks).toString('utf8'))
  } catch {
    return reply(400)
  }
  const sample = parseWebVital(payload)
  if (!sample) return reply(400)
  process.stdout.write(
    `${JSON.stringify({ event: 'web-vital', build, receivedAt: new Date().toISOString(), ...sample })}\n`,
  )
  return reply(204)
}
