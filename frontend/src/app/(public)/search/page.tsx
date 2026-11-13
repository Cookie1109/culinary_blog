import type { Metadata } from 'next'
import RecipesPage from '@/app/(public)/recipes/page'
import { createPageMetadata } from '@/lib/seo'

export const dynamic = 'force-dynamic'
export const metadata: Metadata = createPageMetadata({
  title: 'Tìm kiếm công thức',
  description: 'Tìm kiếm công thức nấu ăn.',
  path: '/search',
  index: false,
  follow: true,
})

export default RecipesPage
