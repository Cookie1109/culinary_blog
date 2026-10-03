import type { Metadata } from 'next'
import { RecipesList } from '@/features/culinary/pages/RecipesList'
import { getPublicCategories, searchPublishedRecipesOnServer } from '@/lib/api/public-content-server'
import { createPageMetadata } from '@/lib/seo'

export const dynamic = 'force-dynamic'

type SearchParams = Promise<Record<string, string | string[] | undefined>>

function first(value: string | string[] | undefined) {
  return Array.isArray(value) ? (value[0] ?? '') : (value ?? '')
}

function positiveInteger(value: string, fallback: number) {
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

export async function generateMetadata({ searchParams }: { searchParams: SearchParams }): Promise<Metadata> {
  const query = await searchParams
  const page = positiveInteger(first(query.page), 1)
  const hasFilters = ['category', 'difficulty', 'maxTime', 'sort'].some((key) => Boolean(first(query[key])))
  const suffix = page > 1 ? ` – Trang ${page}` : ''

  return createPageMetadata({
    title: `Công thức nấu ăn${suffix}`,
    description: 'Khám phá công thức theo danh mục, độ khó và thời gian chế biến.',
    path: page > 1 ? `/recipes?page=${page}` : '/recipes',
    index: !hasFilters,
    follow: true,
  })
}

export default async function RecipesPage({ searchParams }: { searchParams: SearchParams }) {
  const query = await searchParams
  const category = first(query.category)
  const difficulty = first(query.difficulty)
  const maxTime = positiveInteger(first(query.maxTime), 0)
  const sort = first(query.sort) || 'newest'
  const page = positiveInteger(first(query.page), 1)
  const [result, categories] = await Promise.all([
    searchPublishedRecipesOnServer({
      q: '',
      category,
      difficulty,
      maxTime,
      sort,
      page,
      pageSize: 12,
    }),
    getPublicCategories(1800),
  ])

  return <RecipesList result={result} categories={categories} />
}
