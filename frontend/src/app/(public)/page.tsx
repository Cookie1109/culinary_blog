import { connection } from 'next/server'
import { Home } from '@/features/culinary/pages/Home'
import { getPublicCategories, getPublishedRecipes } from '@/lib/api/public-content-server'

export const revalidate = 3600

export default async function HomePage() {
  await connection()
  const [recipes, categories] = await Promise.all([
    getPublishedRecipes(1, 3, revalidate),
    getPublicCategories(revalidate),
  ])

  return <Home recipes={recipes.data} categories={categories} />
}
