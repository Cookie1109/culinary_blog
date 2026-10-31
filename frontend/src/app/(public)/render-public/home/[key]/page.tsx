import type { Metadata } from 'next'
import { notFound } from 'next/navigation'
import { Home } from '@/features/culinary/pages/Home'
import { getPublicCategories, getPublishedRecipes } from '@/lib/api/public-content-server'
import { createPageMetadata, SITE_DESCRIPTION, SITE_NAME } from '@/lib/seo'

export const revalidate = 3600
export const metadata: Metadata = createPageMetadata({
  title: SITE_NAME,
  description: SITE_DESCRIPTION,
  path: '/',
  absoluteTitle: true,
})

// Generate the public root on its first request so builds need no live API.
export function generateStaticParams() {
  return []
}

export default async function HomePage({ params }: { params: Promise<{ key: string }> }) {
  if ((await params).key !== 'index') notFound()
  const [recipePage, categories] = await Promise.all([
    getPublishedRecipes(1, 12, revalidate),
    getPublicCategories(revalidate),
  ])

  return <Home recipePage={recipePage} categories={categories} />
}
