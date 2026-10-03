import { Suspense } from 'react'
import { Search } from '@/features/culinary/pages/Search'
import { Skeleton } from '@/components/ui/skeleton'
import { getPublicCategories, searchPublishedRecipesOnServer } from '@/lib/api/public-content-server'

export const dynamic = 'force-dynamic'

type SearchParams = Promise<Record<string, string | string[] | undefined>>

function first(value: string | string[] | undefined) {
  return Array.isArray(value) ? (value[0] ?? '') : (value ?? '')
}

function nonNegativeInteger(value: string, fallback: number) {
  const parsed = Number.parseInt(value, 10)
  return Number.isFinite(parsed) && parsed >= 0 ? parsed : fallback
}

async function SearchResults({ searchParams }: { searchParams: SearchParams }) {
  const query = await searchParams
  const page = Math.max(1, nonNegativeInteger(first(query.page), 1))
  const [result, categories] = await Promise.all([
    searchPublishedRecipesOnServer({
      q: first(query.q).trim(),
      category: first(query.category),
      difficulty: first(query.difficulty),
      maxTime: nonNegativeInteger(first(query.maxTime), 0),
      sort: first(query.sort) || 'relevance',
      page,
      pageSize: 9,
    }),
    getPublicCategories(1800),
  ])

  return <Search result={result} categories={categories} />
}

export default function SearchPage({ searchParams }: { searchParams: SearchParams }) {
  return (
    <Suspense fallback={<Skeleton className="mx-auto my-16 h-96 max-w-7xl" />}>
      <SearchResults searchParams={searchParams} />
    </Suspense>
  )
}
