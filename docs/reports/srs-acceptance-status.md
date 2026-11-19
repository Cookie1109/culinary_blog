# Trạng thái đáp ứng SRS — 23/10/2026

**Cập nhật 11/11/2026.** Xem [bàn giao G07](../specs/srs-completion-phases/reports/package-G07-result.md) để biết kết quả của bản demo hiện hành, các issue GitHub và điều kiện nghiệm thu còn thiếu. Các phép đo bên dưới thuộc đợt audit ngày 23/10/2026.

**Kết luận: hệ thống chưa hoàn thành toàn bộ SRS và chưa đủ điều kiện ghi “đã nghiệm thu 100%”.** Phần lớn nhóm nghiệp vụ đã có mã nguồn, giao diện và test; vẫn còn sai khác được xác nhận, các kiểm tra chưa đạt và các tiêu chí chưa có bằng chứng trên bản cuối.

## 1. Phạm vi và cách đọc

- Nguồn yêu cầu: [SRS v1.0.0](../specs/SRS_Culinary_Blog_v1.0.0.pdf), 71 trang. Giữ đủ **34 FR**, **30 NFR** và **10 CONS** được đánh mã; SRS có lỗi đếm ở phần tổng hợp.
- PDF đã được đọc lại trong lượt này; SHA-256: `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`. Các dòng FR/NFR/CONS trong báo cáo đã được kiểm tra đủ số lượng, không trùng mã; các liên kết local của báo cáo/checklist đã được kiểm tra tồn tại.
- Kiểm tra worktree ngày 23/10/2026, HEAD `32e203e0b558446c51d6379c71dbec60644319c9`, có nhiều thay đổi **chưa commit**. Không đồng nhất worktree này với mã đã push GitHub hoặc container đã triển khai.
- Đây là kết quả đối chiếu mã/test và chạy các kiểm tra ghi ở mục 2. Không phải xác nhận của giảng viên/Product Owner, không phải kiểm chứng production và không phải kiểm tra thủ công mọi acceptance của PDF.
- **Có triển khai**: tìm thấy mã và test liên quan. Trạng thái này chưa chứng nhận mọi acceptance của FR đã đạt.
- **Một phần / chưa đạt**: có chi tiết thiếu, lệch hoặc phép kiểm tra thất bại được chỉ ra cụ thể.
- **Chưa đủ bằng chứng**: chưa chứng minh trên đúng phiên bản/môi trường yêu cầu; không tự suy ra chức năng đó chưa được viết.
- [Baseline giải mâu thuẫn](../specs/phase-0/00_SRS_Conformance_and_Resolution.md) là quyết định trong repository. Các sai khác so với nguyên văn PDF cần được công khai khi nghiệm thu; nhãn “Accepted for implementation” trong tài liệu không thay thế bằng chứng giảng viên/người nghiệm thu đã chấp thuận.
- [Checklist khắc phục](../specs/srs-completion-checklist.md) chỉ theo dõi sáu nhóm sửa sau audit. [Audit ban đầu](srs-and-course-audit-2026-10-23.md) là ảnh chụp trạng thái cũ. Dùng báo cáo này để đọc trạng thái kiểm tra mới, không cộng các dấu `[x]` thành phần trăm hoàn thành SRS.

## 2. Kiểm tra chạy lại trong lượt này

Runtime frontend: Node **22.23.3**, Next.js **15.5.28**. Log/TRX/coverage tại `tmp/srs-audit/current-conformance/` (Git ignore).

