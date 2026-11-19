# Đối chiếu chương trình với SRS và tài liệu học

- Ngày kiểm tra: 23/10/2026, Asia/Bangkok.
- Mã nguồn: commit `32e203e0b558446c51d6379c71dbec60644319c9`.
- Nguồn: SRS 71 trang và 13 PDF trong `docs/slidebaigiang`, tổng cộng 390 trang bài giảng.
- Phương pháp: trích xuất văn bản PDF theo trang, đối chiếu yêu cầu với route, API, nghiệp vụ, cấu hình và test; chạy lại test và các kiểm tra chất lượng được ghi bên dưới.
- Đây là báo cáo kiểm tra; không sửa mã chương trình, không triển khai, không commit hoặc push.

> Báo cáo này ghi nhận baseline trước đợt sửa theo yêu cầu tiếp theo. Trạng thái triển khai và bằng chứng mới được theo dõi tại [checklist hoàn thiện](../specs/srs-completion-checklist.md); các con số test và điểm thiếu bên dưới là kết quả tại commit được ghi ở đầu báo cáo.

> Kết luận kiểm tra worktree mới nhất: [Trạng thái đáp ứng SRS](srs-acceptance-status.md). Không dùng bảng “Có” hoặc kết quả test trong báo cáo lịch sử này để chứng nhận hoàn thành toàn bộ SRS hiện tại.

## 1. Kết luận

**Chương trình đã có hầu hết các nhóm chức năng nghiệp vụ của SRS, nhưng chưa đáp ứng đầy đủ tất cả chi tiết chức năng, ràng buộc kiến trúc và tiêu chí nghiệm thu.** Có nhiều kiến thức cốt lõi trong bài giảng đã được áp dụng; không thể nói đã áp dụng toàn bộ nội dung của 13 PDF.

Không quy đổi số test đạt thành phần trăm hoàn thành SRS. Test đang chứng minh một tập hành vi của implementation hiện tại; một số yêu cầu còn thiếu hoặc lệch chưa được test phát hiện.

SRS ghi “27 FR” ở trang 17, nhưng các nhóm được liệt kê ở trang 12 có tổng `7 + 5 + 10 + 4 + 2 + 3 + 3 = 34`. Báo cáo giữ đủ **34 mã FR**, phù hợp với ma trận trong repository.

Các quyết định giải mâu thuẫn tại [baseline](../specs/phase-0/00_SRS_Conformance_and_Resolution.md) là ngữ cảnh đối chiếu, không phải bằng chứng mọi thay đổi khác so với PDF đều đã được người dùng/giảng viên chấp thuận. Cần phân biệt sai khác có lý do kỹ thuật với yêu cầu chưa thực hiện.

## 2. Kết quả kiểm tra thực tế

| Kiểm tra | Kết quả trong lượt kiểm tra này | Phạm vi chứng minh |
|---|---|---|
| Backend unit | **67/67 đạt** | Các unit test hiện có |
| Backend architecture | **3/3 đạt** | Quy tắc dependency giữa assembly |
| Backend integration | **72/72 đạt** | PostgreSQL, Redis, MinIO và SMTP thử nghiệm qua Testcontainers; gồm API, quyền, concurrency, tìm kiếm, migration và mật khẩu |
| Frontend tests | **97/97 đạt** | Gồm kiểm tra source/wiring và test tương tác React trong JSDOM; không phải 97 bài E2E trên browser thật |
| Frontend ESLint | **Đạt** | Không có lỗi theo cấu hình hiện có |
| Frontend TypeScript | **Đạt** | `tsc --noEmit` |
| Unit coverage Application | **72,55% line; 65,11% branch** | Coverlet của bộ unit test; thấp hơn 80% line theo NFR-MAINT-002 |
| Unit coverage Domain | **85,61% line** | Không dùng tỷ lệ này để thay thế mức yêu cầu cho Application |
| Backend format | **Không đạt** | `AuthApiFactory.cs`: thứ tự import `CulinaryBlog.Application.Auth`/`Media` |
| Frontend Prettier | **Không đạt** | `src/features/culinary/pages/auth/Login.tsx` |

