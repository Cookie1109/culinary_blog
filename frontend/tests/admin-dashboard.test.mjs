import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'

test('P7-07: CategoryAdmin provides complete CRUD, orderIndex ordering, delete conflict UX, and accessible confirm dialog', async () => {
  const source = await readFile(
    new URL('../src/features/culinary/pages/dashboard/CategoryAdmin.tsx', import.meta.url),
    'utf8',
  )

  // Verify full CRUD mutations & queries
  assert.match(source, /listCategories/, 'Must query categories')
  assert.match(source, /createCategory/, 'Must wire category creation')
  assert.match(source, /updateCategory/, 'Must wire category update')
  assert.match(source, /deleteCategory/, 'Must wire category deletion')

  // Verify Order Index column and sorting
  assert.match(source, /orderIndex/, 'Must handle orderIndex')
  assert.match(source, /Thứ tự/, 'Must display order column header')
  assert.match(source, /sortedCategories/, 'Must sort categories by orderIndex')

  // Verify delete conflict detection & warning
  assert.match(source, /recipeCount > 0/, 'Must detect category delete conflict when recipes exist')
  assert.match(source, /Xung đột xóa danh mục|Xung đột dữ liệu/, 'Must explain delete conflict')

  // Verify accessible modal confirm dialog
  assert.match(source, /role="dialog"/, 'Must render accessible dialog')
  assert.match(source, /aria-modal="true"/, 'Must specify aria-modal')
  assert.match(source, /aria-labelledby="confirm-delete-title"/, 'Must have accessible label')
  assert.match(source, /deleteTarget/, 'Must track delete target entity state')
})

test('P7-08: Admin recipe listing and actions wire /admin/recipes, author display, and audit-compliant mutations', async () => {
  const clientSource = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  assert.match(
    clientSource,
    /export function listAdminRecipes/,
    'content-client must export listAdminRecipes',
  )
  assert.match(clientSource, /\/admin\/recipes/, 'listAdminRecipes must target /admin/recipes')
  assert.match(
    clientSource,
    /export async function getAdminRecipe/,
    'content-client must export getAdminRecipe',
  )

  const myRecipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )

  // Verify Admin scope toggle and query
  assert.match(myRecipesSource, /listAdminRecipes/, 'MyRecipes must import and call listAdminRecipes')
  assert.match(myRecipesSource, /scope === 'all'/, 'MyRecipes must support admin all-recipes scope')
  assert.match(myRecipesSource, /'admin-recipes'/, 'Must have admin-recipes query key')

  // Verify author information display in admin mode
  assert.match(myRecipesSource, /recipe\.author/, 'Must display recipe author in admin scope')
  assert.match(myRecipesSource, /Tác giả/, 'Must have author column header in admin mode')

  // Verify mutation invalidations cover both user and admin queries
  assert.match(myRecipesSource, /invalidateQueries\(\{\s*queryKey:\s*\['admin-recipes'\]\s*\}\)/)
})

test('P7-10: Dashboard layout and configuration wire Hangfire operations for admin', async () => {
  const layoutSource = await readFile(
    new URL('../src/features/culinary/components/layout/DashboardLayout.tsx', import.meta.url),
    'utf8',
  )

  assert.match(layoutSource, /Hangfire Jobs/, 'Dashboard layout must provide Hangfire Jobs link for admin')
  assert.match(layoutSource, /\/jobs/, 'Hangfire link must target /jobs')
  assert.match(layoutSource, /user\?\.role === 'admin'/, 'Hangfire link must be gated to admin users')

  const filterSource = await readFile(
    new URL(
      '../../backend/src/CulinaryBlog.Api/Presentation/AdminDashboardAuthorizationFilter.cs',
      import.meta.url,
    ),
    'utf8',
  )
  assert.match(filterSource, /IsInRole\("Admin"\)/, 'Hangfire authorization must verify Admin role')
  assert.match(
    filterSource,
    /IsPrivateNetwork|IsLoopback/,
    'Hangfire authorization must enforce network restrictions',
  )
})

