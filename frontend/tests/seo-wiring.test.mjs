import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

test('public routes expose canonical, Open Graph, Twitter, and index directives', async () => {
  const [seo, recipe, category, search] = await Promise.all([
    source('src/lib/seo.ts'),
    source('src/app/(public)/recipes/[slug]/page.tsx'),
    source('src/app/(public)/categories/[slug]/page.tsx'),
    source('src/app/(public)/search/page.tsx'),
  ])

  assert.match(seo, /alternates: \{ canonical \}/)
  assert.match(seo, /type: 'article'/)
  assert.match(seo, /card: 'summary_large_image'/)
  assert.match(seo, /robots: \{ index, follow \}/)
  assert.match(recipe, /export async function generateMetadata/)
  assert.match(category, /export async function generateMetadata/)
  assert.match(search, /index: false/)
  assert.match(search, /follow: true/)
})

test('published recipe emits complete and safely serialized Recipe JSON-LD', async () => {
  const [seo, recipe] = await Promise.all([
    source('src/lib/seo.ts'),
    source('src/app/(public)/recipes/[slug]/page.tsx'),
  ])

  for (const property of [
    "'@type': 'Recipe'",
    'recipeIngredient:',
    'recipeInstructions:',
    'author:',
    'prepTime:',
    'cookTime:',
    'totalTime:',
    'recipeYield:',
    'nutrition:',
  ]) {
    assert.match(seo, new RegExp(property))
  }
  assert.match(seo, /replace\(\/<\/g, '\\\\u003c'\)/)
  assert.match(recipe, /type="application\/ld\+json"/)
  assert.match(recipe, /serializeJsonLd\(createRecipeJsonLd\(recipe\)\)/)
})

test('private routes are excluded from crawling and the sitemap remains published-only', async () => {
  const [config, preview, sitemapJob] = await Promise.all([
    source('next.config.mjs'),
    source('src/app/(dashboard)/dashboard/recipes/[id]/preview/page.tsx'),
    source('../backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapGenerationJob.cs'),
  ])

  assert.match(config, /X-Robots-Tag/)
  assert.match(config, /noindex, nofollow/)
  assert.match(preview, /robots: \{ index: false, follow: false \}/)
  assert.match(sitemapJob, /recipe\.Status == RecipeStatus\.Published/)
  assert.match(sitemapJob, /\$"\/categories\/\{Uri\.EscapeDataString\(category\.Slug\)\}"/)
  assert.doesNotMatch(sitemapJob, /\$"\/recipes\?category=/)
})

test('robots points to the canonical sitemap and blocks private routes', async () => {
  const robots = await source('src/app/robots.ts')

  assert.match(robots, /disallow: \['\/dashboard\/', '\/profile', '\/login', '\/register'\]/)
  assert.match(robots, /absoluteUrl\('\/sitemap\.xml'\)/)
})
