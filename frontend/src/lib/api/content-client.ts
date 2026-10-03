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

export function listMyRecipes(status?: RecipeStatus, page: number = 1, pageSize: number = 12) {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  if (page) params.set('page', String(page))
  if (pageSize) params.set('pageSize', String(pageSize))
  const query = params.toString() ? `?${params.toString()}` : ''
  return authenticatedApiRequest<PageEnvelope<Recipe>>(`/me/recipes${query}`)
}

export async function getMyRecipe(id: string) {
  return (await authenticatedApiRequest<DataEnvelope<Recipe>>(`/me/recipes/${id}`)).data
}

export function listAdminRecipes(
  status?: RecipeStatus,
  authorId?: string,
  page: number = 1,
  pageSize: number = 12,
) {
  const params = new URLSearchParams()
  if (status) params.set('status', status)
  if (authorId) params.set('authorId', authorId)
  if (page) params.set('page', String(page))
  if (pageSize) params.set('pageSize', String(pageSize))
  const query = params.toString() ? `?${params.toString()}` : ''
  return authenticatedApiRequest<PageEnvelope<Recipe>>(`/admin/recipes${query}`)
}

export async function getAdminRecipe(id: string) {
  return (await authenticatedApiRequest<DataEnvelope<Recipe>>(`/admin/recipes/${id}`)).data
}

export interface AuditLogEntry {
  id: string
  timestamp: string
  level: string
  eventName: string
  message: string
  userId: string | null
  correlationId: string | null
  requestPath: string | null
  requestMethod: string | null
  statusCode: number | null
  properties: Record<string, string> | null
}

export interface TelemetryStatus {
  seqConfigured: boolean
  otlpConfigured: boolean
  operationalNetworkRestricted: boolean
}

export interface AuditLogsEnvelope {
  data: AuditLogEntry[]
  meta: PageEnvelope<AuditLogEntry>['meta']
  telemetry: TelemetryStatus
}

export function listAuditLogs(params?: {
  level?: string
  search?: string
  page?: number
  pageSize?: number
}) {
  const urlParams = new URLSearchParams()
  if (params?.level && params.level !== 'all') urlParams.set('level', params.level)
  if (params?.search) urlParams.set('search', params.search)
  if (params?.page) urlParams.set('page', String(params.page))
  if (params?.pageSize) urlParams.set('pageSize', String(params.pageSize))
  const query = urlParams.toString() ? `?${urlParams.toString()}` : ''
  return authenticatedApiRequest<AuditLogsEnvelope>(`/admin/audit-logs${query}`)
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

export async function publishRecipe(id: string, version: number) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>(`/recipes/${id}/publish`, {
      method: 'PATCH',
      headers: { 'If-Match': `"${version}"` },
    })
  ).data
}

export async function unpublishRecipe(id: string, version: number) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>(`/recipes/${id}/unpublish`, {
      method: 'PATCH',
      headers: { 'If-Match': `"${version}"` },
    })
  ).data
}

export async function archiveRecipe(id: string, version: number) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>(`/recipes/${id}/archive`, {
      method: 'PATCH',
      headers: { 'If-Match': `"${version}"` },
    })
  ).data
}

export async function unarchiveRecipe(id: string, version: number) {
  return (
    await authenticatedApiRequest<DataEnvelope<Recipe>>(`/recipes/${id}/unarchive`, {
      method: 'PATCH',
      headers: { 'If-Match': `"${version}"` },
    })
  ).data
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
