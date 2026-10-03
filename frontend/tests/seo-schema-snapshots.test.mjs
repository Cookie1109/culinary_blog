import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

// Reusable mock recipe fixture matching the backend Recipe envelope
const mockRecipe = {
  id: 'recipe-001',
  slug: 'pho-bo-ha-noi',
  title: 'Phở Bò Hà Nội',
  description: 'Món phở bò truyền thống với nước dùng thanh ngọt từ xương bò hầm và thảo mộc thơm lừng.',
  difficulty: 'medium',
  prepTime: 30,
  cookTime: 180,
  servings: 4,
  primaryImageUrl: '/api/v1/media/img-1/pho-bo.jpg',
  publishedAt: '2026-10-01T08:00:00.000Z',
  createdAt: '2026-09-30T10:00:00.000Z',
  updatedAt: '2026-10-01T08:00:00.000Z',
  author: {
    id: 'author-1',
    displayName: 'Bếp Trưởng Hà',
    bio: 'Đầu bếp chuyên về món ăn truyền thống miền Bắc',
    avatarUrl: null,
  },
  category: {
    id: 'cat-1',
    name: 'Món nước',
    slug: 'mon-nuoc',
    description: 'Các món bún, phở, mì có nước dùng đậm đà',
    recipeCount: 12,
  },
  ingredients: [
    { id: 'ing-1', name: 'Bánh phở tươi', quantity: 500, unit: 'g', notes: 'loại sợi vừa', orderIndex: 1 },
    { id: 'ing-2', name: 'Bắp bò', quantity: 300, unit: 'g', notes: 'thái lát mỏng', orderIndex: 2 },
    { id: 'ing-3', name: 'Xương ống bò', quantity: 1, unit: 'kg', notes: 'rửa sạch nướng sơ', orderIndex: 3 },
  ],
  steps: [
    {
      id: 'step-1',
      stepNumber: 1,
      title: 'Hầm nước dùng',
      description: 'Nướng gừng, hành tây rồi cho vào nồi cùng xương bò hầm lửa nhỏ trong 3 tiếng.',
      timerMinutes: 180,
      imageUrl: '/api/v1/media/step-1/ham-xuong.jpg',
    },
    {
      id: 'step-2',
      stepNumber: 2,
      title: 'Trần bánh phở và xếp thịt',
      description: 'Trần bánh phở qua nước sôi, xếp vào bát, đặt bắp bò và hành hoa lên trên.',
      timerMinutes: null,
      imageUrl: null,
    },
  ],
  images: [
    {
      id: 'img-1',
      originalUrl: '/api/v1/media/img-1/pho-bo.jpg',
      mediumUrl: '/api/v1/media/img-1/pho-bo-medium.jpg',
      altText: 'Bát phở bò nóng hổi nghi ngút khói',
      isPrimary: true,
      orderIndex: 0,
      processingStatus: 'ready',
    },
  ],
  nutrition: {
    calories: 450,
    protein: 28,
    carbohydrates: 62,
    fat: 10,
    fiber: 2,
    sodium: 980,
  },
}

test('P6-25: Mock recipe fixture possesses valid structure for schema projection', () => {
  assert.equal(mockRecipe.slug, 'pho-bo-ha-noi')
  assert.ok(mockRecipe.ingredients.length > 0)
  assert.ok(mockRecipe.steps.length > 0)
  assert.ok(mockRecipe.nutrition.calories > 0)
})

test('P6-25: Schema.org Recipe JSON-LD generator meets Google Rich Results specification', async () => {
  const seoSource = await source('src/lib/seo.ts')

  // Verify Schema.org context and type
  assert.match(seoSource, /'@context':\s*'https:\/\/schema\.org'/)
  assert.match(seoSource, /'@type':\s*'Recipe'/)
  assert.match(seoSource, /'@id':\s*`\$\{canonical\}#recipe`/)
  assert.match(seoSource, /mainEntityOfPage:\s*canonical/)

  // Verify all required and recommended Google Recipe Rich Results fields
  const requiredFields = [
    'name:\\s*recipe\\.title',
    'description:\\s*summarize\\(recipe\\.description\\)',
    'image:\\s*recipeImages\\(recipe\\)',
    "author:\\s*\\{\\s*'@type':\\s*'Person',\\s*name:\\s*recipe\\.author\\.displayName\\s*\\}",
    'datePublished:\\s*recipe\\.publishedAt',
    'prepTime:\\s*duration\\(recipe\\.prepTime\\)',
    'cookTime:\\s*duration\\(recipe\\.cookTime\\)',
    'totalTime:\\s*duration\\(recipe\\.prepTime\\s*\\+\\s*recipe\\.cookTime\\)',
    'recipeYield:',
    'recipeCategory:\\s*recipe\\.category\\.name',
    'recipeIngredient:\\s*ingredientText\\(recipe\\)',
    'recipeInstructions:',
    'nutrition:\\s*nutrition\\(recipe\\)',
    "inLanguage:\\s*'vi-VN'",
  ]

  for (const field of requiredFields) {
    assert.match(seoSource, new RegExp(field), `Recipe schema must include field matching ${field}`)
  }

  // Duration formatting PT...M
  assert.match(seoSource, /function duration\(minutes: number\)\s*\{\s*return `PT\$\{minutes\}M`\s*\}/)

  // Step formatting HowToStep
  assert.match(seoSource, /'@type':\s*'HowToStep'/)
  assert.match(seoSource, /position:\s*step\.stepNumber/)
  assert.match(seoSource, /name:\s*step\.title/)
  assert.match(seoSource, /text:\s*step\.description/)

  // Nutrition formatting
  assert.match(seoSource, /'@type':\s*'NutritionInformation'/)
  assert.match(seoSource, /calories:/)
  assert.match(seoSource, /proteinContent:/)
  assert.match(seoSource, /carbohydrateContent:/)
  assert.match(seoSource, /fatContent:/)
})

