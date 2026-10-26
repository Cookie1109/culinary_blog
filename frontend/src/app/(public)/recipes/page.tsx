import type { Metadata } from 'next'
import { RecipesList } from '@/features/culinary/pages/RecipesList'
import { getPublicCategories, searchPublishedRecipesOnServer } from '@/lib/api/public-content-server'
import { createPageMetadata } from '@/lib/seo'
import { normalizeRecipeSearch, recipeSearchValidation } from '@/lib/recipe-search'
import type { Category, PageEnvelope, Recipe } from '@/lib/api/content-client'

export const dynamic = 'force-dynamic'

type SearchParams = Promise<Record<string, string | string[] | undefined>>

function first(value: string | string[] | undefined) {
  return Array.isArray(value) ? (value[0] ?? '') : (value ?? '')
}

function positiveInteger(value: string, fallback: number) {
  const parsed = Number(value)
  return Number.isSafeInteger(parsed) && parsed > 0 ? parsed : fallback
}

export async function generateMetadata({ searchParams }: { searchParams: SearchParams }): Promise<Metadata> {
  const query = await searchParams
  const page = positiveInteger(first(query.page), 1)
  const hasFilters = ['q', 'category', 'difficulty', 'maxTime', 'sort'].some((key) =>
    Boolean(first(query[key])),
  )
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
  const q = normalizeRecipeSearch(first(query.q))
  const requestedSort = first(query.sort)
  const sort = !q && requestedSort === 'relevance' ? 'newest' : requestedSort || (q ? 'relevance' : 'newest')
  const page = positiveInteger(first(query.page), 1)
  const initialSearchError = recipeSearchValidation(q)
  let result: PageEnvelope<Recipe> = {
    data: [],
    meta: { page, pageSize: 12, total: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
  }
  let categories: Category[] = []
  let loadError = false
  try {
    ;[result, categories] = await Promise.all([
      initialSearchError
        ? Promise.resolve(result)
        : searchPublishedRecipesOnServer({
            q,
            category,
            difficulty,
            maxTime,
            sort,
            page,
            pageSize: 12,
          }),
      getPublicCategories(1800),
    ])
  } catch {
    console.error('PUBLIC_RECIPE_SEARCH_FAILED')
    loadError = true
  }

  return (
    <RecipesList
      result={result}
      categories={categories}
      initialSearchError={initialSearchError}
      loadError={loadError}
    />
  )
}
