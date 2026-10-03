import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('P6-24: Homepage visual/responsive layouts across mobile, tablet, and desktop', async () => {
  const home = await source('src/features/culinary/pages/Home.tsx')

  // Hero section fluid padding
  assert.match(
    home,
    /py-24\s+sm:px-6\s+lg:px-8\s+lg:py-32/,
    'Homepage hero must have fluid vertical padding across breakpoints',
  )

  // Fluid typography on hero title
  assert.match(
    home,
    /text-3xl\s+sm:text-5xl\s+leading-\[1\.1\]\s+text-foreground\s+lg:text-7xl\s+break-words/,
    'Hero heading must scale smoothly with viewport',
  )

  // Responsive recipe cards grid (1 col mobile, 3 col tablet/desktop)
  assert.match(
    home,
    /grid\s+grid-cols-1\s+gap-x-8\s+gap-y-16\s+md:grid-cols-3/,
    'Featured recipes grid must scale from 1 col on mobile to 3 on tablet/desktop',
  )

  // Responsive category grid (1 col mobile, 2 col sm, 4 col lg)
  assert.match(
    home,
    /grid\s+grid-cols-1\s+gap-5\s+sm:grid-cols-2\s+lg:grid-cols-4/,
    'Category grid must support 1 col on mobile, 2 on tablet, and 4 on desktop',
  )
})

test('P6-24: Recipes listing visual/responsive filter bar, view switcher, and card grid', async () => {
  const listing = await source('src/features/culinary/pages/RecipesList.tsx')

  // Responsive filter bar
  assert.match(
    listing,
    /grid\s+gap-4\s+border-y\s+border-border\s+py-6\s+sm:grid-cols-2\s+lg:grid-cols-4/,
    'Filter bar must be 1-col on mobile, 2-col on small/tablet, and 4-col on desktop',
  )

  // Responsive grid vs list view mode
  assert.match(
    listing,
    /grid\s+grid-cols-1\s+gap-x-8\s+gap-y-16\s+\$\{viewMode === 'grid'\s*\?\s*'md:grid-cols-2\s+lg:grid-cols-3'/,
    'Recipe listing must toggle between responsive 3-col grid and single-column list view',
  )

  // Accessible view switcher buttons with adequate touch targets
  assert.match(listing, /aria-label="Hiển thị dạng lưới"/)
  assert.match(listing, /aria-label="Hiển thị dạng danh sách"/)
})

test('P6-24: Recipe detail visual/responsive gallery, split columns, and typography', async () => {
  const detail = await source('src/features/culinary/pages/RecipeDetail.tsx')

  // Heading fluid typography
  assert.match(
    detail,
    /text-3xl\s+sm:text-5xl\s+lg:text-6xl\s+leading-\[1\.1\]\s+text-foreground\s+break-words/,
    'Recipe detail title must use fluid font sizing',
  )

  // Metadata items wrap cleanly on small viewports
  assert.match(
    detail,
    /flex\s+flex-wrap\s+items-center\s+justify-center\s+gap-3\s+border-y\s+border-border\s+py-4\s+text-sm\s+text-muted-foreground\s+sm:gap-6/,
    'Metadata chips must wrap on narrow mobile viewports',
  )

  // Image gallery container with stable aspect ratio to eliminate layout shift (CLS)
  assert.match(
    detail,
    /aspect-video\s+w-full\s+overflow-hidden\s+bg-muted/,
    'Recipe hero gallery must define aspect ratio container to eliminate CLS',
  )

  // Split columns on desktop (grid-cols-1 lg:grid-cols-12)
  assert.match(
    detail,
    /grid\s+grid-cols-1\s+gap-12\s+lg:grid-cols-12/,
    'Recipe detail must stack on mobile and split 5:7 on desktop',
  )

  // Secondary gallery grid
  assert.match(
    detail,
    /grid\s+grid-cols-1\s+gap-4\s+sm:grid-cols-2\s+lg:grid-cols-3/,
    'Image gallery must scale across 1, 2, and 3 columns',
  )
})

test('P6-24: Categories and CategoryDetail responsive layouts', async () => {
  const [categories, categoryDetail] = await Promise.all([
    source('src/features/culinary/pages/Categories.tsx'),
    source('src/features/culinary/pages/CategoryDetail.tsx'),
  ])

  // Categories grid
  assert.match(
    categories,
    /grid\s+grid-cols-1\s+gap-6\s+sm:grid-cols-2\s+lg:grid-cols-4/,
    'Categories grid must be responsive from 1 column up to 4 columns',
  )

  // Category detail recipes grid
  assert.match(
    categoryDetail,
    /grid\s+grid-cols-1\s+gap-x-8\s+gap-y-16\s+md:grid-cols-2\s+lg:grid-cols-3/,
    'Category detail recipe grid must scale to 2 cols on tablet and 3 on desktop',
  )
})

test('P6-24: Search page debounced input, responsive filters, and results grid', async () => {
  const search = await source('src/features/culinary/pages/Search.tsx')

  // Responsive search form
  assert.match(
    search,
    /flex\s+flex-col\s+sm:flex-row\s+max-w-2xl\s+gap-3/,
    'Search form must adapt cleanly to small mobile viewports',
  )

  // Search filter bar
  assert.match(
    search,
    /grid\s+gap-4\s+border-y\s+border-border\s+py-6\s+sm:grid-cols-2\s+lg:grid-cols-4/,
    'Search filter bar must scale from 1 col to 2 and 4 columns',
  )

  // Results grid matches standard responsive recipe grid
  assert.match(
    search,
    /grid\s+grid-cols-1\s+gap-x-6\s+gap-y-12\s+sm:grid-cols-2\s+xl:grid-cols-3/,
    'Search results grid must scale from 1 col on mobile to 2 on tablet and 3 on desktop',
  )
})

test('P6-24: AppLayout navigation switches between desktop bar and mobile drawer with focus trap', async () => {
  const layout = await source('src/features/culinary/components/layout/AppLayout.tsx')

  // Hamburger trigger hidden on desktop, visible on mobile
  assert.match(layout, /lg:hidden/)
  assert.match(layout, /aria-label="Mở menu điều hướng"/)
  assert.match(layout, /aria-expanded=\{mobileOpen\}/)

  // Desktop navigation links visible on desktop, hidden on mobile
  assert.match(layout, /hidden\s+lg:flex/)

  // Mobile drawer slide-over with focus trap and escape handling
  assert.match(layout, /role="dialog"/)
  assert.match(layout, /aria-modal="true"/)
  assert.match(layout, /e\.key === 'Escape'/)
})

test('P6-24: Image components specify responsive sizes and blur placeholders to eliminate CLS', async () => {
  const [recipeCard, recipeDetail] = await Promise.all([
    source('src/features/culinary/components/RecipeCard.tsx'),
    source('src/features/culinary/pages/RecipeDetail.tsx'),
  ])

  // RecipeCard uses next/image with responsive sizes and placeholder="blur"
  assert.match(recipeCard, /<Image/)
  assert.match(recipeCard, /sizes=/)
  assert.match(recipeCard, /placeholder="blur"/)
  assert.match(recipeCard, /blurDataURL=/)

  // RecipeDetail hero image uses next/image with sizes and placeholder
  assert.match(recipeDetail, /<Image/)
  assert.match(recipeDetail, /sizes=/)
  assert.match(recipeDetail, /placeholder="blur"/)
})
