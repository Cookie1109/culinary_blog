import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('content-client exports publishing lifecycle patch methods', async () => {
  const source = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  assert.match(source, /export async function publishRecipe/)
  assert.match(source, /\/recipes\/\$\{id\}\/publish/)
  assert.match(source, /export async function unpublishRecipe/)
  assert.match(source, /\/recipes\/\$\{id\}\/unpublish/)
  assert.match(source, /export async function archiveRecipe/)
  assert.match(source, /\/recipes\/\$\{id\}\/archive/)
  assert.match(source, /export async function unarchiveRecipe/)
  assert.match(source, /\/recipes\/\$\{id\}\/unarchive/)
  assert.match(source, /'If-Match': `"\$\{version\}"`/)
})

test('recipe dashboard components wire publish and unpublish actions', async () => {
  const components = [
    'src/features/culinary/pages/dashboard/RecipeEditor.tsx',
    'src/features/culinary/pages/dashboard/RecipeCompositionWizard.tsx',
    'src/features/culinary/pages/dashboard/MyRecipes.tsx',
  ]

  for (const path of components) {
    const source = await readFile(new URL(`../${path}`, import.meta.url), 'utf8')
    assert.match(source, /\bpublishRecipe\b/, `${path} must import and use publishRecipe`)
    assert.match(source, /\bunpublishRecipe\b/, `${path} must import and use unpublishRecipe`)
  }
})