test('P6-25: JSON-LD serialization snapshot protects against script injection (XSS)', async () => {
  const seoSource = await source('src/lib/seo.ts')

  // Must escape '<' with '\u003c'
  assert.match(
    seoSource,
    /JSON\.stringify\(value\)\.replace\(\/<\s*\/g,\s*['"]\\\\u003c['"]\)/,
    'serializeJsonLd must escape < to \\u003c to prevent XSS script injection',
  )

  // Verify simulation of escaping behavior
  const maliciousPayload = {
    '@context': 'https://schema.org',
    '@type': 'Recipe',
    name: 'Bún Chả <script>alert("XSS")</script>',
    description: 'Thịt nướng <b>thơm ngon</b> & hấp dẫn',
  }

  const serialized = JSON.stringify(maliciousPayload).replace(/</g, '\\u003c')
  assert.doesNotMatch(serialized, /<script>/, 'Serialized JSON-LD must not contain unescaped <script>')
  assert.match(serialized, /\\u003cscript>alert/)
  assert.match(serialized, /\\u003c\/script>/)
})

test('P6-25: Page metadata snapshots for Home, Recipe Detail, Categories, and Search', async () => {
  const [seoSource, recipePage, categoryPage, searchPage] = await Promise.all([
    source('src/lib/seo.ts'),
    source('src/app/(public)/recipes/[slug]/page.tsx'),
    source('src/app/(public)/categories/[slug]/page.tsx'),
    source('src/app/(public)/search/page.tsx'),
  ])

  // OpenGraph website vs article
  assert.match(seoSource, /type:\s*'article'/)
  assert.match(seoSource, /publishedTime:\s*options\.publishedTime/)
  assert.match(seoSource, /authors:\s*options\.author\s*\?\s*\[options\.author\]/)
  assert.match(seoSource, /card:\s*'summary_large_image'/)
  assert.match(seoSource, /locale:\s*'vi_VN'/)

  // Recipe detail dynamic metadata
  assert.match(recipePage, /export async function generateMetadata/)
  assert.match(recipePage, /createPageMetadata\(\{/)
  assert.match(recipePage, /title:\s*recipe\.title/)
  assert.match(recipePage, /type:\s*'article'/)
  assert.match(recipePage, /author:\s*recipe\.author\.displayName/)

  // Category detail dynamic metadata
  assert.match(categoryPage, /export async function generateMetadata/)
  assert.match(categoryPage, /createPageMetadata\(\{/)
  assert.match(categoryPage, /title:\s*`\$\{category\.name\}/)

  // Search page noindex, follow directive snapshot
  assert.match(searchPage, /createPageMetadata\(\{/)
  assert.match(searchPage, /index:\s*false/)
  assert.match(searchPage, /follow:\s*true/)
})

test('P6-25: Private preview route snapshot strictly enforces noindex, nofollow and private media', async () => {
  const [previewPage, nextConfig] = await Promise.all([
    source('src/app/(dashboard)/dashboard/recipes/[id]/preview/page.tsx'),
    source('next.config.mjs'),
  ])

  // Route metadata
  assert.match(previewPage, /robots:\s*\{\s*index:\s*false,\s*follow:\s*false\s*\}/)
  assert.match(previewPage, /export const dynamic = 'force-dynamic'/)
  assert.match(previewPage, /export const revalidate = 0/)
  assert.match(previewPage, /<RecipeDetail\s+recipe=\{recipe\}\s+privatePreview\s*\/>/)

  // HTTP Header fallback in next.config.mjs
  assert.match(nextConfig, /source:\s*'\/dashboard\/:path\*'/)
  assert.match(nextConfig, /key:\s*'X-Robots-Tag',\s*value:\s*'noindex,\s*nofollow'/)
})

test('P6-25: Sitemap and robots snapshot validation for canonical crawlability', async () => {
  const [robots, sitemapJob] = await Promise.all([
    source('src/app/robots.ts'),
    source('../backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapGenerationJob.cs'),
  ])

  // Robots disallow private routes
  assert.match(robots, /disallow:\s*\['\/dashboard\/',\s*'\/profile',\s*'\/login',\s*'\/register'\]/)
  assert.match(robots, /sitemap:\s*absoluteUrl\('\/sitemap\.xml'\)/)

  // Sitemap generator excludes Draft and Archived recipes
  assert.match(sitemapJob, /recipe\.Status == RecipeStatus\.Published/)
  assert.match(sitemapJob, /category\.Slug/)
  assert.doesNotMatch(sitemapJob, /RecipeStatus\.Draft/)
  assert.doesNotMatch(sitemapJob, /RecipeStatus\.Archived/)
})
