# Phase 4 — Thành phần công thức, media và background jobs

**Trạng thái:** **Implementation Complete / Docker Acceptance Pending**
**Baseline:** [Phase 0](../phase-0/README.md)
**Ngày triển khai:** 15/09/2026

## Phạm vi đã triển khai

| ID | Kết quả | Bằng chứng chính |
|---|---|---|
| P4-01–07 | Entity/configuration/migration cho ingredient và step; nullable quantity/unit; CRUD + reorder; step number liên tục; child mutation tăng Recipe ETag; chặn xóa ingredient/step cuối khỏi Published recipe | `RecipeIngredient`, `RecipeStep`, `ContentService.Composition`, migration `RecipeCompositionMedia` |
| P4-08–15 | `IFileStorageService` + MinIO; bucket private; giới hạn 5 MB theo stream/temp file; JPEG/PNG/WebP allowlist; kiểm tra codec, decode và giới hạn pixel; GUID object key; metadata/primary/order; media proxy kiểm tra Published/owner/Admin; cleanup bù khi DB commit lỗi; upload rate limit 5/phút/IP | `MinioFileStorageService`, `ContentEndpoints`, `ContentService.Composition`, `RecipeImageConfiguration` |
| P4-16–21 | Hangfire PostgreSQL worker/dashboard Admin-only; resize medium tối đa 800×600 và thumbnail tối đa 300×300; delete job idempotent/retry 3 lần; transactional media outbox; correlation bằng Outbox ID; daily reconciliation cho record/object mồ côi | `DependencyInjection`, `MediaOutboxDispatcher`, `ImageProcessingJob`, `ObjectDeletionJob`, `ImageReconciliationJob` |
| P4-22–26 | Authoring wizard cho ingredient → step → image → preview; add/edit/delete/reorder; client format/size validation; upload progress; gallery/primary/alt text; từng mutation save ngay để reload phục hồi Draft | `RecipeCompositionWizard.tsx`, `content-client.ts`, `auth-client.ts` |
| P4-27–31 | Unit tests cho validation/domain; PostgreSQL integration tests cho owner/other Author, ETag, reorder liên tục, MIME/signature giả, oversize, rate limit, path traversal, concurrent primary switch, Draft media isolation và resize/delete idempotency | `RecipeCoreDomainTests`, `RecipeCompositionApiTests` |

Bulk reorder endpoint riêng không được thêm vì đã được xếp Post-v1; reorder v1 dùng child `PUT` trong transaction và normalize toàn bộ thứ tự.

## API đã mở trong phase

- Ingredient: `POST /api/v1/recipes/{id}/ingredients`, `PUT|DELETE /api/v1/recipes/{id}/ingredients/{ingredientId}`.
- Step: `POST /api/v1/recipes/{id}/steps`, `PUT|DELETE /api/v1/recipes/{id}/steps/{stepId}`.
- Image: `POST /api/v1/recipes/{id}/images`, `PATCH|DELETE /api/v1/recipes/{id}/images/{imageId}`.
- Delivery: `GET /api/v1/media/{imageId}/{original|medium|thumbnail}`; anonymous chỉ đọc được media của Published recipe.
- Operations: `/jobs` dùng Hangfire dashboard và chỉ chấp nhận principal có role Admin.

Mọi child mutation yêu cầu strong `If-Match`. Response create/update và header `ETag` trả version aggregate mới; delete trả version mới qua `ETag`.

## Bảo đảm lưu trữ và job

- Upload dùng filename do server sinh dưới namespace `recipes/{recipeId}/{imageId}`; filename từ client không tham gia object key.
- Original được ghi trước DB; lỗi validation không ghi object, lỗi DB sau upload sẽ chạy compensation delete.
- Image metadata và outbox resize commit trong cùng PostgreSQL transaction.
- Resize ghi vào deterministic object key nên replay không sinh duplicate; delete object coi object không tồn tại là thành công.
- Bucket vẫn private theo bootstrap Compose. URL API không cấp quyền trực tiếp lên MinIO.
- Reconciliation hằng ngày log DB record thiếu object, object không có record và enqueue lại image pending quá hạn.

## Kết quả kiểm định

| Gate | Kết quả ngày 15/09/2026 | Trạng thái |
|---|---|---|
| Backend restore/audit | Package khóa phiên bản; pin `Newtonsoft.Json 13.0.4` để loại advisory transitive; SkiaSharp 4.152.0 MIT | Đạt |
| Backend build | .NET SDK 10.0.401, toàn solution `0 warning / 0 error` | Đạt |
| Unit tests | `33/33` pass | Đạt |
| Architecture tests | `3/3` pass | Đạt |
| Migration consistency | `dotnet ef migrations has-pending-model-changes`: không có model change chưa migrate | Đạt |
| Frontend static gates | OpenAPI type generation, TypeScript strict, ESLint, Prettier và `2/2` wiring tests pass | Đạt |
| Frontend production build | Next.js production build thành công | Đạt |
| PostgreSQL/MinIO integration | `RecipeCompositionApiTests` compile nhưng fixture không khởi động: Docker endpoint `npipe://./pipe/docker_engine` không khả dụng | Chờ Docker |
| Worker restart/retry với dependency thật | Cần chạy Hangfire + PostgreSQL + MinIO qua Compose/Testcontainers | Chờ Docker |

## Exit gate

Đã chứng minh bằng build/static/unit gate:

- Aggregate, API, authorization path, ETag và UI authoring đã được triển khai theo contract.
- File bị giới hạn kích thước, MIME/signature/decode; object key không lấy từ filename client.
- Private bucket và controlled media proxy ngăn URL MinIO trực tiếp.
- Job/outbox handler được thiết kế idempotent, có retry và reconciliation.

Chưa ký `Local Accepted` cho đến khi các test PostgreSQL/MinIO/Hangfire chạy thật pass, đặc biệt là upload/compensation, concurrent primary switch, worker restart và reconciliation.

## Lệnh nghiệm thu còn lại khi Docker hoạt động

```powershell
Set-Location backend
dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj `
  --filter "FullyQualifiedName~RecipeCompositionApiTests"

Set-Location ..
docker compose up --detach --build --wait
./scripts/smoke.ps1
```