| Kiểm tra | Kết quả mới | Phạm vi và giới hạn |
|---|---|---|
| Backend unit | **75/75 đạt** | Bộ unit hiện có, không thay thế integration/E2E |
| Backend architecture | **3/3 đạt** | Quy tắc dependency assembly trong các test hiện có |
| Application unit coverage | **62,56% line; 67,24% branch** | **Chưa đạt ≥80% line** của NFR-MAINT-002; Domain 85,61% không thay thế chỉ số Application |
| Backend integration | **41 đạt, 58 lỗi khởi tạo môi trường / 99 tổng** | Toàn bộ 58 lỗi chứa `DockerUnavailableException`; Docker CLI cũng không kết nối được Docker Engine. Không dùng chúng làm bằng chứng lỗi nghiệp vụ, cũng không báo 99/99 đạt trong lượt này |
| Frontend tests | **104/104 đạt** | Có kiểm tra source/wiring và React/JSDOM; không phải năm luồng E2E trên browser thật |
| Production build | **Đạt** | Có cảnh báo cấu hình ESLint thiếu Next.js plugin; build không tự chứng minh đủ NFR |
| ESLint / TypeScript | **Đạt / đạt** | Theo cấu hình hiện có |
| Backend format | **Đạt** | `dotnet format --verify-no-changes` |
| Frontend Prettier | **Không đạt** | Sáu file: `next.config.mjs`, `scripts/check-isr.mjs`, `src/middleware.ts`, `tests/public-content-wiring.test.mjs`, `tests/rendering-cache-wiring.test.mjs`, `tests/visual-responsive-regression.test.mjs` |
| Production ISR qua HTTP | **Chưa đạt cả bài kiểm tra** | Các bước MISS/HIT, `s-maxage=3600`, secret revalidation và nội dung mới của `/` và `/categories` đã chạy qua; bài dừng ở `/categories/missing-category`: **200 thay vì 404** |

Kiểm tra ISR chạy ứng dụng Next standalone thật với **Postman simulation local**, dữ liệu synthetic từ hợp đồng OpenAPI của chính dự án. Không gọi backend production, không dùng dữ liệu người dùng. Các route đứng sau assertion thất bại chưa được kiểm tra trong lần chạy này. Việc refresh qua callback được kiểm tra; chưa chứng minh xuyên suốt mutation backend → scheduler → callback trên stack thật. Log HTTP dependency ở `tmp/srs-audit/isr/`.

Nhật ký checklist ghi một đợt trước đạt 99/99 integration; đó là bằng chứng lịch sử, không thay thế lần chạy mới khi Docker chưa khả dụng. Không sửa chương trình, khởi động stack, áp dụng migration, commit hoặc push trong lượt đánh giá này.

Lệnh tái kiểm tra (backend từ `backend/`, frontend từ `frontend/`, Node theo phiên bản nêu trên):

```powershell
dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --no-restore --collect 'XPlat Code Coverage'
dotnet test tests/CulinaryBlog.ArchitectureTests/CulinaryBlog.ArchitectureTests.csproj --no-restore
dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj --no-restore
dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore

npm test
npm run build
npm run lint
npm run typecheck
npm run format
npm run test:isr -- --skip-build
```

Lệnh ISR cuối dùng build vừa tạo; không áp dụng `--skip-build` khi mã nguồn đã thay đổi so với build. Các exit code và log của lượt này được lưu cùng thư mục bằng chứng.

## 3. Các điểm đang ngăn kết luận hoàn thành

| ID | Kết luận có căn cứ | Việc cần đóng để nghiệm thu |
|---|---|---|
| G01 | Application coverage 62,56%, CI chưa có gate ≥80%; chưa có bộ Playwright năm luồng | Kiểm thử các nhánh nghiệp vụ còn thiếu, gate coverage và browser E2E register/login/create/publish/search theo NFR-MAINT-002 |
| G02 | Google callback trong `frontend/src/auth.ts` bỏ refresh token khỏi session và gọi revoke ngay sau exchange | Hoàn thiện refresh/logout nhánh Google; kiểm thử token expiry và OAuth callback, ghi riêng bằng chứng provider thật |
| G03 | ISR trang chủ/categories có tiến triển và qua các assertion đầu, nhưng bài HTTP chưa xanh; category không tồn tại trả 200; build còn phân loại detail routes là dynamic | Sửa HTTP 404, kiểm chứng loại rendering/TTL theo §5.1 và invalidation xuyên suốt sau mutation; kiểm tra cả đường dẫn nội bộ |
| G04 | Auth limiter vẫn fixed window, chưa có X-RateLimit-Limit/Remaining/Reset; bộ đếm theo process | Hoàn thiện NFR-SEC-003 và §5.2; chứng minh hành vi nhiều instance, không coi cấu hình 10/100/5 là toàn bộ yêu cầu |
| G05 | Có Redis cache-aside và CacheInvalidationBehavior; không tìm thấy ASP.NET Output Cache | Chốt/triển khai cơ chế theo baseline GAP-013 hoặc ghi sai khác được chấp thuận; kiểm tra TTL, privacy và invalidation |
| G06 | SEO helper chỉ cắt description tối đa 160, không bảo đảm 150–160 hoặc title ≤60; sitemap thiếu changefreq/priority; chưa thấy lịch sử slug + 301 | Đóng chi tiết NFR-SEO-002/003/004; yêu cầu ping trong PDF cần quyết định thay đổi riêng nếu bỏ |
| G07 | CQRS đã mở rộng nhiều; audit/sitemap endpoint vẫn gọi dependency trực tiếp. Một số validation vẫn ở HTTP/service; nghiệp vụ lớn còn nằm trong Infrastructure ContentService | Đối chiếu CONS-002/008 và §6.2–6.3 cho mọi use case; test pipeline/quyền/transaction. Không báo “đã đủ CQRS” chỉ vì có handler chuyển tiếp |
| G08 | Prettier đang lỗi sáu file | Sửa và chạy lại gate trên worktree cuối trước khi ghi CI đạt |
| G09 | UAT vẫn Pending; báo cáo load/security/restore là kết quả các đợt cũ/local | Có artifact xác định và bằng chứng mới cho các mục cần staging/production, browser matrix, screen reader, CWV/INP, tải, security, phục hồi và vận hành |

