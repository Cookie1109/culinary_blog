import 'server-only'

import type { Category, CategoryDetailEnvelope, PageEnvelope, Recipe } from '@/lib/api/content-client'

export const PUBLIC_CONTENT_TAG = 'public-content'

const apiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'

interface DataEnvelope<T> {
  data: T
}

interface PublicRequestOptions {
  revalidate?: number
  noStore?: boolean
}

async function publicRequest<T>(path: string, options: PublicRequestOptions): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...(options.noStore
      ? { cache: 'no-store' as const }
      : {
          next: {
            revalidate: options.revalidate,
            tags: [PUBLIC_CONTENT_TAG],
          },
        }),
    headers: { Accept: 'application/json' },
  })

  if (!response.ok) {
    throw new Error(`Public content API returned ${response.status}.`)
  }

  return (await response.json()) as T
}

export async function getPublicCategories(revalidate: number) {
  return (await publicRequest<DataEnvelope<Category[]>>('/categories', { revalidate })).data
}

export function getPublishedRecipes(page: number, pageSize: number, revalidate: number) {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return publicRequest<PageEnvelope<Recipe>>(`/recipes?${query}`, { revalidate })
}

export async function getPublishedRecipe(slug: string, revalidate: number) {
  const response = await fetch(`${apiBaseUrl}/recipes/${encodeURIComponent(slug)}`, {
    next: { revalidate, tags: [PUBLIC_CONTENT_TAG] },
    headers: { Accept: 'application/json' },
  })

  if (response.status === 404) return null
  if (!response.ok) throw new Error(`Recipe API returned ${response.status}.`)
  return ((await response.json()) as DataEnvelope<Recipe>).data
}

export async function getPublishedCategory(slug: string, page: number, revalidate: number) {
  const query = new URLSearchParams({ page: String(page), pageSize: '12' })
  const response = await fetch(`${apiBaseUrl}/categories/${encodeURIComponent(slug)}?${query}`, {
    next: { revalidate, tags: [PUBLIC_CONTENT_TAG] },
    headers: { Accept: 'application/json' },
  })

  if (response.status === 404) return null
  if (!response.ok) throw new Error(`Category API returned ${response.status}.`)
  return (await response.json()) as CategoryDetailEnvelope
}

export interface PublicRecipeFilters {
  q: string
  category: string
  difficulty: string
  maxTime: number
  sort: string
  page: number
  pageSize: number
  categoryId?: string
  maxCookTime?: number
  minServings?: number
  minPrepTime?: number
  maxPrepTime?: number
  sortBy?: string
  sortOrder?: string
}

export function searchPublishedRecipesOnServer(filters: PublicRecipeFilters) {
  const query = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(filters.pageSize),
  })
  if (filters.sort) query.set('sort', !filters.q && filters.sort === 'relevance' ? 'newest' : filters.sort)
  if (filters.sortBy !== undefined) query.set('sortBy', filters.sortBy)
  if (filters.sortOrder !== undefined) query.set('sortOrder', filters.sortOrder)
  if (filters.q) query.set('q', filters.q)
  if (filters.category) query.set('category', filters.category)
  if (filters.difficulty) query.set('difficulty', filters.difficulty)
  if (filters.maxTime > 0) query.set('maxTime', String(filters.maxTime))
  if (filters.categoryId) query.set('categoryId', filters.categoryId)
  for (const key of ['maxCookTime', 'minServings', 'minPrepTime', 'maxPrepTime'] as const) {
    if (filters[key] !== undefined) query.set(key, String(filters[key]))
  }

  const path = filters.q ? '/recipes/search' : '/recipes'
  return publicRequest<PageEnvelope<Recipe>>(`${path}?${query}`, { noStore: true })
}
