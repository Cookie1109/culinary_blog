# Phase 8 — Hardening, UAT và phát hành

> Trạng thái ngày 04/10/2026: **Implementation Complete / Local Accepted** cho P8-01 đến P8-14. Functional regression, performance và security local đã đạt; full npm audit đã được remediation về 0 High/Critical. P8-15 vẫn No-Go chỉ trong khi chờ ZAP evidence từ immutable staging HTTPS. Reliability, browser E2E/UAT và release sign-off chưa hoàn tất.

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
| Functional integration | `dotnet test CulinaryBlog.IntegrationTests --filter FullyQualifiedName!~RecipeDiscoveryPerformanceTests` | 50/50 pass với PostgreSQL/Redis/MinIO Testcontainers |
| Frontend regression | `npm test` | 61/61 pass |

Chạy lại bộ regression bằng `./scripts/functional-regression.ps1`. Test performance được loại khỏi script này vì thuộc P8-05 đến P8-09.

## Performance P8-05 đến P8-09

| Task | Bằng chứng triển khai | Điều kiện đạt |
|---|---|---|
| P8-05 | `performance/k6/public-api.js` có profile smoke, load và stress; load/stress đạt 100 VU | k6 không có quá 1% request lỗi |
| P8-06 | Custom trends tách cache warm/cold; summary ghi avg/min/p50/p95/p99/max | warm p50 ≤150 ms; mọi API p95 ≤500 ms, p99 ≤1 s |
| P8-07 | k6 lấy delta `cache_hits_total`/`cache_misses_total` từ Prometheus sau khi chờ OTEL export | hit rate steady-state ≥80% |
| P8-08 | Release build, bundle budget và Lighthouse CI 3 lượt/route; report lưu local thay vì upload public | LCP ≤2.5 s, CLS ≤0.1, TBT ≤200 ms, JS gzip ≤200 KB và `meta-description = 1` |
| P8-09 | Chỉ mở tối ưu query/index/cache khi một gate đo lường thất bại | Không có bottleneck vượt budget; không tạo thay đổi speculative |

Stack benchmark dùng `performance/compose.performance.yaml`: API 2 CPU/4 GiB, PostgreSQL 2 CPU/4 GiB, Redis 1 CPU/1 GiB. Global API rate limit được nới riêng trong profile này để một load generator không đo nhầm P8-13; cấu hình mặc định và production không đổi. Dataset là seed synthetic Phase 8 (50 recipe, 5 author, 20 category). Network mặc định là localhost qua Nginx, không giả lập WAN.

Chạy stack và toàn bộ gate:

```powershell
docker compose -p culinary-blog-performance -f compose.yaml -f performance/compose.performance.yaml up --detach --build --wait
./scripts/performance.ps1 -Profile all
```

Chạy nhanh smoke API/cache, bỏ Lighthouse:

```powershell
./scripts/performance.ps1 -Profile smoke -SkipLighthouse
```

Mỗi lần chạy tạo `artifacts/performance/<timestamp>/environment.json`, summary JSON của từng profile k6 và Lighthouse reports. Kết quả local ngày 04/10/2026 được tổng hợp tại [báo cáo performance Phase 8](../../reports/phase-8-performance-report.md); tài liệu không dùng số đo giả hoặc số đo từ development server.

## Security P8-10 đến P8-15

- OWASP review, CORS/headers/TLS configuration, session-token hardening và rate-limit tests đã hoàn tất ở local.
- Gitleaks không thấy secret trong 38 commit; production npm dependencies và hai runtime images có 0 Critical/High. Web runtime dùng Node 22 distroless/non-root.
- ZAP local spider 78 URL với 0 FAIL; 7 nhóm warning đã được manual-triage. `X-Powered-By` được remediation và pass ở vòng quét lại.
- Dev toolchain đã remediation: bỏ `@lhci/cli` và `eslint-config-next`, thay bằng Lighthouse runner trực tiếp cùng ESLint flat config; full npm audit đạt 0 High/Critical.
- P8-15 vẫn No-Go: cần ZAP trên immutable staging HTTPS. Workflow sẽ từ chối scan nếu `/api/v1/release` không khớp commit SHA và hai image digest được khai báo.

Chi tiết threat review, scan evidence, ZAP triage và release decision nằm tại [báo cáo Security Phase 8](../../reports/phase-8-security-report.md).

## Gate còn lại

- Chạy lại `./scripts/performance.ps1 -Profile all` trên immutable artifact của staging trước go/no-go; kết quả local hiện tại không thay thế staging sign-off.
- Chạy `release-images.yml`, deploy API/Web theo digest từ release manifests, cấu hình `Release__CommitSha`, `Release__ApiImageDigest`, `Release__WebImageDigest`, rồi chạy `security-dynamic.yml` trên URL staging HTTPS và manual-triage report trước khi đóng P8-15.
- Chạy CJ-01 đến CJ-05 bằng browser trên staging với đúng immutable release artifact và PostgreSQL/Redis/MinIO/Hangfire thật.
- Ghi staging run ID, artifact digest và Product Owner sign-off ở P8-24/P8-25.
- Google OAuth chỉ được đưa vào RC khi có Change Request/credential và bổ sung issuer/audience/expiry/link integration test; hiện là Should-have deferred đã công bố từ Phase 2.
