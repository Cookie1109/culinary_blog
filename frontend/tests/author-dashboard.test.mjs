import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('system overview uses administrative counts and the shared managed list', async () => {
  const source = await readFile(
    new URL('../src/features/culinary/pages/dashboard/SystemOverview.tsx', import.meta.url),
    'utf8',
  )
  assert.match(source, /listAdminRecipes\(status, undefined, 1, 1\)/)
  assert.match(source, /Tổng quan hệ thống/)
  assert.match(source, /<MyRecipes systemOverview/)
  assert.match(source, /to="\/dashboard\/audit"/)
  assert.doesNotMatch(source, /listMyRecipes/)
})

test('dashboard sidebar keeps navigation states exclusive and links the user card to profile', async () => {
  const source = await readFile(
    new URL('../src/features/culinary/components/layout/DashboardLayout.tsx', import.meta.url),
    'utf8',
  )

  assert.match(
    source,
    /to: '\/dashboard\/recipes', label: 'Công thức của tôi', icon: BookOpen, end: true/,
    'The recipe list item must not stay active on the new-recipe route',
  )
  assert.match(source, /<Link\s+to="\/profile"\s+aria-label="Đi đến hồ sơ của tôi"/)
  assert.doesNotMatch(source, /Trang công khai/)
  assert.doesNotMatch(source, /SECONDARY_ITEMS/)
  assert.match(source, /to: '\/dashboard', label: 'Tổng quan', icon: LayoutDashboard, end: true/)
})

test('P7-02: content-client supports pagination and MyRecipes wires filter, table/card, and actions', async () => {
  const clientSource = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  assert.match(
    clientSource,
    /export function listMyRecipes\(status\?: RecipeStatus,\s*page:\s*number\s*=\s*1,\s*pageSize:\s*number\s*=\s*12,\s*q\?: string\)/,
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
  assert.match(
    editorSource,
    /useUnsavedChanges\(hasUnsavedChanges/,
    'Must register the shared navigation guard',
  )
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
  assert.match(myRecipesSource, /<Modal/, 'MyRecipes must render dialog')
  assert.match(myRecipesSource, /confirmDialog/, 'MyRecipes must manage confirmDialog state')
  assert.match(myRecipesSource, /Xác nhận xóa công thức/, 'MyRecipes must confirm deletion')

  // Wizard modal checks
  assert.match(wizardSource, /<Modal/, 'Wizard must render confirm dialog')
  assert.match(wizardSource, /Xác nhận xuất bản công thức/, 'Wizard must confirm publication')

  // Editor modal checks
  assert.match(editorSource, /<Modal/, 'Editor must render dialogs')
  assert.match(editorSource, /confirmModal/, 'Editor must manage confirmModal state')
})

test('P7-05: Validation summary checklist calculates criteria and provides deep link navigation', async () => {
  const [wizardSource, validationSource] = await Promise.all([
    readFile(
      new URL('../src/features/culinary/pages/dashboard/RecipeCompositionWizard.tsx', import.meta.url),
      'utf8',
    ),
    readFile(
      new URL('../src/features/culinary/pages/dashboard/recipe-validation.ts', import.meta.url),
      'utf8',
    ),
  ])

  // Verify computeRecipeValidation exists and checks all critical elements
  assert.match(validationSource, /export function computeRecipeValidation/)
  assert.match(validationSource, /recipe\.title.*length/)
  assert.match(validationSource, /recipe\.description.*length/)
  assert.match(validationSource, /recipe\.category/)
  assert.match(validationSource, /recipe\.prepTime/)
  assert.match(validationSource, /recipe\.ingredients\.length/)
  assert.match(validationSource, /recipe\.steps\.length/)
  assert.match(validationSource, /recipe\.images\.length/)

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

test('P7-20: Dashboard supports Author/Admin and the recipe list retains loading, empty and error states', async () => {
  const dashboardSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/Dashboard.tsx', import.meta.url),
    'utf8',
  )
  const recipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )

  assert.match(dashboardSource, /return <PersonalOverview \/>/)
  assert.doesNotMatch(dashboardSource, /SystemOverview|listAdminRecipes/)
  assert.doesNotMatch(dashboardSource, /navigate\(/)
  assert.match(dashboardSource, /role="status"/)

  assert.match(recipesSource, /recipesQuery\.isPending.*role="status"/s)
  assert.match(recipesSource, /recipesQuery\.isError.*role="alert"/s)
  assert.match(recipesSource, /recipes\.length === 0/)
  assert.match(recipesSource, /Thử lại/, 'Recipe list error state must be retryable')
})

test('SRS 5.1: Author dashboard retains personal totals, recent recipes and route access', async () => {
  const personal = await readFile(
    new URL('../src/features/culinary/pages/dashboard/PersonalOverview.tsx', import.meta.url),
    'utf8',
  )
  const route = await readFile(new URL('../src/app/(dashboard)/dashboard/page.tsx', import.meta.url), 'utf8')
  assert.match(personal, /listMyRecipes\(undefined, 1, 5\)/)
  for (const status of ['published', 'draft', 'archived'])
    assert.ok(personal.includes(`listMyRecipes('${status}', 1, 1)`))
  assert.match(personal, /Công thức gần đây/)
  assert.doesNotMatch(route, /redirect|roles.includes/)
})
