const unavailableResponse = () =>
  new Response('Sitemap is not available yet.', {
    status: 503,
    headers: { 'Content-Type': 'text/plain; charset=utf-8', 'Retry-After': '60' },
  })

export const dynamic = 'force-dynamic'

export async function GET() {
  const apiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'

  try {
    const response = await fetch(`${apiBaseUrl}/sitemap.xml`, { cache: 'no-store' })
    if (!response.ok || !response.body) return unavailableResponse()

    return new Response(response.body, {
      headers: {
        'Content-Type': 'application/xml; charset=utf-8',
        'Cache-Control': 'public, max-age=60, stale-if-error=86400',
      },
    })
  } catch {
    return unavailableResponse()
  }
}