Lệnh chính:

```powershell
# Từ backend
dotnet test CulinaryBlog.slnx --no-restore --verbosity quiet
dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --no-build --collect 'XPlat Code Coverage' --results-directory ../tmp/srs-audit/coverage
dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore --verbosity quiet

# Từ frontend
node --test --test-reporter=spec tests/*.test.mjs
node node_modules/eslint/bin/eslint.js . --max-warnings=0
node node_modules/typescript/bin/tsc --noEmit
node node_modules/prettier/bin/prettier.cjs --check .
```

Coverage XML và văn bản PDF theo trang được lưu dưới `tmp/srs-audit/` (được Git ignore). Bản build production frontend đã đạt ở lượt triển khai chức năng mật khẩu trước đó; không chạy lại build trong lượt audit này. Không chạy lại k6, Lighthouse, ZAP hoặc production monitoring; báo cáo cũ của các phép đo đó không được xem là kết quả mới tại commit đang kiểm tra.

Hai lỗi format nằm trong các tệp được thay đổi ở lần bổ sung chức năng mật khẩu vừa rồi. Workflow [CI](../../.github/workflows/ci.yml) chạy cả `dotnet format` và `npm run format`, nên các bước này hiện có thể làm CI thất bại dù test đạt. Cần sửa trước lần phát hành tiếp theo.

## 3. Các điểm thiếu hoặc lệch cần xử lý

