import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('public sitemap route streams the generated backend artifact without caching stale content', async () => {
  const source = await readFile(new URL('../src/app/sitemap.xml/route.ts', import.meta.url), 'utf8')

  assert.match(source, /\/sitemap\.xml/)
  assert.match(source, /cache: 'no-store'/)
  assert.match(source, /application\/xml/)
})

test('revalidation callback is secret-protected and invalidates public content paths', async () => {
  const source = await readFile(new URL('../src/app/api/revalidate/route.ts', import.meta.url), 'utf8')

  assert.match(source, /x-revalidation-secret/)
  assert.match(source, /timingSafeEqual/)
  assert.match(source, /const paths = \['\/', '\/recipes', '\/categories', '\/sitemap\.xml'\]/)
  assert.match(source, /revalidatePath\('\/recipes\/\[slug\]'/)
  assert.match(source, /revalidatePath\('\/categories\/\[slug\]'/)
  assert.match(source, /revalidateTag\(PUBLIC_CONTENT_TAG\)/)
})

test('robots advertises the canonical sitemap URL', async () => {
  const source = await readFile(new URL('../src/app/robots.ts', import.meta.url), 'utf8')

  assert.match(source, /sitemap: absoluteUrl\('\/sitemap\.xml'\)/)
})
