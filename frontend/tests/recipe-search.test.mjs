import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import test from 'node:test'
import vm from 'node:vm'
import ts from 'typescript'

const require = createRequire(import.meta.url)
function compile(file, dependencies = {}) {
  const source = readFileSync(new URL(file, import.meta.url), 'utf8')
  const js = ts.transpileModule(source, {
    compilerOptions: {
      target: ts.ScriptTarget.ES2020,
      module: ts.ModuleKind.CommonJS,
      jsx: ts.JsxEmit.ReactJSX,
      esModuleInterop: true,
    },
  }).outputText
  const exports = {}
  vm.runInNewContext(`(function(require,exports){${js}\n})`, { console, URLSearchParams })(
    (name) => dependencies[name] ?? require(name),
    exports,
  )
  return exports
}
const search = compile('../src/lib/recipe-search.ts')

test('Vietnamese keyword normalization handles combining marks, spaces, uppercase and đ', () => {
  assert.equal(search.normalizeRecipeSearch('  gà   nướng  '), 'gà nướng')
  assert.equal(search.foldVietnamese('ĐẬU ĐỎ'), 'dau do')
  assert.equal(search.foldVietnamese('PHỞ BÒ'), 'pho bo')
})

test('highlights retain the original accents without highlighting gạo when gà is present', () => {
  const parts = search.recipeSearchParts('Gạo nếp và mỡ gà.', 'ga')
  assert.equal(
    parts
      .filter((part) => part.matched)
      .map((part) => part.value)
      .join('|'),
    'gà',
  )
  assert.equal(parts.map((part) => part.value).join(''), 'Gạo nếp và mỡ gà.')
  assert.equal(
    search
      .recipeSearchParts('Phở bò', 'ph')
      .filter((part) => part.matched)
      .map((part) => part.value)
      .join('|'),
    'Phở',
  )
})

test('match labels explain description, combined fields and approximate results', () => {
  assert.equal(search.recipeSearchMatch('Gà kho', 'Gà ta kho gừng.', 'ga'), '')
  assert.equal(search.recipeSearchMatch('Xôi xéo', 'Mỡ gà.', 'ga'), 'Khớp mô tả')
  assert.equal(search.recipeSearchMatch('Phở gà', 'Nước dùng gừng nướng.', 'ga nuong'), 'Khớp tên và mô tả')
  assert.equal(search.recipeSearchMatch('Gà nướng', 'Gà ta.', 'ga nuongg'), 'Tên món gần đúng')
  assert.equal(search.recipeSearchMatch('Cà ri', 'Rau củ.', 'cá'), 'Khớp không dấu')
})

test('description snippets expose a keyword near the end of long content', () => {
  const text = `${'Món ăn thơm ngon. '.repeat(16)}Có mỡ gà ở cuối.`
  const snippet = search.recipeSearchSnippet(text, 'ga')
  assert.ok(snippet.startsWith('… '))
  assert.ok(snippet.includes('gà'))
  assert.ok(snippet.length <= 164)
})

function pageHarness(fail = false) {
  const requests = []
  const result = {
    data: [],
    meta: { page: 1, pageSize: 12, total: 0, totalPages: 0, hasNextPage: false, hasPreviousPage: false },
  }
  const page = compile('../src/app/(public)/recipes/page.tsx', {
    '@/lib/recipe-search': search,
    '@/lib/seo': { createPageMetadata: (value) => value },
    '@/features/culinary/pages/RecipesList': { RecipesList: () => null },
    '@/lib/api/public-content-server': {
      searchPublishedRecipesOnServer: async (filters) => {
        requests.push(filters)
        if (fail) throw new Error('offline')
        return result
      },
      getPublicCategories: async () => [],
    },
  }).default
  return { page, requests }
}

test('SSR defaults keyword search to relevance and browsing to newest, preserving explicit sort', async () => {
  const h = pageHarness()
  await h.page({ searchParams: Promise.resolve({ q: ' gà  nướng ' }) })
  assert.equal(h.requests[0].q, 'gà nướng')
  assert.equal(h.requests[0].sort, 'relevance')
  await h.page({ searchParams: Promise.resolve({}) })
  assert.equal(h.requests[1].sort, 'newest')
  await h.page({ searchParams: Promise.resolve({ q: 'ga', sort: 'quickest' }) })
  assert.equal(h.requests[2].sort, 'quickest')
})

test('SSR rejects invalid short query locally and renders retry state on upstream failure', async () => {
  const invalid = pageHarness()
  const empty = await invalid.page({ searchParams: Promise.resolve({ q: 'á' }) })
  assert.equal(invalid.requests.length, 0)
  assert.match(empty.props.initialSearchError, /ít nhất 2/)
  const offline = pageHarness(true)
  const failed = await offline.page({ searchParams: Promise.resolve({ q: 'ga', category: 'mon-xao' }) })
  assert.equal(failed.props.loadError, true)
  assert.equal(offline.requests[0].category, 'mon-xao')
})

test('pagination stays bounded while exposing first, last and adjacent pages', () => {
  assert.equal(search.recipeSearchPages(50, 100).join(','), '1,49,50,51,100')
  assert.equal(search.recipeSearchPages(1, 2).join(','), '1,2')
  assert.equal(search.recipeSearchPages(1, 0).length, 0)
})

test('keyword validation agrees for URL and form input without sending punctuation-only requests', () => {
  assert.match(search.recipeSearchValidation('@@'), /chữ hoặc số/)
  assert.match(search.recipeSearchValidation('á'), /ít nhất 2/)
  assert.match(search.recipeSearchValidation('a'.repeat(101)), /100/)
  assert.equal(search.recipeSearchValidation('   '), '')
  assert.equal(search.recipeSearchValidation('gà'), '')
})