Nguồn trực tiếp: [CI](../../.github/workflows/ci.yml), [Google callback](../../frontend/src/auth.ts), [rate limiter](../../backend/src/CulinaryBlog.Api/Presentation/DependencyInjection.cs), [Content endpoints](../../backend/src/CulinaryBlog.Api/Presentation/ContentEndpoints.cs), [ContentService](../../backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs), [SEO helper](../../frontend/src/lib/seo.ts), [sitemap XML](../../backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapXml.cs), [UAT](../release/uat-signoff.md).

## 4. Đối chiếu 34 yêu cầu chức năng

Mục này đánh giá năng lực nghiệp vụ riêng; các điều kiện chung G01–G09 và CONS/NFR vẫn áp dụng. “Có triển khai” không có nghĩa mọi endpoint đã được chạy lại thành công trong lượt Docker không khả dụng.

Các bộ bằng chứng được dẫn trong bảng:

- **AUTH**: [AuthEndpoints](../../backend/src/CulinaryBlog.Api/Presentation/AuthEndpoints.cs), [AuthService](../../backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs), [AuthLifecycleTests](../../backend/tests/CulinaryBlog.IntegrationTests/AuthLifecycleTests.cs), [Profile](../../frontend/src/features/culinary/pages/Profile.tsx).
- **CORE**: [ContentEndpoints](../../backend/src/CulinaryBlog.Api/Presentation/ContentEndpoints.cs), [ContentService](../../backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs), [RecipeCoreApiTests](../../backend/tests/CulinaryBlog.IntegrationTests/RecipeCoreApiTests.cs), [RecipeCompositionApiTests](../../backend/tests/CulinaryBlog.IntegrationTests/RecipeCompositionApiTests.cs).
- **DISC**: [RecipeDiscoveryApiTests](../../backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryApiTests.cs), [RecipesList](../../frontend/src/features/culinary/pages/RecipesList.tsx), [search query](../../backend/src/CulinaryBlog.Application/Features/Recipes/Queries/SearchPublishedRecipesQuery.cs).
- **JOB**: [WelcomeEmailJobTests](../../backend/tests/CulinaryBlog.IntegrationTests/WelcomeEmailJobTests.cs), [ImageProcessingJob](../../backend/src/CulinaryBlog.Infrastructure/Jobs/ImageProcessingJob.cs), [ObjectDeletionJob](../../backend/src/CulinaryBlog.Infrastructure/Jobs/ObjectDeletionJob.cs), [SitemapGenerationJobTests](../../backend/tests/CulinaryBlog.IntegrationTests/SitemapGenerationJobTests.cs).
- **OBS**: [Program](../../backend/src/CulinaryBlog.Api/Program.cs), [LivenessTests](../../backend/tests/CulinaryBlog.IntegrationTests/LivenessTests.cs), [AuditMutationMiddlewareTests](../../backend/tests/CulinaryBlog.IntegrationTests/AuditMutationMiddlewareTests.cs), [TelemetryExtensions](../../backend/src/CulinaryBlog.Api/Telemetry/TelemetryExtensions.cs).

