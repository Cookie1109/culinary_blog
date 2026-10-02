import { timingSafeEqual } from 'node:crypto'
import { revalidatePath } from 'next/cache'

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

  return Response.json({ revalidated: true })
}
