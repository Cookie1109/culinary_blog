export const dynamic = 'force-dynamic'

const allowedVariants = new Set(['original', 'medium', 'thumbnail'])

export async function GET(
  _request: Request,
  { params }: { params: Promise<{ imageId: string; variant: string }> },
) {
  const { imageId, variant } = await params
  if (!allowedVariants.has(variant)) return new Response(null, { status: 404 })

  const apiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
  const response = await fetch(
    `${apiBaseUrl}/media/${encodeURIComponent(imageId)}/${encodeURIComponent(variant)}`,
    {
      cache: 'no-store',
    },
  )

  if (!response.ok || !response.body) return new Response(null, { status: response.status })

  return new Response(response.body, {
    headers: {
      'Cache-Control': 'no-store',
      'Content-Type': response.headers.get('content-type') ?? 'application/octet-stream',
    },
  })
}