| Mã SRS | Trạng thái triển khai | Bằng chứng và phần còn lại |
|---|---|---|
| FR-AUTH-001 | Có triển khai | AUTH + JOB: đăng ký Author, auto-login, outbox welcome HTML/Hangfire đã bổ sung; chưa chạy lại được phần DB/SMTP trong lượt này |
| FR-AUTH-002 | Có triển khai | AUTH: login, sai mật khẩu/lockout; rate limit còn G04, browser E2E còn G01 |
| FR-AUTH-003 | **Một phần** | Có provider, backend xác minh token và ánh xạ avatar user mới; vòng đời phiên Google còn G02, chưa xác minh provider thật |
| FR-AUTH-004 | **Một phần** | Rotation/reuse detection cho local có mã/test AUTH; nhánh Google còn G02 |
| FR-AUTH-005 | **Một phần khi đối chiếu baseline** | Có revoke/idempotent logout. Endpoint/command chưa truyền principal để kiểm tra bearer khác chủ refresh token theo GAP-007; nhánh Google còn G02 |
| FR-AUTH-006 | Có triển khai | AUTH: GET me và UI profile; kiểm tra unauthenticated/user không tồn tại cần giữ trong lần integration xanh |
| FR-AUTH-007 | Có triển khai | AUTH: sửa tên/bio/avatar, xóa avatar; đã bổ sung form/test React và test API không cập nhật một phần khi URL sai |
| FR-CAT-001 | Có triển khai | CORE + DISC: danh mục, đếm Published, đã đổi Name tăng dần; cache/rendering còn G03/G05 |
| FR-CAT-002 | **Một phần** | CORE + DISC có phân trang/nội dung Published; HTTP UI category không tồn tại trả 200, rendering còn G03 |
| FR-CAT-003 | Có triển khai | CORE: Admin tạo, tên/slug unique; kiến trúc/validation còn G07 |
| FR-CAT-004 | Có triển khai | CORE: Admin sửa, slug ổn định; cần lần integration xanh trên bản cuối |
| FR-CAT-005 | Có triển khai theo baseline | CORE: từ chối xóa danh mục có recipe, soft delete theo GAP-009; không gọi soft delete là khớp mọi câu hard delete trong PDF |
| FR-RCP-001 | Có triển khai | CORE + DISC: public/private list, bộ lọc đã mở rộng, sort/phân trang; browser E2E và cache còn G01/G05 |
| FR-RCP-002 | Có triển khai phần nghiệp vụ | CORE: detail/composition, private owner/Admin; rendering/SEO/HTTP boundary chưa nghiệm thu đủ, G03/G06 |
| FR-RCP-003 | Có triển khai | CORE: Draft và wizard; CreateRecipeAsync đã dùng unitOfWork. Nhận xét cũ “chỉ delete dùng UoW” không còn dùng làm kết luận |
| FR-RCP-004 | Có triển khai | CORE: sửa/ownership/ETag/stale conflict; lịch sử slug/301 còn G06 |
| FR-RCP-005 | Có triển khai theo baseline | CORE: publish/unpublish, ingredient + step theo GAP-010; cache/invalidation xuyên suốt còn G03/G05 |
| FR-RCP-006 | Có triển khai | CORE: archive/unarchive; cần nghiệm thu visibility sau thay trạng thái trên stack cuối |
| FR-RCP-007 | Có triển khai theo baseline | DeleteRecipeCommand + CORE, soft delete GAP-008; không khớp đoạn hard delete mâu thuẫn trong PDF |
| FR-RCP-008 | Có triển khai | CORE: upload/primary/delete, quyền ảnh và ETag; test ảnh dùng storage giả lập tại một số đường đi, cần giữ phân biệt với MinIO thật |
| FR-RCP-009 | Có triển khai | CORE: CRUD/order nguyên liệu, quantity/unit nullable theo contract; chưa đủ E2E browser |
| FR-RCP-010 | Có triển khai | CORE: CRUD bước và renumber; cần lần integration xanh trên bản cuối |
| FR-SRCH-001 | Có triển khai theo baseline | DISC: q ≥2, accent-insensitive FTS/rank/relevanceScore/Published; simple+unaccent theo GAP-016, lỗi query 400 theo GAP-002 |
| FR-SRCH-002 | Có triển khai | DISC: bộ lọc kết hợp AND, category/time/serving/prep; form và URL đã bổ sung |
| FR-SRCH-003 | Có triển khai | DISC: sort allowlist, hai chiều, relevance cho keyword, tie-break ổn định; còn browser E2E |
| FR-SRCH-004 | Có triển khai | DISC: offset/pageSize/boundaries/meta; cần lần integration xanh trên bản cuối |
| FR-FILE-001 | Có triển khai | CORE: 5 MB, MIME/signature, object key an toàn/private; MinIO fixture và adapter có, không suy ra mọi test media dùng MinIO thật |
| FR-FILE-002 | Có triển khai | JOB + CORE: object deletion idempotent/retry/outbox; chưa chạy lại đầy đủ đường lỗi storage trong lượt này |
| FR-JOB-001 | Có triển khai sau bổ sung | JOB: HTML/link/Hangfire/outbox/retry 1/5/30; code/test đã có, integration SMTP lần này không khởi tạo được Docker |
| FR-JOB-002 | Có triển khai sau bổ sung | JOB + CORE: assertion decode medium 800×600/thumbnail 300×300; fallback/retry cần xác minh đầy đủ trên artifact cuối |
| FR-JOB-003 | **Một phần so với nguyên văn** | JOB: 02:00 UTC, retry 2, public URLs và giữ artifact khi lỗi có; chi tiết sitemap/ping còn G06 |
| FR-OBS-001 | Có triển khai | OBS: aggregate/live/ready, không lộ chi tiết dependency công khai; runtime probe trên môi trường cuối chưa kiểm tra lại |
| FR-OBS-002 | **Một phần bằng chứng** | OBS: structured request/audit và pipeline logging; chưa chứng minh context đầy đủ trên mọi log/use case, còn G07 |
| FR-OBS-003 | **Chưa đủ bằng chứng vận hành** | OBS có HTTP/EF/custom metrics và cấu hình OTLP; chưa kiểm tra exporter/trace/metrics xuyên suốt trên bản hiện tại |

