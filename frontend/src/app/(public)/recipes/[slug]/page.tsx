import { notFound } from 'next/navigation'
import { RecipeDetail } from '@/features/culinary/pages/RecipeDetail'
import type { Recipe } from '@/lib/api/content-client'

export const dynamic = 'force-dynamic'

interface RecipeEnvelope {
  data: Recipe
}

async function loadRecipe(slug: string) {
  const baseUrl = process.env.INTERNAL_API_BASE_URL ?? 'http://localhost:5000/api/v1'
  const response = await fetch(`${baseUrl}/recipes/${encodeURIComponent(slug)}`, { cache: 'no-store' })
  if (response.status === 404) notFound()
  if (!response.ok) throw new Error(`Recipe API returned ${response.status}.`)
  return ((await response.json()) as RecipeEnvelope).data
}

export default async function RecipePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params
  return <RecipeDetail recipe={await loadRecipe(slug)} />
}
