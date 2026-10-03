import { notFound } from 'next/navigation'
import { CategoryDetail } from '@/features/culinary/pages/CategoryDetail'
import { getPublishedCategory } from '@/lib/api/public-content-server'

export const revalidate = 600

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
  const result = await getPublishedCategory(slug, page, revalidate)
  if (!result) notFound()

  return <CategoryDetail result={result} />
}
