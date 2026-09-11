# Product Baseline v1

> Trạng thái: Accepted for implementation  
> Ngày chốt: 11/09/2026  
> Quyết định giải mâu thuẫn: [00_SRS_Conformance_and_Resolution.md](./00_SRS_Conformance_and_Resolution.md)

## 1. Product goal

Culinary Blog v1 cho phép Guest khám phá công thức Published, Author tạo và tự quản lý vòng đời công thức của mình, và Admin quản lý danh mục/can thiệp mọi công thức. Sản phẩm phải chạy được như hệ thống full-stack có bảo mật, dữ liệu bền vững, media pipeline, tìm kiếm, SEO và khả năng vận hành.

## 2. Functional Requirement count

Baseline dùng **34 FR** theo tổng nhóm trong SRS:

| Module | Số FR | Phạm vi |
|---|---:|---|
| FR-AUTH | 7 | Register, local/Google login, refresh, logout, view/update profile |
| FR-CAT | 5 | Public reads và Admin CRUD category |
| FR-RCP | 10 | Recipe, lifecycle, image, ingredient, step |
| FR-SRCH | 4 | Search, filter, sort, pagination |
| FR-FILE | 2 | Upload/delete object |
| FR-JOB | 3 | Welcome email, resize, sitemap |
| FR-OBS | 3 | Health, structured logs, tracing/metrics |
| **Tổng** | **34** | Số “27” trong phần mô tả được xem là lỗi đếm tài liệu |

Các FR-SRCH-002/003/004 được tích hợp vào query recipe/search nhưng vẫn giữ mã riêng để trace và test.

## 3. Actor và product permissions

- Guest không phải DB role; chỉ dùng public endpoints và chỉ thấy Published.
- Tài khoản mới nhận role Author.
- Author tự publish recipe của mình trong v1; moderation nằm ngoài scope.
- Admin được seed/gán bằng quy trình đặc quyền; không có public self-elevation.
- Author chỉ thao tác recipe có `AuthorId` của mình; Admin được bypass ownership.
- Operational access cho logs/telemetry không được suy ra chỉ từ Admin role ứng dụng.

## 4. Must/Should/Post-v1

### Must-have v1

- FR-AUTH-001/002/004/005: register, local login, refresh rotation/reuse detection và logout.
- FR-CAT-001-004: public category reads và Admin create/update; delete giữ đúng mức Should của FR-CAT-005.
- FR-RCP-001-005/007-010: public Recipe reads, Draft create/update, publish/unpublish, soft delete, image, ingredient và step management.
- FR-SRCH-001-004, FR-FILE-001/002, FR-JOB-001-003 và FR-OBS-001-003.
- PostgreSQL 16, Redis 7, MinIO/S3, Hangfire PostgreSQL, .NET 10 Minimal APIs, Next.js 15 App Router và các NFR bắt buộc của SRS.
- Responsive UI, WCAG 2.1 AA, SEO metadata/JSON-LD/sitemap, security, performance, reliability và maintainability gates.

### Should-have v1

- FR-AUTH-003/006/007: Google login và view/update profile.
- FR-CAT-005: xóa category khi không còn Recipe liên quan.
- FR-RCP-006: archive và supporting action unarchive về Draft.

### Post-v1 hoặc Change Request riêng

- Email confirmation endpoints, password reset/change-email/OTP và Admin deactivate/reactivate user.
- Public author profile, moderation/approval workflow, bulk reorder và restore soft-deleted resource.
- Comment/rating, favorite/bookmark, real-time notification và direct messaging.
- Native mobile, commerce, GraphQL, AI recommendation và full i18n.

## 5. Publish invariant

Recipe chỉ được chuyển Draft → Published khi:

1. Owner/Admin có quyền và user đang active.
2. Title, description, category, prep/cook time, servings và difficulty hợp lệ.
3. Category tồn tại và chưa soft-delete.
4. Có ít nhất 1 ingredient.
5. Có ít nhất 1 step và StepNumber liên tục từ 1.
6. Concurrency token còn mới.

Ảnh không phải điều kiện publish vì SRS không quy định nhất quán rằng ảnh là bắt buộc. Recipe không có ảnh dùng asset mặc định cho card, Open Graph và JSON-LD.

Published recipe không được bị mutation về trạng thái thiếu ingredient/step. API trả `409 RECIPE_STATE_CONFLICT` và yêu cầu unpublish trước khi xóa thành phần cuối cùng.

## 6. Retention và deletion baseline

- Recipe/Category dùng soft delete theo BaseEntity và NFR-REL-003.
- V1 không có purge vật lý hoặc restore API vì SRS không đặc tả hai use case này.
- Category chỉ soft-delete khi không còn Recipe chưa bị xóa tham chiếu.
- Soft-delete Recipe giữ nguyên child metadata và object để bảo toàn durability; public/private query filter loại toàn bộ dữ liệu này.
- Xóa một image riêng lẻ theo FR-RCP-008: xóa metadata, enqueue object-delete idempotent và chọn primary mới nếu cần.
- Retention audit log, refresh token cleanup và hard purge phải được cấu hình vận hành hoặc phê duyệt bằng Change Request; không dùng nhầm retention backup 30 ngày làm retention nghiệp vụ.

## 7. Product-level acceptance

- Guest không thể truy cập Draft/Archived/deleted qua API, cache, ISR, object URL, sitemap hoặc metadata.
- Author A không thể đọc/sửa/xóa nội dung private của Author B.
- Author hoàn thành `register → create Draft → compose → publish → discover` không cần thao tác thủ công ngoài UI.
- Admin quản lý category và deactivate user có audit trail.
- Hệ thống vẫn phục vụ core reads từ DB khi Redis unavailable.
- Failed email/resize job không rollback business transaction và có retry/visibility.

## 8. Backlog ưu tiên

| Epic | Ưu tiên | Phase | Điều kiện hoàn thành chính |
|---|---|---:|---|
| Foundation/Observability | Must | 1 | Build/compose/CI/health/log/trace hoạt động |
| Identity lifecycle | Must | 2 | Auth, recovery, deactivate và token security test pass |
| Category | Must | 3 | Public read/Admin CRUD và delete conflict |
| Recipe Core | Must | 3 | Draft CRUD, ownership, soft delete, ETag |
| Composition/Media | Must | 4 | Ingredient/step/nutrition/image/jobs |
| Publishing/Discovery | Must | 5 | Lifecycle/search/filter/cache isolation |
| Public Web/SEO | Must | 6 | Responsive, a11y, JSON-LD/metadata/sitemap |
| Dashboard/Admin | Must | 7 | End-to-end author/admin workflows |
| Google Auth/Profile | Should | 2 | Secure link/create flow và profile theo FR-AUTH-003/006/007 |
| Category delete/Archive | Should | 3–5 | Chỉ làm sau Must scope; đúng FR-CAT-005/FR-RCP-006 |
| Hardening/Release | Must | 8 | UAT/security/load/restore gates |

## 9. Scope change rule

Sau khi baseline được Accepted, chức năng/entity/endpoint/route mới phải có Change Request nêu business value, ảnh hưởng API/data/security/test/timeline và quyết định Must/Should/Post-v1.
