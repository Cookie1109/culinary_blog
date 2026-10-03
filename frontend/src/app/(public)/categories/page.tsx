import type { Metadata } from 'next'
import { connection } from 'next/server'
import { Categories } from '@/features/culinary/pages/Categories'
import { getPublicCategories } from '@/lib/api/public-content-server'
import { createPageMetadata } from '@/lib/seo'

export const revalidate = 3600
export const metadata: Metadata = createPageMetadata({
  title: 'Danh mục món ăn',
  description: 'Duyệt các công thức nấu ăn theo từng danh mục.',
  path: '/categories',
})

export default async function CategoriesPage() {
  await connection()
  return <Categories categories={await getPublicCategories(revalidate)} />
}
