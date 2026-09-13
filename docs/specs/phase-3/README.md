# Phase 3 — Category và Recipe Core

**Trạng thái:** **Implementation Complete / Local Accepted**  
**Baseline:** [Phase 0](../phase-0/README.md)  
**Ngày triển khai:** 14/09/2026

## Phạm vi đã triển khai

| ID | Kết quả | Bằng chứng chính |
|---|---|---|
| P3-01–07 | `BaseEntity`, audit/soft delete, `Version bigint`, aggregate `Category`/`Recipe`, nutrition owned columns, global filters, check constraints, indexes, migration và seed category idempotent | `Domain`, `Persistence/Configurations`, `AuditSaveChangesInterceptor`, migration `RecipeCore`, `DatabaseInitializer` |
| P3-08–11 | Public category list/detail chỉ chiếu Published recipe; Admin create/update/soft-delete; slug ổn định; unique name/slug toàn bảng; chặn xóa category còn recipe | `ContentService`, `ContentEndpoints` |
| P3-12–19 | Draft create/update/read/list/delete, owner/Admin authorization, public Published-only read, Admin read mọi owner/trạng thái, collision suffix, ETag/`If-Match` và `409 RECIPE_CONCURRENCY_CONFLICT` | `IContentService`, `ContentService`, `ContentEndpoints` |
| P3-20–25 | Dashboard dùng API thật với loading/empty/error; explicit save form basic + nutrition; conflict reload UX; Admin category CRUD và role-aware navigation | `content-client.ts`, `MyRecipes.tsx`, `RecipeEditor.tsx`, `CategoryAdmin.tsx` |
| P3-26–30 | Domain tests cho slug/state/soft-delete/nutrition; integration matrix Guest/owner/other/Admin; concurrent stale ETag; public Draft isolation; global filter; category delete conflict; unique race | `RecipeCoreDomainTests`, `RecipeCoreApiTests` |

## API đã mở trong phase

- Public: `GET /api/v1/categories`, `GET /api/v1/categories/{slug}`, `GET /api/v1/recipes`, `GET /api/v1/recipes/{slug}`.
- Author/Admin: `POST /api/v1/recipes`, `PUT /api/v1/recipes/{id}`, `DELETE /api/v1/recipes/{id}`, `GET /api/v1/me/recipes`, `GET /api/v1/me/recipes/{id}`.
- Admin: `POST|PUT|DELETE /api/v1/categories`, `GET /api/v1/admin/recipes`, `GET /api/v1/admin/recipes/{id}`.

Recipe mutation yêu cầu strong ETag dạng `If-Match: "{version}"`. Thiếu precondition trả `400 PRECONDITION_REQUIRED`; stale version trả `409 RECIPE_CONCURRENCY_CONFLICT`, kèm ETag hiện tại trong header và Problem Details.

## Phạm vi chủ ý chưa mở

- Ingredient, step và image vẫn trả mảng rỗng; CRUD các thành phần này thuộc Phase 4.
- Publish/unpublish endpoint thuộc Phase 5 vì publish invariant cần ingredient và step hoàn chỉnh.
- Domain đã có archive/unarchive transition theo P3-03; API/UI lifecycle hoàn chỉnh được giữ cho Phase 5/7.
- Public filter/search/sort/cache chưa được tối ưu trong Phase 3; public read hiện chỉ thực thi privacy boundary Published.

## Kết quả kiểm định

| Gate | Kết quả ngày 14/09/2026 | Trạng thái |
|---|---|---|
| Backend build | .NET SDK 10.0.401, toàn solution `0 warning / 0 error` | Đạt |
| Unit tests | `13/13` pass | Đạt |
| Architecture tests | `3/3` pass | Đạt |
| PostgreSQL integration tests | `14/14` pass, gồm authorization matrix, concurrent ETag, slug collision và unique category race | Đạt |
| Migration consistency | `dotnet ef migrations has-pending-model-changes`: không có model change chưa migrate | Đạt |
| Migration clean/upgrade | Testcontainers áp `IdentityAndSessions` rồi `RecipeCore` trên PostgreSQL 16 sạch trong integration suite | Đạt |
| Frontend static gates | TypeScript strict, ESLint và Prettier pass | Đạt |
| Frontend production build | Next.js build thành công, sinh 14 route gồm `/dashboard/categories` | Đạt |

## Exit gate

- Author tạo/xem/sửa/soft-delete Draft của mình qua API và dashboard.
- Author khác nhận 403; Guest/public route không thấy Draft hoặc deleted recipe.
- Admin quản lý category và xem recipe của mọi owner; backend policy vẫn là lớp kiểm soát bắt buộc.
- Hai update đồng thời cùng ETag chỉ có một request thành công; request còn lại nhận conflict và current ETag.
- Category có recipe không thể bị xóa; name/slug không tái sử dụng kể cả sau soft delete.
- Migration apply sạch và nối tiếp migration Phase 2 thành công trên PostgreSQL Testcontainers.

## Việc cần làm để đóng phase

- Chạy lại `scripts/verify.ps1` trên clean CI runner có .NET 10, Node 20 và Docker.
- Thực hiện review bảo mật/QA độc lập; không chuyển trạng thái `Accepted / Closed` nếu còn issue Critical/High.
