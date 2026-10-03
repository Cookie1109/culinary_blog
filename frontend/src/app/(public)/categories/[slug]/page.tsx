import type { Metadata } from 'next'
import { notFound } from 'next/navigation'
import { CategoryDetail } from '@/features/culinary/pages/CategoryDetail'
import { getPublishedCategory } from '@/lib/api/public-content-server'
import { createPageMetadata, summarize } from '@/lib/seo'

export const revalidate = 600

type CategoryPageProps = {
  params: Promise<{ slug: string }>
  searchParams: Promise<{ page?: string | string[] }>
}

async function routeState({ params, searchParams }: CategoryPageProps) {
  const { slug } = await params
  const rawPage = (await searchParams).page
  const requestedPage = Number.parseInt(Array.isArray(rawPage) ? (rawPage[0] ?? '1') : (rawPage ?? '1'), 10)
  const page = Number.isFinite(requestedPage) && requestedPage > 0 ? requestedPage : 1
  return { slug, page }
}

export async function generateMetadata(props: CategoryPageProps): Promise<Metadata> {
  const { slug, page } = await routeState(props)
  const result = await getPublishedCategory(slug, page, revalidate)
  if (!result) {
    return { title: 'Không tìm thấy danh mục', robots: { index: false, follow: false } }
  }

  const { category } = result.data
  const suffix = page > 1 ? ` – Trang ${page}` : ''
  return createPageMetadata({
    title: `${category.name}${suffix}`,
    description: summarize(category.description ?? `Khám phá công thức thuộc danh mục ${category.name}.`),
    path:
      page > 1
        ? `/categories/${encodeURIComponent(category.slug)}?page=${page}`
        : `/categories/${encodeURIComponent(category.slug)}`,
    image: category.imageUrl,
    imageAlt: category.name,
  })
}

export default async function CategoryPage(props: CategoryPageProps) {
  const { slug, page } = await routeState(props)
  const result = await getPublishedCategory(slug, page, revalidate)
  if (!result) notFound()

  return <CategoryDetail result={result} />
}