| ID | Yêu cầu và vị trí nguồn | Hiện trạng đã xác nhận | Cách xử lý |
|---|---|---|---|
| A01 | CONS-002/008, SRS tr.14–15, 51–52: mỗi use case là Command/Query riêng; validation qua pipeline | Chỉ có 3 handler trong `Application/Features`: list recipe, search recipe và delete recipe. Đăng ký/đăng nhập, category CRUD, tạo/sửa/publish và composition gọi service trực tiếp; endpoint tự gọi validator. | Đưa các use case còn lại qua handler và ValidationBehavior; giữ endpoint ở vai trò HTTP adapter. |
| A02 | SRS tr.50; NFR-USE-003; Chương 4 cũ và mới: React Hook Form + Zod | Dependency đã khai báo nhưng không thấy `useForm`, `useFieldArray` hoặc `zodResolver` trong `frontend/src`. Form chủ yếu dùng `useState` và validation thủ công. Zod được dùng cho env/auth/Problem Details, chưa phải schema form nghiệp vụ. | Tích hợp RHF/Zod cho form đăng ký, hồ sơ, công thức; giữ backend validation. |
| A03 | FR-AUTH-007, SRS tr.23/62: cập nhật tên và AvatarUrl | Backend hỗ trợ `avatarUrl`, nhưng Profile chỉ gửi `name` và `bio`; giao diện chỉ hiển thị ảnh, chưa có ô/luồng sửa ảnh đại diện. | Thêm cách cập nhật avatar và kiểm thử xuyên suốt UI → API. |
| A04 | FR-AUTH-003/004 và Chương 2: Google login và duy trì phiên | Có provider và API xác minh Google ID token, nhưng chưa xác minh OAuth với tài khoản Google thật. Trong callback Google, frontend thu hồi ngay refresh token vừa nhận; không lưu refresh token cho nhánh này, trong khi cơ chế refresh phía client cần token đó. Avatar Google cũng chưa được gán khi tạo user. | Hoàn thiện vòng đời phiên Google, ánh xạ avatar và kiểm thử callback/refresh/expiry. Việc phiên Google không được gia hạn là kết luận từ luồng mã, chưa phải kết quả đăng nhập Google thật. |
| A05 | FR-JOB-001, SRS tr.38: welcome email HTML, link ứng dụng, Hangfire | Có outbox và retry 1/5/30 phút, nhưng dùng `WelcomeEmailWorker : BackgroundService`; nội dung plain text chỉ có lời chào, không có link ứng dụng; không phải WelcomeEmailJob trong Hangfire. | Hoàn thiện template và chọn triển khai đúng Hangfire, hoặc ghi nhận/phê duyệt deviation. Email kích hoạt chỉ yêu cầu “nếu cần”; không tự coi thiếu email confirmation là thiếu FR riêng. |
| A06 | FR-JOB-002, SRS tr.38: medium 800×600, thumbnail 300×300 | `ImageProcessingJob` đang resize theo khung tối đa **1200×900** và **480×480**, giữ tỷ lệ, không phóng lớn ảnh nhỏ. | Chốt lại hợp đồng kích thước; chỉnh code và thêm assertion kích thước ảnh đầu ra theo hợp đồng. |
| A07 | SRS §5.1 tr.46: trang `/` và `/categories` dùng ISR 3600 | Hai page khai báo `revalidate` nhưng gọi `await connection()`, vì vậy route được render theo request. Data Cache vẫn có revalidation; không đồng nghĩa route đang là ISR đúng yêu cầu. | Dùng ISR như SRS hoặc ghi nhận thay đổi rendering có lý do và phép đo. |
| A08 | FR-SRCH-001/002/003, SRS tr.27, 36–37 | Backend có FTS/rank, nhiều filter và sort. UI `/recipes` chỉ có một phần filter/sort; không có minServings, khoảng prepTime, sort giảm dần theo các field như API. Khi tìm từ khóa, page mặc định gửi `sort=newest`; UI không có lựa chọn relevance. DTO không có `relevanceScore`. | Bổ sung UI cho các tiêu chí cần nghiệm thu; ưu tiên relevance cho tìm kiếm và chốt response score. Không coi “có tìm kiếm” là “đã đủ mọi acceptance”. |
| A09 | NFR-USE-004, SRS tr.42: optimistic update, rollback, toast | Có loading, progress ảnh, thông báo inline và component ToastProvider. Không tìm thấy `onMutate` hoặc lời gọi `useToast` trong các luồng nghiệp vụ; phần lớn chờ API xong mới invalidate/refetch. | Bổ sung optimistic update ở thao tác phù hợp, rollback và toast; không bắt buộc optimistic cho mọi mutation nguy hiểm. |
| A10 | NFR-MAINT-002, SRS tr.43: Application unit coverage ≥80%; 5 E2E flows | Coverage đo được **72,55%**. Không tìm thấy bộ Playwright/E2E browser trong source; các frontend test hiện có không thay thế hoàn toàn 5 E2E register/login/create/publish/search. CI chưa có gate coverage 80%. | Thêm test các nhánh chưa được bao phủ, gate coverage và E2E browser thật. |
| A11 | NFR-MAINT-001 và CI | Hai phép kiểm format thất bại như bảng mục 2. | Sắp xếp import và format `Login.tsx`, rồi chạy lại đúng hai gate. |
| A12 | CONS-005; SRS cache/pipeline tr.27–28, 52; baseline GAP-013 | Đã có Redis cache-aside, tag invalidation và CachingBehavior. Không thấy ASP.NET Output Cache hoặc CacheInvalidationBehavior như mô tả; invalidation đặt ở endpoint filter. | Nếu chấm đúng cơ chế, cần triển khai hoặc phê duyệt thay thế; không gọi đây là “thiếu cache”. |
| A13 | FR-CAT-001, SRS tr.24: Name tăng dần | Code ưu tiên `OrderIndex`, sau đó mới `Name`. | Chốt yêu cầu thứ tự danh mục; sửa hoặc ghi nhận việc cho Admin điều khiển thứ tự. |
| A14 | NFR-SEC-003; §5.2 tr.41/47: auth sliding window, rate headers | Có đúng các mức 10/100/5, trả 429 và Retry-After. Auth hiện dùng fixed window; chưa thấy các header X-RateLimit-Limit/Remaining/Reset. Bộ đếm đang nằm trong limiter cục bộ từng instance. | Hoàn thiện theo hợp đồng nếu yêu cầu chấm sát PDF; kiểm tra giới hạn khi chạy nhiều API instance. |
| A15 | NFR-SEO-002/003/004, SRS tr.44–45 | Có metadata, canonical, JSON-LD, robots và sitemap. Sitemap không có changefreq/priority; chưa có cơ chế lịch sử slug + 301 cho draft đổi slug. Helper metadata chưa giới hạn title 60 ký tự hoặc bảo đảm description tối thiểu 150 ký tự. | Chốt chi tiết SEO cần giữ theo SRS và thêm test. Những thuộc tính đã lỗi thời cần sửa yêu cầu thay vì thêm máy móc. |
| A16 | NFR-PERF/REL/SCALE/USE và nghiệm thu | Có script/report local, nhưng UAT staging vẫn Pending; chưa có bằng chứng mới cho uptime 99,5%, 100 concurrent users trên artifact hiện tại, field INP, browser matrix, screen reader, scaling/CDN/MinIO multi-node thực tế. | Hoàn thành đo lường và nghiệm thu đúng môi trường; phân biệt cấu hình có sẵn với bằng chứng đạt mục tiêu. |

