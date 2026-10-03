import { notFound } from 'next/navigation'
import { CategoryDetail } from '@/features/culinary/pages/CategoryDetail'
import type { CategoryDetailEnvelope } from '@/lib/api/content-client'

export const dynamic = 'force-dynamic'

async function loadCategory(slug: string, page: number) {
  const baseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
  const response = await fetch(`${baseUrl}/categories/${encodeURIComponent(slug)}?page=${page}&pageSize=12`, {
    cache: 'no-store',
  })
  if (response.status === 404) notFound()
  if (!response.ok) throw new Error(`Category API returned ${response.status}.`)
  return (await response.json()) as CategoryDetailEnvelope
}

export default async function CategoryPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>
  searchParams: Promise<{ page?: string | string[] }>
}) {
  const { slug } = await params
  const rawPage = (await searchParams).page
  const requestedPage = Number.parseInt(Array.isArray(rawPage) ? (rawPage[0] ?? '1') : (rawPage ?? '1'), 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1

  return <CategoryDetail result={await loadCategory(slug, page)} />
}
