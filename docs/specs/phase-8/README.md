# Phase 8 — Functional regression

> Trạng thái ngày 03/10/2026: **Implementation Complete / Local Accepted** cho P8-01 đến P8-04. Việc chạy browser E2E trên staging với PostgreSQL, Redis, MinIO và Hangfire thật vẫn thuộc staging/UAT gate P8-24/P8-25, chưa được tài liệu này tuyên bố hoàn tất.

## Phạm vi

| Task | Kết quả |
|---|---|
| P8-01 | Đối chiếu toàn bộ 34 FR với API/job, screen và bằng chứng test; Google OAuth giữ nguyên ngoại lệ Should-have đã ghi ở Phase 2. |
| P8-02 | Regression Guest/Author/Admin và CJ-01 đến CJ-05 pass ở local integration/wiring suite. |
| P8-03 | Có test nâng database từ migration `RecipeSearchVector` lên release candidate `RecipeDiscoveryIndexes`, kiểm tra dữ liệu và search backfill không mất. |
| P8-04 | Seed idempotent tạo 50 recipe, 5 author và 20 category giả lập; email dùng miền reserved `example.test`, author seed không có password. |

## Traceability execution

| Requirement | API/job/screen chính | Bằng chứng regression | Kết quả local |
|---|---|---|---|
| FR-AUTH-001 | `POST /auth/register`; `/register` | `AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure`; frontend auth/profile wiring | Pass |
| FR-AUTH-002 | `POST /auth/login`; `/login` | `AuthLifecycleTests.FiveInvalidPasswordsLockTheAccountWithoutEnumeratingIt` | Pass |
| FR-AUTH-003 | `POST /auth/google`; `/login` | Phase 2 documented exception | Deferred — Should-have, không thuộc RC hiện tại |
| FR-AUTH-004 | `POST /auth/refresh`; auth provider | refresh/replay/race tests trong `AuthLifecycleTests` | Pass |
| FR-AUTH-005 | `POST /auth/logout`; account menu | lifecycle test kiểm tra logout lặp và token cũ | Pass |
| FR-AUTH-006 | `GET /auth/me`; `/profile` | auth lifecycle và frontend profile wiring | Pass |
| FR-AUTH-007 | `PATCH /auth/me`; `/profile` | auth lifecycle cập nhật profile | Pass |
| FR-CAT-001 | `GET /categories`; public pages | `RecipeCompositionApiTests`; `public-content-wiring.test.mjs` | Pass |
| FR-CAT-002 | `GET /categories/{slug}`; `/categories/[slug]` | publish lifecycle privacy; public content wiring | Pass |
| FR-CAT-003 | `POST /categories`; category admin | `RecipeCoreApiTests.AdminCategorySlugIsStableAndDeleteWithRecipeConflicts`; admin dashboard tests | Pass |
| FR-CAT-004 | `PUT /categories/{id}`; category admin | stable-slug integration test; admin dashboard tests | Pass |
| FR-CAT-005 | `DELETE /categories/{id}`; category admin | delete-conflict integration/UI tests | Pass |
| FR-RCP-001 | public/owner/Admin list; public/dashboard screens | discovery role-cache test; author/admin dashboard tests | Pass |
| FR-RCP-002 | public/owner/Admin detail; detail/edit screens | ownership integration and publication lifecycle tests | Pass |
| FR-RCP-003 | `POST /recipes`; new wizard | recipe core/composition integration; author dashboard tests | Pass |
| FR-RCP-004 | `PUT /recipes/{id}`; edit wizard | stale ETag race integration; conflict UX test | Pass |
| FR-RCP-005 | publish/unpublish endpoints; preview/edit | composition lifecycle; domain invariant; publication wiring | Pass |
| FR-RCP-006 | archive/unarchive endpoints; dashboard | composition lifecycle; author dashboard tests | Pass |
| FR-RCP-007 | `DELETE /recipes/{id}`; dashboard | composition lifecycle và delete command tests | Pass |
| FR-RCP-008 | image endpoints; wizard/gallery | `RecipeCompositionApiTests.ImageUploadValidatesContentAndKeepsDraftMediaPrivate` | Pass |
| FR-RCP-009 | ingredient endpoints; wizard | composition CRUD/order integration và domain validation | Pass |
| FR-RCP-010 | step endpoints; wizard | composition reorder/continuous-number integration | Pass |
| FR-SRCH-001 | `GET /recipes/search`; `/search` | accent/rank/privacy integration; public search wiring | Pass |
| FR-SRCH-002 | list/search filters | discovery combined-filter integration | Pass |
| FR-SRCH-003 | list/search sort | discovery allowlist/invalid-sort integration | Pass |
| FR-SRCH-004 | list/search/category pagination | discovery pagination/boundary integration | Pass |
| FR-FILE-001 | image upload | MIME/signature/size/privacy integration | Pass |
| FR-FILE-002 | object-delete job | image deletion/idempotency path trong composition integration | Pass |
| FR-JOB-001 | welcome-email outbox worker | register transaction/outbox integration; retry schedule giữ theo Phase 2 | Pass với deviation đã ghi: hosted worker thay Hangfire |
| FR-JOB-002 | image processing job | resize/variant assertions trong composition integration | Pass |
| FR-JOB-003 | sitemap recurring job | `SitemapGenerationJobTests`; frontend sitemap/revalidation tests | Pass |
| FR-OBS-001 | `/health/live`, `/health/ready`, `/health` | `LivenessTests` | Pass |
| FR-OBS-002 | correlation/log/audit middleware | audit middleware, admin audit API và behavior tests | Pass |
| FR-OBS-003 | OTEL traces/metrics | Phase 7 observability verification và telemetry wiring | Pass theo Phase 7 local evidence |

