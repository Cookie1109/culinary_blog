export const dynamic = 'force-dynamic'
export const runtime = 'nodejs'

export async function GET(request: Request) {
  const abort = new AbortController()
  const disconnect = () => abort.abort()
  request.signal.addEventListener('abort', disconnect, { once: true })
  const api = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
  let upstream: Response
  try {
    upstream = await fetch(`${api}/recipe-events`, {
      cache: 'no-store',
      headers: { Accept: 'text/event-stream' },
      signal: abort.signal,
    })
  } catch {
    request.signal.removeEventListener('abort', disconnect)
    return new Response(null, { status: 503 })
  }
  if (!upstream.ok || !upstream.body) {
    abort.abort()
    request.signal.removeEventListener('abort', disconnect)
    return new Response(null, { status: 503 })
  }

  const reader = upstream.body.getReader()
  const body = new ReadableStream({
    async start(controller) {
      try {
        while (!abort.signal.aborted) {
          // eslint-disable-next-line no-await-in-loop -- SSE frames must be consumed in order.
          const { value, done } = await reader.read()
          if (done) break
          controller.enqueue(value)
        }
        controller.close()
      } catch (error) {
        if (!abort.signal.aborted) controller.error(error)
      } finally {
        abort.abort()
        request.signal.removeEventListener('abort', disconnect)
        await reader.cancel().catch(() => {})
      }
    },
    cancel() {
      abort.abort()
    },
  })
  return new Response(body, {
    headers: {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache, no-transform',
      'X-Accel-Buffering': 'no',
    },
  })
}
