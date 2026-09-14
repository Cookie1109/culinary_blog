import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const publicPages = [
  ['src/features/culinary/pages/Home.tsx', 'listPublishedRecipes'],
  ['src/features/culinary/pages/RecipesList.tsx', 'listPublishedRecipes'],
  ['src/features/culinary/pages/Search.tsx', 'listPublishedRecipes'],
  ['src/features/culinary/pages/Categories.tsx', 'listCategories'],
]

test('public pages use API data instead of bundled recipe fixtures', async () => {
  for (const [path, expectedClient] of publicPages) {
    const source = await readFile(new URL(`../${path}`, import.meta.url), 'utf8')
    assert.match(source, new RegExp(`\\b${expectedClient}\\b`), `${path} must call ${expectedClient}`)
    assert.doesNotMatch(
      source,
      /\b(?:DUMMY_RECIPE|DUMMY_LIST|ALL_RECIPES|INITIAL_CATEGORIES|FEATURED_RECIPES)\b/,
      `${path} must not bundle public content fixtures`,
    )
  }
})

test('recipe detail route maps an API 404 to the Next.js not-found response', async () => {
  const source = await readFile(new URL('../src/app/(site)/recipes/[slug]/page.tsx', import.meta.url), 'utf8')

  assert.match(source, /response\.status === 404/)
  assert.match(source, /notFound\(\)/)
  assert.match(source, /cache: 'no-store'/)
})
