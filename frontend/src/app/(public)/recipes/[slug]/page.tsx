import { notFound } from 'next/navigation'
import { RecipeDetail } from '@/features/culinary/pages/RecipeDetail'
import { getPublishedRecipe } from '@/lib/api/public-content-server'

export const revalidate = 300

export default async function RecipePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params
  const recipe = await getPublishedRecipe(slug, revalidate)
  if (!recipe) notFound()

  return <RecipeDetail recipe={recipe} />
}