Đổi/quên/đặt lại mật khẩu là yêu cầu bổ sung trực tiếp của người dùng, đã có API/UI/test; không thêm chúng vào số 34 FR của PDF.

## 5. Đối chiếu 30 yêu cầu phi chức năng

Nguồn: SRS trang 40–45. Dòng có nhiều điều kiện chỉ đạt khi đủ các điều kiện bắt buộc, hoặc có quyết định sai khác được chấp thuận. Không dùng báo cáo cũ làm phép đo mới.

| Mã SRS | Trạng thái | Bằng chứng hiện có / điều kiện chưa đóng |
|---|---|---|
| NFR-PERF-001 | Chưa đủ bằng chứng hiện tại | [Load report cũ](phase-8-performance-report.md) có p50/p95/p99 local; chưa đo lại artifact hiện tại/môi trường yêu cầu |
| NFR-PERF-002 | Chưa đủ bằng chứng hiện tại | Báo cáo cũ có 100 VU; chưa nghiệm thu 100 concurrent users và scaling trên bản cuối |
| NFR-PERF-003 | Một phần | Redis TTL/tag invalidation có; hit rate ≥80% chưa đo lại; cơ chế theo GAP-013 còn G05 |
| NFR-PERF-004 | Một phần bằng chứng | Có batch projection/index/EXPLAIN performance test; chưa review mọi query/WHERE/ORDER BY và chạy lại DB test được |
| NFR-PERF-005 | Chưa đủ bằng chứng hiện tại | Có build/budget/Lighthouse harness và báo cáo cũ; chưa đo lại LCP ≤2,5s, CLS ≤0,1, INP ≤200ms, JS ≤200KB trên bản cuối. TBT không thay INP |
| NFR-SEC-001 | Có kiểm soát, chưa nghiệm thu lại đủ | Identity/PBKDF2/password options và AUTH test; test DB không chạy lại được |
| NFR-SEC-002 | Một phần | JWT/rotation/hash/TTL có; Google còn G02, khác biệt entropy theo GAP-006; chưa đủ end-to-end |
| NFR-SEC-003 | **Chưa đạt đầy đủ** | Có quota 10/100/5, 429/Retry-After; thiếu auth sliding window và các rate headers, G04 |
| NFR-SEC-004 | Một phần | FluentValidation/MIME/magic bytes/CSP/parameterization có; còn rà toàn bộ input qua pipeline và bảo đảm file size trước buffering, G07 |
| NFR-SEC-005 | Chưa đủ bằng chứng deployment | Có Nginx TLS/HSTS, CORS allowlist/production guards; cần kiểm tra HTTPS/CORS/header/cookie trên deployment cuối |
| NFR-SEC-006 | Một phần bằng chứng | Policies/ownership/audit có test; một số handler còn giao logic cho service; cần review mọi quyền tại Application, G07 |
| NFR-SEC-007 | Chưa đủ bằng chứng hiện tại | Có env/User Secrets/scanning workflow; không chạy lại secret/dependency/image scans trong lượt này |
| NFR-USE-001 | Chưa đủ bằng chứng hiện tại | Có responsive CSS/source tests và [report cũ](phase-6-responsive-accessibility-report.md); chưa kiểm tra đủ viewport/browser/device của bản cuối |
| NFR-USE-002 | Chưa đủ bằng chứng hiện tại | Có semantic/ARIA/focus tests; không dùng JSDOM/source assertions để chứng nhận WCAG AA/contrast/NVDA/VoiceOver trên bản cuối |
| NFR-USE-003 | Một phần | Đã bổ sung field errors/RHF/Zod, có Problem Details; chưa xác minh mọi form/5xx và i18n-ready error codes |
| NFR-USE-004 | Một phần | Loading/progress có, toast và optimistic rollback đã bổ sung ở luồng phù hợp; chưa nghiệm thu mọi async/write operation |
| NFR-REL-001 | Chưa đủ bằng chứng vận hành | Probe/alert/maintenance cấu hình có; chưa có chuỗi đo uptime ≥99,5% cho môi trường vận hành |
| NFR-REL-002 | Một phần bằng chứng | Global handler/Redis fallback/job retry có; [failure drill cũ](phase-8-reliability-report.md), chưa chạy lại trên bản cuối |
| NFR-REL-003 | Một phần bằng chứng | Persistent volumes/token DB/soft delete/backup và [restore drill cũ](phase-8-reliability-report.md) có; backup hiện theo interval, chưa xác nhận đúng lịch 03:00 như PDF, retention/restore trên deployment cuối |
| NFR-MAINT-001 | **Chưa đạt** | Backend format/lint/typecheck/build đạt; frontend Prettier lỗi. Cần chốt tool/ruleset so với SonarAnalyzer/StyleCop/Airbnb và review PR theo PDF |
| NFR-MAINT-002 | **Chưa đạt** | Application 62,56% <80%; thiếu Playwright năm luồng và coverage CI gate. Chưa lập bằng chứng happy/error cho từng endpoint; frontend dùng Node test runner/JSDOM, cần ghi sai khác với Jest/Testing Library của PDF |
| NFR-MAINT-003 | Một phần bằng chứng | Có README/Scalar/OpenAPI/ADR/CHANGELOG; chưa kiểm tra lại fresh setup <5 phút và độ khớp docs toàn bộ bản cuối |
| NFR-MAINT-004 | Một phần | Ba architecture test đạt, có bốn layer; CQRS/placement nghiệp vụ toàn diện còn G07; Domain BCL theo GAP-026 |
| NFR-SCALE-001 | Một phần | JWT stateless/Redis/Hangfire shared queue có; rate counter cục bộ, distributed singleton lock cần ghi rõ cơ chế thay RedLock nếu khác; chưa thử nhiều instance |
| NFR-SCALE-002 | Một phần bằng chứng | Có Npgsql pooling/B-tree/GIN; chưa xác nhận max 100 connections/instance toàn deployment. Read replica/partitioning được PDF ghi tùy chọn/nâng cao |
| NFR-SCALE-003 | Chưa đủ bằng chứng deployment | Có Docker/Nginx templates; chưa chứng minh MinIO 4+ nodes, CDN và API load balancing theo phần production scaling |
| NFR-SEO-001 | Một phần bằng chứng | Có Recipe JSON-LD/ingredients/steps/nutrition khi có dữ liệu; chưa Rich Results Test bản cuối. Không tạo rating giả, theo GAP-033 |
| NFR-SEO-002 | **Chưa đạt đầy đủ** | Có metadata/OG/Twitter/canonical/robots; title/description lengths còn G06, soft 404 còn G03 |
| NFR-SEO-003 | **Một phần so với nguyên văn** | Có recurring sitemap/robots/public URLs; thiếu changefreq/priority; quyết định bỏ ping cần ghi riêng, G06 |
| NFR-SEO-004 | **Chưa đạt đầy đủ** | Có slug không dấu/unique/stable sau publish; chưa thấy lịch sử slug và 301 từ draft slug cũ, G06 |

