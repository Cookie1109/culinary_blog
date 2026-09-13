import { authenticatedApiRequest } from '@/lib/api/auth-client'
import { apiRequest } from '@/lib/api/client'

export type RecipeStatus = 'draft' | 'published' | 'archived'
export type RecipeDifficulty = 'easy' | 'medium' | 'hard' | 'expert'

export interface Category {
  id: string
  name: string
  slug: string
  description: string | null
  imageUrl: string | null
  orderIndex: number
  recipeCount: number
}

export interface Nutrition {
  calories: number | null
  protein: number | null
  carbohydrates: number | null
  fat: number | null
  fiber: number | null
  sodium: number | null
}

export interface Recipe {
  id: string
  title: string
  slug: string
  description: string
  prepTime: number
  cookTime: number
  servings: number
  difficulty: RecipeDifficulty
  status: RecipeStatus
  primaryImageUrl: string | null
  category: Category
  author: { id: string; displayName: string }
  createdAt: string
  publishedAt: string | null
  version: number
  instructions: string | null
  nutrition: Nutrition | null
}

export interface RecipeWrite {
  title: string
  description: string
  categoryId: string
  prepTime: number
  cookTime: number
  servings: number
  difficulty: RecipeDifficulty
  instructions: string | null
  nutrition: Nutrition | null
}

export interface PageEnvelope<T> {
  data: T[]
  meta: {
    page: number
    pageSize: number
    total: number
    totalPages: number
    hasNextPage: boolean
    hasPreviousPage: boolean
  }
}

interface DataEnvelope<T> {
  data: T
}

export async function listCategories() {
  return (await apiRequest<DataEnvelope<Category[]>>('/categories')).data
}

export async function createCategory(data: Omit<Category, 'id' | 'slug' | 'recipeCount'>) {
  return (
    await authenticatedApiRequest<DataEnvelope<Category>>('/categories', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    })
  ).data
}

export async function updateCategory(id: string, data: Omit<Category, 'id' | 'slug' | 'recipeCount'>) {
  return (
    await authenticatedApiRequest<DataEnvelope<Category>>(`/categories/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    })
  ).data
}

export function deleteCategory(id: string) {
  return authenticatedApiRequest<void>(`/categories/${id}`, { method: 'DELETE' })
}

export function listMyRecipes(status?: RecipeStatus) {
  const query = status ? `?status=${status}` : ''
  return authenticatedApiRequest<PageEnvelope<Recipe>>(`/me/recipes${query}`)
}

export async function getMyRecipe(id: string) {
  return (await authenticatedApiRequest<DataEnvelope<Recipe>>(`/me/recipes/${id}`)).data
}

export async function createRecipe(data: RecipeWrite) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>('/recipes', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    })
  ).data
}

export async function updateRecipe(id: string, version: number, data: RecipeWrite) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>(`/recipes/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', 'If-Match': `"${version}"` },
      body: JSON.stringify(data),
    })
  ).data
}

export function deleteRecipe(id: string, version: number) {
  return authenticatedApiRequest<void>(`/recipes/${id}`, {
    method: 'DELETE',
    headers: { 'If-Match': `"${version}"` },
  })
}
