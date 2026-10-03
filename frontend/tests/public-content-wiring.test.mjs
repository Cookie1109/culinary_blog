import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const publicPages = [
  ['src/features/culinary/pages/Home.tsx', 'listPublishedRecipes'],
  ['src/features/culinary/pages/RecipesList.tsx', 'searchPublishedRecipes'],
  ['src/features/culinary/pages/Search.tsx', 'searchPublishedRecipes'],
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

test('home, listing, and detail expose the required public discovery content', async () => {
  const home = await readFile(new URL('../src/features/culinary/pages/Home.tsx', import.meta.url), 'utf8')
  const listing = await readFile(
    new URL('../src/features/culinary/pages/RecipesList.tsx', import.meta.url),
    'utf8',
  )
  const detail = await readFile(
    new URL('../src/features/culinary/pages/RecipeDetail.tsx', import.meta.url),
    'utf8',
  )

  assert.match(home, /listCategories/)
  for (const urlState of ['category', 'difficulty', 'maxTime', 'sort', 'page']) {
    assert.match(listing, new RegExp(`searchParams\\.get\\('${urlState}'\\)`))
  }
  for (const detailContent of ['recipe.ingredients', 'recipe.steps', 'recipe.nutrition', 'recipe.images']) {
    assert.match(detail, new RegExp(detailContent.replace('.', '\\.')))
  }
})

test('category detail route uses the public category endpoint and maps API 404 to not-found', async () => {
  const route = await readFile(
    new URL('../src/app/(public)/categories/[slug]/page.tsx', import.meta.url),
    'utf8',
  )
  const categories = await readFile(
    new URL('../src/features/culinary/pages/Categories.tsx', import.meta.url),
    'utf8',
  )

  assert.match(route, /\/categories\/\$\{encodeURIComponent\(slug\)\}/)
  assert.match(route, /response\.status === 404/)
  assert.match(route, /notFound\(\)/)
  assert.match(categories, /to=\{`\/categories\/\$\{category\.slug\}`\}/)
})

test('search debounces URL updates and public query failures offer retry and offline guidance', async () => {
  const search = await readFile(new URL('../src/features/culinary/pages/Search.tsx', import.meta.url), 'utf8')
  const queryError = await readFile(
    new URL('../src/features/culinary/components/PublicQueryError.tsx', import.meta.url),
    'utf8',
  )
  const networkStatus = await readFile(
    new URL('../src/features/culinary/components/NetworkStatusBanner.tsx', import.meta.url),
    'utf8',
  )

  assert.match(search, /setTimeout\(\(\) =>/)
  assert.match(search, /, 400\)/)
  assert.match(queryError, /onRetry/)
  assert.match(networkStatus, /navigator\.onLine/)
  assert.match(networkStatus, /addEventListener\('offline'/)
})

test('recipe detail route maps an API 404 to the Next.js not-found response', async () => {
  const source = await readFile(
    new URL('../src/app/(public)/recipes/[slug]/page.tsx', import.meta.url),
    'utf8',
  )

  assert.match(source, /response\.status === 404/)
  assert.match(source, /notFound\(\)/)
  assert.match(source, /cache: 'no-store'/)
})
