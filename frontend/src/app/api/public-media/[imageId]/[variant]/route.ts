import { PUBLIC_CONTENT_TAG } from '@/lib/api/public-content-server'

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
      next: { revalidate: 300, tags: [PUBLIC_CONTENT_TAG] },
    },
  )

  if (!response.ok || !response.body) return new Response(null, { status: response.status })

  return new Response(response.body, {
    headers: {
      'Cache-Control': 'public, max-age=300',
      'Content-Type': response.headers.get('content-type') ?? 'application/octet-stream',
    },
  })
}
