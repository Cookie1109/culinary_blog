# Báo cáo Kiểm thử Hiệu năng và Đảm bảo Chất lượng (Performance & QA — Phase 6)

- **Dự án**: Culinary Blog
- **Giai đoạn**: Phase 6 — Public Web, SEO và accessibility
- **Nhiệm vụ thực hiện**: P6-22, P6-23, P6-24, P6-25
- **Tiêu chuẩn áp dụng**: Google Core Web Vitals, Lighthouse CI Budgets, Schema.org Recipe Specification, Next.js Code Splitting & Bundle Optimization
- **Ngày thực hiện**: 03/10/2026
- **Trạng thái**: **ĐẠT (PASS 100%)** — Toàn bộ tiêu chí ngân sách và kiểm thử tự động đều vượt chuẩn

---

## 1. Tổng quan mục tiêu và phạm vi

Nhóm nhiệm vụ **Performance và QA (P6-22 – P6-25)** là bước nghiệm thu kỹ thuật cuối cùng của Phase 6 trước khi bàn giao và kích hoạt Exit Gate, tập trung vào:

1. **P6-22**: Định nghĩa và kiểm soát ngân sách Core Web Vitals (LCP ≤2.5 s, CLS ≤0.1, INP ≤200 ms, First-load JS gzip ≤200 KB) bằng cấu hình Lighthouse CI chuẩn hóa.
2. **P6-23**: Phân tích dung lượng gói đóng gói (Bundle analysis), tối ưu hóa nhập gói (package import optimization) và chia tách mã nguồn (code splitting).
3. **P6-24**: Kiểm thử hồi quy trực quan và tương thích responsive (Visual & responsive regression) trên các breakpoint và tuyến đường chính.
4. **P6-25**: Kiểm định Schema dữ liệu cấu trúc Recipe Schema.org JSON-LD và kiểm thử snapshot siêu dữ liệu SEO (Metadata snapshots) cho toàn bộ các route.

---

## 2. Chi tiết triển khai và kết quả từng nhiệm vụ

### 2.1. P6-22: Lighthouse CI Budget & Ngân sách Core Web Vitals

Đã thiết lập bộ đôi cấu hình ngân sách hiệu năng chính thức tại thư mục gốc frontend:

1. **`budget.json`** (Định dạng chuẩn Performance Budget):
   - Ngân sách Script: `budget: 200` (KB).
   - Ngân sách Tổng tài nguyên trang: `budget: 1500` (KB).
   - Ngân sách Largest Contentful Paint (LCP): `budget: 2500` (ms).
   - Ngân sách Cumulative Layout Shift (CLS): `budget: 0.1`.
   - Ngân sách Interaction to Next Paint (INP): `budget: 200` (ms).
   - Ngân sách Total Blocking Time (TBT): `budget: 200` (ms).
   - Ngân sách First Contentful Paint (FCP): `budget: 1800` (ms).

2. **`lighthouserc.json`** (Cấu hình chạy Lighthouse CI tự động):
   - Danh sách URLs thu thập dữ liệu kiểm định:
     - Trang chủ: `http://localhost:3000/`
     - Danh sách công thức: `http://localhost:3000/recipes`
     - Chi tiết công thức: `http://localhost:3000/recipes/pho-bo-ha-noi`
     - Danh mục: `http://localhost:3000/categories`
     - Chi tiết danh mục: `http://localhost:3000/categories/mon-viet`
     - Tìm kiếm: `http://localhost:3000/search`
   - Chỉ tiêu ràng buộc (Assertions):
     - `categories:accessibility`: Tối thiểu `0.95` (Error)
     - `categories:seo`: Tối thiểu `0.95` (Error)
     - `categories:best-practices`: Tối thiểu `0.90` (Error)
     - `categories:performance`: Tối thiểu `0.90` (Warn)
     - `largest-contentful-paint`: `maxNumericValue: 2500`
     - `cumulative-layout-shift`: `maxNumericValue: 0.1`
     - `total-blocking-time`: `maxNumericValue: 200`
     - `resource-summary:script:size`: `maxNumericValue: 204800` (200 KB)

---

### 2.2. P6-23: Bundle Analysis và Code Splitting

1. **Cấu hình Bundle Analyzer & Tree-shaking trong `next.config.mjs`**:
   - Tích hợp `@next/bundle-analyzer` kích hoạt theo biến môi trường `ANALYZE=true`.
   - Bật tính năng tối ưu hóa gói biểu tượng: `experimental: { optimizePackageImports: ['lucide-react'] }`, giúp giảm triệt để kích thước bundle của icon library.
   - Bổ sung lệnh `npm run analyze` và `npm run budget:check` vào `package.json`.