test('P7-11: Admin application audit/log view is implemented with telemetry security policies', async () => {
  const clientSource = await readFile(new URL('../src/lib/api/content-client.ts', import.meta.url), 'utf8')
  assert.match(clientSource, /export function listAuditLogs/, 'content-client must export listAuditLogs')
  assert.match(clientSource, /\/admin\/audit-logs/, 'listAuditLogs must query /admin/audit-logs')

  const auditViewSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/AuditLogAdmin.tsx', import.meta.url),
    'utf8',
  )

  // Verify admin authorization gate
  assert.match(auditViewSource, /user\?\.role !== 'admin'/, 'AuditLogAdmin must check admin role')

  // Verify table and filters
  assert.match(auditViewSource, /Nhật ký kiểm toán/, 'Must render audit log header')
  assert.match(auditViewSource, /Correlation ID|Mã tương quan/, 'Must display correlation ID')
  assert.match(auditViewSource, /copyToClipboard/, 'Must support copying correlation ID')
  assert.match(auditViewSource, /LEVEL_FILTERS/, 'Must provide level filters')
  assert.match(auditViewSource, /role="dialog"/, 'Must provide detail modal dialog')

  // Verify operational telemetry policy notice
  assert.match(
    auditViewSource,
    /Raw Seq.*OTLP.*telemetry.*operational authentication.*network control|Chính sách an toàn Telemetry/i,
    'Must display operational notice regarding Seq and OTLP network restrictions',
  )

  // Verify page route
  const pageRouteSource = await readFile(
    new URL('../src/app/(dashboard)/dashboard/audit/page.tsx', import.meta.url),
    'utf8',
  )
  assert.match(pageRouteSource, /AuditLogAdmin/, 'Dashboard audit route must export AuditLogAdmin')
})

test('P7-18/P7-19: Direct navigation and admin scope remain role-gated', async () => {
  const middlewareSource = await readFile(new URL('../src/middleware.ts', import.meta.url), 'utf8')
  const authSource = await readFile(new URL('../src/auth.ts', import.meta.url), 'utf8')
  const layoutSource = await readFile(
    new URL('../src/features/culinary/components/layout/DashboardLayout.tsx', import.meta.url),
    'utf8',
  )
  const recipesSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/MyRecipes.tsx', import.meta.url),
    'utf8',
  )
  const categorySource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/CategoryAdmin.tsx', import.meta.url),
    'utf8',
  )
  const auditSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/AuditLogAdmin.tsx', import.meta.url),
    'utf8',
  )

  assert.match(
    middlewareSource,
    /matcher:\s*\[.*\/dashboard\/:path\*/s,
    'Direct dashboard URLs must pass Auth.js middleware',
  )
  assert.match(authSource, /authorized\(\{ auth \}\).*Boolean\(auth\?\.accessToken && !auth\.error\)/s)
  assert.match(layoutSource, /user\?\.role === 'admin'/, 'Admin navigation must be hidden from authors')
  assert.match(
    recipesSource,
    /isAdmin && scopeParam === 'all'/,
    'Query-string scope escalation must require Admin',
  )
  assert.match(categorySource, /if \(user\?\.role !== 'admin'\)/)
  assert.match(categorySource, /Chỉ quản trị viên có thể quản lý danh mục/)
  assert.match(auditSource, /if \(user\?\.role !== 'admin'\)/)
  assert.match(auditSource, /Chỉ quản trị viên có thể truy cập nhật ký kiểm toán/)
})

test('P7-20: Admin screens cover loading, empty, error, and permission-denied states', async () => {
  const categorySource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/CategoryAdmin.tsx', import.meta.url),
    'utf8',
  )
  const auditSource = await readFile(
    new URL('../src/features/culinary/pages/dashboard/AuditLogAdmin.tsx', import.meta.url),
    'utf8',
  )

  assert.match(categorySource, /categories\.isPending.*role="status"/s)
  assert.match(categorySource, /categories\.isError.*role="alert"/s)
  assert.match(categorySource, /sortedCategories\.length === 0/)
  assert.match(categorySource, /user\?\.role !== 'admin'.*role="alert"/s)

  assert.match(auditSource, /logsQuery\.isPending.*role="status"/s)
  assert.match(auditSource, /logsQuery\.isError.*role="alert"/s)
  assert.match(auditSource, /logs\.length === 0/)
  assert.match(auditSource, /user\?\.role !== 'admin'.*role="alert"/s)
})
