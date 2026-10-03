import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('P7-01: Dashboard queries real API data and does not use hardcoded fixtures', async () => {
  const source = await readFile(
    new URL('../src/features/culinary/pages/dashboard/Dashboard.tsx', import.meta.url),
    'utf8',
  )

  // Verify listMyRecipes is imported and called for recent recipes and counts
  assert.match(source, /import\s+.*listMyRecipes.*from\s+['"]@\/lib\/api\/content-client['"]/)
  assert.match(source, /listMyRecipes\(undefined,\s*1,\s*5\)/, 'Must fetch recent recipes')
  assert.match(source, /listMyRecipes\('published',\s*1,\s*1\)/, 'Must count published recipes')
  assert.match(source, /listMyRecipes\('draft',\s*1,\s*1\)/, 'Must count draft recipes')
  assert.match(source, /listMyRecipes\('archived',\s*1,\s*1\)/, 'Must count archived recipes')

  // Verify hardcoded RECENT_RECIPES is gone
  assert.doesNotMatch(source, /const RECENT_RECIPES\s*=/, 'Dashboard must not use static mock recipes')

  // Verify loading, error, and empty states
  assert.match(source, /role="status"/, 'Must have loading status element')
  assert.match(source, /role="alert"/, 'Must have error alert element')
  assert.match(source, /Bạn chưa có công thức nào/, 'Must have empty state description')
})

test('P7-02: content-client supports pagination and MyRecipes wires filter, table/card, and actions', async () => {
  const clientSource = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  assert.match(
    clientSource,
    /export function listMyRecipes\(status\?: RecipeStatus,\s*page:\s*number\s*=\s*1,\s*pageSize:\s*number\s*=\s*12\)/,
    'listMyRecipes must accept status, page, and pageSize',
  )

  const myRecipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )

  // Verify status filter options
  assert.match(myRecipesSource, /'published'/)
  assert.match(myRecipesSource, /'draft'/)
  assert.match(myRecipesSource, /'archived'/)

  // Verify pagination controls
  assert.match(myRecipesSource, /totalPages/, 'Must compute or use totalPages')
  assert.match(myRecipesSource, /Trang \{page\} \/ \{totalPages\}/, 'Must show page indicator')
  assert.match(myRecipesSource, /setPage/, 'Must have page state setter')

  // Verify responsive table and card layouts
  assert.match(myRecipesSource, /hidden md:block/, 'Must have desktop table container')
  assert.match(myRecipesSource, /md:hidden/, 'Must have mobile card container')

  // Verify valid actions
  assert.match(myRecipesSource, /publishRecipe/, 'Must wire publish')
  assert.match(myRecipesSource, /unpublishRecipe/, 'Must wire unpublish')
  assert.match(myRecipesSource, /archiveRecipe/, 'Must wire archive')
  assert.match(myRecipesSource, /unarchiveRecipe/, 'Must wire unarchive')
  assert.match(myRecipesSource, /deleteRecipe/, 'Must wire delete')
})

test('P7-03: RecipeEditor provides 5-step wizard, save state tracking, and navigation guard', async () => {
  const editorSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeEditor.tsx', import.meta.url),
    'utf8',
  )

  // Verify wizard steps
  assert.match(editorSource, /'basic'/)
  assert.match(editorSource, /'ingredients'/)
  assert.match(editorSource, /'steps'/)
  assert.match(editorSource, /'images'/)
  assert.match(editorSource, /'preview'/)

  // Verify save state
  assert.match(editorSource, /isDirty/, 'Must track dirty state')
  assert.match(editorSource, /Có thay đổi chưa lưu/, 'Must alert on unsaved changes')
  assert.match(editorSource, /Đã lưu bản nháp/, 'Must indicate successful save')

  // Verify navigation guard
  assert.match(editorSource, /addEventListener\('beforeunload'/, 'Must wire beforeunload guard')
  assert.match(editorSource, /navGuardTarget/, 'Must have in-app navigation guard dialog state')
  assert.match(editorSource, /Có thay đổi chưa lưu/, 'Must show in-app guard message')
})

test('P7-04: Confirmation UX is implemented for publish, unpublish, archive, unarchive, and soft-delete', async () => {
  const myRecipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )
  const wizardSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeCompositionWizard.tsx', import.meta.url),
    'utf8',
  )
  const editorSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeEditor.tsx', import.meta.url),
    'utf8',
  )

  // MyRecipes modal checks
  assert.match(myRecipesSource, /role="dialog"/, 'MyRecipes must render dialog')
  assert.match(myRecipesSource, /confirmDialog/, 'MyRecipes must manage confirmDialog state')
  assert.match(myRecipesSource, /Xác nhận xóa công thức/, 'MyRecipes must confirm deletion')

  // Wizard modal checks
  assert.match(wizardSource, /role="dialog"/, 'Wizard must render confirm dialog')
  assert.match(wizardSource, /Xác nhận xuất bản công thức/, 'Wizard must confirm publication')

  // Editor modal checks
  assert.match(editorSource, /role="dialog"/, 'Editor must render dialogs')
  assert.match(editorSource, /confirmModal/, 'Editor must manage confirmModal state')
})

