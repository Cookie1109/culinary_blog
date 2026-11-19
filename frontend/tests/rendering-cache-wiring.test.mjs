import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('public realtime pages render fresh content while keeping backend cache support', async () => {
  const [home, categories, category, recipe, serverClient] = await Promise.all([
    source('src/app/(public)/render-public/home/[key]/page.tsx'),
    source('src/app/(public)/render-public/categories/[key]/page.tsx'),
    source('src/app/(public)/categories/[slug]/page.tsx'),
    source('src/app/(public)/recipes/[slug]/page.tsx'),
    source('src/lib/api/public-content-server.ts'),
  ])

  assert.match(home, /export const dynamic = 'force-dynamic'/)
  assert.doesNotMatch(home, /generateStaticParams/)
  assert.doesNotMatch(home, /connection\(/)
  assert.match(home, /key !== 'index'\) notFound\(\)/)
  assert.match(categories, /export const dynamic = 'force-dynamic'/)
  assert.doesNotMatch(categories, /generateStaticParams/)
  assert.doesNotMatch(categories, /connection\(/)
  assert.match(categories, /key !== 'index'\) notFound\(\)/)
  assert.match(category, /export const dynamic = 'force-dynamic'/)
  assert.match(recipe, /export const dynamic = 'force-dynamic'/)
  assert.match(serverClient, /PUBLIC_CONTENT_TAG = 'public-content'/)
  assert.match(serverClient, /tags: \[PUBLIC_CONTENT_TAG\]/)
  assert.match(serverClient, /export function getPublishedRecipes/)
})

test('recipe listing renders search and filter query-state data dynamically on the server', async () => {
  const route = await source('src/app/(public)/recipes/page.tsx')
  assert.match(route, /export const dynamic = 'force-dynamic'/)
  assert.match(route, /await searchParams/)
  assert.match(route, /searchPublishedRecipesOnServer/)

  const serverClient = await source('src/lib/api/public-content-server.ts')
  assert.match(serverClient, /noStore: true/)
  assert.match(serverClient, /cache: 'no-store'/)
})

test('private preview uses authenticated no-store reads and a private media proxy', async () => {
  const [preview, mediaProxy] = await Promise.all([
    source('src/app/(dashboard)/dashboard/recipes/[id]/preview/page.tsx'),
    source('src/app/api/private-media/[imageId]/[variant]/route.ts'),
  ])

  assert.match(preview, /export const dynamic = 'force-dynamic'/)
  assert.match(preview, /export const revalidate = 0/)
  assert.match(preview, /roles.includes\('Admin'\) \? 'admin' : 'me'/)
  assert.match(preview, /\$\{scope\}\/recipes\/\$\{encodeURIComponent\(id\)\}/)
  assert.match(preview, /cache: 'no-store'/)
  assert.match(preview, /Authorization: `Bearer \$\{session\.accessToken\}`/)
  assert.match(preview, /index: false, follow: false/)
  assert.match(mediaProxy, /'Cache-Control': 'private, no-store'/)
})

test('public recipe imagery uses next/image with responsive sizes and placeholders', async () => {
  for (const path of [
    'src/features/culinary/components/RecipeCard.tsx',
    'src/features/culinary/pages/RecipeDetail.tsx',
  ]) {
    const component = await source(path)
    assert.match(component, /from 'next\/image'/)
    assert.match(component, /sizes=/)
    assert.match(component, /placeholder="blur"/)
    assert.match(component, /blurDataURL=/)
  }

  const mediaProxy = await source('src/app/api/public-media/[imageId]/[variant]/route.ts')
  assert.match(mediaProxy, /cache: 'no-store'/)
  assert.match(mediaProxy, /'Cache-Control': 'no-store'/)
})
