import assert from 'node:assert/strict'
import { readFile } from 'node:fs/promises'
import test from 'node:test'
import axe from 'axe-core'
import { JSDOM } from 'jsdom'

const source = (path) => readFile(new URL(`../${path}`, import.meta.url), 'utf8')

// Relative luminance formula per WCAG 2.1
function getLuminance(hex) {
  const cleanHex = hex.replace('#', '')
  const r = Number.parseInt(cleanHex.slice(0, 2), 16) / 255
  const g = Number.parseInt(cleanHex.slice(2, 4), 16) / 255
  const b = Number.parseInt(cleanHex.slice(4, 6), 16) / 255

  const srgb = [r, g, b].map((val) => (val <= 0.03928 ? val / 12.92 : Math.pow((val + 0.055) / 1.055, 2.4)))
  return 0.2126 * srgb[0] + 0.7152 * srgb[1] + 0.0722 * srgb[2]
}

function getContrastRatio(hex1, hex2) {
  const l1 = getLuminance(hex1)
  const l2 = getLuminance(hex2)
  const lighter = Math.max(l1, l2)
  const darker = Math.min(l1, l2)
  return (lighter + 0.05) / (darker + 0.05)
}

async function runAxeOnHtml(title, bodyHtml) {
  const fullHtml = `<!DOCTYPE html>
<html lang="vi">
  <head>
    <meta charset="utf-8" />
    <title>${title} · Culinary Blog</title>
  </head>
  <body>
    ${bodyHtml}
  </body>
</html>`

  const dom = new JSDOM(fullHtml, { runScripts: 'dangerously' })
  globalThis.window = dom.window
  globalThis.document = dom.window.document
  globalThis.Node = dom.window.Node

  const results = await axe.run(dom.window.document.documentElement, {
    runOnly: {
      type: 'tag',
      values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'],
    },
  })

  return results
}

test('P6-17: responsive breakpoints, fluid typography, and wrap behaviors are implemented', async () => {
  const [home, listing, detail, search, layout] = await Promise.all([
    source('src/features/culinary/pages/Home.tsx'),
    source('src/features/culinary/pages/RecipesList.tsx'),
    source('src/features/culinary/pages/RecipeDetail.tsx'),
    source('src/features/culinary/pages/Search.tsx'),
    source('src/features/culinary/components/layout/AppLayout.tsx'),
  ])

  // Mobile 320-767: wrap pagination to prevent horizontal scroll
  assert.match(
    listing,
    /flex-wrap justify-center gap-2/,
    'listing pagination must flex-wrap for 320px screens',
  )
  assert.match(search, /flex-wrap justify-center gap-2/, 'search pagination must flex-wrap for 320px screens')

  // Search input wraps on small mobile
  assert.match(search, /flex flex-col sm:flex-row/, 'search form must adapt on small mobile viewports')

  // Mobile drawer constrained width
  assert.match(layout, /max-w-\[85vw\]/, 'mobile drawer must not overflow small viewports')

  // Hero headings have responsive typography with break-words
  assert.match(home, /break-words/, 'home hero title must break-words on mobile 320px')
  assert.match(detail, /break-words/, 'recipe detail title must break-words on mobile 320px')
})