test('P7-05: Validation summary checklist calculates criteria and provides deep link navigation', async () => {
  const wizardSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeCompositionWizard.tsx', import.meta.url),
    'utf8',
  )

  // Verify computeRecipeValidation exists and checks all critical elements
  assert.match(wizardSource, /export function computeRecipeValidation/)
  assert.match(wizardSource, /recipe\.title.*length/)
  assert.match(wizardSource, /recipe\.description.*length/)
  assert.match(wizardSource, /recipe\.category/)
  assert.match(wizardSource, /recipe\.prepTime/)
  assert.match(wizardSource, /recipe\.ingredients\.length/)
  assert.match(wizardSource, /recipe\.steps\.length/)
  assert.match(wizardSource, /recipe\.images\.length/)

  // Verify deep links to wizard steps
  assert.match(wizardSource, /onNavigateToStep/)
  assert.match(wizardSource, /Chuyển đến: \{item\.stepLabel\}/)

  const editorSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeEditor.tsx', import.meta.url),
    'utf8',
  )
  assert.match(editorSource, /handleDeepLink/, 'Editor must provide handleDeepLink function')
  assert.match(editorSource, /Tổng hợp điều kiện xuất bản/, 'Editor must render validation summary banner')
})

test('P7-06: Concurrency conflict UX provides reload, discard, and clipboard preservation', async () => {
  const editorSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeEditor.tsx', import.meta.url),
    'utf8',
  )

  assert.match(editorSource, /RECIPE_CONCURRENCY_CONFLICT/, 'Must detect concurrency conflict code')
  assert.match(editorSource, /conflictModalOpen/, 'Must toggle conflict modal')
  assert.match(editorSource, /Tải lại phiên bản mới nhất/, 'Must offer reload server version action')
  assert.match(editorSource, /Hủy thay đổi cục bộ/, 'Must offer discard local changes action')
  assert.match(editorSource, /Sao chép nội dung cục bộ/, 'Must offer copy local edits action')
})

test('P7-17: Author UI wires the complete recipe lifecycle to real API operations', async () => {
  const clientSource = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  const editorSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/RecipeEditor.tsx', import.meta.url),
    'utf8',
  )
  const recipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )

  for (const operation of [
    'createRecipe',
    'updateRecipe',
    'createIngredient',
    'createStep',
    'publishRecipe',
    'unpublishRecipe',
    'archiveRecipe',
    'unarchiveRecipe',
    'deleteRecipe',
  ]) {
    assert.match(
      clientSource,
      new RegExp(`export (?:async )?function ${operation}`),
      `${operation} must be exported`,
    )
  }

  assert.match(editorSource, /createRecipe/, 'Editor must create drafts through the API client')
  assert.match(editorSource, /updateRecipe/, 'Editor must save edits through the API client')
  assert.match(recipesSource, /publishRecipe/)
  assert.match(recipesSource, /unpublishRecipe/)
  assert.match(recipesSource, /archiveRecipe/)
  assert.match(recipesSource, /unarchiveRecipe/)
  assert.match(recipesSource, /deleteRecipe/)
})

test('P7-20: Author dashboard surfaces loading, empty, error, and unauthenticated states', async () => {
  const dashboardSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/Dashboard.tsx', import.meta.url),
    'utf8',
  )
  const recipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )

  assert.match(dashboardSource, /if \(!user\)/, 'Dashboard must deny an unauthenticated render')
  assert.match(dashboardSource, /navigate\('\/login'\)/, 'Dashboard denial must link to login')
  assert.match(
    dashboardSource,
    /isLoading.*role="status"/s,
    'Dashboard must expose an accessible loading state',
  )
  assert.match(dashboardSource, /isError.*role="alert"/s, 'Dashboard must expose an accessible error state')
  assert.match(dashboardSource, /recentRecipes\.length === 0/, 'Dashboard must expose an empty state')

  assert.match(recipesSource, /recipesQuery\.isPending.*role="status"/s)
  assert.match(recipesSource, /recipesQuery\.isError.*role="alert"/s)
  assert.match(recipesSource, /filtered\.length === 0/)
  assert.match(recipesSource, /Thử lại/, 'Recipe list error state must be retryable')
})
