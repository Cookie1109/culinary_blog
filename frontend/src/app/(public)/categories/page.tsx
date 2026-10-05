import type { Metadata } from 'next'
import { connection } from 'next/server'
import { Categories } from '@/features/culinary/pages/Categories'
import { getPublicCategories, getPublishedRecipes } from '@/lib/api/public-content-server'
import { createPageMetadata } from '@/lib/seo'

export const revalidate = 3600
export const metadata: Metadata = createPageMetadata({
  title: 'Danh mục món ăn',
  description: 'Duyệt các công thức nấu ăn theo từng danh mục.',
  path: '/categories',
})

const PREFERRED_CATEGORY_IMAGES: Record<string, string> = {
  'mon-nuoc': '/api/v1/media/f9b63828-3e72-91a1-2e19-9a0c890dfbaa/medium',
}

export default async function CategoriesPage() {
  await connection()
  const categories = await getPublicCategories(revalidate)
  for (const cat of categories) {
    if (PREFERRED_CATEGORY_IMAGES[cat.slug]) {
      cat.imageUrl = PREFERRED_CATEGORY_IMAGES[cat.slug]
    }
  }

  const missingCategories = categories.filter((c) => !c.imageUrl && c.recipeCount > 0)
  if (missingCategories.length > 0) {
    try {
      const page = await getPublishedRecipes(1, 50, revalidate)
      const recipeByCat = new Map<string, string>()
      for (const recipe of page.data) {
        const primaryImage = recipe.images.find((image) => image.isPrimary)
        const img = primaryImage?.mediumUrl ?? primaryImage?.originalUrl ?? recipe.primaryImageUrl
        if (img && !recipeByCat.has(recipe.category.id)) {
          recipeByCat.set(recipe.category.id, img)
        }
      }
      for (const cat of categories) {
        if (!cat.imageUrl && recipeByCat.has(cat.id)) {
          cat.imageUrl = recipeByCat.get(cat.id) ?? null
        }
      }
    } catch {
      // Gracefully continue with original categories
    }
  }
  return <Categories categories={categories} />
}