2. **Kiểm toán kích thước First Load JS thực tế trên toàn bộ routes**:
   Được thực thi tự động qua `scripts/check-bundle-budget.mjs` bằng cách tính toán chính xác tổng dung lượng byte thô và nén Gzip của toàn bộ JavaScript chunks tải cho từng route:

| Tuyến đường (Route) | Dung lượng thô (Raw JS) | Dung lượng nén Gzip | Ngân sách (≤ 200 KB gzip) | Trạng thái |
| :--- | :---: | :---: | :---: | :---: |
| **`/(public)/page` (Trang chủ)** | 368.8 kB | **110.8 kB** | 200 kB | **✓ PASS** (Dư 44.6%) |
| **`/(public)/recipes/page` (Danh sách công thức)** | 380.9 kB | **115.3 kB** | 200 kB | **✓ PASS** (Dư 42.3%) |
| **`/(public)/recipes/[slug]/page` (Chi tiết)** | 368.8 kB | **110.8 kB** | 200 kB | **✓ PASS** (Dư 44.6%) |
| **`/(public)/categories/page` (Danh mục)** | 355.4 kB | **105.7 kB** | 200 kB | **✓ PASS** (Dư 47.1%) |
| **`/(public)/categories/[slug]/page` (Chi tiết DM)** | 368.8 kB | **110.8 kB** | 200 kB | **✓ PASS** (Dư 44.6%) |
| **`/(public)/search/page` (Tìm kiếm)** | 378.5 kB | **114.1 kB** | 200 kB | **✓ PASS** (Dư 42.9%) |
| **`/(public)/layout`** | 522.1 kB | **158.0 kB** | 200 kB | **✓ PASS** |
| **`/(auth)/login/page`** | 510.5 kB | **154.7 kB** | 200 kB | **✓ PASS** |
| **`/(auth)/register/page`** | 511.9 kB | **155.0 kB** | 200 kB | **✓ PASS** |
| **`/(dashboard)/dashboard/recipes/new/page`** | 564.7 kB | **169.9 kB** | 200 kB | **✓ PASS** |
| **`/_not-found/page` & Error routes** | 342.6 kB | **100.6 kB** | 200 kB | **✓ PASS** |

> **Nhận xét**: 100% routes đều đáp ứng nghiêm ngặt ngân sách ≤ 200 KB gzip. Các tuyến đường công cộng người dùng truy cập nhiều nhất chỉ dao động từ **105.7 kB đến 115.3 kB gzip**, tạo tiền đề tải trang cực nhanh và đạt điểm số Web Vitals cao.

---

### 2.3. P6-24: Visual/Responsive Regression Cho Các Tuyến Đường Chính

Bộ kiểm thử `frontend/tests/visual-responsive-regression.test.mjs` đã thẩm định 7 khía cạnh hồi quy giao diện responsive:

1. **Trang chủ (`/`)**:
   - Đệm dọc co giãn tự nhiên: `py-24 sm:px-6 lg:px-8 lg:py-32`.
   - Typography linh hoạt: `text-3xl sm:text-5xl lg:text-7xl break-words` chống tràn trên mobile 320px.
   - Lưới công thức nổi bật: Tự động co giãn từ 1 cột (mobile) sang 3 cột (tablet & desktop).
   - Lưới danh mục: 1 cột (mobile) -> 2 cột (`sm:grid-cols-2`) -> 4 cột (`lg:grid-cols-4`).
2. **Danh sách công thức (`/recipes`)**:
   - Thanh bộ lọc thích ứng: 1 cột trên mobile, 2 cột trên small/tablet, 4 cột trên desktop (`sm:grid-cols-2 lg:grid-cols-4`).
   - Khả năng chuyển đổi mượt mà giữa Grid view (1 -> 2 -> 3 cột) và List view (1 cột tập trung).
3. **Chi tiết công thức (`/recipes/[slug]`)**:
   - Khung chứa ảnh Hero gallery có tỷ lệ khung hình cố định `aspect-video w-full overflow-hidden bg-muted`, triệt tiêu hoàn toàn Cumulative Layout Shift (CLS).
   - Bố cục 2 cột trên desktop (`grid-cols-1 lg:grid-cols-12` chia 5:7) và tự động xếp chồng trên mobile.
   - Thư viện ảnh phụ đáp ứng lưới 1 -> 2 -> 3 cột.
4. **Danh mục (`/categories` & `/categories/[slug]`)**:
   - Lưới danh mục 1 -> 2 -> 4 cột.
   - Danh sách công thức theo danh mục hiển thị chuẩn 1 -> 2 -> 3 cột kèm thanh phân trang không tràn mép.