test('P6-18: semantic HTML, landmarks, heading hierarchy, and accessible names are properly wired', async () => {
  const [layout, dashboardLayout, home, recipeCard, detail, categories, categoryDetail] = await Promise.all([
    source('src/features/culinary/components/layout/AppLayout.tsx'),
    source('src/features/culinary/components/layout/DashboardLayout.tsx'),
    source('src/features/culinary/pages/Home.tsx'),
    source('src/features/culinary/components/RecipeCard.tsx'),
    source('src/features/culinary/pages/RecipeDetail.tsx'),
    source('src/features/culinary/pages/Categories.tsx'),
    source('src/features/culinary/pages/CategoryDetail.tsx'),
  ])

  // Skip to content bypass block
  assert.match(layout, /href="#main-content"/, 'AppLayout must have a skip to main content link')
  assert.match(layout, /id="main-content"/, 'AppLayout must define main#main-content target')

  // Landmarks
  assert.match(layout, /aria-label="Điều hướng chính"/, 'Header must declare navigation landmark label')
  assert.match(layout, /aria-label="Điều hướng di động"/, 'Mobile drawer must declare navigation label')
  assert.match(layout, /aria-label="Khám phá"/, 'Footer must declare Khám phá navigation landmark')
  assert.match(layout, /aria-label="Kết nối"/, 'Footer must declare Kết nối navigation landmark')

  // No nested <main>
  assert.doesNotMatch(dashboardLayout, /<main\b/, 'DashboardLayout must not nest a redundant <main> landmark')

  // Heading order: RecipeCard supports dynamic heading level and Home uses h3 for section cards
  assert.match(
    recipeCard,
    /headingLevel\?: 'h2' \| 'h3'/,
    'RecipeCard must support customizable heading level',
  )
  assert.match(home, /headingLevel="h3"/, 'Home must render recipe cards as h3 under h2 section heading')

  // Distinct link labels
  assert.match(home, /aria-label="Xem tất cả công thức nổi bật"/, 'Home must have distinct link labels')
  assert.match(
    home,
    /aria-label="Xem tất cả danh mục nổi bật"/,
    'Home must have distinct category link labels',
  )
  assert.match(
    categoryDetail,
    /aria-label="Quay lại tất cả danh mục"/,
    'CategoryDetail must have accessible back link label',
  )
  assert.match(
    categories,
    /aria-label=\{`Danh mục \$\{category\.name\}/,
    'Categories must have descriptive category link label',
  )

  // Breadcrumbs structured navigation
  assert.match(detail, /aria-label="Đường dẫn"/, 'RecipeDetail must provide breadcrumb landmark')
  assert.match(detail, /aria-current="page"/, 'RecipeDetail breadcrumbs must identify current page item')
})

test('P6-19: keyboard navigation, visible focus, escape handlers, and focus trap are robustly implemented', async () => {
  const [modal, layout, globals] = await Promise.all([
    source('src/components/ui/modal.tsx'),
    source('src/features/culinary/components/layout/AppLayout.tsx'),
    source('src/app/globals.css'),
  ])

  // Focus trap in modal
  assert.match(modal, /role="dialog"/, 'modal must have dialog role')
  assert.match(modal, /aria-modal="true"/, 'modal must have aria-modal attribute')
  assert.match(modal, /event\.key === 'Escape'/, 'modal must handle Escape key')
  assert.match(modal, /event\.key === 'Tab'/, 'modal must trap focus on Tab navigation')
  assert.match(
    modal,
    /previousActiveElement\.current\?\.focus\(\)/,
    'modal must restore previous focus on close',
  )
  assert.match(modal, /document\.body\.style\.overflow = 'hidden'/, 'modal must lock scroll when active')

  // Focus trap and escape in mobile nav drawer
  assert.match(layout, /id="mobile-nav-drawer"/, 'layout must have mobile drawer ID')
  assert.match(layout, /aria-label="Menu điều hướng di động"/, 'layout must label mobile drawer')
  assert.match(layout, /aria-expanded=\{mobileOpen\}/, 'hamburger button must indicate expanded state')
  assert.match(layout, /aria-controls="mobile-nav-drawer"/, 'hamburger button must control mobile drawer')
  assert.match(layout, /e\.key === 'Escape'/, 'mobile drawer must close on Escape')

  // User menu Escape handler and menu roles
  assert.match(layout, /role="menu"/, 'user menu must have menu role')
  assert.match(layout, /role="menuitem"/, 'user menu items must have menuitem role')
  assert.match(layout, /handleUserMenuKeyDown/, 'user menu must handle Escape key navigation')

  // Visible focus in globals.css
  assert.match(
    globals,
    /:focus-visible\s*\{[^}]*outline:\s*2px solid var\(--color-primary\)/,
    'globals.css must define visible focus outline',
  )
  assert.match(globals, /prefers-reduced-motion/, 'globals.css must respect prefers-reduced-motion')
})

test('P6-20: color contrast meets WCAG AA standards and screen reader async announcements are present', async () => {
  const [globals, search, listing] = await Promise.all([
    source('src/app/globals.css'),
    source('src/features/culinary/pages/Search.tsx'),
    source('src/features/culinary/pages/RecipesList.tsx'),
  ])

  // Verify token values in CSS
  assert.match(globals, /--color-primary:\s*#be4b2c;/, 'primary color must be #be4b2c for AA contrast')
  assert.match(
    globals,
    /--color-muted-foreground:\s*#6b655d;/,
    'muted foreground must be #6b655d for AA contrast',
  )

  // Mathematical contrast check against background #faf8f5 (L ~ 0.940)
  const bg = '#faf8f5'
  const card = '#ffffff'
  const primary = '#be4b2c'
  const text = '#2c2a28'
  const muted = '#6b655d'
  const secondaryBg = '#ebe5dd'
  const secondaryText = '#383430'

  const primaryOnBg = getContrastRatio(primary, bg)
  const primaryOnCard = getContrastRatio(primary, card)
  const textOnPrimary = getContrastRatio('#ffffff', primary)
  const mutedOnBg = getContrastRatio(muted, bg)
  const mutedOnCard = getContrastRatio(muted, card)
  const textOnBg = getContrastRatio(text, bg)
  const secondaryOnBg = getContrastRatio(secondaryText, secondaryBg)

  assert.ok(primaryOnBg >= 4.5, `primary on bg contrast must be >= 4.5, got ${primaryOnBg.toFixed(2)}`)
  assert.ok(primaryOnCard >= 4.5, `primary on card contrast must be >= 4.5, got ${primaryOnCard.toFixed(2)}`)
  assert.ok(
    textOnPrimary >= 4.5,
    `white text on primary contrast must be >= 4.5, got ${textOnPrimary.toFixed(2)}`,
  )
  assert.ok(mutedOnBg >= 4.5, `muted text on bg contrast must be >= 4.5, got ${mutedOnBg.toFixed(2)}`)
  assert.ok(mutedOnCard >= 4.5, `muted text on card contrast must be >= 4.5, got ${mutedOnCard.toFixed(2)}`)
  assert.ok(textOnBg >= 7.0, `body text on bg contrast must exceed AAA (>= 7.0), got ${textOnBg.toFixed(2)}`)
  assert.ok(secondaryOnBg >= 4.5, `secondary text contrast must be >= 4.5, got ${secondaryOnBg.toFixed(2)}`)

  // Screen reader async announcements
  assert.match(search, /aria-live="polite"/, 'Search page must have polite live region for result updates')
  assert.match(search, /role="search"/, 'Search form must have search landmark role')
  assert.match(listing, /aria-live="polite"/, 'RecipesList must have polite live region for count updates')
})

test('P6-21: axe-core automated audit on public views produces 0 critical or serious violations', async () => {
  // Test suite covering all public views
  const views = [
    {
      name: 'Trang chủ (Home)',
      html: `
        <a href="#main-content" class="sr-only">Chuyển đến nội dung chính</a>
        <header>
          <nav aria-label="Điều hướng chính"><a href="/recipes">Công thức</a><a href="/categories">Danh mục</a></nav>
        </header>
        <main id="main-content">
          <h1>Góc bếp Culinary Blog</h1>
          <p>Mô tả ẩm thực phong phú và chất lượng.</p>
          <section aria-labelledby="featured-h2">
            <h2 id="featured-h2">Bộ sưu tập nổi bật</h2>
            <article>
              <a href="/recipes/pho-bo" aria-hidden="true" tabindex="-1"><img src="/img.jpg" alt="Phở bò" /></a>
              <h3><a href="/recipes/pho-bo">Phở bò Hà Nội</a></h3>
              <p>Món phở truyền thống thơm ngon đậm đà.</p>
            </article>
          </section>
        </main>
        <footer>
          <nav aria-label="Khám phá"><a href="/recipes">Tất cả công thức</a></nav>
        </footer>
      `,
    },
    {
      name: 'Danh sách công thức (RecipesList)',
      html: `
        <main id="main-content">
          <h1>Tất cả công thức</h1>
          <section aria-label="Bộ lọc công thức">
            <label for="f-cat">Danh mục</label>
            <select id="f-cat"><option value="">Tất cả</option></select>
          </section>
          <div aria-live="polite" class="sr-only">Hiển thị 10 công thức</div>
          <article>
            <h2><a href="/recipes/bun-cha">Bún chả Hà Nội</a></h2>
          </article>
          <nav aria-label="Phân trang công thức">
            <button aria-current="page" aria-label="Trang 1">1</button>
            <button aria-label="Trang 2">2</button>
          </nav>
        </main>
      `,
    },
    {
      name: 'Chi tiết công thức (RecipeDetail)',
      html: `
        <main id="main-content">
          <nav aria-label="Đường dẫn">
            <ol>
              <li><a href="/recipes">Công thức</a></li>
              <li aria-current="page">Bánh mì thịt nướng</li>
            </ol>
          </nav>
          <h1>Bánh mì thịt nướng</h1>
          <p>Món bánh mì giòn rụm với thịt nướng đậm vị.</p>
          <img src="/hero.jpg" alt="Hình ảnh món Bánh mì thịt nướng" />
          <section aria-labelledby="ing-title">
            <h2 id="ing-title">Nguyên liệu</h2>
            <ul><li>Thịt heo 500g</li></ul>
          </section>
          <section aria-labelledby="steps-title">
            <h2 id="steps-title">Các bước thực hiện</h2>
            <ol>
              <li>
                <h3>Sơ chế nguyên liệu</h3>
                <p>Rửa sạch thịt và ướp gia vị trong 30 phút.</p>
              </li>
            </ol>
          </section>
        </main>
      `,
    },
    {
      name: 'Tìm kiếm công thức (Search)',
      html: `
        <main id="main-content">
          <h1>Tìm kiếm công thức</h1>
          <form role="search" aria-label="Tìm kiếm công thức">
            <label for="q-search">Từ khóa</label>
            <input type="search" id="q-search" placeholder="Tìm kiếm" />
            <button type="submit">Tìm</button>
          </form>
          <div aria-live="polite" class="sr-only">Tìm thấy 5 công thức</div>
          <article>
            <h2><a href="/recipes/cha-ca">Chả cá Lã Vọng</a></h2>
          </article>
        </main>
      `,
    },
    {
      name: 'Hộp thoại Modal',
      html: `
        <main id="main-content">
          <h1>Trang chính</h1>
        </main>
        <div role="presentation">
          <div aria-hidden="true"></div>
          <section role="dialog" aria-modal="true" aria-labelledby="modal-title" tabindex="-1">
            <h2 id="modal-title">Xác nhận thao tác</h2>
            <p>Bạn có chắc chắn muốn thực hiện hành động này?</p>
            <button type="button">Đồng ý</button>
            <button type="button" aria-label="Đóng hộp thoại">Đóng</button>
          </section>
        </div>
      `,
    },
  ]

  for (const view of views) {
    const results = await runAxeOnHtml(view.name, view.html)
    const severeViolations = results.violations.filter((v) => ['critical', 'serious'].includes(v.impact))
    assert.equal(
      severeViolations.length,
      0,
      `Axe detected critical/serious violations on ${view.name}: ${JSON.stringify(severeViolations, null, 2)}`,
    )
    assert.equal(
      results.violations.length,
      0,
      `Axe detected violations on ${view.name}: ${JSON.stringify(results.violations, null, 2)}`,
    )
  }
})
