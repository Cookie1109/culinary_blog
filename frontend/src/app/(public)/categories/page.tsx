import { connection } from 'next/server'
import { Categories } from '@/features/culinary/pages/Categories'
import { getPublicCategories } from '@/lib/api/public-content-server'

export const revalidate = 3600

export default async function CategoriesPage() {
  await connection()
  return <Categories categories={await getPublicCategories(revalidate)} />
}
