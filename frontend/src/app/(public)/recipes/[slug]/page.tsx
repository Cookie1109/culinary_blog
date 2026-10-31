import type { Metadata } from 'next'
import { notFound } from 'next/navigation'
import { RecipeDetail } from '@/features/culinary/pages/RecipeDetail'
import { getPublishedRecipe } from '@/lib/api/public-content-server'
import { createPageMetadata, createRecipeJsonLd, recipeImages, serializeJsonLd, summarize } from '@/lib/seo'

export const revalidate = 300

export function generateStaticParams() {
  return []
}

type RecipePageProps = { params: Promise<{ slug: string }> }

export async function generateMetadata({ params }: RecipePageProps): Promise<Metadata> {
  const { slug } = await params
  const recipe = await getPublishedRecipe(slug, revalidate)
  if (!recipe) notFound()

  return createPageMetadata({
    title: recipe.title,
    description: summarize(recipe.description),
    path: `/recipes/${encodeURIComponent(recipe.slug)}`,
    image: recipeImages(recipe)[0],
    imageAlt: recipe.title,
    type: 'article',
    publishedTime: recipe.publishedAt,
    author: recipe.author.displayName,
  })
}

export default async function RecipePage({ params }: RecipePageProps) {
  const { slug } = await params
  const recipe = await getPublishedRecipe(slug, revalidate)
  if (!recipe) notFound()

  return (
    <>
      <script type="application/ld+json">{serializeJsonLd(createRecipeJsonLd(recipe))}</script>
      <RecipeDetail recipe={recipe} />
    </>
  )
}