Các điều kiện SRS tự mâu thuẫn hoặc khó đo cần ghi cách hiểu chính thức trước nghiệm thu, không tự chọn cách dễ để đánh đạt. Ví dụ: NFR-REL-001 ghi cả 99,5% và “3,65 giờ downtime/năm”, hai con số không tương đương.

## 6. Ràng buộc thiết kế và giao diện

Nguồn CONS: trang 14–15; UI §5.1: trang 46–47; kiến trúc §6: trang 50–52; data/API: Chương 7–8. Mã/table ở trên không thay việc đối chiếu schema, status/header, quyền và các acceptance chi tiết tại các chương này.

| Ràng buộc | Trạng thái hiện tại |
|---|---|
| CONS-001 | Có bốn layer; architecture tests đạt; chưa chứng minh mọi logic được đặt đúng tầng |
| CONS-002 | Nhiều named Command/Query đã bổ sung; audit/sitemap và rà logic từng use case còn G07 |
| CONS-003 | Có .NET 10 Minimal APIs và Next.js App Router |
| CONS-004 | Có Identity/PBKDF2/JWT/TTL; Google session còn G02 |
| CONS-005 | Có /api/v1 và Problem Details; cần đủ contract assertions từng endpoint, sai khác HTTP/SEO còn G03/G06 |
| CONS-006 | Có PostgreSQL/EF Code First/parameterized queries; migration upgrade test chưa chạy lại được |
| CONS-007 | Có giới hạn 5 MB và allowlist JPEG/PNG/WebP/AVIF/MIME checks; còn nghiệm thu full upload path |
| CONS-008 | Có pipeline validators; vẫn cần rà hết validation ngoài pipeline, G07 |
| CONS-009 | Có multi-stage Dockerfiles/Compose; không build/run lại container trong lượt Docker không khả dụng |
| CONS-010 | Có Serilog/correlation/audit; chưa chứng minh CorrelationId/RequestPath/UserId trên mọi log entry |