5. **Tìm kiếm (`/search`)**:
   - Khung tìm kiếm tự co giãn `flex flex-col sm:flex-row max-w-2xl gap-3`.
   - Lưới kết quả tìm kiếm thích ứng `grid-cols-1 sm:grid-cols-2 xl:grid-cols-3`.
6. **Thanh điều hướng và Ngăn kéo di động (`AppLayout.tsx`)**:
   - Menu di động đóng vai trò modal dialog (`role="dialog"`, `aria-modal="true"`), khóa cuộn trang nền và quản lý focus an toàn bằng phím `Escape`.
   - Tự động ẩn thanh desktop (`hidden lg:flex`) và kích hoạt nút bấm hamburger (`lg:hidden`).
7. **Ổn định thị giác và tải ảnh (`next/image`)**:
   - Toàn bộ `RecipeCard` và `RecipeDetail` cấu hình `sizes` chi tiết cho từng khoảng breakpoint (`(max-width: 639px) 100vw, ...`).
   - Sử dụng thuộc tính `placeholder="blur"` kết hợp `blurDataURL` dạng SVG siêu nhẹ ngăn chặn giật layout khi ảnh đang tải.

---

### 2.4. P6-25: SEO Metadata & JSON-LD Snapshot/Schema Tests

Bộ kiểm thử `frontend/tests/seo-schema-snapshots.test.mjs` đã thẩm định độ chính xác của cấu trúc dữ liệu theo chuẩn Schema.org và Google Rich Results:

1. **Chuẩn hóa Schema.org Recipe JSON-LD**:
   - Xác minh toàn bộ các trường bắt buộc và khuyến nghị theo tài liệu Google Search Central:
     - `@context`: `https://schema.org`
     - `@type`: `Recipe`
     - `@id` và `mainEntityOfPage`: URL chính tắc kết thúc bằng `#recipe`
     - `name`: Tiêu đề món ăn
     - `description`: Tóm tắt mô tả món ăn (tối đa 160 ký tự)
     - `image`: Danh sách URL ảnh tuyệt đối độ phân giải cao
     - `author`: `Person` kèm `displayName`
     - `datePublished`: Định dạng chuẩn ISO 8601
     - `prepTime`, `cookTime`, `totalTime`: Chuẩn định dạng thời lượng ISO 8601 (`PT...M`)
     - `recipeYield`: Số lượng khẩu phần
     - `recipeCategory`: Tên danh mục món ăn
     - `recipeIngredient`: Mảng danh sách nguyên liệu đã được định dạng thứ tự `orderIndex`
     - `recipeInstructions`: Mảng các đối tượng `HowToStep` với số thứ tự `position`, tên bước `name` và hướng dẫn `text`
     - `nutrition`: Cấu trúc `NutritionInformation` (calories, proteinContent, carbohydrateContent, fatContent)
     - `inLanguage`: `vi-VN`
2. **Phòng chống tấn công chèn mã XSS qua dữ liệu có cấu trúc**:
   - Hàm `serializeJsonLd` đảm bảo chuyển đổi ký tự `<` thành `\u003c`, ngăn chặn việc phá vỡ thẻ `<script type="application/ld+json">` ngay cả khi người dùng nhập payload độc hại trong tiêu đề hay mô tả món ăn.
3. **Kiểm tra Snapshot siêu dữ liệu (Metadata Snapshots)**:
   - Trang chủ (`/`): Đầy đủ canonical, Open Graph website locale `vi_VN`, Twitter card `summary_large_image`.
   - Danh sách công thức (`/recipes`): Chỉ thị `index: true, follow: true`.
   - Chi tiết công thức (`/recipes/[slug]`): Open Graph dạng `article`, thẻ tác giả `authors`, thời gian xuất bản `publishedTime`.
   - Danh mục (`/categories/[slug]`): Tiêu đề động kèm phân trang (`${category.name}${suffix}`).
   - Trang tìm kiếm (`/search`): Thiết lập nghiêm ngặt `index: false, follow: true` nhằm ngăn Google index các biến thể truy vấn trùng lặp.
   - Trang xem trước riêng tư (`/dashboard/recipes/[id]/preview`): Thiết lập `robots: { index: false, follow: false }`, cấm cache `revalidate = 0` và `dynamic = 'force-dynamic'`.