## Critical journeys và role regression

| Journey | Bằng chứng chính | Kết quả local |
|---|---|---|
| CJ-01 — register/auto-login/profile | `AuthLifecycleTests.RegisterProfileRefreshLogoutLifecycleIsSecure` | Pass |
| CJ-02 — login/refresh/logout | lockout, rotation/replay/race và logout assertions trong `AuthLifecycleTests` | Pass |
| CJ-03 — Author tạo Draft hoàn chỉnh | `RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous`; image validation test | Pass |
| CJ-04 — publish/discovery | `P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle`; discovery/search/sitemap tests | Pass |
| CJ-05 — ownership/Admin | `RecipeCoreApiTests.P7OwnershipRoleEscalationAndAdminOverrideAreEnforced`; public cache isolation; admin dashboard authorization | Pass |

Role matrix được kiểm tra ở API và UI wiring: Guest chỉ thấy Published; Author chỉ thao tác resource sở hữu; Author khác nhận 403; Admin được override resource policy; route Admin và Hangfire dashboard vẫn role/network gated.

## Migration và dữ liệu seed

- `MigrationUpgradeTests.PreviousReleaseDatabaseUpgradesToReleaseCandidateWithoutDataLoss` dựng PostgreSQL 16 sạch, migrate đến version liền trước, chèn fixture có dấu, migrate lên RC rồi xác minh row còn nguyên, `SearchTitle` được backfill và migration RC đã được ghi nhận.
- `FunctionalRegressionSeedTests.Phase8SeedIsSyntheticCompleteAndIdempotent` chạy initializer lần hai để chứng minh idempotency, sau đó xác minh đúng 5 synthetic Author, 20 Category, 50 Recipe và đủ ba trạng thái Draft/Published/Archived.
- Tài khoản seed dùng `phase8-author-XX@example.test`, không có password hash, secret hay PII thật. Admin seed vẫn tắt mặc định và chỉ nhận credential qua configuration khi được bật rõ ràng.

## Kết quả chạy local

| Gate | Lệnh | Kết quả |
|---|---|---|
| Format/build | `dotnet format ... --verify-no-changes`; `dotnet build ... -c Release` | Pass, 0 warning/error |
| Unit | `dotnet test CulinaryBlog.UnitTests` | 58/58 pass |
| Architecture | `dotnet test CulinaryBlog.ArchitectureTests` | 3/3 pass |
| Functional integration | `dotnet test CulinaryBlog.IntegrationTests --filter FullyQualifiedName!~RecipeDiscoveryPerformanceTests` | 45/45 pass với PostgreSQL/Redis Testcontainers |
| Frontend regression | `npm test` | 54/54 pass |

Chạy lại bộ regression bằng `./scripts/functional-regression.ps1`. Test performance được loại khỏi script này vì thuộc P8-05 đến P8-09.

## Gate còn lại

- Chạy CJ-01 đến CJ-05 bằng browser trên staging với đúng immutable release artifact và PostgreSQL/Redis/MinIO/Hangfire thật.
- Ghi staging run ID, artifact digest và Product Owner sign-off ở P8-24/P8-25.
- Google OAuth chỉ được đưa vào RC khi có Change Request/credential và bổ sung issuer/audience/expiry/link integration test; hiện là Should-have deferred đã công bố từ Phase 2.
