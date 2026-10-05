import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('P6-24: Homepage visual/responsive layouts across mobile, tablet, and desktop', async () => {
  const [home, homeRoute, categories, categoryCard] = await Promise.all([
    source('src/features/culinary/pages/Home.tsx'),
    source('src/app/(public)/page.tsx'),
    source('src/features/culinary/pages/Categories.tsx'),
    source('src/features/culinary/components/CategoryCard.tsx'),
  ])

  // Hero section is a full-bleed layout with the active recipe image and text overlay.
  assert.match(home, /relative w-full overflow-hidden/, 'Homepage hero must be full-bleed with overlay copy')
  assert.match(home, /featuredImage/)
  assert.match(home, /<Image/)

  // Fluid typography on hero title
  assert.match(
    home,
    /text-3xl\s+sm:text-5xl\s+leading-\[1\.1\]\s+text-white\s+lg:text-7xl\s+break-words/,
    'Hero heading must scale smoothly with viewport',
  )

  // Featured recipes carousel shows one card on mobile and three on desktop.
  assert.match(
    home,
    /basis-full snap-start md:basis-\[calc\(\(100%-4rem\)\/3\)\]/,
    'Featured recipe carousel must scale from one card to three cards',
  )

  // Featured categories use the same shared card as the categories page.
  assert.match(
    home,
    /basis-full snap-start sm:basis-\[calc\(\(100%-1\.25rem\)\/2\)\] lg:basis-\[calc\(\(100%-3\.75rem\)\/4\)\]/,
    'Category carousel must scale from one to two and four cards',
  )
  assert.match(home, /<CategoryCard category=\{category\} headingLevel="h3"/)
  assert.match(categories, /<CategoryCard key=\{category\.id\} category=\{category\}/)
  assert.match(categoryCard, /aspect-\[4\/3\]/)

  // The latest carousel receives exactly five recipes and advances every 15 seconds.
  assert.match(homeRoute, /getPublishedRecipes\(1, 12, revalidate\)/)
  assert.match(home, /recipePage\.data\.slice\(0, 5\)/)
  assert.match(home, /listPublishedRecipes\(loadedPage \+ 1, recipePage\.meta\.pageSize\)/)
  assert.match(home, /loadingMoreRecipes/)
  assert.match(home, /setInterval/)
  assert.match(home, /15_000/)
  assert.match(home, /label="Xem công thức mới trước đó"/)
  assert.match(home, /label="Xem công thức mới tiếp theo"/)

  // Both featured lists provide labelled previous/next controls.
  assert.match(home, /label="Xem các công thức nổi bật trước"/)
  assert.match(home, /label="Xem thêm công thức nổi bật"/)
  assert.match(home, /label="Xem các danh mục trước"/)
  assert.match(home, /label="Xem thêm danh mục"/)
  assert.match(home, /aspect-square.*md:aspect-\[3\/1\]/)
  assert.match(home, /aspect-\[4\/3\].*sm:aspect-\[8\/3\] lg:aspect-\[16\/3\]/)
  assert.match(home, /relative px-10 sm:px-14/)
  assert.match(home, /-left-10.*sm:-left-14/)
  assert.match(home, /-right-10.*sm:-right-14/)
})

test('P6-24: Recipes listing visual/responsive search, sidebar filters, and card grid', async () => {
  const listing = await source('src/features/culinary/pages/RecipesList.tsx')

  // Search form matches the centered desktop treatment and stacks on mobile.
  assert.match(
    listing,
    /mx-auto\s+flex\s+max-w-3xl\s+flex-col\s+sm:flex-row/,
    'Recipe search form must stack on mobile and stay inline on larger screens',
  )
  assert.match(listing, /role="search"/)

  // Filters stack above content on mobile and become a sidebar on desktop.
  assert.match(
    listing,
    /grid\s+gap-10\s+lg:grid-cols-\[14rem_minmax\(0,1fr\)\]/,
    'Recipe listing must switch to a fixed-width filter sidebar on desktop',
  )
  assert.match(listing, /type="range"/)
  assert.match(listing, /categories\.slice\(0, 4\)/)
  assert.match(listing, /Xem tất cả/)
  assert.match(listing, /<Modal/)

  // Results retain a responsive one, two, and three-column card grid.
  assert.match(
    listing,
    /grid\s+grid-cols-1\s+gap-x-6\s+gap-y-12\s+md:grid-cols-2\s+xl:grid-cols-3/,
    'Recipe listing must scale from one column to three columns',
  )
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

test('P6-24: AppLayout navigation switches between desktop bar and mobile drawer with focus trap', async () => {
  const layout = await source('src/features/culinary/components/layout/AppLayout.tsx')

  // Hamburger trigger hidden on desktop, visible on mobile
  assert.match(layout, /lg:hidden/)
  assert.match(layout, /aria-label="Mở menu điều hướng"/)
  assert.match(layout, /aria-expanded=\{mobileOpen\}/)

  // Desktop navigation links visible on desktop, hidden on mobile
  assert.match(layout, /hidden\s+lg:flex/)

  // Desktop logo is centered against the viewport instead of unequal side content.
  assert.match(layout, /relative flex h-20 items-center justify-between/)
  assert.match(layout, /lg:absolute lg:left-1\/2 lg:-translate-x-1\/2/)

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
  assert.match(
    recipeCard,
    /primaryImage\?\.mediumUrl\s*\?\?\s*primaryImage\?\.originalUrl\s*\?\?\s*recipe\.primaryImageUrl/,
    'Recipe cards must not stretch the thumbnail variant',
  )

  // RecipeDetail hero image uses next/image with sizes and placeholder
  assert.match(recipeDetail, /<Image/)
  assert.match(recipeDetail, /sizes=/)
  assert.match(recipeDetail, /placeholder="blur"/)
  assert.match(
    recipeDetail,
    /primaryImage\?\.originalUrl\s*\?\?\s*primaryImage\?\.mediumUrl\s*\?\?\s*recipe\.primaryImageUrl/,
    'Recipe hero must prefer the original image over resized variants',
  )
})
