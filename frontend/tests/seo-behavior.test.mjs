import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import vm from 'node:vm'
import test from 'node:test'
import ts from 'typescript'
import * as jsxRuntime from 'react/jsx-runtime'
import { renderToStaticMarkup } from 'react-dom/server'
import { JSDOM } from 'jsdom'

const compiled = ts.transpileModule(readFileSync(new URL('../src/lib/seo.ts', import.meta.url), 'utf8'), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText
const moduleExports = {}
vm.runInNewContext(compiled, {
  exports: moduleExports,
  URL,
  process: { env: { SITE_URL: 'https://food.example' } },
  require: () => ({ publicMediaUrl: (value) => value.replace('/api/v1/media/', '/api/public-media/') }),
})

test('the real recipe page renders parseable JSON-LD without allowing a closing-script injection', async () => {
  const recipe = {
    title: 'Phở </script><script>alert(1)</script>',
    slug: 'pho',
    description: 'Phở bò',
    prepTime: 10,
    cookTime: 20,
    servings: 2,
    author: { displayName: 'Tác giả' },
    category: { name: 'Món nước' },
    ingredients: [],
    steps: [],
    images: [],
    nutrition: null,
  }
  const pageExports = {}
  const page = ts.transpileModule(
    readFileSync(new URL('../src/app/(public)/recipes/[slug]/page.tsx', import.meta.url), 'utf8'),
    {
      compilerOptions: {
        module: ts.ModuleKind.CommonJS,
        target: ts.ScriptTarget.ES2022,
        jsx: ts.JsxEmit.ReactJSX,
      },
    },
  ).outputText
  vm.runInNewContext(page, {
    exports: pageExports,
    require: (name) => {
      if (name === 'react/jsx-runtime') return jsxRuntime
      if (name === '@/lib/seo') return moduleExports
      if (name.endsWith('/RecipeDetail')) return { RecipeDetail: () => null }
      if (name.endsWith('/public-content-server'))
        return {
          getPublishedRecipe: async (slug, ttl) => {
            assert.equal(slug, 'pho')
            assert.equal(ttl, 300)
            return recipe
          },
        }
      if (name === 'next/navigation')
        return {
          notFound: () => {
            throw new Error('Unexpected 404')
          },
        }
      throw new Error(`Unexpected page dependency: ${name}`)
    },
  })
  const markup = renderToStaticMarkup(await pageExports.default({ params: Promise.resolve({ slug: 'pho' }) }))
  const dom = new JSDOM(markup)
  try {
    assert.equal(dom.window.document.querySelectorAll('script').length, 1)
    const script = dom.window.document.querySelector('script[type="application/ld+json"]')
    assert.equal(JSON.parse(script.textContent).name, recipe.title)
    assert.ok(!script.textContent.includes('</script>'))
  } finally {
    dom.window.close()
  }
})

test('metadata bounds the complete branded title and provides a meaningful 150–160 character description', () => {
  for (const title of ['Phở bò', 'Món ăn rất ngon '.repeat(20)]) {
    for (const description of [
      'Món phở bò truyền thống.',
      'Mô tả công thức với nguyên liệu tươi ngon. '.repeat(20),
    ]) {
      const metadata = moduleExports.createPageMetadata({
        title,
        description,
        path: '/recipes/pho-bo',
        type: 'article',
      })
      assert.ok(metadata.title.absolute.length <= 60)
      assert.ok(metadata.title.absolute.endsWith(' | Culinary Blog'))
      assert.ok(metadata.description.length >= 150 && metadata.description.length <= 160)
      assert.equal(metadata.openGraph.title, metadata.title.absolute)
      assert.equal(metadata.twitter.description, metadata.description)
      assert.equal(metadata.alternates.canonical, 'https://food.example/recipes/pho-bo')
      assert.equal(metadata.openGraph.images[0].width, 1200)
      assert.equal(metadata.openGraph.images[0].height, 630)
    }
  }
  const page = moduleExports.createPageMetadata({
    title: 'Công thức – Trang 2',
    description: '',
    path: '/recipes?page=2',
  })
  assert.ok(page.title.absolute.includes('Trang 2'))
  assert.equal(page.alternates.canonical, 'https://food.example/recipes?page=2')
  const privatePage = moduleExports.createPageMetadata({
    title: 'Bản nháp',
    description: '',
    path: '/dashboard',
    index: false,
  })
  assert.equal(privatePage.robots.index, false)
  assert.equal(privatePage.robots.follow, false)
})

test('Recipe JSON-LD uses actual values, preserves original images after resize failure and escapes script content', () => {
  const recipe = {
    title: 'Phở </script><script>alert(1)</script>',
    slug: 'pho',
    description: 'Phở bò',
    prepTime: 10,
    cookTime: 0,
    servings: 2,
    publishedAt: '2026-10-25T00:00:00Z',
    author: { displayName: 'Tác giả' },
    category: { name: 'Món nước' },
    ingredients: [{ name: 'Muối', quantity: null, unit: null, notes: 'vừa đủ', orderIndex: 0 }],
    steps: [{ stepNumber: 1, title: 'Trộn', description: 'Trộn đều.', imageUrl: null }],
    images: [
      {
        originalUrl: '/api/v1/media/one/original',
        mediumUrl: null,
        isPrimary: true,
        orderIndex: 0,
        processingStatus: 'failed',
      },
    ],
    nutrition: { calories: 120, protein: null, sodium: 0 },
  }
  const value = JSON.parse(moduleExports.serializeJsonLd(moduleExports.createRecipeJsonLd(recipe)))
  assert.equal(value.prepTime, 'PT10M')
  assert.equal(value.cookTime, 'PT0M')
  assert.equal(value.totalTime, 'PT10M')
  assert.equal(value.recipeYield, '2 khẩu phần')
  assert.deepEqual(value.recipeIngredient, ['Muối vừa đủ'])
  assert.equal(value.recipeInstructions[0].text, 'Trộn đều.')
  assert.equal(value.image[0], 'https://food.example/api/public-media/one/original')
  assert.equal(value.nutrition.calories, '120 kcal')
  assert.equal(value.nutrition.sodiumContent, '0 mg')
  assert.equal(value.nutrition.proteinContent, undefined)
  assert.equal(value.aggregateRating, undefined)
  assert.ok(!moduleExports.serializeJsonLd(value).includes('<'))
  recipe.images = []
  recipe.nutrition = null
  const withoutOptionalData = JSON.parse(
    moduleExports.serializeJsonLd(moduleExports.createRecipeJsonLd(recipe)),
  )
  assert.equal(withoutOptionalData.nutrition, undefined)
  assert.equal(withoutOptionalData.image[0], 'https://food.example/opengraph-image')
})