UI công khai và dashboard đã có phần lớn màn hình. **Trang chủ đã có danh mục**; PDF yêu cầu featured recipes + categories, không bắt buộc bố trí sidebar. Phần này không được đánh thiếu chỉ vì danh mục hiển thị bằng carousel.

Rendering cần nghiệm thu riêng: `/` và `/categories` ISR 3600; `/recipes/[slug]` ISR 300; `/categories/[slug]` ISR 600; listing/search SSR; private routes có auth. Lần build này detail routes được ghi `ƒ dynamic`; khai báo `revalidate` trong source chưa đủ để đánh đúng loại rendering. Các alias `/search`, `/auth/login`, `/auth/register` có redirect; cần ghi rõ destination/status thực tế khi nghiệm thu đúng URL trong PDF.

## 7. Điều kiện để chuyển kết luận sang hoàn thành

1. Đóng các sai khác G01–G08 bằng mã và test trên cùng worktree/commit; không sửa tài liệu thành “đạt” để che lỗi.
2. Chạy lại integration với Docker khả dụng; giữ nguyên assertion về quyền, ETag, cache/privacy, SMTP, media và migration. Không dùng 41/99 hiện tại như một tỷ lệ hoàn thành nghiệp vụ.
3. Gắn từng FR/NFR/CONS với mã, acceptance test và kết quả của artifact cuối; bổ sung review chi tiết contract/UI/data của Chương 5–8.
4. Chốt rõ mọi sai khác so với PDF/baseline (400/422, soft delete, public/private, FTS config, Output Cache, Google ping, toolchain...), giữ số FR đúng, không tự thêm chức năng ngoài phạm vi.
5. Hoàn thành bằng chứng môi trường tương ứng và UAT. Các report local cũ hoặc file mẫu Pending chưa chứng nhận release mới.

Đợt đánh giá này xác nhận được các lý do cụ thể để trả lời **“chưa hoàn thành toàn bộ”**. Chưa có cơ sở để đưa ra phần trăm hoàn thành chính xác, hoặc khẳng định danh sách G01–G09 đã bao quát mọi sai khác nhỏ trong toàn bộ PDF.