### Dẫn chứng mã nguồn chính

- A01: [AuthEndpoints](../../backend/src/CulinaryBlog.Api/Presentation/AuthEndpoints.cs), [ContentEndpoints](../../backend/src/CulinaryBlog.Api/Presentation/ContentEndpoints.cs), [Application/Features](../../backend/src/CulinaryBlog.Application/Features), [ContentService](../../backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs).
- A02/A09: [RecipeEditor](../../frontend/src/features/culinary/pages/dashboard/RecipeEditor.tsx), [RecipeCompositionWizard](../../frontend/src/features/culinary/pages/dashboard/RecipeCompositionWizard.tsx), [ToastProvider](../../frontend/src/components/ui/toast.tsx).
- A03: [Profile](../../frontend/src/features/culinary/pages/Profile.tsx), [AuthService](../../backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs).
- A04: [Auth.js callbacks](../../frontend/src/auth.ts), [AuthContext refresh](../../frontend/src/features/culinary/contexts/AuthContext.tsx), [session token store](../../frontend/src/lib/api/session-token-store.ts).
- A05/A06: [WelcomeEmailWorker](../../backend/src/CulinaryBlog.Infrastructure/Email/WelcomeEmailWorker.cs), [ImageProcessingJob](../../backend/src/CulinaryBlog.Infrastructure/Jobs/ImageProcessingJob.cs).
- A07: [Home page](../../frontend/src/app/(public)/render-public/home/[key]/page.tsx), [Categories page](../../frontend/src/app/(public)/render-public/categories/[key]/page.tsx). Theo [tài liệu Next.js](https://nextjs.org/docs/app/api-reference/functions/connection), `connection()` chủ động chuyển phần bên dưới sang dynamic rendering.
- A08: [Recipes route](../../frontend/src/app/(public)/recipes/page.tsx), [RecipesList](../../frontend/src/features/culinary/pages/RecipesList.tsx), [ContentContracts](../../backend/src/CulinaryBlog.Application/Content/ContentContracts.cs), [Search query](../../backend/src/CulinaryBlog.Application/Features/Recipes/Queries/SearchPublishedRecipesQuery.cs).
- A10/A11: [CI](../../.github/workflows/ci.yml), [frontend tests](../../frontend/tests), [AuthApiFactory](../../backend/tests/CulinaryBlog.IntegrationTests/AuthApiFactory.cs), [Login](../../frontend/src/features/culinary/pages/auth/Login.tsx).
- A12/A14: [RedisApplicationCache](../../backend/src/CulinaryBlog.Infrastructure/Caching/RedisApplicationCache.cs), [PublicCacheInvalidationFilter](../../backend/src/CulinaryBlog.Api/Presentation/PublicCacheInvalidationFilter.cs), [rate limiting](../../backend/src/CulinaryBlog.Api/Presentation/DependencyInjection.cs).
- A15/A16: [SitemapXml](../../backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapXml.cs), [SEO helper](../../frontend/src/lib/seo.ts), [UAT](../release/uat-signoff.md), [Phase 8](../specs/phase-8/README.md).

## 4. Đối chiếu đủ 34 FR

“Có” dưới đây nghĩa là có năng lực nghiệp vụ và bằng chứng implementation/test liên quan. Không có nghĩa mọi chi tiết kiến trúc, UI và NFR đi kèm đều đã đạt. Các sai khác chung ở A01–A16 vẫn áp dụng.

| Mã | Chức năng | Kết luận | Bằng chứng/điểm còn lại |
|---|---|---|---|
| FR-AUTH-001 | Đăng ký, Author mặc định, auto-login | Có; welcome email chưa khớp đầy đủ | AuthEndpoints, AuthService; AuthLifecycleTests; A01/A05 |
| FR-AUTH-002 | Login email/password, lockout | Có | AuthLifecycleTests, AuthRateLimitTests; A01/A14 |
| FR-AUTH-003 | Google OAuth | Một phần, chưa xác minh OAuth thật | Có provider và backend validation; A04 |
| FR-AUTH-004 | Refresh rotation/reuse detection | Có cho local login; nhánh Google còn thiếu | AuthLifecycleTests; A04 |
| FR-AUTH-005 | Logout, revoke refresh token | Có | AuthLifecycleTests. Theo baseline GAP-007 còn cần chốt kiểm tra owner nếu bearer khác chủ token được gửi kèm; endpoint hiện không đối chiếu principal với token owner. |
| FR-AUTH-006 | Xem hồ sơ | Có | GET /auth/me, Profile; AuthLifecycleTests |
| FR-AUTH-007 | Cập nhật hồ sơ | Một phần trên UI | API sửa tên/avatar/bio; UI chưa sửa avatar; A03 |
| FR-CAT-001 | Danh sách danh mục + số recipe Published | Có, lệch thứ tự | ContentService, Categories/Home; A13 |
| FR-CAT-002 | Chi tiết danh mục + recipe phân trang | Có theo public/private split của baseline | Category detail route, RecipeDiscoveryApiTests |
| FR-CAT-003 | Admin tạo danh mục | Có | CategoryAdmin, RecipeCoreApiTests |
| FR-CAT-004 | Admin sửa danh mục | Có | CategoryAdmin, RecipeCoreApiTests |
| FR-CAT-005 | Admin xóa danh mục rỗng | Có | Kiểm tra recipe tham chiếu, soft delete theo baseline; RecipeCoreApiTests |
| FR-RCP-001 | List recipe, filter/sort/page, private owner/Admin | Có API; UI chưa đủ mọi tùy chọn | RecipeDiscoveryApiTests; A08 |
| FR-RCP-002 | Chi tiết recipe và composition | Có | RecipeDetail, RecipeCompositionApiTests, ownership/privacy tests |
| FR-RCP-003 | Tạo Draft | Có qua wizard nhiều bước | RecipeEditor, RecipeCompositionApiTests; không có CreateRecipeCommand như A01 |
| FR-RCP-004 | Update, ownership, ETag/concurrency | Có | RecipeCoreApiTests.StaleRecipeUpdateReturnsConflictAndCurrentEtag |
| FR-RCP-005 | Publish/unpublish | Có | Invariant ingredient+step theo baseline; RecipeCompositionApiTests |
| FR-RCP-006 | Archive/unarchive | Có | RecipeCompositionApiTests.P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle |
| FR-RCP-007 | Xóa recipe | Có soft delete theo baseline | DeleteRecipeCommand, unit và integration tests; khác đoạn hard delete của PDF nhưng phù hợp NFR-REL-003 |
| FR-RCP-008 | Upload, primary, delete ảnh | Có | MIME/size/signature/privacy checks; RecipeCompositionApiTests |
| FR-RCP-009 | CRUD nguyên liệu | Có | RecipeCompositionWizard, composition API tests |
| FR-RCP-010 | CRUD bước và renumber | Có | Composition API tests kiểm tra thứ tự liên tục |
| FR-SRCH-001 | FTS không dấu, rank | Có API; một số acceptance/UI chưa đủ | RecipeDiscoveryApiTests; A08 |
| FR-SRCH-002 | Lọc kết hợp AND | Có API; UI một phần | API hỗ trợ rộng hơn UI; A08 |
| FR-SRCH-003 | Sắp xếp nhiều field/chiều | Có API list; UI một phần | A08 |
| FR-SRCH-004 | Phân trang | Có | PageEnvelope, giới hạn pageSize, boundary tests |
| FR-FILE-001 | MinIO upload và kiểm tra file | Có | IFileStorageService, MinioFileStorageService; upload/privacy integration |
| FR-FILE-002 | Xóa object idempotent, retry | Có | ObjectDeletionJob, media outbox |
| FR-JOB-001 | Welcome email job | Một phần so với yêu cầu chính xác | Outbox/retry có; HTML/link/Hangfire thiếu; A05 |
| FR-JOB-002 | Resize medium/thumbnail | Có xử lý nhưng kích thước lệch | A06 |
| FR-JOB-003 | Sitemap 02:00 UTC, retry | Có phần chính | Program, SitemapGenerationJob, SitemapGenerationJobTests; chi tiết A15 |
| FR-OBS-001 | Health aggregate/live/ready | Có | LivenessTests, health check implementations |
| FR-OBS-002 | Structured log, correlation, audit | Có phần chính; pipeline chưa phủ mọi use case | Program, middleware, audit tests, A01; context UserId/RequestPath chưa được chứng minh trên mọi log entry |
| FR-OBS-003 | Tracing/metrics | Có cấu hình/instrumentation | TelemetryExtensions, metrics, OTLP; chưa đo lại exporter/monitoring production trong lượt này |

Đổi mật khẩu và quên mật khẩu là chức năng bổ sung theo yêu cầu trực tiếp của người dùng, đã có API/UI/SMTP và test. SRS v1.0.0 không liệt kê chúng thành FR-AUTH riêng; không cần sửa số 34 thành 36.

## 5. Đối chiếu từng tài liệu học

Các slide là tài liệu kiến thức, bao gồm cả lựa chọn thay thế và bài tập mở rộng. Không mặc định mọi ví dụ trong slide là yêu cầu sản phẩm. Trạng thái dưới đây đánh giá việc áp dụng kiến thức, không đánh giá khả năng giải thích lý thuyết của người học.

| Tài liệu | Trang PDF | Kiến thức đã thấy trong dự án | Phần chưa áp dụng/khác hoặc cần hoàn thiện |
|---|---:|---|---|
| [Chương 1: Web và RESTful API](../slidebaigiang/Chuong_01_Kien_truc_Web_va_RESTful_API.pdf) | 27 | 4 project, DI, Minimal APIs, REST/versioning, Problem Details, OpenAPI/Scalar, CQRS có triển khai | CQRS mới 3 handler, endpoint còn làm validation/orchestration; A01 |
| [Chương 2: Authentication/Authorization](../slidebaigiang/Chuong_02_Xac_thuc_Phan_quyen_va_Bao_mat_API.pdf) | 31 | Identity/PBKDF2, JWT, rotation/reuse, role/policy/resource checks, CORS, rate limit, lockout | Google chưa đủ vòng đời phiên và chưa test thật; một số cấu hình mẫu khác SRS, nên lấy SRS làm chuẩn |
| [Chương 3: Dữ liệu/tối ưu](../slidebaigiang/Chuong_03_Thiet_ke_Du_lieu_va_Toi_uu_Truy_van.pdf) | 28 | PostgreSQL, EF Core, migrations, relationships/owned nutrition, index/GIN, FTS/unaccent, EXPLAIN benchmark | Repository/UoW mới được sử dụng trực tiếp trong delete handler; không có CategoryRepository hoặc CreateRecipeCommandHandler. Có package Bogus nhưng initializer hiện dùng VietnameseSeedData, không dùng Faker. |
| [Chương 4: Next.js bản cũ](../slidebaigiang/Chuong_04_Kien_truc_Frontend_voi_Nextjs.pdf) | 33 | Next.js 15 App Router, route groups, server fetch, client hooks, TanStack Query, Auth.js, Axios interceptors, Zustand, next/image, SEO | RHF/Zod form, useFieldArray, Server Actions, useInfiniteQuery, optimistic update chưa thấy; A02/A07/A09 |
| [Chương 4: Next.js Updated](../slidebaigiang/Chuong_04_Kien_truc_Frontend_voi_Nextjs_Updated.pdf) | 53 | Nhiều nền tảng trùng bản cũ; đã có callback `/api/revalidate` | Slide dùng Next.js 16.4/Cache Components/PPR/proxy.ts; dự án vẫn Next.js 15/middleware.ts. Chưa thấy use cache/cacheLife/cacheTag, useActionState, Server Actions, queryOptions như bài cập nhật. Đây là chênh lệch chương trình học với SRS, không tự coi giữ Next.js 15 là sai SRS. |
| [S05: EF Core cơ bản](../slidebaigiang/S05_ORM_EF_Core_Basic.pdf) | 24 | DbContext/DbSet, Fluent API, quan hệ, migrations, LINQ, tracking/SaveChanges, AsNoTracking/projection | Dự án dùng PostgreSQL theo SRS; không cần đổi sang SQL Server chỉ để giống ví dụ slide |
| [S06: EF Core nâng cao](../slidebaigiang/S06_EF_Core_Advanced.pdf) | 20 | Transaction, concurrency token, query filters, parameterized SQL, owned type, ExecuteUpdate, audit interceptor, Testcontainers | Chưa thấy đầy đủ compiled model/query, JSON mapping, stored procedure/DbFunction, LEFT/RIGHT JOIN mới. Không bắt buộc thêm nếu không có nhu cầu. Version bigint là lựa chọn PostgreSQL thay rowversion SQL Server. |
| [S07: JWT Authentication](../slidebaigiang/S07_JWT_Authentication.pdf) | 28 | Claims/signature/lifetime, JWT bearer, refresh hash/rotation, policies, CORS/rate limit/lockout, token storage | Google còn A04; chưa có 2FA, một chủ đề mở rộng của slide |
| [S08: Repository/UoW](../slidebaigiang/S08_Repository_UnitOfWork.pdf) | 19 | IRepository, IRecipeRepository, UnitOfWork, delete handler phụ thuộc abstraction; unit test mock | Chưa áp dụng vào phần lớn command. Chưa thấy Specification hay UoW nhiều repository. Slide cũng cho phép mô hình lai và nêu tradeoff; không cần bọc mọi query cho có pattern. |
| [S09: Query Optimization](../slidebaigiang/S09_Query_Optimization.pdf) | 22 | Index/composite/partial indexes, parameterization, AsNoTracking, truy vấn theo batch tránh N+1, offset paging, EXPLAIN ANALYZE trên 10.000 recipe trong test | Chưa có cursor paging, partitioning, indexed views, compiled query. Đây là kỹ thuật lựa chọn; SRS yêu cầu offset nên không đánh dấu thiếu chức năng vì chưa có cursor. |
| [S10: Caching](../slidebaigiang/S10_Caching_Intro.pdf) | 25 | Redis cache-aside, key/tags/TTL, invalidation, failure fallback, metric hit/miss, ETag cho concurrency, Next data revalidation | Không thấy ASP.NET Output Cache, distributed stampede protection, SWR/write-behind/negative cache đầy đủ. ETag If-Match hiện chủ yếu cho concurrency; chưa thấy If-None-Match/304 HTTP revalidation. Không cần dùng đồng thời mọi caching pattern. |
| [S11: Security/Identity](../slidebaigiang/S11_Security_And_Identity.pdf) | 55 | Identity, roles/policies/resource-based auth, chống thao tác recipe người khác, JWT/refresh, security stamp khi đổi mật khẩu, secret configuration | Google A04; chưa có Keycloak/BFF hoàn chỉnh, 2FA, passkey, UI quản trị user/role. MapIdentityApi và local JWT/IdP ngoài là các hướng thay thế, không phải bắt buộc dùng cùng nhau. |
| [S12: Logging](../slidebaigiang/S12_Logging_Intro.pdf) | 25 | ILogger, LoggerMessage, structured Serilog, scope CorrelationId, console/file/Seq, retention, audit riêng, telemetry | MediatR logging chưa phủ use case bypass handler; frontend logging/telemetry và context đầy đủ trên mọi log entry chưa được chứng minh; cần test log sink failure nếu nghiệm thu phần này |

## 6. Những điểm không nên đánh giá nhầm là thiếu chức năng

1. **Trang chủ đã có danh mục.** Home hiển thị `Danh mục nổi bật`, lấy dữ liệu API, dùng carousel, có nút xem tất cả và liên kết tới category detail. SRS §5.1 tr.46 chỉ ghi featured recipes + categories, không bắt buộc sidebar. Vì vậy thiếu sidebar là khác với checklist ảnh trước đó, không phải một lỗi riêng của SRS này.
2. `/search` đã được redirect sang `/recipes`; `/auth/login` và `/auth/register` cũng có redirect sang route hiện tại. Không coi mất trang tìm kiếm/đăng nhập chỉ vì tên route trong source đã đổi. Cần chốt hợp đồng redirect nếu giảng viên kiểm tra đúng URL/status 301 so với 308.
3. Soft delete, private bucket, public chỉ thấy Published, search `simple + unaccent` và version bigint là các quyết định có lý do được ghi trong baseline để giải các mâu thuẫn PDF. Không tự đổi lại hard delete/bucket public/SQL Server rowversion.
4. Comments, likes/ratings/bookmarks, realtime, nhắn tin và commerce có xuất hiện trong ví dụ/bài tập nhưng bị loại khỏi phạm vi SRS §1.2.3. Nếu cần nộp riêng bài thực hành tương ứng, đánh giá như bài tập bổ sung, không như FR còn thiếu của Culinary Blog v1.
5. Next.js 16.4 của slide Updated và Next.js 15 của SRS không thể cùng là version hiện hành của một app. Cần giảng viên/người dùng chốt chuẩn ưu tiên trước khi nâng phiên bản; không nâng chỉ để giống mọi ví dụ.
6. Yêu cầu ping sitemap Google trong PDF đã lỗi thời: Google thông báo endpoint này bị ngừng, ping trả 404. Dùng khai báo sitemap qua robots.txt và Search Console; nên cập nhật SRS thay vì thêm một request vô ích. Nguồn: [Google Search Central](https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping).

## 7. Thứ tự hoàn thiện đề xuất

1. Sửa hai lỗi format làm hỏng gate CI; thêm coverage gate và nâng Application coverage từ 72,55% lên ít nhất 80%.
2. Hoàn thiện avatar hồ sơ, vòng đời phiên Google, kích thước resize và welcome email; bổ sung acceptance tests cho đúng yêu cầu thay vì chỉ test implementation hiện tại.
3. Đưa các use case còn lại qua CQRS/MediatR/ValidationBehavior và mở rộng Repository/UoW ở command cần thiết. Tránh tạo abstraction để dùng mọi pattern trong slide.
4. Tích hợp RHF/Zod cho form; hoàn thiện filter/sort/relevance và phản hồi UX; chốt ISR và các sai khác cache/SEO/route.
5. Có 5 E2E browser thật và hoàn tất staging UAT, accessibility/browser matrix, load/security/operational evidence trên đúng artifact.
6. Chỉ bổ sung Next.js 16/PPR, 2FA/Keycloak, cursor/compiled query/partitioning và các bài tập ngoài SRS khi đó là mục tiêu học hoặc yêu cầu bổ sung được chọn rõ ràng.

## 8. Nguồn PDF

Các đường dẫn PDF trong mục 5 và tài liệu dưới đây là nguồn kiểm tra; số trang được ghi theo vị trí vật lý trong PDF, không theo footer nếu hai số khác nhau.

- [SRS v1.0.0](../specs/SRS_Culinary_Blog_v1.0.0.pdf): §2.5 tr.14–15; FR tr.17–39; NFR tr.40–45; UI/API tr.46–48; architecture tr.50–52; data/API contract tr.54–66.
- 13 tài liệu học đã được liệt kê và đối chiếu riêng tại mục 5.

Tại thời điểm lập báo cáo gốc, chưa sửa hoặc push thay đổi chức năng từ kết quả kiểm tra này. Các PDF đang được người dùng chuyển sang `docs/slidebaigiang` được giữ nguyên.
