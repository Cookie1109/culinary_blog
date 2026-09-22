import { authenticatedApiRequest, authenticatedUpload } from '@/lib/api/auth-client'
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
  ingredients: Ingredient[]
  steps: RecipeStep[]
  images: RecipeImage[]
}

export interface Ingredient {
  id: string
  name: string
  quantity: number | null
  unit: string | null
  notes: string | null
  orderIndex: number
}

export type IngredientWrite = Omit<Ingredient, 'id'>

export interface RecipeStep {
  id: string
  stepNumber: number
  title: string
  description: string
  timerMinutes: number | null
  imageUrl: string | null
}

export interface StepWrite {
  title: string
  description: string
  timerMinutes: number | null
  imageUrl: string | null
  stepNumber?: number
}

export interface RecipeImage {
  id: string
  originalUrl: string
  mediumUrl: string | null
  thumbnailUrl: string | null
  altText: string | null
  isPrimary: boolean
  orderIndex: number
  processingStatus: 'pending' | 'ready' | 'failed'
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

export interface CategoryDetailEnvelope {
  data: {
    category: Category
    recipes: Recipe[]
  }
  meta: PageEnvelope<Recipe>['meta']
}

interface DataEnvelope<T> {
  data: T
}

interface MutationEnvelope<T> extends DataEnvelope<T> {
  meta: { recipeVersion: number }
}

export async function listCategories() {
  return (await apiRequest<DataEnvelope<Category[]>>('/categories')).data
}

export function listPublishedRecipes(page = 1, pageSize = 12) {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return apiRequest<PageEnvelope<Recipe>>(`/recipes?${query}`)
}

export function searchPublishedRecipes(filters: {
  q: string
  category: string
  difficulty: string
  maxTime: number
  sort: string
  page: number
  pageSize: number
}) {
  const query = new URLSearchParams({
    page: String(filters.page),
    pageSize: String(filters.pageSize),
    sort: filters.sort,
  })
  if (filters.q) query.set('q', filters.q)
  if (filters.category) query.set('category', filters.category)
  if (filters.difficulty) query.set('difficulty', filters.difficulty)
  if (filters.maxTime > 0) query.set('maxTime', String(filters.maxTime))
  return apiRequest<PageEnvelope<Recipe>>('/recipes/search?' + query)
}

export function listPublishedRecipesByCategory(slug: string, page = 1, pageSize = 12) {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return apiRequest<CategoryDetailEnvelope>(`/categories/${encodeURIComponent(slug)}?${query}`)
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

const versionHeaders = (version: number) => ({
  'Content-Type': 'application/json',
  'If-Match': `"${version}"`,
})

export function createIngredient(recipeId: string, version: number, data: IngredientWrite) {
  return authenticatedApiRequest<MutationEnvelope<Ingredient>>(`/recipes/${recipeId}/ingredients`, {
    method: 'POST',
    headers: versionHeaders(version),
    body: JSON.stringify(data),
  })
}

export function updateIngredient(
  recipeId: string,
  ingredientId: string,
  version: number,
  data: IngredientWrite,
) {
  return authenticatedApiRequest<MutationEnvelope<Ingredient>>(
    `/recipes/${recipeId}/ingredients/${ingredientId}`,
    {
      method: 'PUT',
      headers: versionHeaders(version),
      body: JSON.stringify(data),
    },
  )
}

export function deleteIngredient(recipeId: string, ingredientId: string, version: number) {
  return authenticatedApiRequest<void>(`/recipes/${recipeId}/ingredients/${ingredientId}`, {
    method: 'DELETE',
    headers: { 'If-Match': `"${version}"` },
  })
}

export function createStep(recipeId: string, version: number, data: StepWrite) {
  return authenticatedApiRequest<MutationEnvelope<RecipeStep>>(`/recipes/${recipeId}/steps`, {
    method: 'POST',
    headers: versionHeaders(version),
    body: JSON.stringify(data),
  })
}

export function updateStep(recipeId: string, stepId: string, version: number, data: StepWrite) {
  return authenticatedApiRequest<MutationEnvelope<RecipeStep>>(`/recipes/${recipeId}/steps/${stepId}`, {
    method: 'PUT',
    headers: versionHeaders(version),
    body: JSON.stringify(data),
  })
}

export function deleteStep(recipeId: string, stepId: string, version: number) {
  return authenticatedApiRequest<void>(`/recipes/${recipeId}/steps/${stepId}`, {
    method: 'DELETE',
    headers: { 'If-Match': `"${version}"` },
  })
}

export function uploadRecipeImage(
  recipeId: string,
  version: number,
  file: File,
  altText: string,
  isPrimary: boolean,
  onProgress: (percent: number) => void,
) {
  const form = new FormData()
  form.set('file', file)
  form.set('altText', altText)
  form.set('isPrimary', String(isPrimary))
  return authenticatedUpload<MutationEnvelope<RecipeImage>>(
    `/recipes/${recipeId}/images`,
    form,
    onProgress,
    version,
  )
}

export function updateRecipeImage(
  recipeId: string,
  imageId: string,
  version: number,
  data: { altText: string | null; isPrimary: boolean; orderIndex: number },
) {
  return authenticatedApiRequest<MutationEnvelope<RecipeImage>>(`/recipes/${recipeId}/images/${imageId}`, {
    method: 'PATCH',
    headers: versionHeaders(version),
    body: JSON.stringify(data),
  })
}

export function deleteRecipeImage(recipeId: string, imageId: string, version: number) {
  return authenticatedApiRequest<void>(`/recipes/${recipeId}/images/${imageId}`, {
    method: 'DELETE',
    headers: { 'If-Match': `"${version}"` },
  })
}