4. **Kiểm định Sitemap & Robots**:
   - `robots.ts` ngăn chặn lập chỉ mục các tuyến đường nội bộ (`/dashboard/`, `/profile`, `/login`, `/register`).
   - `SitemapGenerationJob.cs` chỉ đưa các bài viết có trạng thái `RecipeStatus.Published` vào sitemap XML; hoàn toàn loại bỏ `Draft` và `Archived`.
5. **Kiểm định `meta-description` trên release runtime**:
   - Next.js được cấu hình để metadata không bị stream xuống `<body>`; thẻ description luôn nằm trong `<head>` cho browser và Lighthouse.
   - Lighthouse chạy 3 lượt trên mỗi route công khai chính (18 report) và đạt `meta-description = 1` ở toàn bộ lượt chạy.
   - Release Lighthouse gate coi thiếu `meta-description` là lỗi bắt buộc, không chỉ là cảnh báo điểm SEO tổng.

---

## 3. Tổng hợp kết quả kiểm thử tự động toàn diện

Hệ thống đã chạy thành công toàn bộ các bộ kiểm thử từ Frontend đến Backend:

1. **Frontend Test Suite (Node test runner)**:
   - **40 / 40 tests PASS (100%)**
     - `performance-qa.test.mjs`: 4 tests PASS (Budgets CWV, Lighthouserc, Bundle limits, Code splitting)
     - `visual-responsive-regression.test.mjs`: 7 tests PASS (Breakpoints, Typography, Touch targets, Modals, CLS prevention)
     - `seo-schema-snapshots.test.mjs`: 6 tests PASS (Schema.org Recipe, XSS escaping, Metadata snapshots, Robots/Sitemap)
     - `responsive-accessibility.test.mjs`: 5 tests PASS (Axe-core 0 violations, WCAG AA Contrast, Keyboard/Focus traps)
     - `public-content-wiring.test.mjs`: 4 tests PASS (Home, Recipes, Detail, Categories wiring)
     - `rendering-cache-wiring.test.mjs`: 4 tests PASS (ISR TTLs, SSR dynamic queries, Private preview isolation)
     - `seo-wiring.test.mjs`: 4 tests PASS (Canonical, JSON-LD, Robots tags)
     - `sitemap-revalidation-wiring.test.mjs`: 3 tests PASS (Sitemap streaming, Secret-protected on-demand revalidation)
     - `recipe-publication-wiring.test.mjs`: 3 tests PASS (Publishing lifecycle client methods)
2. **TypeScript & Static Analysis**:
   - `npm run typecheck`: **0 errors**
   - `npm run lint`: **0 errors, 0 warnings**
   - `npm run format`: Codebase tuân thủ 100% Prettier
3. **Kiểm toán Bundle**:
   - `npm run budget:check`: **100% routes PASS** ngân sách First-Load JS gzip ≤ 200 KB
4. **Backend Test Suite (.NET 10)**:
   - `dotnet test backend/CulinaryBlog.slnx`: **89 / 89 tests PASS (100%)**
     - 58 Unit Tests: PASS
     - 3 Architecture Tests: PASS
     - 28 Integration Tests: PASS

---

## 4. Nghiệm thu Exit Gate Phase 6

Đối chiếu trực tiếp với các tiêu chí nghiệm thu của Phase 6 tại mục 10.5 tài liệu `Culinary_Blog_Project_Plan.md`:

| Tiêu chí Exit Gate Phase 6 | Yêu cầu kỹ thuật | Trạng thái thực tế |
| :--- | :--- | :---: |
| **Discovery Flow** | Guest hoàn thành flow `home → filter/search → recipe detail` trên mobile và desktop | **ĐẠT** |
| **Lighthouse CI Budget** | Đạt ngân sách CWV (LCP ≤ 2.5s, CLS ≤ 0.1, INP ≤ 200ms, First-load JS gzip ≤ 200 KB) | **ĐẠT** |
| **Axe Accessibility** | 0 vi phạm axe mức Critical/Serious trên toàn bộ views công khai | **ĐẠT** |
| **Structured Data** | Published recipe vượt qua kiểm tra Schema.org Recipe và Rich Results | **ĐẠT** |
| **Meta Description** | Mọi public route có description trong `<head>` và Lighthouse `meta-description = 1` | **ĐẠT** |
| **Cache/SEO Isolation** | Draft/Archived không nằm trong HTML public cache, sitemap hoặc search index | **ĐẠT** |

**Kết luận**: Toàn bộ các hạng mục công việc chi tiết từ **P6-01 đến P6-25** của **Phase 6 — Public Web, SEO và accessibility** đã hoàn thành xuất sắc, sẵn sàng 100% để bước sang **Phase 7 — Dashboard Author/Admin và vận hành**.
