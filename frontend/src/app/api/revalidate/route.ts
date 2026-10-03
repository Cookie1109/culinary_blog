import { timingSafeEqual } from 'node:crypto'
import { revalidatePath, revalidateTag } from 'next/cache'
import { PUBLIC_CONTENT_TAG } from '@/lib/api/public-content-server'

const paths = ['/', '/recipes', '/categories', '/sitemap.xml'] as const

function secretsMatch(actual: string | null, expected: string) {
  if (!actual) return false
  const actualBytes = Buffer.from(actual)
  const expectedBytes = Buffer.from(expected)
  return actualBytes.length === expectedBytes.length && timingSafeEqual(actualBytes, expectedBytes)
}

export async function POST(request: Request) {
  const secret = process.env.REVALIDATION_SECRET
  if (!secret) {
    return Response.json({ error: 'Revalidation is not configured.' }, { status: 503 })
  }

  if (!secretsMatch(request.headers.get('x-revalidation-secret'), secret)) {
    return Response.json({ error: 'Unauthorized.' }, { status: 401 })
  }

  for (const path of paths) revalidatePath(path)
  revalidatePath('/recipes/[slug]', 'page')
  revalidatePath('/categories/[slug]', 'page')
  revalidateTag(PUBLIC_CONTENT_TAG)

  return Response.json({ revalidated: true })
}
