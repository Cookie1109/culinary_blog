# Route Map and Low-Fidelity Wireframes v1

> Trạng thái: Accepted for implementation  
> Nguồn: SRS Chương 5, Route Map trang 46-47; baseline visibility GAP-014.

## 1. Route map

| Route | Rendering | Auth | Dữ liệu/khả năng |
|---|---|---|---|
| `/` | ISR 3600s | Public | Featured Published recipes và categories |
| `/recipes` | SSR dynamic | Public | Published list, filter, sort, pagination |
| `/recipes/[slug]` | ISR 300s | Public | Published detail, ingredient, step, nutrition, JSON-LD |
| `/categories` | ISR 3600s | Public | Category navigation |
| `/categories/[slug]` | ISR 600s | Public | Category và Published recipes |
| `/search` | SSR | Public | FTS, filter và pagination |
| `/auth/login` | CSR | Public | Local login và Google Should-have |
| `/auth/register` | CSR | Public | Registration và auto-login |
| `/profile` | CSR | Authenticated | View/update profile, Should-have |
| `/dashboard` | CSR | Author/Admin | Counts và recent recipes |
| `/dashboard/recipes` | CSR | Author/Admin | Author dùng `/me/recipes`; Admin có mode dùng `/admin/recipes` |
| `/dashboard/recipes/new` | CSR | Author/Admin | Recipe wizard |
| `/dashboard/recipes/[id]/edit` | CSR | Owner/Admin | Private detail/edit/composition/lifecycle |
| `/dashboard/categories` | CSR | Admin | Category create/update; delete Should-have |
| `/hangfire` | Server dashboard | Admin + network policy | Job operations |

Draft/Archived không được render vào public route cache. Preview/edit luôn lấy dữ liệu từ `/me/recipes/{id}` và dùng `no-store`/private cache.

## 2. Public listing wireframe

```text
+----------------------------------------------------------+
| Header | Logo | Recipes | Categories | Search | Account  |
+----------------------------------------------------------+
| H1 Recipes                                               |
| [Category] [Difficulty] [Max cook time] [Sort]           |
+----------------------------------------------------------+
| Recipe card | Recipe card | Recipe card                  |
| image/title | image/title | image/title                  |
| time/diff   | time/diff   | time/diff                    |
+----------------------------------------------------------+
|                 Previous  1 2 3  Next                    |
+----------------------------------------------------------+
```

Mobile chuyển card thành một cột; filter nằm trong panel có focus trap, Escape đóng và nút Apply rõ ràng.

## 3. Recipe detail wireframe

```text
+----------------------------------------------------------+
| Breadcrumb > Category > Recipe                           |
| H1 Title                         [Primary/default image]  |
| Author | Prep | Cook | Servings | Difficulty             |
+----------------------------+-----------------------------+
| Ingredients                | Nutrition                   |
+----------------------------+-----------------------------+
| Ordered steps 1..n                                       |
| Gallery                                                  |
+----------------------------------------------------------+
```

Trang Published có canonical, Open Graph, Twitter Card và JSON-LD Recipe. Nếu không có ảnh, dùng asset mặc định; ảnh không phải publish invariant.

## 4. Author wizard wireframe

```text
[1 Basic] -> [2 Ingredients] -> [3 Steps] -> [4 Images] -> [5 Preview]

+----------------------------------------------------------+
| Save status | Validation summary | Current ETag/version   |
| Form for current step                                    |
| Back                                      Save & Continue |
+----------------------------------------------------------+
| Publish: enabled when category + >=1 ingredient + >=1 step|
+----------------------------------------------------------+
```

- Mỗi bước save Draft độc lập; lỗi không làm mất phần đã commit.
- Mutation gửi `If-Match`; stale version hiển thị Reload/Discard, không overwrite.
- Published Recipe không được xóa ingredient/step cuối; phải unpublish trước.

## 5. Required states

Mọi route có loading, empty, error, success và permission-denied phù hợp. Form có inline validation; write có toast; upload có progress. Keyboard, visible focus, semantic headings, contrast WCAG AA và screen-reader announcement là bắt buộc.
