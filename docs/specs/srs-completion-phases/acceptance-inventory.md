# Inventory acceptance và hợp đồng SRS

Ngày đối chiếu: **23/10/2026 (Asia/Bangkok)**. Phạm vi D0-01 và D0-05; đây là đầu vào viết test, **không phải biên bản hệ thống đạt SRS**. Không sửa code/runtime và không tự phê duyệt sai khác.

## Nguồn và cách đọc

- Nguồn gốc: [SRS v1.0.0](../SRS_Culinary_Blog_v1.0.0.pdf), 71 trang; SHA-256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`. Số trang bên dưới là trang PDF vật lý, trùng footer, tính từ 1. Đã trích xuất trực tiếp bản hiện tại; bảng rộng tr.63 có phần vượt mép phải trong PDF, đã xem ảnh render và đối chiếu FR/baseline, không suy cột response bị cắt.
- [Traceability hiện có](../phase-0/06_Traceability_Matrix.md) được mở rộng xuống từng acceptance, không thay thế nó.
- [Conformance](../phase-0/00_SRS_Conformance_and_Resolution.md), [Decision Log GAP-001–033](../phase-0/02_Decision_Log.md), [Product Baseline](../phase-0/01_Product_Baseline.md), [Data Dictionary](../phase-0/03_Data_Dictionary.md), [Error Catalog](../phase-0/04_Error_Catalog.md), [Routes](../phase-0/05_Routes_and_Wireframes.md) là baseline đang ghi Accepted trong repo; trạng thái này không phải một approval mới của lượt inventory.
- [Báo cáo trạng thái cũ](../../reports/srs-acceptance-status.md) (E-HIST) và [checklist gốc](../srs-completion-checklist.md) là context lịch sử, không lấy test count/measurement cũ làm bằng chứng runtime mới. Report nguồn đang chưa tracked tại thời điểm bắt đầu.
- ID `FR-…/A01`, `NFR-…/A01`, `CONS-…/A01` là điều kiện nhỏ. Metadata nguồn/ưu tiên/đối tượng/owner/evidence/quyết định ngay trên bảng **áp dụng cho từng dòng trong bảng đó**. Dòng mới phải giữ ID ổn định, không đánh lại số dòng cũ.
- FR có MoSCoW riêng lấy từ SRS. FR-SRCH-002–004 và FILE/JOB/OBS chỉ có bảng tóm tắt, dùng Must theo Product Baseline; không giả SRS gán MoSCoW riêng. NFR chỉ có mức nhóm (Cao/Rất cao/Trung bình); Must là mức baseline để làm gate, không phải trích nguyên văn MoSCoW. Circuit breaker, read replica, partition và key rotation khuyến nghị vẫn ghi rõ tính tùy chọn.
- `Chưa kiểm runtime` là trạng thái acceptance của mọi dòng lần này; source/test trong evidence chỉ là điểm vào có thật, không hàm ý test đó đã bao phủ đủ hoặc pass. Sai khác source đã nhìn thấy được ghi riêng ở cuối. `Chờ quyết định` nằm ở decision reference, không bị biến thành miễn yêu cầu.
- API paths trong FR/CT-API mặc định tiền tố `/api/v1`; health và dashboard giữ root path. Mỗi IT phải có happy/error, actor Guest/owner/Author khác/Admin/inactive khi áp dụng, synthetic DB/container/SMTP/storage riêng. Không đổi stack người dùng.

## Chỉ mục evidence hiện có

Các đường dẫn này là artifact tái dùng/điểm bắt đầu review. Bằng chứng **mới của lượt này** là review source và kiểm cấu trúc inventory trong [report](reports/phase-01-result.md), không phải kết quả chạy bộ test ứng dụng.

| Ref | Implementation/contract | Bộ kiểm có thật để tái dùng (chưa chạy lần này) |
|---|---|---|
| E-AUTH | [Auth endpoints](../../../backend/src/CulinaryBlog.Api/Presentation/AuthEndpoints.cs), [Auth DTO](../../../backend/src/CulinaryBlog.Application/Auth/AuthContracts.cs), [AuthService](../../../backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs), [Auth.js](../../../frontend/src/auth.ts) | [AuthLifecycleTests](../../../backend/tests/CulinaryBlog.IntegrationTests/AuthLifecycleTests.cs), [AuthSecurityTests](../../../backend/tests/CulinaryBlog.UnitTests/AuthSecurityTests.cs), [AuthRateLimitTests](../../../backend/tests/CulinaryBlog.IntegrationTests/AuthRateLimitTests.cs) |
| E-CONTENT | [Content endpoints](../../../backend/src/CulinaryBlog.Api/Presentation/ContentEndpoints.cs), [DTO](../../../backend/src/CulinaryBlog.Application/Content/ContentContracts.cs), [ContentService](../../../backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs) | [Core](../../../backend/tests/CulinaryBlog.IntegrationTests/RecipeCoreApiTests.cs), [Composition](../../../backend/tests/CulinaryBlog.IntegrationTests/RecipeCompositionApiTests.cs), [Discovery](../../../backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryApiTests.cs), [Pipeline](../../../backend/tests/CulinaryBlog.UnitTests/ContentPipelineTests.cs) |
| E-DATA | [EF configurations](../../../backend/src/CulinaryBlog.Infrastructure/Persistence/Configurations), [Model snapshot](../../../backend/src/CulinaryBlog.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs), [BaseEntity](../../../backend/src/CulinaryBlog.Domain/Common/BaseEntity.cs) | [MigrationUpgradeTests](../../../backend/tests/CulinaryBlog.IntegrationTests/MigrationUpgradeTests.cs), [FunctionalRegressionSeedTests](../../../backend/tests/CulinaryBlog.IntegrationTests/FunctionalRegressionSeedTests.cs) |
| E-CACHE | [Cache contracts](../../../backend/src/CulinaryBlog.Application/Abstractions/Caching), [Behaviors](../../../backend/src/CulinaryBlog.Application/Behaviors) | [RedisApplicationCacheTests](../../../backend/tests/CulinaryBlog.IntegrationTests/RedisApplicationCacheTests.cs), [PublicCacheKeyTests](../../../backend/tests/CulinaryBlog.UnitTests/PublicCacheKeyTests.cs), Discovery tests |
| E-JOB | [Jobs](../../../backend/src/CulinaryBlog.Infrastructure/Jobs), [Email](../../../backend/src/CulinaryBlog.Infrastructure/Email) | [WelcomeEmailJobTests](../../../backend/tests/CulinaryBlog.IntegrationTests/WelcomeEmailJobTests.cs), [SitemapGenerationJobTests](../../../backend/tests/CulinaryBlog.IntegrationTests/SitemapGenerationJobTests.cs), Composition tests (fake/real storage phải phân biệt) |
| E-OBS | [API DI](../../../backend/src/CulinaryBlog.Api/Presentation/DependencyInjection.cs), [Program](../../../backend/src/CulinaryBlog.Api/Program.cs), [Health writer](../../../backend/src/CulinaryBlog.Api/Health/HealthResponseWriter.cs), [GlobalExceptionHandler](../../../backend/src/CulinaryBlog.Api/Errors/GlobalExceptionHandler.cs) | [LivenessTests](../../../backend/tests/CulinaryBlog.IntegrationTests/LivenessTests.cs), [SecurityHardeningTests](../../../backend/tests/CulinaryBlog.IntegrationTests/SecurityHardeningTests.cs), [AuditMutationMiddlewareTests](../../../backend/tests/CulinaryBlog.IntegrationTests/AuditMutationMiddlewareTests.cs) |
| E-ARCH | [Application DI](../../../backend/src/CulinaryBlog.Application/DependencyInjection.cs), [Domain project](../../../backend/src/CulinaryBlog.Domain/CulinaryBlog.Domain.csproj), [backend solution](../../../backend/CulinaryBlog.slnx) | [DependencyDirectionTests](../../../backend/tests/CulinaryBlog.ArchitectureTests/DependencyDirectionTests.cs), [ApplicationBehaviorTests](../../../backend/tests/CulinaryBlog.UnitTests/ApplicationBehaviorTests.cs) |
| E-WEB | [app routes](../../../frontend/src/app), [next.config](../../../frontend/next.config.mjs), [middleware](../../../frontend/src/middleware.ts), [public server client](../../../frontend/src/lib/api/public-content-server.ts) | [HTTP ISR harness](../../../frontend/scripts/check-isr.mjs), [UI tests](../../../frontend/tests/ux-ui-behavior.test.mjs), [SEO tests](../../../frontend/tests/seo-schema-snapshots.test.mjs), [a11y](../../../frontend/tests/responsive-accessibility.test.mjs) |
| E-PERF | [k6](../../../performance/k6/public-api.js), [performance script](../../../scripts/performance.ps1), [frontend scripts](../../../frontend/package.json) | [RecipeDiscoveryPerformanceTests](../../../backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryPerformanceTests.cs), budget:check/lighthouse:release; chưa có INP/RUM mới |
| E-OPS | [Compose](../../../compose.yaml), [CI](../../../.github/workflows/ci.yml), [operations runbook](../../operations/runbook.md), [scripts](../../../scripts) | restore-drill.ps1/failure-drill.ps1/release-gate.ps1 có thật; không chạy phá hủy dữ liệu trong inventory |
| E-TEST | [frontend package/scripts](../../../frontend/package.json), [backend tests](../../../backend/tests) | xUnit/Node test runner+JSDOM hiện có; browser Playwright/matrix endpoint đầy đủ do phase24 tiếp nhận |
| E-OPENAPI | [OpenAPI local](../phase-0/openapi.v1.yaml), [generated types](../../../frontend/src/lib/api/schema.d.ts), [Postman local](../../../postman) | generate:api có thật; không generate/push workspace hoặc sửa contract trong lượt này |

## Acceptance của 34 FR, 30 NFR và 10 CONS

### FR-AUTH-001

- Nguồn: SRS tr. 17–18. Ưu tiên: Must.
- Đối tượng: POST /auth/register; /auth/register; welcome job. Phase tiếp nhận: 07, 20, 25.
- Hiện trạng/evidence: E-AUTH; E-JOB; có endpoint/DTO và test để tái dùng. Quyết định/điểm mở: GAP-004/006/021/023; lỗi validation dùng GAP-002.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-001/A01 | Đăng ký hợp lệ trả 201, data chứa accessToken, refreshToken, expiresAt và safe user; tự đăng nhập | IT response schema + browser đăng ký rồi vào dashboard | Chưa kiểm runtime |
| FR-AUTH-001/A02 | User mới nhận Author, không tự cấp Admin | IT truy vấn roles và thử payload tự nâng quyền | Chưa kiểm runtime |
| FR-AUTH-001/A03 | Password được PBKDF2 hash; raw password không nằm DB/log/response | IT kiểm hash và log đã lược secrets | Chưa kiểm runtime |
| FR-AUTH-001/A04 | JWT HS256 sống 15 phút; refresh 512-bit sống 7 ngày, chỉ persist hash | IT claims/clock/DB theo NFR-SEC-002 | Chưa kiểm runtime |
| FR-AUTH-001/A05 | DisplayName/email/password không hợp lệ trả 400 với errors theo field; không tạo user/token/job | IT boundary/empty/format/password và so DB/outbox trước-sau | Chưa kiểm runtime |
| FR-AUTH-001/A06 | Email đã tồn tại trả 409 AUTH_EMAIL_EXISTS kể cả khác hoa thường | IT duplicate normalized email và concurrent register | Chưa kiểm runtime |
| FR-AUTH-001/A07 | Sau commit có welcome job bền vững; SMTP lỗi vẫn giữ user/phiên | IT outbox + MailHog riêng, không email thật | Chưa kiểm runtime |
| FR-AUTH-001/A08 | DB không kết nối trả 500 Problem Details, không lộ stack trace | IT fault injection và kiểm log/response | Chưa kiểm runtime |

### FR-AUTH-002

- Nguồn: SRS tr. 18–19. Ưu tiên: Must.
- Đối tượng: POST /auth/login; /auth/login. Phase tiếp nhận: 07, 25.
- Hiện trạng/evidence: E-AUTH; có login và lockout configuration. Quyết định/điểm mở: GAP-006/021; NFR-SEC-003 cho 429.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-002/A01 | Đăng nhập đúng trả 200 và cặp token mới, persist refresh hash | IT login hai lần, kiểm session độc lập | Chưa kiểm runtime |
| FR-AUTH-002/A02 | Email không tồn tại và password sai trả 401 cùng thông báo generic | IT so status/code/body tránh enumeration | Chưa kiểm runtime |
| FR-AUTH-002/A03 | Sau 5 lần sai khóa 15 phút; còn khóa trả 423 kèm thời gian unlock | IT clock/fail-count/lockout boundary | Chưa kiểm runtime |
| FR-AUTH-002/A04 | Đăng nhập thành công reset AccessFailedCount | IT sai vài lần rồi login đúng | Chưa kiểm runtime |
| FR-AUTH-002/A05 | Email/password sai định dạng trả 400, không sinh session | IT validator và DB delta | Chưa kiểm runtime |
| FR-AUTH-002/A06 | Vượt quota auth trả 429 và Retry-After | IT rate boundary theo CT-HTTP và NFR-SEC-003 | Chưa kiểm runtime |

### FR-AUTH-003

- Nguồn: SRS tr. 19–20. Ưu tiên: Should.
- Đối tượng: Auth.js Google callback; POST /auth/google; /auth/login. Phase tiếp nhận: 08, 10, 25.
- Hiện trạng/evidence: E-AUTH; callback/service có; G02 trong E-HIST chưa được đóng. Quyết định/điểm mở: GAP-005; INV-G04 (status provider).

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-003/A01 | Authorization Code + PKCE và state bảo vệ callback; scopes openid/email/profile | Browser/provider thử được phép; lưu trace đã loại token | Chưa kiểm runtime |
| FR-AUTH-003/A02 | Backend tự kiểm chữ ký, issuer, audience, expiry của idToken | IT provider có kiểm soát, từng claim sai | Chưa kiểm runtime |
| FR-AUTH-003/A03 | Google lần đầu tạo Author với email/displayName/avatar; liên kết identity | IT DB user/login/roles và response safe session | Chưa kiểm runtime |
| FR-AUTH-003/A04 | Email local đã có được link an toàn, không tạo user thứ hai | IT verified email và provider-key collision | Chưa kiểm runtime |
| FR-AUTH-003/A05 | Token invalid/expired trả 400 theo catalog; email unverified/thiếu profile trả lỗi rõ | IT từng nhánh, đối chiếu GAP-005 và INV-G04 | Chưa kiểm runtime |
| FR-AUTH-003/A06 | Provider unavailable có thông báo phù hợp, local login vẫn dùng được | Simulation riêng + browser/provider thật riêng; 502 trong FR so 503 hiện tại chờ INV-G04 | Chưa kiểm runtime |
| FR-AUTH-003/A07 | Phiên Google còn refresh được sau 15 phút, logout revoke đúng phiên | IT session lifecycle + browser; không suy từ login 200 | Chưa kiểm runtime |
| FR-AUTH-003/A08 | User hủy consent hoặc callback lỗi không bị loop/giữ phiên nửa vời | Browser state/cancel/error, không gọi đó là Google thật nếu dùng fake | Chưa kiểm runtime |

### FR-AUTH-004

- Nguồn: SRS tr. 20–21. Ưu tiên: Must.
- Đối tượng: POST /auth/refresh; auth provider. Phase tiếp nhận: 08, 25.
- Hiện trạng/evidence: E-AUTH; có rotation service/tests; runtime chưa tái chạy. Quyết định/điểm mở: GAP-006/007; INV-G04 cho inactive status.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-004/A01 | Body refreshToken hợp lệ không cần access token trả 200 và phiên mới | IT request khi AT hết hạn | Chưa kiểm runtime |
| FR-AUTH-004/A02 | Token cũ được revoke, lưu RevokedAt và ReplacedByTokenHash; token mới giữ family | IT DB trước-sau trong transaction | Chưa kiểm runtime |
| FR-AUTH-004/A03 | Phiên mới có AT 15 phút và RT 7 ngày | IT fake clock/expiry boundaries | Chưa kiểm runtime |
| FR-AUTH-004/A04 | Token không có, hết hạn, đã revoke trả 401 | IT từng nhánh riêng với catalog code | Chưa kiểm runtime |
| FR-AUTH-004/A05 | Reuse token cũ revoke toàn family và log security warning không lộ token | IT kiểm tất cả descendants và captured log | Chưa kiểm runtime |
| FR-AUTH-004/A06 | Hai refresh cạnh tranh không cấp hai nhánh còn sống trái rotation | IT barrier/concurrent requests trên PostgreSQL riêng | Chưa kiểm runtime |
| FR-AUTH-004/A07 | User bị xóa/khóa không nhận token mới; inactive đối chiếu catalog 403 | IT deleted/locked/inactive và INV-G04, không tự coi các trạng thái đồng nghĩa | Chưa kiểm runtime |

### FR-AUTH-005

- Nguồn: SRS tr. 21–22. Ưu tiên: Must.
- Đối tượng: POST /auth/logout; account menu. Phase tiếp nhận: 09, 25.
- Hiện trạng/evidence: E-AUTH; endpoint anonymous và LogoutAsync không nhận principal. Quyết định/điểm mở: GAP-007; lỗi khác owner chờ Phase 02/09.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-005/A01 | Logout bằng refreshToken revoke đúng token, ghi RevokedAt, trả 204 không body | IT DB delta và HTTP body length | Chưa kiểm runtime |
| FR-AUTH-005/A02 | Refresh sau logout thất bại 401 | IT logout rồi dùng lại token | Chưa kiểm runtime |
| FR-AUTH-005/A03 | Token không tồn tại hoặc request lặp vẫn 204 | IT idempotency và không disclosure | Chưa kiểm runtime |
| FR-AUTH-005/A04 | Access token hết hạn vẫn revoke được bằng refresh token hợp lệ | IT expired bearer và body RT | Chưa kiểm runtime |
| FR-AUTH-005/A05 | Bearer còn hạn phải cùng owner refresh token | IT user A bearer + user B RT, assert B không bị revoke; status chờ quyết định | Chưa kiểm runtime |
| FR-AUTH-005/A06 | Client xóa phiên và về UI guest; JWT đã cấp tự hết hạn theo stateless | Browser local/Google logout, kiểm private route sau logout | Chưa kiểm runtime |

### FR-AUTH-006

- Nguồn: SRS tr. 22–23. Ưu tiên: Should.
- Đối tượng: GET /auth/me; /profile. Phase tiếp nhận: 07.
- Hiện trạng/evidence: E-AUTH; UserDto và profile screen có. Quyết định/điểm mở: GAP-020/021.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-006/A01 | JWT hợp lệ trả 200 data hồ sơ của claim userId, không nhận userId từ client | IT A/B profile isolation | Chưa kiểm runtime |
| FR-AUTH-006/A02 | Response có id/displayName/email/avatarUrl/roles/emailConfirmed/createdAt và bio theo baseline | IT schema với nullable avatar/bio | Chưa kiểm runtime |
| FR-AUTH-006/A03 | Không trả PasswordHash, SecurityStamp hoặc raw token | IT recursive response key assertions | Chưa kiểm runtime |
| FR-AUTH-006/A04 | Thiếu/invalid/expired bearer trả 401 | IT authentication matrix | Chưa kiểm runtime |
| FR-AUTH-006/A05 | User đã xóa sau cấp JWT trả 404 theo FR, đối chiếu active policy trước handler | IT deleted user, không tự đánh pass từ source | Chưa kiểm runtime |

### FR-AUTH-007

- Nguồn: SRS tr. 23. Ưu tiên: Should.
- Đối tượng: PATCH /auth/me; /profile. Phase tiếp nhận: 07.
- Hiện trạng/evidence: E-AUTH; presence flags trong UpdateProfileRequest và UI avatar có. Quyết định/điểm mở: GAP-020; CT-DATA-USER.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-AUTH-007/A01 | PATCH chỉ đổi field đã cung cấp và trả 200 hồ sơ mới | IT omit từng field, kiểm trường khác giữ nguyên | Chưa kiểm runtime |
| FR-AUTH-007/A02 | DisplayName trim 2–100; avatar safe URL; bio theo dictionary ≤2000 | UT boundaries + IT không partial update khi sai | Chưa kiểm runtime |
| FR-AUTH-007/A03 | Avatar/bio null được xóa; omit giữ giá trị cũ | IT omit so null so empty + browser clear avatar | Chưa kiểm runtime |
| FR-AUTH-007/A04 | Email/UserName không được đổi qua profile | IT payload thêm field và so DB | Chưa kiểm runtime |
| FR-AUTH-007/A05 | Chưa xác thực trả 401; validation trả 400 errors | IT auth/field matrix | Chưa kiểm runtime |
| FR-AUTH-007/A06 | UI giữ dữ liệu khi lưu lỗi, hiển thị avatar/tên/bio mới sau lưu | React tương tác + browser CSP/ảnh thật | Chưa kiểm runtime |

### FR-CAT-001

- Nguồn: SRS tr. 23–24. Ưu tiên: Must.
- Đối tượng: GET /categories; / và /categories. Phase tiếp nhận: 14, 15, 17.
- Hiện trạng/evidence: E-CONTENT; category endpoint/config; source chưa thay phép đo. Quyết định/điểm mở: GAP-013/021; không dùng TTL 60 phút trong FR.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-CAT-001/A01 | Guest/Author/Admin đều nhận 200 danh sách danh mục chưa xóa | IT ba actor và deleted category | Chưa kiểm runtime |
| FR-CAT-001/A02 | Mỗi category có id/name/slug/description/imageUrl/recipeCount theo envelope data | IT schema/nullability | Chưa kiểm runtime |
| FR-CAT-001/A03 | RecipeCount chỉ đếm Published chưa xóa | IT cùng category chứa Draft/Published/Archived/deleted | Chưa kiểm runtime |
| FR-CAT-001/A04 | Danh sách sắp Name tăng dần, không theo orderIndex quản trị | IT tên A/Z với orderIndex đảo ngược | Chưa kiểm runtime |
| FR-CAT-001/A05 | Không có danh mục trả 200 data=[] | IT empty DB | Chưa kiểm runtime |
| FR-CAT-001/A06 | Cache hit không query lại; TTL category 1800 giây; miss đọc DB | IT Redis TTL và counter/repository trace, không chờ 30 phút | Chưa kiểm runtime |

### FR-CAT-002

- Nguồn: SRS tr. 24–25. Ưu tiên: Must.
- Đối tượng: GET /categories/{slug}; /categories/[slug]. Phase tiếp nhận: 11, 12, 17.
- Hiện trạng/evidence: E-CONTENT; E-WEB; G03 lịch sử còn mở. Quyết định/điểm mở: GAP-014/021.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-CAT-002/A01 | Slug tồn tại trả category và chỉ Published recipes trong data, meta phân trang | IT schema và Guest/Author/Admin đều không thấy Draft qua public URL | Chưa kiểm runtime |
| FR-CAT-002/A02 | Page mặc định 1, pageSize 12, count trước OFFSET/LIMIT | IT nhiều trang/empty/page vượt cuối | Chưa kiểm runtime |
| FR-CAT-002/A03 | Slug thiếu hoặc soft-deleted trả API 404 Problem Details | IT missing/deleted slug | Chưa kiểm runtime |
| FR-CAT-002/A04 | Trang UI missing category thực sự trả HTTP 404 | Production HTTP và browser, không chỉ render chữ Not Found | Chưa kiểm runtime |
| FR-CAT-002/A05 | ISR category detail 600 giây, không cache dữ liệu private | Production HTTP MISS/HIT/revalidation theo CT-UI | Chưa kiểm runtime |

### FR-CAT-003

- Nguồn: SRS tr. 25–26. Ưu tiên: Must.
- Đối tượng: POST /categories; /dashboard/categories. Phase tiếp nhận: 05, 17.
- Hiện trạng/evidence: E-CONTENT; AdminPolicy/CategoryWriteRequest/config có. Quyết định/điểm mở: GAP-019; validation GAP-002.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-CAT-003/A01 | Admin tạo hợp lệ trả 201 data category, Location /api/v1/categories/{slug} | IT status/header/schema và DB | Chưa kiểm runtime |
| FR-CAT-003/A02 | Guest trả 401; Author không Admin trả 403; không ghi DB/cache | IT quyền tại route và Application | Chưa kiểm runtime |
| FR-CAT-003/A03 | Name trim 2–100, reject HTML; request sai trả 400 | UT boundary/markup + IT side effects | Chưa kiểm runtime |
| FR-CAT-003/A04 | Name normalized trùng trả 409 CATEGORY_NAME_EXISTS | IT case/whitespace/concurrent duplicate | Chưa kiểm runtime |
| FR-CAT-003/A05 | Slug lowercase không dấu; collision thêm -2/-3 đến unique | UT tiếng Việt + IT collision/race | Chưa kiểm runtime |
| FR-CAT-003/A06 | Commit thành công invalidates category/recipe public cache liên quan | IT cache warm trước create, đọc lại | Chưa kiểm runtime |

### FR-CAT-004

- Nguồn: SRS tr. 26. Ưu tiên: Must.
- Đối tượng: PUT /categories/{id}; /dashboard/categories. Phase tiếp nhận: 17.
- Hiện trạng/evidence: E-CONTENT; slug được lưu riêng tên. Quyết định/điểm mở: GAP-019; INV-G01 cho PUT optional fields.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-CAT-004/A01 | Admin sửa name/description và field mở rộng imageUrl/orderIndex trả 200 mới | IT đầy đủ schema + browser save | Chưa kiểm runtime |
| FR-CAT-004/A02 | Đổi name không đổi slug | IT so slug trước-sau | Chưa kiểm runtime |
| FR-CAT-004/A03 | ID thiếu/deleted trả 404, không Admin trả 403, guest 401 | IT permission/missing matrix | Chưa kiểm runtime |
| FR-CAT-004/A04 | Name trùng trả 409, invalid trả 400; lỗi không cập nhật một phần | IT duplicate và invalid URL/name | Chưa kiểm runtime |
| FR-CAT-004/A05 | Sau save invalidate cache; UI optimistic lỗi phải rollback | IT Redis + React tương tác API fail | Chưa kiểm runtime |

### FR-CAT-005

- Nguồn: SRS tr. 26–27. Ưu tiên: Should.
- Đối tượng: DELETE /categories/{id}; /dashboard/categories. Phase tiếp nhận: 17.
- Hiện trạng/evidence: E-CONTENT; soft-delete baseline có. Quyết định/điểm mở: GAP-009; chỉ đếm recipe chưa xóa.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-CAT-005/A01 | Admin xóa category không còn recipe active trả 204 không body | IT DB flag/global filter | Chưa kiểm runtime |
| FR-CAT-005/A02 | Category còn Draft/Published/Archived trả 409 CATEGORY_DELETE_HAS_RECIPES kèm count | IT từng status và error detail | Chưa kiểm runtime |
| FR-CAT-005/A03 | Recipe đã soft-delete không cản xóa theo baseline | IT retention case | Chưa kiểm runtime |
| FR-CAT-005/A04 | Không tồn tại trả 404; Author khác quyền 403; guest 401 | IT actor/missing matrix | Chưa kiểm runtime |
| FR-CAT-005/A05 | Danh mục xóa không còn trong reads/cache, slug không tái sử dụng | IT warm cache rồi delete/create collision | Chưa kiểm runtime |

### FR-RCP-001

- Nguồn: SRS tr. 27–28. Ưu tiên: Must.
- Đối tượng: GET /recipes; /me/recipes; /admin/recipes; listing/dashboard. Phase tiếp nhận: 17, 18.
- Hiện trạng/evidence: E-CONTENT; list/filter contracts và tests có. Quyết định/điểm mở: GAP-014/021/029; CT-QUERY.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-001/A01 | Public list chỉ Published chưa xóa với mọi actor | IT Guest/owner/other/Admin và cache isolation | Chưa kiểm runtime |
| FR-RCP-001/A02 | Private /me chỉ recipe của claim owner; /admin chỉ Admin và thấy mọi status | IT authorization và no-store response | Chưa kiểm runtime |
| FR-RCP-001/A03 | Filters category/difficulty/cook/servings/prep kết hợp AND | IT mỗi filter và giao rỗng; CT-QUERY | Chưa kiểm runtime |
| FR-RCP-001/A04 | CategoryId hợp lệ nhưng không tồn tại trả 200 data=[] | IT không biến thành 404 | Chưa kiểm runtime |
| FR-RCP-001/A05 | Sort default mới nhất, hỗ trợ asc/desc trong allowlist | IT tie-break ổn định qua trang và sort sai 400 | Chưa kiểm runtime |
| FR-RCP-001/A06 | Phân trang total trước limit, page≥1 và pageSize 1–50 | IT boundaries/meta; invalid 400 | Chưa kiểm runtime |
| FR-RCP-001/A07 | Cache list public TTL 900 giây, key vary mọi query; private không shared cache | IT TTL/key/actor isolation | Chưa kiểm runtime |

### FR-RCP-002

- Nguồn: SRS tr. 28–29. Ưu tiên: Must.
- Đối tượng: GET /recipes/{slug}; private detail; /recipes/[slug]. Phase tiếp nhận: 11, 17, 21.
- Hiện trạng/evidence: E-CONTENT; E-WEB; nested DTO có. Quyết định/điểm mở: GAP-013/014/018/021.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-002/A01 | Published detail trả 200 với category/author/nutrition/images/ingredients/steps | IT nested response schema | Chưa kiểm runtime |
| FR-RCP-002/A02 | Ingredients theo orderIndex, steps theo stepNumber, gallery đúng thứ tự/primary | IT dữ liệu cố ý đảo thứ tự insert | Chưa kiểm runtime |
| FR-RCP-002/A03 | Missing/deleted/private public slug trả 404; không lộ title/media/nutrition | IT Guest/owner/Admin với public route | Chưa kiểm runtime |
| FR-RCP-002/A04 | Private detail owner/Admin được đọc, Author khác bị từ chối | IT /me và /admin actor matrix 401/403/404 theo catalog | Chưa kiểm runtime |
| FR-RCP-002/A05 | Public detail cache TTL 300 giây, tag recipes và recipe:{slug} | IT Redis TTL/tag mutation invalidation | Chưa kiểm runtime |
| FR-RCP-002/A06 | UI detail missing trả HTTP 404, Published có JSON-LD và ISR 300 | Production HTTP + browser + SEO validator | Chưa kiểm runtime |

### FR-RCP-003

- Nguồn: SRS tr. 29–30. Ưu tiên: Must.
- Đối tượng: POST /recipes; /dashboard/recipes/new. Phase tiếp nhận: 05, 17, 26.
- Hiện trạng/evidence: E-CONTENT; RecipeWriteRequest hiện không chứa steps/ingredients. Quyết định/điểm mở: GAP-019/031; INV-G02 cho composition cùng request.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-003/A01 | Author/Admin tạo recipe Draft trả 201 resource mới | IT response/status/default state | Chưa kiểm runtime |
| FR-RCP-003/A02 | AuthorId lấy từ identity; client không thể gán tác giả khác | IT forged userId/role payload | Chưa kiểm runtime |
| FR-RCP-003/A03 | Title 5–200, description 1–2000, prep>0, cook≥0, servings>0, enum hợp lệ | UT từng boundary và IT 400 không ghi DB | Chưa kiểm runtime |
| FR-RCP-003/A04 | Category phải tồn tại chưa xóa; invalid category trả 400 | IT FK/soft-deleted/empty Guid | Chưa kiểm runtime |
| FR-RCP-003/A05 | Slug sinh từ title, collision thêm suffix; race chưa resolve trả 409 | IT trùng title/concurrent create | Chưa kiểm runtime |
| FR-RCP-003/A06 | Cho phép tạo steps/ingredients/nutrition cùng transaction hoặc thêm sau | IT POST nested composition rồi đọc detail, rollback tất cả nếu child sai; INV-G02 chưa khớp DTO | Chưa kiểm runtime |
| FR-RCP-003/A07 | Guest 401, actor không đủ role 403 | IT authorization route và Application | Chưa kiểm runtime |
| FR-RCP-003/A08 | Sau commit invalidate public tags; recipe Draft chưa xuất hiện public/search | IT warm cache/create/read | Chưa kiểm runtime |

### FR-RCP-004

- Nguồn: SRS tr. 30–31. Ưu tiên: Must.
- Đối tượng: PUT /recipes/{id}; /dashboard/recipes/[id]/edit. Phase tiếp nhận: 05, 17, 26.
- Hiện trạng/evidence: E-CONTENT; ETag/version/UoW có điểm vào; chưa runtime. Quyết định/điểm mở: GAP-003/015/031; INV-G01.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-004/A01 | Owner hoặc Admin cập nhật hợp lệ trả 200 resource mới | IT owner/Admin và DB values | Chưa kiểm runtime |
| FR-RCP-004/A02 | Author khác 403; guest 401; missing/deleted recipe 404 | IT cả route lẫn direct Application call | Chưa kiểm runtime |
| FR-RCP-004/A03 | Thiếu If-Match trả 400; stale version trả 409 RECIPE_CONCURRENCY_CONFLICT | IT header missing/malformed/stale | Chưa kiểm runtime |
| FR-RCP-004/A04 | Hai writer cùng version chỉ một commit, response version/ETag mới | IT concurrent PostgreSQL và reload | Chưa kiểm runtime |
| FR-RCP-004/A05 | Nutrition và basic fields cập nhật nguyên tử; invalid request không ghi/cache/job | IT fault injection giữa thao tác | Chưa kiểm runtime |
| FR-RCP-004/A06 | Đổi title khi Draft có slug mới; sau publish lần đầu slug bất biến | IT rename Draft/published/unpublished và CT-SEO | Chưa kiểm runtime |
| FR-RCP-004/A07 | Invalidate recipes và tag recipe slug liên quan sau commit | IT warm cache trước update; scheduler failure riêng | Chưa kiểm runtime |
| FR-RCP-004/A08 | UI stale conflict cho reload/discard, không overwrite và không mất input âm thầm | Browser hai phiên chỉnh sửa | Chưa kiểm runtime |

### FR-RCP-005

- Nguồn: SRS tr. 31–32. Ưu tiên: Must.
- Đối tượng: PATCH /recipes/{id}/publish và /unpublish; wizard preview. Phase tiếp nhận: 17, 26.
- Hiện trạng/evidence: E-CONTENT; domain lifecycle có. Quyết định/điểm mở: GAP-010; NFR-SEO-004.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-005/A01 | Draft đủ dữ liệu + ≥1 ingredient + ≥1 step liên tục được publish 200 | UT domain + IT PublishedAt/version/category | Chưa kiểm runtime |
| FR-RCP-005/A02 | Thiếu ingredient hoặc step trả 400 RECIPE_PUBLISH_INCOMPLETE, không thay trạng thái | IT từng điều kiện độc lập | Chưa kiểm runtime |
| FR-RCP-005/A03 | Không bắt buộc ảnh để publish; dùng ảnh mặc định trên web | IT recipe không ảnh + browser detail | Chưa kiểm runtime |
| FR-RCP-005/A04 | Published unpublish về Draft 200 và biến mất khỏi public/search/sitemap/cache/media | IT mutation xuyên backend→Next + browser | Chưa kiểm runtime |
| FR-RCP-005/A05 | Publish/unpublish đúng trạng thái đích có tính idempotent 200 theo FR | IT lặp action với version mới; phân biệt stale request | Chưa kiểm runtime |
| FR-RCP-005/A06 | Owner/Admin mới thao tác; thiếu recipe 404, khác owner 403, guest 401 | IT actor/status/If-Match matrix | Chưa kiểm runtime |
| FR-RCP-005/A07 | UpdatedAt đổi, PublishedAt ổn định khi republish theo dictionary | IT clock và publish/unpublish/republish | Chưa kiểm runtime |

### FR-RCP-006

- Nguồn: SRS tr. 32. Ưu tiên: Should.
- Đối tượng: PATCH archive/unarchive; dashboard. Phase tiếp nhận: 17.
- Hiện trạng/evidence: E-CONTENT; supporting unarchive endpoint có. Quyết định/điểm mở: GAP-011.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-006/A01 | Owner/Admin archive trả 200 Archived, vẫn giữ dữ liệu DB | IT lifecycle + child retention | Chưa kiểm runtime |
| FR-RCP-006/A02 | Archived không hiển thị public listing/detail/search/media/cache | IT ba actor và cache warm | Chưa kiểm runtime |
| FR-RCP-006/A03 | Unarchive trả Draft, không tự publish | IT transition theo GAP-011 | Chưa kiểm runtime |
| FR-RCP-006/A04 | Missing 404; Author khác 403; guest 401 | IT permission/If-Match matrix | Chưa kiểm runtime |
| FR-RCP-006/A05 | Sau transition invalidate public views liên quan | IT backend→Next callback và query lại | Chưa kiểm runtime |

### FR-RCP-007

- Nguồn: SRS tr. 32–33. Ưu tiên: Must.
- Đối tượng: DELETE /recipes/{id}; dashboard. Phase tiếp nhận: 05, 17.
- Hiện trạng/evidence: E-CONTENT; DeleteRecipeCommand; baseline giữ child/object. Quyết định/điểm mở: GAP-008; không test hard delete như acceptance hiện hành.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-007/A01 | Owner/Admin delete trả 204 không body, recipe IsDeleted=true | IT soft-delete flag, audit/version transaction | Chưa kiểm runtime |
| FR-RCP-007/A02 | Reads public/private/search loại recipe đã xóa; HTTP resource missing 404 | IT global filter và cache invalidation | Chưa kiểm runtime |
| FR-RCP-007/A03 | Child metadata/object giữ theo retention baseline, không purge khi soft-delete recipe | IT DB/storage trước-sau; đối chiếu FR-FILE-002 cho xóa image riêng | Chưa kiểm runtime |
| FR-RCP-007/A04 | Missing recipe trả 404; khác owner 403; guest 401 | IT permission/missing/concurrency matrix | Chưa kiểm runtime |
| FR-RCP-007/A05 | Không lộ media qua URL cũ sau xóa; không rollback business vì cache/job lỗi | IT warm media/cache rồi delete + failure injection | Chưa kiểm runtime |

### FR-RCP-008

- Nguồn: SRS tr. 33–34. Ưu tiên: Must.
- Đối tượng: Image POST/PATCH/DELETE; wizard gallery. Phase tiếp nhận: 19, 26.
- Hiện trạng/evidence: E-CONTENT; E-JOB; image config partial unique index. Quyết định/điểm mở: GAP-012/022/028; CT-DATA-IMAGE.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-008/A01 | Multipart file hợp lệ ≤5*1024*1024 bytes trả 201 ảnh; filename GUID trong recipes/{recipeId} | IT MinIO adapter thật và response URL | Chưa kiểm runtime |
| FR-RCP-008/A02 | Chỉ JPEG/PNG/WebP/AVIF, MIME khớp magic bytes/decode | IT từng format, spoof và file hỏng trả 400 | Chưa kiểm runtime |
| FR-RCP-008/A03 | Ảnh đầu tự primary, mỗi recipe tối đa một primary active | IT upload đầu + concurrent upload/primary | Chưa kiểm runtime |
| FR-RCP-008/A04 | PATCH metadata đổi altText/isPrimary/orderIndex trả 200, bỏ primary cũ nguyên tử | IT transaction và hai writer | Chưa kiểm runtime |
| FR-RCP-008/A05 | DELETE trả 204 và queue xóa object; nếu xóa primary thì chọn ảnh còn lại | IT DB/outbox/storage với retry | Chưa kiểm runtime |
| FR-RCP-008/A06 | Parent/child thiếu hoặc child thuộc recipe khác trả 404; sai owner 403; guest 401 | IT ID substitution và authorization | Chưa kiểm runtime |
| FR-RCP-008/A07 | Storage unavailable trả 503 khi upload, không metadata mồ côi | IT dependency failure + cleanup evidence | Chưa kiểm runtime |
| FR-RCP-008/A08 | Mỗi mutation tăng aggregate version và invalidates public media/detail | IT ETag + HTTP read sau mutation | Chưa kiểm runtime |

### FR-RCP-009

- Nguồn: SRS tr. 34–35. Ưu tiên: Must.
- Đối tượng: Ingredient POST/PUT/DELETE; wizard. Phase tiếp nhận: 17, 26.
- Hiện trạng/evidence: E-CONTENT; IngredientWriteRequest/config có. Quyết định/điểm mở: GAP-018/030; nullable theo Chương 7/8; INV-G01.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-009/A01 | POST ingredient trả 201, PUT trả 200, DELETE trả 204 | IT từng endpoint rồi đọc detail | Chưa kiểm runtime |
| FR-RCP-009/A02 | Name 1–200; quantity null hoặc >0; unit nullable ≤50; notes nullable ≤500 | UT boundary + IT explicit null/omit/invalid không partial write | Chưa kiểm runtime |
| FR-RCP-009/A03 | OrderIndex ≥0, default 0, response hiển thị đúng order | IT thêm/sửa thứ tự rồi đọc detail | Chưa kiểm runtime |
| FR-RCP-009/A04 | Recipe/ingredient thiếu hoặc không cùng parent trả 404 | IT child ID recipe B dùng trên recipe A | Chưa kiểm runtime |
| FR-RCP-009/A05 | Guest 401; non-owner 403; Admin bypass ownership | IT matrix tại Application và route | Chưa kiểm runtime |
| FR-RCP-009/A06 | Child mutation tăng recipe version, stale 409, invalidate cache sau commit | IT If-Match/race/cache | Chưa kiểm runtime |
| FR-RCP-009/A07 | Published không cho xóa ingredient cuối; trả 409 và giữ dữ liệu | IT invariant theo Product Baseline, unpublish rồi xóa được | Chưa kiểm runtime |

### FR-RCP-010

- Nguồn: SRS tr. 35–36. Ưu tiên: Must.
- Đối tượng: Step POST/PUT/DELETE; wizard. Phase tiếp nhận: 17, 26.
- Hiện trạng/evidence: E-CONTENT; StepWriteRequest/config có. Quyết định/điểm mở: GAP-017; INV-G01.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-RCP-010/A01 | POST tự append stepNumber=max+1 hoặc 1; trả 201 | IT empty/nonempty; không tin số tự chọn nếu trái baseline | Chưa kiểm runtime |
| FR-RCP-010/A02 | PUT sửa title/description/timer/image và vị trí trả 200 | IT resource readback + optional semantics INV-G01 | Chưa kiểm runtime |
| FR-RCP-010/A03 | Title 1–200, description 1–2000, timer null hoặc ≥0, image URL safe nullable ≤500 | UT boundary + IT invalid 400 không side effects | Chưa kiểm runtime |
| FR-RCP-010/A04 | DELETE trả 204; renumber còn lại liên tục 1..N trong transaction | IT xóa đầu/giữa/cuối và inject failure | Chưa kiểm runtime |
| FR-RCP-010/A05 | Không tạo duplicate active (RecipeId,StepNumber) khi cạnh tranh | IT PostgreSQL unique constraint và concurrent reorder | Chưa kiểm runtime |
| FR-RCP-010/A06 | Recipe/step thiếu hoặc không thuộc parent trả 404; non-owner 403; guest 401 | IT actor/parent matrix | Chưa kiểm runtime |
| FR-RCP-010/A07 | Child mutation tăng version, stale 409, cache invalidation sau commit | IT If-Match và warm detail | Chưa kiểm runtime |
| FR-RCP-010/A08 | Published không cho xóa step cuối, trả 409; unpublish mới xóa được | IT domain invariant | Chưa kiểm runtime |

### FR-SRCH-001

- Nguồn: SRS tr. 36–37. Ưu tiên: Must.
- Đối tượng: GET /recipes/search; /search alias. Phase tiếp nhận: 18, 26.
- Hiện trạng/evidence: E-CONTENT; search handler/repository; E-WEB alias có. Quyết định/điểm mở: GAP-016/013/021; INV-G06 cho message gợi ý.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-SRCH-001/A01 | q trim dài 2–100; missing/rỗng/1 ký tự trả 400 | UT/IT boundary; không gọi search để browse | Chưa kiểm runtime |
| FR-SRCH-001/A02 | pho tìm phở; nhiều từ prefix matching kết hợp, rank cao lên trước | IT PostgreSQL unaccent/weighted tsvector/ts_rank với dataset có kiểm soát | Chưa kiểm runtime |
| FR-SRCH-001/A03 | Trigger cập nhật vector khi title/description đổi; có GIN index | IT mutation rồi search, catalog/index/EXPLAIN | Chưa kiểm runtime |
| FR-SRCH-001/A04 | Chỉ Published chưa xóa, trả relevanceScore khi có keyword | IT private/deleted/rank schema | Chưa kiểm runtime |
| FR-SRCH-001/A05 | Không có kết quả trả 200 data=[] và UI gợi ý; API message theo INV-G06 | IT zero match + browser empty state | Chưa kiểm runtime |
| FR-SRCH-001/A06 | Ký tự đặc biệt/SQL injection không làm lỗi 500 hoặc thay dữ liệu | IT parameterization và tsquery sanitizer | Chưa kiểm runtime |
| FR-SRCH-001/A07 | Search cache TTL 60 giây, key đủ normalized query/filter/sort/page | IT Redis TTL và key isolation | Chưa kiểm runtime |

### FR-SRCH-002

- Nguồn: SRS tr. 37; 63. Ưu tiên: Must theo baseline.
- Đối tượng: Recipe/search filters; /recipes. Phase tiếp nhận: 18.
- Hiện trạng/evidence: E-CONTENT; filter contracts có; không dùng report cũ làm pass. Quyết định/điểm mở: GAP-029; CT-QUERY.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-SRCH-002/A01 | CategoryId/slug và difficulty lọc đúng; cùng gửi ID/slug phải cùng khớp | IT từng filter và mismatch | Chưa kiểm runtime |
| FR-SRCH-002/A02 | maxCookTime và minServings lọc đúng ranh giới; cookTime=0 hợp lệ | IT zero/exact/off-by-one | Chưa kiểm runtime |
| FR-SRCH-002/A03 | minPrepTime/maxPrepTime và maxTime hợp nhất theo baseline/API hiện có | IT khoảng đầu/cuối, đảo khoảng trả 400 | Chưa kiểm runtime |
| FR-SRCH-002/A04 | Mọi filter kết hợp AND, không union kết quả | IT dataset phân biệt từng predicate | Chưa kiểm runtime |
| FR-SRCH-002/A05 | URL/UI giữ filters khi đổi sort/trang; đổi filter/pageSize về trang 1 | React tương tác + browser reload/back/forward | Chưa kiểm runtime |

### FR-SRCH-003

- Nguồn: SRS tr. 37. Ưu tiên: Must theo baseline.
- Đối tượng: sort trên list/search; dropdown /recipes. Phase tiếp nhận: 18.
- Hiện trạng/evidence: E-CONTENT; allowlist hiện có; E-WEB. Quyết định/điểm mở: GAP-029; INV-G03 cho sortBy/sortOrder.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-SRCH-003/A01 | sort=createdAt/title/cookTime và tiền tố - đổi chiều đúng | IT mỗi field/chiều với dữ liệu không đồng thứ tự | Chưa kiểm runtime |
| FR-SRCH-003/A02 | Browse mặc định -createdAt; keyword mặc định relevance giảm dần | IT thiếu sort + UI label nhất quán | Chưa kiểm runtime |
| FR-SRCH-003/A03 | Sort alias hiện có và tie-break ID cho thứ tự trang ổn định | IT alias/duplicate values/page overlap | Chưa kiểm runtime |
| FR-SRCH-003/A04 | Field/hướng không thuộc contract trả 400, không nội suy SQL | IT invalid/injection; INV-G03 không coi thiếu alias là đã được duyệt | Chưa kiểm runtime |

### FR-SRCH-004

- Nguồn: SRS tr. 37. Ưu tiên: Must theo baseline.
- Đối tượng: Pagination list/search/category; UI. Phase tiếp nhận: 18.
- Hiện trạng/evidence: E-CONTENT; PageMeta và endpoint defaults có. Quyết định/điểm mở: GAP-021; INV-G03 cho default khác bảng API.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-SRCH-004/A01 | Default page=1,pageSize=12; page≥1 và pageSize trong 1–50 | IT omit/boundary/zero/negative/51/parse invalid | Chưa kiểm runtime |
| FR-SRCH-004/A02 | Offset=(page-1)*pageSize; total tính trước OFFSET | IT trang đầu/giữa/cuối/ngoài cuối | Chưa kiểm runtime |
| FR-SRCH-004/A03 | Meta có page,pageSize,total,totalPages,hasNextPage,hasPreviousPage đúng | IT empty dataset và số bản ghi không chia hết | Chưa kiểm runtime |
| FR-SRCH-004/A04 | UI chuyển trang giữ filters/sort và phản ánh URL | Browser navigation + React behavior | Chưa kiểm runtime |

### FR-FILE-001

- Nguồn: SRS tr. 38. Ưu tiên: Must theo baseline.
- Đối tượng: IFileStorageService upload; recipe images. Phase tiếp nhận: 19.
- Hiện trạng/evidence: E-CONTENT; E-JOB; storage abstraction có. Quyết định/điểm mở: GAP-022.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-FILE-001/A01 | Upload tới bucket culinary-blog qua abstraction, giữ Content-Type gốc | IT MinIO thật và object metadata | Chưa kiểm runtime |
| FR-FILE-001/A02 | ≤5 MB và đúng MIME/signature allowlist, quá giới hạn 400 | IT boundary trước buffering, xem NFR-SEC-004 | Chưa kiểm runtime |
| FR-FILE-001/A03 | Object key server sinh GUID và folder recipe, không tên/path client | IT traversal/extension/collision attempts | Chưa kiểm runtime |
| FR-FILE-001/A04 | Bucket private; Published media qua controlled URL, private cần authorization | IT direct object URL + media proxy với các actor | Chưa kiểm runtime |
| FR-FILE-001/A05 | Local FileSystem fallback chỉ development khi được cấu hình, production cần MinIO | IT config/profile; không tự đổi storage đang chạy | Chưa kiểm runtime |

### FR-FILE-002

- Nguồn: SRS tr. 38. Ưu tiên: Must theo baseline.
- Đối tượng: IFileStorageService delete; object-delete job. Phase tiếp nhận: 19.
- Hiện trạng/evidence: E-JOB; media outbox và job có. Quyết định/điểm mở: GAP-008/023.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-FILE-002/A01 | Xóa object image đã yêu cầu xóa, gồm variants, qua job sau commit | IT real MinIO object existence + outbox | Chưa kiểm runtime |
| FR-FILE-002/A02 | Object đã thiếu thì delete idempotent, không throw | IT delete cùng object hai lần | Chưa kiểm runtime |
| FR-FILE-002/A03 | Kết nối storage lỗi retry tối đa 3, sau cùng log lỗi/Failed state | IT fault injection và Hangfire state/retry schedule | Chưa kiểm runtime |
| FR-FILE-002/A04 | Lỗi job không đổi HTTP response đã commit, không xóa object recipe khác | IT delayed failure/parent ownership/object-key isolation | Chưa kiểm runtime |

### FR-JOB-001

- Nguồn: SRS tr. 38. Ưu tiên: Must theo baseline.
- Đối tượng: WelcomeEmailJob; /hangfire. Phase tiếp nhận: 20.
- Hiện trạng/evidence: E-JOB; outbox/Hangfire job và MailHog tests có. Quyết định/điểm mở: GAP-023; không cam kết SMTP exactly-once.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-JOB-001/A01 | Đăng ký commit enqueue job fire-and-forget bền trên PostgreSQL | IT DB/outbox/Hangfire restart, chỉ SMTP synthetic | Chưa kiểm runtime |
| FR-JOB-001/A02 | Email HTML có tên user đã encode và link ứng dụng tin cậy; activation link chỉ nếu cần | IT captured MailHog content + HTML injection payload | Chưa kiểm runtime |
| FR-JOB-001/A03 | Retry đúng 1/5/30 phút, sau 3 retry chuyển Failed và log error | IT clock/job state/sanitized logs | Chưa kiểm runtime |
| FR-JOB-001/A04 | SMTP fail không rollback user/phiên; dispatcher cạnh tranh không enqueue trùng thông thường | IT parallel worker + SMTP outage, ghi giới hạn crash sau SMTP accept | Chưa kiểm runtime |
| FR-JOB-001/A05 | Dashboard /hangfire chỉ Admin theo policy/network | HTTP Guest/Author/Admin/operational access matrix | Chưa kiểm runtime |

### FR-JOB-002

- Nguồn: SRS tr. 38. Ưu tiên: Must theo baseline.
- Đối tượng: ImageProcessingJob; gallery/media. Phase tiếp nhận: 19.
- Hiện trạng/evidence: E-JOB; resize job và decode assertions có. Quyết định/điểm mở: GAP-022/023; lựa chọn canvas giữ tỷ lệ cần ghi evidence.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-JOB-002/A01 | Upload thành công enqueue resize bất đồng bộ | IT outbox/job correlation, HTTP không chờ resize | Chưa kiểm runtime |
| FR-JOB-002/A02 | Output medium 800×600 và thumbnail 300×300 decode được | IT đọc bytes WebP trên storage, kiểm pixel dimensions | Chưa kiểm runtime |
| FR-JOB-002/A03 | Giữ original cùng hai variants; DB URLs/status cập nhật sau job | IT object count/key + media projection | Chưa kiểm runtime |
| FR-JOB-002/A04 | Retry 3 lần; fail cuối original vẫn xem được, variants thiếu không gây ảnh vỡ | IT failure/restart/idempotency và browser fallback | Chưa kiểm runtime |
| FR-JOB-002/A05 | Job của recipe đã xóa/ảnh đã xóa không phục hồi nội dung private | IT race delete/resize và visibility | Chưa kiểm runtime |

### FR-JOB-003

- Nguồn: SRS tr. 38–39. Ưu tiên: Must theo baseline.
- Đối tượng: SitemapGenerationJob; /sitemap.xml. Phase tiếp nhận: 20, 21.
- Hiện trạng/evidence: E-JOB; cron job/lock có; G06 lịch sử còn mở. Quyết định/điểm mở: D0-03 ping/lock chờ Phase 02; CT-SEO.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-JOB-003/A01 | Recurring chạy 02:00 UTC hàng ngày với shared persistent queue | IT registration/cron/timezone + restart | Chưa kiểm runtime |
| FR-JOB-003/A02 | Sitemap gồm Published recipes/categories/static pages, không private/deleted | IT parse XML và dataset trạng thái hỗn hợp | Chưa kiểm runtime |
| FR-JOB-003/A03 | Upload artifact thành công nguyên tử; lỗi giữ artifact hợp lệ trước | IT storage outage/fault injection | Chưa kiểm runtime |
| FR-JOB-003/A04 | Retry 2 lần và log URL count/result | IT retry metadata + captured Serilog | Chưa kiểm runtime |
| FR-JOB-003/A05 | Singleton khi nhiều worker/instance | Multi-instance test; RedLock so Hangfire lock chờ Phase 02 | Chưa kiểm runtime |
| FR-JOB-003/A06 | Ping Google sau update là yêu cầu nguyên văn còn mở | Decision D0-03 trước khi gọi provider; không gửi ping trong inventory | Chưa kiểm runtime |

### FR-OBS-001

- Nguồn: SRS tr. 39; 65–66. Ưu tiên: Must theo baseline.
- Đối tượng: GET /health, /health/live, /health/ready. Phase tiếp nhận: 33.
- Hiện trạng/evidence: E-OBS; HealthResponseWriter chỉ status; chưa runtime. Quyết định/điểm mở: INV-G05 cho response entries.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-OBS-001/A01 | /health tổng hợp DB/Redis/MinIO; healthy 200, unhealthy 503 | IT từng dependency down và recovery trong stack riêng | Chưa kiểm runtime |
| FR-OBS-001/A02 | /health/live chỉ process, còn sống trả 200 dù dependency down | IT dependency outage/live probe | Chưa kiểm runtime |
| FR-OBS-001/A03 | /health/ready kiểm DB + Redis, lỗi trả 503 và load balancer ngừng route | IT failure + staging probe evidence | Chưa kiểm runtime |
| FR-OBS-001/A04 | Aggregate response contract status/entries cần quyết định privacy | Schema review INV-G05; không suy status-only khớp nguyên văn | Chưa kiểm runtime |
| FR-OBS-001/A05 | Health endpoints public không tiêu rate quota | IT request vượt quota API rồi probe health | Chưa kiểm runtime |

### FR-OBS-002

- Nguồn: SRS tr. 39. Ưu tiên: Must theo baseline.
- Đối tượng: HTTP middleware/CQRS logs; operations/audit. Phase tiếp nhận: 06, 33.
- Hiện trạng/evidence: E-OBS; middleware/behaviors có; chưa capture runtime. Quyết định/điểm mở: CONS-010 và NFR-SEC-006.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-OBS-002/A01 | Mỗi HTTP log có correlation ID, method/path/status/elapsed ms và userId nếu auth | IT captured structured events Guest/Author | Chưa kiểm runtime |
| FR-OBS-002/A02 | Client X-Correlation-ID được trả về hoặc server sinh mới, truyền qua downstream | IT HTTP→log→job/dependency correlation | Chưa kiểm runtime |
| FR-OBS-002/A03 | Mọi Command/Query được log với request type/time, không lộ password/token/secret | IT pipeline success/validation/exception và redaction | Chưa kiểm runtime |
| FR-OBS-002/A04 | Request/pipeline >500ms log Warning performance | IT controllable delay và captured level | Chưa kiểm runtime |
| FR-OBS-002/A05 | Console JSON, file rolling daily, Seq development; level Debug dev/Information prod | Config review + runtime sink output/failure | Chưa kiểm runtime |

### FR-OBS-003

- Nguồn: SRS tr. 39. Ưu tiên: Must theo baseline.
- Đối tượng: OpenTelemetry HTTP/EF/custom metrics; OTLP. Phase tiếp nhận: 33.
- Hiện trạng/evidence: E-OBS; telemetry registration có; exporter chưa kiểm. Quyết định/điểm mở: Không miễn phần exporter chỉ vì cấu hình tồn tại.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| FR-OBS-003/A01 | HTTP request và EF operation sinh trace spans | IT collector riêng, inspect parent/child spans | Chưa kiểm runtime |
| FR-OBS-003/A02 | Activity.TraceId xuất hiện trong structured log và khớp trace | IT trace/log correlation | Chưa kiểm runtime |
| FR-OBS-003/A03 | Recipe created/published counter chỉ tăng sau thành công | IT business metrics trước-sau success/failure | Chưa kiểm runtime |
| FR-OBS-003/A04 | Request count/duration histogram/error rate được export OTLP | IT traffic synthetic + collector values | Chưa kiểm runtime |
| FR-OBS-003/A05 | Dev Seq hoặc production Jaeger/Tempo nhận traces đúng cấu hình | Runtime exporter evidence theo môi trường; không thay bằng source assertion | Chưa kiểm runtime |

### NFR-PERF-001

- Nguồn: SRS tr. 40. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: mọi API endpoint. Phase tiếp nhận: 30.
- Hiện trạng/evidence: E-PERF; k6 harness có, số đo lịch sử không tái dùng. Quyết định/điểm mở: Đo production tải thực, warm hit≥80%; không thay bằng localhost.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-PERF-001/A01 | GET cached p50≤150ms | k6/OTEL theo endpoint, lưu workload/hardware/artifact/histogram | Chưa kiểm runtime |
| NFR-PERF-001/A02 | Mọi API kể cả write p95≤500ms | Load riêng reads/writes, không chỉ average | Chưa kiểm runtime |
| NFR-PERF-001/A03 | p99≤1000ms | Load report percentile/error count trên cửa sổ đo chốt ở Phase 02/30 | Chưa kiểm runtime |

### NFR-PERF-002

- Nguồn: SRS tr. 40. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: API single/multiple instances. Phase tiếp nhận: 30, 35.
- Hiện trạng/evidence: E-PERF; chưa benchmark hiện tại. Quyết định/điểm mở: D0-04 định nghĩa degradation/tăng tuyến tính.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-PERF-002/A01 | ≥100 concurrent users trên 2 vCPU/4GB single instance không degradation | k6 smoke→load→stress, resource/error/latency so baseline | Chưa kiểm runtime |
| NFR-PERF-002/A02 | Thêm instance tăng throughput tuyến tính theo tiêu chí đã chốt | Multi-instance benchmark cùng dataset/tải; chờ D0-04 | Chưa kiểm runtime |

### NFR-PERF-003

- Nguồn: SRS tr. 40. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: Redis public content cache. Phase tiếp nhận: 14, 15, 30.
- Hiện trạng/evidence: E-CACHE; cache-aside có; cơ chế Output Cache còn G05. Quyết định/điểm mở: GAP-013; D0-03 cơ chế cache.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-PERF-003/A01 | Steady-state Redis cache hit rate≥80% | Runtime hit/miss metrics với workload định nghĩa rõ | Chưa kiểm runtime |
| NFR-PERF-003/A02 | Category TTL1800/detail300/list900/search60 giây | IT TTL query/clock, đủ cả hit và miss | Chưa kiểm runtime |
| NFR-PERF-003/A03 | Create/update/delete invalidates mọi cache liên quan sau commit | IT mutation matrix + backend→Next + hai instance | Chưa kiểm runtime |

### NFR-PERF-004

- Nguồn: SRS tr. 40–41. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: EF queries/indexes. Phase tiếp nhận: 30.
- Hiện trạng/evidence: E-DATA; E-PERF; query/index source có. Quyết định/điểm mở: Không suy mọi WHERE/ORDER BY đã tối ưu từ vài index.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-PERF-004/A01 | Không có N+1; projection/Include cần thiết cho collection | IT command count khi tăng số recipes/children | Chưa kiểm runtime |
| NFR-PERF-004/A02 | Mỗi WHERE/ORDER BY column có B-tree tương ứng theo nguyên văn | Schema/query inventory + EXPLAIN; ngoại lệ GIN/composite cần decision review | Chưa kiểm runtime |
| NFR-PERF-004/A03 | Query>100ms có slow Warning | IT delay/slow query captured log | Chưa kiểm runtime |
| NFR-PERF-004/A04 | EXPLAIN ANALYZE được review trước merge | Lưu plans/dataset/version và reviewer thực, không tự ký review | Chưa kiểm runtime |

### NFR-PERF-005

- Nguồn: SRS tr. 41. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: Next.js public/private pages. Phase tiếp nhận: 31.
- Hiện trạng/evidence: E-WEB; E-PERF; budget harness có. Quyết định/điểm mở: Lighthouse TBT không thay INP.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-PERF-005/A01 | LCP≤2.5s | Lighthouse/browser production artifact + network/device profile | Chưa kiểm runtime |
| NFR-PERF-005/A02 | CLS≤0.1 | Lighthouse/browser layout shift trace | Chưa kiểm runtime |
| NFR-PERF-005/A03 | INP≤200ms | Browser interaction/RUM đủ mẫu theo quyết định, ghi percentile/cửa sổ | Chưa kiểm runtime |
| NFR-PERF-005/A04 | First Load JS≤200KB gzip cho từng route | Build đúng Node22.23.3 rồi npm run budget:check; lưu từng route | Chưa kiểm runtime |
| NFR-PERF-005/A05 | ISR/Image optimization/code splitting hỗ trợ ngân sách | Production HTTP/image/network trace; không đánh pass từ keyword source | Chưa kiểm runtime |

### NFR-SEC-001

- Nguồn: SRS tr. 41. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: Identity registration/password storage. Phase tiếp nhận: 07, 32.
- Hiện trạng/evidence: E-AUTH; Identity options/service có. Quyết định/điểm mở: Không log hash/token khi thu evidence.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-001/A01 | PBKDF2-HMACSHA512 iteration≥100000, không plaintext DB | IT kiểm format/hash parameters và captured redacted logs | Chưa kiểm runtime |
| NFR-SEC-001/A02 | Password≥8 có hoa/thường/số/ký tự đặc biệt | UT từng class thiếu và boundaries + IT Identity | Chưa kiểm runtime |

### NFR-SEC-002

- Nguồn: SRS tr. 41. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: JWT/refresh local và Google. Phase tiếp nhận: 08, 09, 10.
- Hiện trạng/evidence: E-AUTH; lifecycle còn G02. Quyết định/điểm mở: GAP-006 chọn 512-bit thay 128-bit.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-002/A01 | JWT HS256 TTL15 phút có userId/email/roles/jti và verify issuer/audience/signature/expiry | IT decoded claims và invalid variations, không persist raw evidence | Chưa kiểm runtime |
| NFR-SEC-002/A02 | RT CSPRNG512-bit chỉ SHA256 hash trong DB TTL7 ngày | UT generator + IT DB/expiry | Chưa kiểm runtime |
| NFR-SEC-002/A03 | Rotation revoke token cũ ngay và reuse revoke toàn family | IT normal/race/reuse/restart | Chưa kiểm runtime |
| NFR-SEC-002/A04 | Logout revoke phiên đúng owner cả Google/local | IT owner mismatch/expired access và browser | Chưa kiểm runtime |

### NFR-SEC-003

- Nguồn: SRS tr. 41; 47. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: auth/global/upload IP limit. Phase tiếp nhận: 16.
- Hiện trạng/evidence: E-OBS; auth hiện fixed window, counters process-local. Quyết định/điểm mở: D0-03 và CT-HTTP.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-003/A01 | Auth10/phút/IP dùng sliding window | IT tại ranh giới cửa sổ, request thứ11 bị 429 | Chưa kiểm runtime |
| NFR-SEC-003/A02 | Global100/phút/IP fixed window | IT request101, IP độc lập và health exempt | Chưa kiểm runtime |
| NFR-SEC-003/A03 | Upload5/phút/IP fixed window | IT request6, tương tác quota global | Chưa kiểm runtime |
| NFR-SEC-003/A04 | 429 Problem Details có Retry-After giây và X-RateLimit headers | IT HTTP/cors, reset Unix timestamp | Chưa kiểm runtime |
| NFR-SEC-003/A05 | Quota chia sẻ qua instance, không tin IP spoof ngoài trusted proxy | IT xen kẽ hai API và spoof forwarded headers | Chưa kiểm runtime |

### NFR-SEC-004

- Nguồn: SRS tr. 41. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: mọi input/upload. Phase tiếp nhận: 04, 19, 32.
- Hiện trạng/evidence: E-CONTENT; validation pipeline có, chưa rà đủ. Quyết định/điểm mở: CONS-008; CT-ARCH.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-004/A01 | Input validate tại Application trước xử lý/side effects | IT invalid body/query/ID/sort/file metadata, DB/job/cache không đổi | Chưa kiểm runtime |
| NFR-SEC-004/A02 | SQL user input parameterized; không injection qua search/sort | Static review + IT malicious input | Chưa kiểm runtime |
| NFR-SEC-004/A03 | XSS input sanitization và CSP bảo vệ render | Browser stored/reflected payload, header evidence | Chưa kiểm runtime |
| NFR-SEC-004/A04 | Upload đọc magic bytes và decode thay vì tin Content-Type | IT spoof/corrupt từng format | Chưa kiểm runtime |
| NFR-SEC-004/A05 | Kiểm size trước đọc stream, không buffer file quá giới hạn vào RAM | Instrumented stream/HTTP oversize test và memory evidence | Chưa kiểm runtime |
| NFR-SEC-004/A06 | Filename GUID không dùng tên client, chặn path traversal | IT filename/path payload + storage key assertions | Chưa kiểm runtime |

### NFR-SEC-005

- Nguồn: SRS tr. 41. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: Nginx HTTPS/CORS/auth transport. Phase tiếp nhận: 32.
- Hiện trạng/evidence: E-OPS; Nginx/CORS/HSTS config có. Quyết định/điểm mở: GAP-007 body RT; cookie rule có điều kiện.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-005/A01 | Production mọi traffic HTTPS TLS≥1.2, HTTP redirect HTTPS | Staging TLS scan + HTTP headers | Chưa kiểm runtime |
| NFR-SEC-005/A02 | HSTS max-age31536000 | HTTPS response kiểm header | Chưa kiểm runtime |
| NFR-SEC-005/A03 | CORS chỉ origin cấu hình, không wildcard, đúng methods/headers | HTTP allowed/disallowed origin + OPTIONS | Chưa kiểm runtime |
| NFR-SEC-005/A04 | Nếu dùng refresh cookie thì SameSite Strict/Secure; baseline dùng body | Review transport thực; không tự thêm cookie để test điều kiện | Chưa kiểm runtime |

### NFR-SEC-006

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: Application authz/write audit. Phase tiếp nhận: 05, 06, 32.
- Hiện trạng/evidence: E-CONTENT; policies/ownership có; chưa đầy đủ Application. Quyết định/điểm mở: INV-G07 cho VerifiedAuthor trang13.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-006/A01 | Application kiểm resource owner cho Author, Admin bypass hợp lệ | Direct handler test không chỉ endpoint filter | Chưa kiểm runtime |
| NFR-SEC-006/A02 | AuthorPolicy/AdminPolicy kiểm active user, không nhận role/userId client tự khai | IT forged payload/inactive accounts | Chưa kiểm runtime |
| NFR-SEC-006/A03 | DELETE/publish kiểm sensitive ownership trước commit | IT concurrent ownership/user state change và rollback | Chưa kiểm runtime |
| NFR-SEC-006/A04 | Mọi write log userId+timestamp | IT captured audit success/failure và actor đúng | Chưa kiểm runtime |

### NFR-SEC-007

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Rất cao, không gán MoSCoW từng mã).
- Đối tượng: Git/runtime secrets. Phase tiếp nhận: 32.
- Hiện trạng/evidence: E-OPS; scan workflow/config có. Quyết định/điểm mở: Rotate90 ngày là khuyến nghị, không tự rotate key.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEC-007/A01 | Không secrets trong Git history/artifacts | Gitleaks/truffleHog scan đúng phạm vi, lưu summary không in secrets | Chưa kiểm runtime |
| NFR-SEC-007/A02 | Dev User Secrets; production env/Kubernetes Secrets, không hardcode | Config/deployment review | Chưa kiểm runtime |
| NFR-SEC-007/A03 | Pre-commit secret scanning hook hoạt động | Synthetic fake-secret test trong sandbox riêng, không commit secret | Chưa kiểm runtime |
| NFR-SEC-007/A04 | Key rotation90 ngày có hướng dẫn/trạng thái khuyến nghị | Operations review; không tự đổi credentials | Chưa kiểm runtime |

### NFR-USE-001

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: mọi screen/breakpoint. Phase tiếp nhận: 27, 28.
- Hiện trạng/evidence: E-WEB; source/JSDOM tests không chứng nhận thiết bị. Quyết định/điểm mở: GAP-024.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-USE-001/A01 | 320–767 mobile một cột, touch-friendly | Browser 320/375/767 và iOS/Android screenshot | Chưa kiểm runtime |
| NFR-USE-001/A02 | 768–1199 tablet hai cột | Browser 768/1024/1199 visual inspection | Chưa kiểm runtime |
| NFR-USE-001/A03 | ≥1200 desktop full layout, không overflow | Browser 1200/1440 và long-content fixture | Chưa kiểm runtime |
| NFR-USE-001/A04 | Tailwind utility-first, không framework override; BrowserStack/device matrix | Source review + device/browser evidence riêng | Chưa kiểm runtime |

### NFR-USE-002

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: tất cả màn hình tương tác. Phase tiếp nhận: 27, 28.
- Hiện trạng/evidence: E-WEB; a11y tests hiện là nguồn/DOM. Quyết định/điểm mở: WCAG2.1AA vẫn bắt buộc.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-USE-002/A01 | Semantic article/nav/main/aside và heading hợp lý | Axe + browser DOM review | Chưa kiểm runtime |
| NFR-USE-002/A02 | Interactive có aria-label/expanded/role phù hợp | Axe + expanded/collapsed interaction | Chưa kiểm runtime |
| NFR-USE-002/A03 | Mọi chức năng dùng Tab/Enter/Escape, focus thấy được/không kẹt | Browser keyboard toàn critical flows | Chưa kiểm runtime |
| NFR-USE-002/A04 | Contrast text≥4.5:1 và UI≥3:1 | Browser contrast measurements từng state | Chưa kiểm runtime |
| NFR-USE-002/A05 | NVDA Windows và VoiceOver macOS/iOS đọc/thao tác được | Người kiểm/screen reader thật, ghi phiên bản/kịch bản | Chưa kiểm runtime |

### NFR-USE-003

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: API errors và mọi form. Phase tiếp nhận: 27.
- Hiện trạng/evidence: E-AUTH; E-WEB; field-error/RHF có. Quyết định/điểm mở: GAP-002; CT-ERR.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-USE-003/A01 | API lỗi RFC7807 type/title/status/detail/errors theo trường | IT schema và lỗi validation/domain/system | Chưa kiểm runtime |
| NFR-USE-003/A02 | Frontend inline errors cạnh field bằng RHF, actionable | React/browser submit invalid + server errors | Chưa kiểm runtime |
| NFR-USE-003/A03 | 5xx UI thân thiện, không stack trace | Browser API fault response và raw body review | Chưa kiểm runtime |
| NFR-USE-003/A04 | Error codes dùng branching/i18n-ready, không phụ thuộc message locale | Source + IT đổi message nhưng giữ code | Chưa kiểm runtime |

### NFR-USE-004

- Nguồn: SRS tr. 42. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: fetch/write/upload UI. Phase tiếp nhận: 27.
- Hiện trạng/evidence: E-WEB; loading/toast/optimistic một số luồng có. Quyết định/điểm mở: Phải rà mọi async operation, không chỉ category.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-USE-004/A01 | Fetch hiển thị skeleton/loading không blank screen | Browser slow network cho từng screen | Chưa kiểm runtime |
| NFR-USE-004/A02 | Optimistic write cập nhật ngay và rollback khi lỗi | React/browser fail category save và mọi luồng áp dụng | Chưa kiểm runtime |
| NFR-USE-004/A03 | Write có toast success/error | Browser từng mutation thành công/thất bại | Chưa kiểm runtime |
| NFR-USE-004/A04 | Upload hiển thị progress % thời gian thực | Browser throttled multipart, error/cancel | Chưa kiểm runtime |

### NFR-REL-001

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: production monitoring/maintenance. Phase tiếp nhận: 36.
- Hiện trạng/evidence: E-OPS; chưa có cửa sổ uptime hiện tại. Quyết định/điểm mở: D0-04:99.5% không tương đương3.65h/năm.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-REL-001/A01 | Uptime≥99.5% theo cửa sổ/ngân sách downtime đã chốt | Monitoring series thực và công thức; không tự dùng3.65h | Chưa kiểm runtime |
| NFR-REL-001/A02 | Maintenance thông báo banner trước48 giờ | Lịch maintenance được phép và evidence timestamp; không gửi thông báo lúc inventory | Chưa kiểm runtime |
| NFR-REL-001/A03 | Readiness probe mỗi10 giây | Deployment config + observed probe interval | Chưa kiểm runtime |
| NFR-REL-001/A04 | Down>1 phút có alert receiver được phép | Controlled staging drill + delivery evidence, không tự nhắn người khác | Chưa kiểm runtime |

### NFR-REL-002

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: exception/DB/Redis/jobs. Phase tiếp nhận: 15, 19, 20, 33.
- Hiện trạng/evidence: E-OBS; E-JOB; E-CACHE; drill cũ chưa tái chạy. Quyết định/điểm mở: Circuit breaker optional, retry sitemap2 theo FR cụ thể.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-REL-002/A01 | Unhandled exception trả500 Problem Details và log, process còn sống | IT injected exception rồi health/live | Chưa kiểm runtime |
| NFR-REL-002/A02 | DB pool reconnect, timeout30 giây | Isolated DB outage/recovery và configuration/runtime timing | Chưa kiểm runtime |
| NFR-REL-002/A03 | Redis down đọc DB không throw, write đã commit không hỏng | IT timeout/down/recovery và business values | Chưa kiểm runtime |
| NFR-REL-002/A04 | Jobs retry tối đa3 với backoff, sitemap riêng2 | IT job state/restart, không suy từ attribute | Chưa kiểm runtime |
| NFR-REL-002/A05 | Circuit breaker external HTTP chỉ tùy chọn nâng cao | Ghi không dùng như mandatory gate nếu không có yêu cầu khác | Chưa kiểm runtime |

### NFR-REL-003

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: DB/MinIO/token/backup. Phase tiếp nhận: 34.
- Hiện trạng/evidence: E-OPS; backup/restore harness có. Quyết định/điểm mở: GAP-008; D0-04 múi giờ03:00.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-REL-003/A01 | PostgreSQL WAL/ACID giữ dữ liệu commit qua crash/restart | Dedicated crash/restart drill + consistency query | Chưa kiểm runtime |
| NFR-REL-003/A02 | pg_dump tự động03:00 hằng ngày giữ30 ngày | Scheduler/timezone/retention evidence; chờ D0-04 timezone | Chưa kiểm runtime |
| NFR-REL-003/A03 | Restore backup khôi phục dataset kiểm đếm được | Dedicated restore DB/object target, không stack user | Chưa kiểm runtime |
| NFR-REL-003/A04 | MinIO persistent volume giữ ảnh qua restart | Isolated volume/restart/read/hash test | Chưa kiểm runtime |
| NFR-REL-003/A05 | Refresh token DB còn dùng được qua restart | IT token rotation trước-sau restart | Chưa kiểm runtime |
| NFR-REL-003/A06 | Recipe soft-delete không mất child/object theo baseline | IT deletion/recovery dữ liệu; restore API không trong v1 | Chưa kiểm runtime |

### NFR-MAINT-001

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: CI/review/toolchain. Phase tiếp nhận: 03, 29.
- Hiện trạng/evidence: E-OPS; analyzer/lint config có, tool khác nguyên văn. Quyết định/điểm mở: D0-03 SonarAnalyzer/StyleCop/Airbnb.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-MAINT-001/A01 | .NET qua SonarAnalyzer/StyleCop/EditorConfig hoặc sai khác được chấp thuận | Config + CI gate thực, không gọi analyzer khác là tương đương tự động | Chưa kiểm runtime |
| NFR-MAINT-001/A02 | TS/React ESLint Airbnb và Prettier hoặc quyết định sai khác | Config review + lint/format đúng runtime | Chưa kiểm runtime |
| NFR-MAINT-001/A03 | Build CI không compiler warnings | Build/CI logs đúng artifact | Chưa kiểm runtime |
| NFR-MAINT-001/A04 | PR có ít nhất1 reviewer phê duyệt | GitHub review thực; commit/push không thay approval | Chưa kiểm runtime |

### NFR-MAINT-002

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: test suite/coverage/CI. Phase tiếp nhận: 23, 24, 25, 26, 29.
- Hiện trạng/evidence: E-TEST; coverage62.56% chỉ lịch sử E-HIST. Quyết định/điểm mở: D0-03 Jest/Testing Library khác Node/JSDOM.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-MAINT-002/A01 | Application unit line coverage≥80% cho command/query/validator | dotnet unit XPlat + report/gate, không dùng integration coverage thay | Chưa kiểm runtime |
| NFR-MAINT-002/A02 | Mỗi API endpoint≥1 happy+1 error có assertion | Endpoint matrix→test method→run evidence | Chưa kiểm runtime |
| NFR-MAINT-002/A03 | 5 critical browser E2E register/login/create/publish/search | Playwright thật trên stack test riêng, trace mỗi flow | Chưa kiểm runtime |
| NFR-MAINT-002/A04 | Backend xUnit/frontend Jest+Testing Library/E2E Playwright hoặc quyết định khác | Manifest/config review, không đổi công cụ trong inventory | Chưa kiểm runtime |
| NFR-MAINT-002/A05 | CI thực sự fail khi dưới coverage hoặc E2E lỗi | Controlled negative gate proof và CI URL | Chưa kiểm runtime |

### NFR-MAINT-003

- Nguồn: SRS tr. 43. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: README/Scalar/ADR/CHANGELOG. Phase tiếp nhận: 37.
- Hiện trạng/evidence: E-OPS; tài liệu hiện có. Quyết định/điểm mở: Fresh setup phải đo, không suy từ README.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-MAINT-003/A01 | Fresh dev setup Docker Compose<5 phút | Clean isolated setup timing, ghi hardware/network/cache | Chưa kiểm runtime |
| NFR-MAINT-003/A02 | API docs tự sinh XML comments+Scalar tại /scalar, khớp contract | IT docs/schema + HTTP UI | Chưa kiểm runtime |
| NFR-MAINT-003/A03 | Mọi quyết định kiến trúc quan trọng có ADR | Review ADR↔implementation↔decision register | Chưa kiểm runtime |
| NFR-MAINT-003/A04 | CHANGELOG mỗi release theo Keep a Changelog+SemVer | Artifact/release review | Chưa kiểm runtime |

### NFR-MAINT-004

- Nguồn: SRS tr. 43–44. Ưu tiên: Must theo baseline (SRS nhóm Trung bình, không gán MoSCoW từng mã).
- Đối tượng: dependency graph/CQRS. Phase tiếp nhận: 04, 05, 06, 29.
- Hiện trạng/evidence: E-ARCH; 3 architecture tests lịch sử chưa tái chạy. Quyết định/điểm mở: GAP-026 Domain chỉ BCL.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-MAINT-004/A01 | Domain không phụ thuộc layer/package ngoài; Application không Infrastructure | dotnet architecture tests + project dependency review | Chưa kiểm runtime |
| NFR-MAINT-004/A02 | Infrastructure implements Application interfaces | Static reference/use-case review | Chưa kiểm runtime |
| NFR-MAINT-004/A03 | Command ghi, Query chỉ đọc, mỗi use case handler riêng | Use-case inventory và handler tests; không coi wrapper rỗng đủ business placement | Chưa kiểm runtime |
| NFR-MAINT-004/A04 | Architecture tests chặn violation trong CI | Negative fixture/gate proof phù hợp, CI logs | Chưa kiểm runtime |

### NFR-SCALE-001

- Nguồn: SRS tr. 44. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: API/Redis/Hangfire multiple workers. Phase tiếp nhận: 35.
- Hiện trạng/evidence: E-CACHE; E-JOB; rate counter hiện process-local. Quyết định/điểm mở: D0-03 RedLock so Hangfire PostgreSQL lock.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SCALE-001/A01 | API JWT stateless, không session affinity/server session | Requests xen kẽ instances với cùng identity | Chưa kiểm runtime |
| NFR-SCALE-001/A02 | Shared state/cache/rate counters Redis, không IMemoryCache local | Multi-instance cache/quota consistency test | Chưa kiểm runtime |
| NFR-SCALE-001/A03 | Singleton sitemap có distributed lock được chấp thuận | Concurrent workers/restart/lock expiry; decision D0-03 | Chưa kiểm runtime |
| NFR-SCALE-001/A04 | Hangfire multiple workers dùng chung PostgreSQL queue, restart không mất job | Multi-instance job execution/idempotency drill | Chưa kiểm runtime |

### NFR-SCALE-002

- Nguồn: SRS tr. 44. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: Npgsql/PostgreSQL. Phase tiếp nhận: 30, 35.
- Hiện trạng/evidence: E-DATA; connection pool config cần runtime review. Quyết định/điểm mở: Replica optional; partition>1 triệu rows nâng cao.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SCALE-002/A01 | Pool max100 connections/instance | Config + observed pg_stat_activity dưới tải nhiều instances | Chưa kiểm runtime |
| NFR-SCALE-002/A02 | B-tree equality/range, GIN tsvector | Catalog và EXPLAIN ANALYZE dataset yêu cầu | Chưa kiểm runtime |
| NFR-SCALE-002/A03 | Read replica/split routing chỉ tùy chọn | Ghi topology có/không, không tự thêm hạ tầng | Chưa kiểm runtime |
| NFR-SCALE-002/A04 | Partition khi>1 triệu rows là nâng cao | Ghi initial scale≤10000 recipes, không bắt buộc triển khai lúc inventory | Chưa kiểm runtime |

### NFR-SCALE-003

- Nguồn: SRS tr. 44. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: production topology. Phase tiếp nhận: 35.
- Hiện trạng/evidence: E-OPS; Docker/Nginx templates không chứng minh deployment. Quyết định/điểm mở: D0-04 phạm vi production scaling.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SCALE-003/A01 | API/Postgres/Redis/MinIO/Nginx là service container riêng | Deployment inspect đúng digest/topology | Chưa kiểm runtime |
| NFR-SCALE-003/A02 | Nginx upstream phân tải nhiều API instance | Runtime round-robin/failover evidence | Chưa kiểm runtime |
| NFR-SCALE-003/A03 | Production storage MinIO distributed≥4 nodes | Production topology/availability evidence hoặc decision chưa chốt | Chưa kiểm runtime |
| NFR-SCALE-003/A04 | _next/static qua CDN Cloudflare | Public response/cache/network trace đúng release | Chưa kiểm runtime |

### NFR-SEO-001

- Nguồn: SRS tr. 44. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: /recipes/[slug] JSON-LD. Phase tiếp nhận: 21.
- Hiện trạng/evidence: E-WEB; SEO harness có, chưa Rich Results Test mới. Quyết định/điểm mở: GAP-033 không rating giả; nutrition chỉ dữ liệu thật.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEO-001/A01 | Recipe JSON-LD có name/description/image/author/datePublished | Parse HTML/JSON-LD, default image hợp lệ | Chưa kiểm runtime |
| NFR-SEO-001/A02 | prepTime/cookTime/totalTime/yield đúng kiểu duration/servings | Structured-data assertions với zero cookTime | Chưa kiểm runtime |
| NFR-SEO-001/A03 | Ingredient/instructions theo thứ tự; nutrition field thực khi có | IT API→HTML mapping + omit null, không bịa dữ liệu | Chưa kiểm runtime |
| NFR-SEO-001/A04 | Google Rich Results Test pass100% cho trang test được phép | Provider/deployed URL + saved result; không chỉ snapshot | Chưa kiểm runtime |
| NFR-SEO-001/A05 | JSON-LD escape chống đóng script/XSS | Browser malicious title/description và schema parse | Chưa kiểm runtime |

### NFR-SEO-002

- Nguồn: SRS tr. 44. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: metadata mọi page. Phase tiếp nhận: 21.
- Hiện trạng/evidence: E-WEB; G06 lengths còn mở. Quyết định/điểm mở: Không thêm text vô nghĩa để đủ độ dài.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEO-002/A01 | Title hoàn chỉnh {Recipe Name} \| Culinary Blog≤60 ký tự | HTML assertions với tên ngắn/dài, cả suffix | Chưa kiểm runtime |
| NFR-SEO-002/A02 | Meta description150–160 ký tự có nghĩa | HTML/content review trường hợp ngắn/dài | Chưa kiểm runtime |
| NFR-SEO-002/A03 | OG title/description/image1200×630/url/type đủ | HTTP image decode + HTML metadata | Chưa kiểm runtime |
| NFR-SEO-002/A04 | Twitter summary_large_image và canonical slug URL đúng | HTML assertions gồm paging/query/alias | Chưa kiểm runtime |
| NFR-SEO-002/A05 | Published index/follow; Draft/Archived noindex không public cached | HTTP robots/header + actor/cache matrix | Chưa kiểm runtime |

### NFR-SEO-003

- Nguồn: SRS tr. 44–45. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: sitemap.xml/robots.txt. Phase tiếp nhận: 20, 21.
- Hiện trạng/evidence: E-JOB; SitemapXml hiện cần review fields. Quyết định/điểm mở: D0-03 ping Google còn mở.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEO-003/A01 | Sitemap chuẩn XML chứa loc/lastmod/changefreq/priority | Parse XSD/content, check date values | Chưa kiểm runtime |
| NFR-SEO-003/A02 | 02:00 UTC, Published+categories+static, không private | IT cron/dataset và generated artifact | Chưa kiểm runtime |
| NFR-SEO-003/A03 | robots cho crawlers và khai báo sitemap tuyệt đối | Production HTTP robots.txt | Chưa kiểm runtime |
| NFR-SEO-003/A04 | Ping sau update theo nguyên văn hoặc decision điều chỉnh | D0-03 + provider evidence nếu được duyệt; chưa gọi ngoài | Chưa kiểm runtime |

### NFR-SEO-004

- Nguồn: SRS tr. 45. Ưu tiên: Must theo baseline (SRS nhóm Cao, không gán MoSCoW từng mã).
- Đối tượng: Recipe/category slug và redirects. Phase tiếp nhận: 21.
- Hiện trạng/evidence: E-WEB; E-DATA; chưa thấy history table trong current config. Quyết định/điểm mở: GAP-031; D6-11.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| NFR-SEO-004/A01 | URL /recipes/{slug}, /categories/{slug} lowercase không dấu/hyphen/unique | UT slug Unicode + HTTP route/canonical | Chưa kiểm runtime |
| NFR-SEO-004/A02 | Slug tự sinh từ title và bất biến sau publish đầu tiên | IT Draft→Published→Draft rename | Chưa kiểm runtime |
| NFR-SEO-004/A03 | Draft slug đổi lưu history và redirect301 từ cũ sang mới an toàn | HTTP status/Location/no loop/no private disclosure | Chưa kiểm runtime |
| NFR-SEO-004/A04 | Nội dung chính dùng path; query chỉ filter/sort/page | Browser/canonical route review | Chưa kiểm runtime |

### CONS-001

- Nguồn: SRS tr. 14. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: backend4 layers. Phase tiếp nhận: 04, 29.
- Hiện trạng/evidence: E-ARCH. Quyết định/điểm mở: GAP-026.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-001/A01 | Tách Domain/Application/Infrastructure/Presentation đúng dependency hướng vào | Architecture tests + csproj graph | Chưa kiểm runtime |
| CONS-001/A02 | Domain chỉ.NET BCL không package ngoài | Review csproj/lock/transitive references | Chưa kiểm runtime |

### CONS-002

- Nguồn: SRS tr. 14. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: mọi use case Application. Phase tiếp nhận: 04, 06.
- Hiện trạng/evidence: E-ARCH; E-CONTENT. Quyết định/điểm mở: Audit/sitemap hiện còn gọi adapter trực tiếp.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-002/A01 | Mỗi use case Command hoặc Query Handler riêng qua MediatR | Inventory endpoint→request→handler, cả audit/sitemap | Chưa kiểm runtime |
| CONS-002/A02 | Không trộn read/write hoặc đặt orchestration nghiệp vụ tại endpoint | Direct handler test + code review use case | Chưa kiểm runtime |

### CONS-003

- Nguồn: SRS tr. 14. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: backend/frontend framework. Phase tiếp nhận: 03, 29.
- Hiện trạng/evidence: E-ARCH; E-WEB. Quyết định/điểm mở: GAP-025.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-003/A01 | Backend.NET10 Minimal APIs không MVC Controllers | csproj/route review và build đúng SDK | Chưa kiểm runtime |
| CONS-003/A02 | Frontend Next.js15 App Router không Pages Router | package.json/app tree/build route manifest | Chưa kiểm runtime |

### CONS-004

- Nguồn: SRS tr. 14. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: auth/security. Phase tiếp nhận: 07, 08.
- Hiện trạng/evidence: E-AUTH. Quyết định/điểm mở: GAP-006.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-004/A01 | JWT stateless AT15 phút/RT7 ngày | IT claims/expiry/restart không affinity | Chưa kiểm runtime |
| CONS-004/A02 | Identity PBKDF2 hash password không plaintext | IT hash policy/storage theo NFR-SEC-001 | Chưa kiểm runtime |

### CONS-005

- Nguồn: SRS tr. 14. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: REST/API errors. Phase tiếp nhận: 22, 24.
- Hiện trạng/evidence: E-CONTENT; E-OBS. Quyết định/điểm mở: GAP-002/003/021; CT-HTTP/ERR.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-005/A01 | REST method/path/resource và URL version /api/v1 đúng inventory | OpenAPI/source route comparison + HTTP | Chưa kiểm runtime |
| CONS-005/A02 | Lỗi RFC7807 application/problem+json có code/type ổn định | IT từng status/error mapping kể cả middleware/auth | Chưa kiểm runtime |

### CONS-006

- Nguồn: SRS tr. 15. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: EF/PostgreSQL migrations. Phase tiếp nhận: 22, 30.
- Hiện trạng/evidence: E-DATA. Quyết định/điểm mở: PostgreSQL16/EF10 theo baseline.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-006/A01 | PostgreSQL là DBMS duy nhất, EF Code First migrations tái lập | Fresh/upgrade DB riêng + catalog/assertion | Chưa kiểm runtime |
| CONS-006/A02 | Raw SQL nếu có phải parameterized qua EF, LINQ cho input query | Static scan + SQL capture injection tests | Chưa kiểm runtime |

### CONS-007

- Nguồn: SRS tr. 15. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: file uploads. Phase tiếp nhận: 19.
- Hiện trạng/evidence: E-CONTENT; E-JOB. Quyết định/điểm mở: GAP-022.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-007/A01 | File≤5MB, chỉ image/jpeg/png/webp/avif | IT boundary/từng MIME | Chưa kiểm runtime |
| CONS-007/A02 | Kiểm MIME thực qua signature, không chỉ extension | IT rename/spoof/corrupt payload | Chưa kiểm runtime |

### CONS-008

- Nguồn: SRS tr. 15. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: validation pipeline. Phase tiếp nhận: 04.
- Hiện trạng/evidence: E-ARCH; E-CONTENT. Quyết định/điểm mở: HTTP parse khác nghiệp vụ, không tự miễn endpoint rules.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-008/A01 | Input validation FluentValidation qua MediatR behavior trước handler | Pipeline test invalid→no handler/side effects | Chưa kiểm runtime |
| CONS-008/A02 | Không validation nghiệp vụ tại endpoint handler | Review từng endpoint/query/body/file/ETag + direct Application tests | Chưa kiểm runtime |

### CONS-009

- Nguồn: SRS tr. 15. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: Docker build/Compose. Phase tiếp nhận: 03, 29, 35.
- Hiện trạng/evidence: E-OPS. Quyết định/điểm mở: GAP-025 pin patch/digest, không latest production.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-009/A01 | Backend Docker multi-stage SDK→aspnet runtime | Dockerfile review + image build/inspect | Chưa kiểm runtime |
| CONS-009/A02 | Ứng dụng đóng gói container, Docker Compose local chạy được | Dedicated compose smoke, dependency/network/health evidence | Chưa kiểm runtime |

### CONS-010

- Nguồn: SRS tr. 15. Ưu tiên: Must (SRS §2.5 bắt buộc).
- Đối tượng: Serilog mọi log entry. Phase tiếp nhận: 06, 33.
- Hiện trạng/evidence: E-OBS. Quyết định/điểm mở: Job context phải có reference nguồn, không bịa RequestPath.

| Acceptance ID | Expected có thể kiểm tra | Cách kiểm tra / evidence cần thu | Trạng thái |
|---|---|---|---|
| CONS-010/A01 | Structured Serilog có CorrelationId/RequestPath và UserId khi auth | Captured logs success/error/background, review context propagation | Chưa kiểm runtime |
| CONS-010/A02 | Log không chứa password/token/secret hoặc raw sensitive payload | Redaction tests cả validation/provider/SMTP exceptions | Chưa kiểm runtime |

## Hợp đồng UI và giao diện ngoài — Chương 5

Mỗi dòng CT là một contract cần kiểm thêm, không thêm vào tổng 74 mã. Ưu tiên **Must theo baseline**, trừ màn Should đã ghi ở FR. Hiện trạng chung: **đã định vị source, chưa kiểm runtime**. Cột evidence là điểm vào, cách kiểm là bài cần thu; không phải kết quả chạy. §5.1–5.4 đã được review, kể cả giao tiếp mạng/phần cứng. Quyết định chưa rõ dẫn tới D0-02/03/04 của Phase 02.

| ID | Trang/route hoặc giao diện | Expected / auth / redirect | Cách kiểm; hiện trạng source/evidence; owner |
|---|---|---|---|
| CT-UI-01 | 46 `/` | ISR3600; public; featured Published recipes + categories, không bắt buộc sidebar | Production HTTP cache/TTL + browser home; E-WEB rewrite về render-public/home/index; phase12/27 |
| CT-UI-02 | 46 `/recipes` | SSR dynamic; public; filter/sort/search/page từ query | Production HTTP hai bộ query + browser URL navigation; E-WEB; phase11/18 |
| CT-UI-03 | 46 `/recipes/[slug]` | ISR300; public Published; ingredients/steps/nutrition/JSON-LD; missing404 | HTTP TTL/status/schema + browser; E-WEB; phase11/12/21 |
| CT-UI-04 | 46 `/categories` | ISR3600; public danh sách category | HTTP MISS/HIT/TTL + browser; E-WEB rewrite render-public/categories/index; phase12 |
| CT-UI-05 | 46 `/categories/[slug]` | ISR600; public Published list; missing404 | HTTP missing/trailing segments/TTL + browser; E-WEB; phase11/12 |
| CT-UI-06 | 46 `/auth/login` | CSR; public; nếu đã login redirect phù hợp; local+Google | HTTP hiện cấu hình temporary→`/login` (status thực chưa đo); GAP-005, alias cần Phase02/11 chốt; E-WEB |
| CT-UI-07 | 46 `/auth/register` | CSR; public; auto-login sau register | HTTP hiện cấu hình temporary→`/register`, đo Location/status/query; alias cần Phase02/11; E-WEB |
| CT-UI-08 | 46 `/dashboard` | CSR Author/Admin, dữ liệu cá nhân | Browser direct URL Guest/Author/Admin; E-WEB; phase11/25 |
| CT-UI-09 | 46 `/dashboard/recipes` | CSR authenticated; owner list và mode Admin theo quyền | Browser auth/private no-store, không cross-user cache; E-WEB/E-CONTENT; phase11/26 |
| CT-UI-10 | 46 `/dashboard/recipes/new` | CSR Author/Admin; wizard nhiều bước có save/validation/preview | Browser tạo Draft/compose/publish; E-WEB; phase26 |
| CT-UI-11 | 46 `/dashboard/recipes/[id]/edit` | CSR Owner/Admin; stale conflict reload/discard | Browser user khác và hai writer; E-WEB/E-CONTENT; phase11/26 |
| CT-UI-12 | 46 `/dashboard/categories` | CSR chỉ Admin | Browser direct URL/disabled control không thay server authorization; E-WEB; phase11/27 |
| CT-UI-13 | 47 `/profile` | CSR authenticated; view/update safe profile | Browser auth/clear avatar/CSP/error; E-AUTH/E-WEB; phase07/11 |
| CT-UI-14 | 47 `/search` | SSR public search; alias giữ q/filter/page/canonical | Source permanent→`/recipes`; HTTP status thực chưa đo, 308 cấu hình Next không tự chứng nhận 301 của slug; Phase02/11 chốt alias; E-WEB |
| CT-UI-15 | 46–47 mọi page | loading/empty/error/success/permission-denied, không soft404, auth redirect không loop | Browser slow/error/unknown URL/internal render-public route, CT-UI-01–14; E-WEB; phase11/27 |
| CT-HTTP-01 | 47 §5.2 network/base | HTTP1.1/2 qua HTTPS TLS≥1.2; dev localhost:5000/api/v1; prod api.culinaryblog.com/api/v1 theo deployment config | Staging protocol/TLS/proxy check; E-OPS; phase32; hostname ví dụ không tự deploy |
| CT-HTTP-02 | 47 content negotiation | JSON camelCase application/json;charset=utf-8; multipart/form-data upload; error application/problem+json | IT raw headers/body/malformed/unsupported media; E-OPENAPI/E-OBS; phase22/24 |
| CT-HTTP-03 | 47 auth transport | Authorization Bearer AT; refreshToken body; không tự đổi refresh sang cookie | IT/browser network redacted; E-AUTH; GAP-007; phase08/09 |
| CT-HTTP-04 | 47; 61 envelopes | Single `{data:resource}`; list `{data:[],meta}`; category `{data:{category,recipes},meta}` | IT schema mọi endpoint; GAP-021; E-CONTENT/E-OPENAPI; phase22/24 |
| CT-HTTP-05 | 47 versioning | URL /api/v1; breaking change qua /api/v2 và duy trì v1≥6 tháng khi phát sinh | Contract/version support review; E-OPENAPI; phase22/37; chưa có v2, không tự thêm |
| CT-HTTP-06 | 47 CORS | Configured origins, methods GET/POST/PUT/PATCH/DELETE/OPTIONS; headers Content-Type/Authorization/X-Correlation-ID, thêm If-Match cho concurrency | HTTP OPTIONS allowed/denied origin, expose ETag/Retry-After/correlation; E-OBS; phase16/32 |
| CT-HTTP-07 | 47 quota headers | X-RateLimit-Limit,Remaining,Reset Unix timestamp; Retry-After giây khi429 | HTTP boundary/reset/CORS exposed; source chưa có đủ rate headers; E-OBS; phase16 |
| CT-HTTP-08 | 47 correlation | Nhận hoặc sinh X-Correlation-ID; response trả lại, mọi log có context | IT header→middleware→handler→dependency/log; E-OBS; phase06/33 |
| CT-HTTP-09 | 30–31; 54 concurrency | ETag quoted aggregate Version bigint; mutation gửi If-Match; missing400/stale409; child trả meta.recipeVersion | IT response headers/version/race, DELETE no-body; GAP-003/015; E-CONTENT; phase17/22 |
| CT-EXT-01 | 47–48 Google | Code+PKCE scopes openid/email/profile; credential User Secrets/env; callback do Auth.js sở hữu theo GAP-005 | Config review + provider/browser evidence riêng; E-AUTH; phase10; không đọc/in credential |
| CT-EXT-02 | 48 MinIO | AWSSDK.S3 endpoint override, bucket culinary-blog; private theo GAP-022; direct presigned upload optional | Real MinIO adapter/URL auth test; E-JOB; phase19 |
| CT-EXT-03 | 48 Hangfire | Core/AspNetCore/PostgreSql in-process; schema hangfire; /hangfire Admin-only | Queue/restart/concurrent workers + dashboard permissions; E-JOB; phase19/20/35 |
| CT-EXT-04 | 48 Serilog/Seq | Console JSON/file rolling/Seq dev HTTP ingest; production sink theo cấu hình | Runtime log fields/sink outage/redaction; E-OBS; phase06/33 |
| CT-EXT-05 | 48 OTEL | SDK instrument HttpClient/EF/AspNetCore; OTLP endpoint env; dev Seq/prod Tempo/Jaeger | Collector/export trace verification; E-OBS; phase33 |
| CT-EXT-06 | 48 SMTP | IEmailSender/MailKit TLS, env secrets; dev MailHog | Dedicated MailHog send/failure/retry, TLS staging khi cần; E-JOB; phase20/32 |
| CT-EXT-07 | 48 Google ping | GET google.com/ping?sitemap={url} nguyên văn; chưa thực hiện, không tự coi đã miễn | Phase02 D0-03 quyết định và nguồn chính thức trước khi phase20/21 thực thi; E-JOB |
| CT-HW-01 | 48–49 phần cứng | Dev2 cores/8GB/20GB SSD; prod2vCPU/4GB/50GB SSD/1Gbps/IP tĩnh; so khác20GB tại§2.4 | Record environment thực và sizing decision D0-04; E-OPS; phase30/35 |
| CT-HW-02 | 14; 49 browser | Chrome112+/Firefox113+/Edge112+/Safari16+; mobile iOS14+/AndroidChrome90+ khi platform cao hơn; không IE | GAP-024; browser/device matrix thật, không dùng latest thay minimum; phase28 |

## Hợp đồng kiến trúc/triển khai — Chương 6

Ưu tiên Must baseline trừ optional ghi rõ. Tất cả dòng chưa kiểm runtime; source là điểm vào. Không tự thêm abstraction hoặc nâng Next.js để đóng inventory.

| ID | Trang | Expected có thể kiểm tra | Cách kiểm; evidence; owner/decision |
|---|---|---|---|
| CT-ARCH-01 | 50 §6.1 | Browser→Next15 App Router TS/Tailwind/Auth.js5/TanStack/RHF+Zod→REST API | Manifest + request path review/browser; E-WEB; GAP-025, phase03/27 |
| CT-ARCH-02 | 50 | Nginx SSL/LB/static cache→frontend3000/API5000; API→PG16/Redis7/MinIO/Email | Config/network smoke riêng; E-OPS; phase03/32/35 |
| CT-ARCH-03 | 50–51 §6.2 | Dependency hướng Domain, không Domain/Application→Infrastructure | Architecture suite + csproj graph; E-ARCH; phase04/29 |
| CT-ARCH-04 | 51 Domain | Entity/enum/value objects/domain invariant ở Domain; không NuGet ngoài BCL; domain events optional | Domain/source review và UT; GAP-026; E-ARCH; phase04/05 |
| CT-ARCH-05 | 51 Application | Mỗi use case handler, DTO/validator/behavior/service interface đặt đúng tầng | Route→handler→service inventory, test direct Application; E-CONTENT/E-AUTH; phase04/06 |
| CT-ARCH-06 | 51 Infrastructure | EF/migration/repository/JWT/storage/email/cache/Hangfire/audit interceptor implements interfaces | Implementation/dependency review và IT adapters; E-DATA/E-JOB; phase05/19/22 |
| CT-ARCH-07 | 51 Presentation | Minimal API groups/middleware/DI/auth/OpenAPI; Scalar /scalar và XML comments | OpenAPI/HTTP docs test; E-OBS/E-OPENAPI; phase22/37 |
| CT-ARCH-08 | 52 §6.3 | Logging→Validation→Caching→Handler→post-success Invalidation; performance warning>500ms | Pipeline captured order; hiện DI thêm Performance giữa validation/cache; E-ARCH; phase04/06 |
| CT-ARCH-09 | 52 | Validation fail không handler/write/job/cache invalidation | Pipeline tests với DI thật và doubles có nhãn; E-CONTENT; phase04/06 |
| CT-ARCH-10 | 52 | Cacheable queries hit bỏ handler, miss xử lý; commands invalidation chỉ sau commit thành công | Pipeline success/error/disconnect/cache down; E-CACHE; phase06/15 |
| CT-ARCH-11 | 52 §6.4 | Recipe1:N Steps/Ingredients/Images,1:1 owned Nutrition,N:1 Category/Author; User1:N RefreshTokens | EF catalog/FK/runtime query; GAP-028/032; E-DATA; phase22; INV-G08 |
| CT-ARCH-12 | 52–53 §6.5 | Compose tách dev/prod configs; backend multi-stage SDK→aspnet, frontend standalone | Docker build/compose smoke; filenames hiện compose.yaml/compose.dev.yaml, không đổi tên chỉ để khớp ví dụ; E-OPS; phase03/29 |
| CT-ARCH-13 | 53 | Nginx80/443, API host5000→8080, frontend3000; PG5432/Redis6379/MinIO9000+9001 trong local topology | Dedicated compose inspect/ports; production không suy bắt buộc public DB ports; E-OPS; phase03/32 |
| CT-ARCH-14 | 53 | PG data/Redis data AOF/MinIO persistent volumes; Seq5341, MailHog1025/8025 dev-only | Volume/restart/config inspect, secrets env không output; E-OPS; phase34/35 |
| CT-ARCH-15 | 13–16; 53 | Pin .NET10/Next15/Node22.23.3/PG16/Redis7 và production image digest, không latest | Lockfile/runtime/image inspect; GAP-025/ADR-010; E-OPS; phase03/29 |
| CT-ARCH-16 | 15–16 §2.6 | Seed50 recipes/5 authors bằng Bogus; initial≤10000 recipes/5000 users/50 categories | Dedicated seed counts/idempotent test; E-DATA; phase22; manual seed khác Bogus thuộc D6-14, không ghi đè user data |
| CT-ARCH-17 | 15–16 dependencies | Google unavailable còn local login; Redis down đọc DB; MinIO local FS dev-only; SMTP fail không rollback registration; PG required | Isolated failure matrix; E-AUTH/E-CACHE/E-JOB; phase10/15/19/20/33 |

## Hợp đồng dữ liệu — Chương 7, §6.4 và baseline

Các cột `?` là **nullable**, khác field JSON có thể omit. DB NOT NULL không có nghĩa client phải truyền PK/default. Expected dưới đây giữ cả nguyên văn và điều chỉnh baseline; chưa được phép chọn ngầm giữa hai phiên bản. Ưu tiên Must; hiện trạng source review/chưa kiểm DB runtime cho toàn bộ bảng. Mỗi constraint cần kiểm migration/catalog **và** insert/update boundary bằng DB riêng; OpenAPI/type cũng phải khớp. E-DATA/E-OPENAPI là evidence đầu vào chung, owner phase22; concurrency/nghiệp vụ còn phase17/19.

| ID | Trang/field | Expected type / length / null / default / constraint | Kiểm tra cụ thể; hiện trạng/decision |
|---|---|---|---|
| CT-DATA-BASE-01 | 54 Base.Id | uuid PK UUIDv4, PDF DEFAULT gen_random_uuid(); mỗi entity bảng riêng, không bảng BaseEntity/TPH | Catalog default + generated UUID; server Guid thay DB default phải ghi khác biệt khi phase22 review |
| CT-DATA-BASE-02 | 54 CreatedAt | timestamptz NOT NULL DEFAULT NOW(), AuditInterceptor insert UTC | Insert không timestamp; so schema/interceptor/clock |
| CT-DATA-BASE-03 | 54 UpdatedAt | timestamptz nullable; interceptor set khi update | Insert rồi mutation clock; kiểm timezone serialization |
| CT-DATA-BASE-04 | 54 IsDeleted | boolean NOT NULL false; global query filter !IsDeleted | Raw DB so API reads cho từng entity/child |
| CT-DATA-BASE-05 | 54 RowVersion | PDF bytea timestamp NOT NULL; baseline thay Version bigint default1 concurrency token | GAP-015; ETag/If-Match stale race; child tăng aggregate Version |
| CT-DATA-BASE-06 | baseline audit | CreatedBy/UpdatedBy/DeletedBy nullable string, DeletedAt timestamptz?; actor/system đúng | Audit insert/update/delete/job; không tự thêm field này vào lời trích PDF |
| CT-DATA-RCP-01 | 54 Title | varchar200 NOT NULL; API5–200; title không unique; trigram title index optional | Boundary4/5/200/201 + duplicate title; source config200 |
| CT-DATA-RCP-02 | 54–55 Slug | varchar220 NOT NULL unique B-tree, lowercase/no-accent/hyphen; suffix collision; bất biến sau publish | GAP-031; catalog + concurrent collision + rename |
| CT-DATA-RCP-03 | 55 Description | text NOT NULL ≤2000; baseline/API1–2000 | DB/validator boundary; source HasMaxLength2000 cần so kiểu text/varchar khi phase22 |
| CT-DATA-RCP-04 | 55 Instructions | PDF text NOT NULL legacy markdown; dictionary/DTO nullable ≤10000 | Source nullable/string max10000; **INV-G09**, chờ Phase02/22; test null/omit/empty/10001 |
| CT-DATA-RCP-05 | 55 PrepTime | integer NOT NULL CHECK>0 | 0/1 boundary tại DB/API |
| CT-DATA-RCP-06 | 55 CookTime | integer NOT NULL CHECK≥0 | GAP-019; -1/0 boundary, maxCookTime0 URL |
| CT-DATA-RCP-07 | 55 Servings | integer NOT NULL CHECK>0 | 0/1 boundary |
| CT-DATA-RCP-08 | 55 Difficulty | PDF smallint default1; values1Easy/2Medium/3Hard/4Expert; JSON easy/medium/hard/expert | Source HasConversion<int>() thay smallint, default cần catalog; **INV-G09** |
| CT-DATA-RCP-09 | 55 Status | PDF smallint default0;0Draft/1Published/2Archived; JSON draft/published/archived | Source HasConversion<int>(); **INV-G09**; invalid enum API/DB |
| CT-DATA-RCP-10 | 55 CategoryId | uuid NOT NULL FK Categories.Id ON DELETE RESTRICT + B-tree | Real FK/delete constraint + soft-delete business check |
| CT-DATA-RCP-11 | 55 AuthorId | PDF/baseline varchar450 NOT NULL FK AspNetUsers.Id+B-tree | Source Recipe.AuthorId Guid/IdentityUser<Guid>, **INV-G08**; không tự đổi FK/migrate |
| CT-DATA-RCP-12 | 56 SearchVector | tsvector nullable; GIN, trigger title/description, unaccent; simple config theo GAP-016 | Catalog trigger/extensions + update/search + weighted rank; SearchTitle trigram extra ghi riêng |
| CT-DATA-RCP-13 | 56 PublishedAt | timestamptz nullable+B-tree; null trước publish, baseline giữ ngày publish đầu | Clock lifecycle/readback; không thay ngày khi republish |
| CT-DATA-RCP-14 | 55–56 indexes | UniqueSlug, B-treeDifficulty/Status/CategoryId/AuthorId/PublishedAt; partialIsDeleted; NFR WHERE/ORDER indexes | Source có composite status/filter indexes; standalone Difficulty/partialIsDeleted cần catalog/plan review, không suy tương đương |
| CT-DATA-NUT-01 | 56 Nutrition ownership | 1:1 owned cột trong Recipes, không bảng riêng, object nullable baseline | Catalog owned mapping + null nutrition request/detail; navigation IsRequired hiện tại cần verify semantics |
| CT-DATA-NUT-02 | 56 Calories | Nutrition_Calories decimal(8,2)? kcal/serving, không âm baseline | Boundary precision/scale/null/negative DB/API |
| CT-DATA-NUT-03 | 56 Protein | Nutrition_Protein decimal(8,2)? gram/serving, không âm | DB/API boundary + JSON-LD mapping |
| CT-DATA-NUT-04 | 56 Carbohydrates | Nutrition_Carbohydrates decimal(8,2)? gram/serving, không âm | Không nhầm carbs field cũ; DB/API mapping |
| CT-DATA-NUT-05 | 56 Fat | Nutrition_Fat decimal(8,2)? gram/serving, không âm | DB/API boundary |
| CT-DATA-NUT-06 | 56 Fiber | Nutrition_Fiber decimal(8,2)? gram/serving, không âm | DB/API boundary |
| CT-DATA-NUT-07 | 56 Sodium | Nutrition_Sodium decimal(8,2)? mg/serving, không âm | DB/API boundary + unit JSON-LD |
| CT-DATA-STEP-01 | 57 Id/RecipeId | uuid PK, RecipeId uuid NOT NULL FK CASCADE | Catalog relation; physical cascade chỉ kiểm DB riêng, soft-delete theo baseline không purge |
| CT-DATA-STEP-02 | 57 StepNumber | integer NOT NULL>0; unique(RecipeId,StepNumber), liên tục1..N | Config partial unique active; delete/reorder/concurrency, GAP-017 |
| CT-DATA-STEP-03 | 57 Title | varchar200 NOT NULL; API1–200 | UT/IT boundary + schema |
| CT-DATA-STEP-04 | 57 Description | text NOT NULL; FR max2000 | API/DB boundary; source HasMaxLength2000 cần review storage type |
| CT-DATA-STEP-05 | 57 TimerMinutes | integer nullable CHECK≥0 | GAP-017 thay DurationMinutes; null/0/-1 |
| CT-DATA-STEP-06 | 57 ImageUrl | varchar500 nullable safe URL | null/http(s)/unsafe scheme/length boundaries |
| CT-DATA-ING-01 | 57 Id/RecipeId | uuid PK/FK NOT NULL Recipe CASCADE | FK/parent isolation; soft-delete filter |
| CT-DATA-ING-02 | 57 Name | varchar200 NOT NULL;1–200 theo GAP-030 | Boundary1/200/201, reject blank |
| CT-DATA-ING-03 | 57 Quantity | decimal(10,3) nullable, >0 nếu có | null/0/negative/precision-scale/max, không suy null→0 |
| CT-DATA-ING-04 | 57 Unit | varchar50 nullable, trim, null hợp lệ theo Chương8 | Boundary50/51, null/omit, so khác FR bắt buộc |
| CT-DATA-ING-05 | 57 Notes | varchar500 nullable, render escaped | Boundary500/501/null + XSS |
| CT-DATA-ING-06 | 57 OrderIndex | integer NOT NULL default0, baseline≥0; sort theo field này | GAP-018; negative/default/reorder, nonunique allowed |
| CT-DATA-IMAGE-01 | 58 Id/RecipeId | uuid PK/FK NOT NULL Recipe CASCADE; RecipeImages riêng1:N | GAP-028; catalog/FK, child cross-parent404 |
| CT-DATA-IMAGE-02 | 58 OriginalUrl | PDF varchar500 NOT NULL; baseline lưu ObjectKey varchar500 NOT NULL, delivery URL projection có thể nullable | GAP-022; source RecipeImageDto OriginalUrl nonnullable/OpenAPI nullable: **INV-G10** |
| CT-DATA-IMAGE-03 | 58 MediumUrl | varchar500 nullable khi pending; medium800×600 | Source MediumObjectKey→delivery URL; real storage decode + pending/fail fallback |
| CT-DATA-IMAGE-04 | 58 ThumbnailUrl | varchar500 nullable khi pending; thumbnail300×300 | Source ThumbnailObjectKey→URL; decode + pending/fail |
| CT-DATA-IMAGE-05 | 58 AltText | varchar200 nullable, không tự biến thành publish invariant | Boundary/null/accessibility review; dictionary nói nên có |
| CT-DATA-IMAGE-06 | 58 IsPrimary | boolean NOT NULL defaultfalse, tối đa1 active/recipe; first upload tự primary | Partial unique index + concurrency/delete primary |
| CT-DATA-IMAGE-07 | 58 OrderIndex | integer NOT NULL default0,≥0 baseline | Gallery order/default/negative |
| CT-DATA-IMAGE-08 | baseline processing | processingStatus pending/ready/failed; internal MIME/contentType và keys không client chọn | OpenAPI↔DTO↔job state/runtime media access; extension của baseline |
| CT-DATA-CAT-01 | 58 Id/Name | uuid PK; Name varchar100 NOT NULL unique normalized,2–100 | GAP-019; unique cả soft-deleted; source NormalizedName unique |
| CT-DATA-CAT-02 | 58 Slug | varchar120 NOT NULL unique+B-tree; stable rename/nonreuse | Collision/default/generation/catalog |
| CT-DATA-CAT-03 | 58 Description | text nullable, baseline≤2000 | Null/omit/2000/2001 + so source varchar length |
| CT-DATA-CAT-04 | 58 ImageUrl | varchar500 nullable safe URL | Boundary/unsafe schemes/null |
| CT-DATA-CAT-05 | 58 OrderIndex | integer NOT NULL default0, baseline≥0; navigation order khác list NameASC | DB/default + public vs admin sorting |
| CT-DATA-USER-01 | 58–59 Id/Identity | PDF IdentityUser<string>,Id varchar450; inherited username/normalized/email/hash/stamp/phone/2FA/lockout fields | GAP-032 cũng chọn string nhưng source IdentityUser<Guid>; **INV-G08**, auth DTO string không chứng minh storage string |
| CT-DATA-USER-02 | 59 DisplayName | varchar100 NOT NULL,2–100 trim, không public username | GAP-004/020; boundary and Identity username=normalized email behavior |
| CT-DATA-USER-03 | 59 AvatarUrl | varchar500 nullable, safe URL; Google profile nguồn avatar | Profile PATCH omit/null/clear + browser CSP |
| CT-DATA-USER-04 | 59 Bio | text nullable; baseline≤2000 | Storage type/source max2000 review + boundary |
| CT-DATA-USER-05 | 59 IsActive | boolean NOT NULL defaulttrue | Deactivated user không refresh/mutate; INV-G04 status |
| CT-DATA-USER-06 | 59 CreatedAt | timestamptz NOT NULL defaultNOW() | Insert/default/UTC serialization; không coi C# initializer là DB default |
| CT-DATA-USER-07 | baseline email/roles | NormalizedEmail unique; roles Author/Admin từ Identity; không expose hash/stamp | Concurrent normalized email duplicate, roles join/safe response |
| CT-DATA-RT-01 | 59 Id/UserId | uuid PK; UserId PDF varchar450 NOT NULL FK User CASCADE | Source Guid FK, **INV-G08**; migration retention |
| CT-DATA-RT-02 | 59 TokenHash | varchar64 NOT NULL UNIQUE+index, SHA256 raw512-bit theo GAP-006 | DB no raw token; dictionary char64 so config max64 cần phase22 review |
| CT-DATA-RT-03 | 59 ExpiresAt/CreatedAt | timestamptz NOT NULL; created defaultNOW(); TTL7 ngày | Clock boundary/restart/calendar timestamp |
| CT-DATA-RT-04 | 59 RevokedAt/ReplacedByTokenHash | timestamptz? và varchar64?; null active; link rotation | Atomic rotation/reuse family, no dangling active descendants |
| CT-DATA-RT-05 | 60 CreatedByIp | PDF varchar45 nullable; source HasMaxLength64 | **INV-G11**, DB/API IPv4/IPv6; quyết định Phase02/22 |
| CT-DATA-RT-06 | baseline FamilyId/UA | FamilyId uuid NOT NULL; dictionary UserAgentHash varchar64?; source UserAgent raw512, RevokedByIp64 | GAP-006; **INV-G11** về retention/privacy; không in UA/token trong report |
| CT-DATA-EXTRA-01 | baseline outbox | Outbox/event id,occurred/type/payload jsonb/processed/attempt/next/error; không secret; SMTP/media dispatch bền | GAP-023/ADR-009; source WelcomeEmailOutbox/MediaOutbox configs; actual schema khác generic table cần phase22 map rõ |
| CT-DATA-EXTRA-02 | current migration | WelcomeEmailHangfire thêm JobId nullable, dữ liệu cũ giữ được, seed/restart idempotent | MigrationUpgradeTests + DB upgrade riêng; không chạy migration lên user stack; phase22 |

## Hợp đồng request/response — Chương 8

Ưu tiên Must trừ FR Should; **tất cả chưa kiểm HTTP runtime**. E-OPENAPI là contract đang dùng, E-AUTH/E-CONTENT/E-OBS là source đã định vị. Đây là inventory test, không phải endpoint-matrix đã chạy của D8-01 (phase24).

`?` trong request nghĩa optional; nullable chỉ khi CT-DATA/schema nói cho phép null. Common success resource=`{data:T}`, list=`{data:T[],meta:PageMeta}`; 204 luôn không body. Common errors: malformed/validation400, thiếu bearer401, sai role/owner403, missing404, conflict409, quota429, unhandled500 và dependency503 khi không fallback; **phải assert code và headers theo case**, không bắt mọi endpoint trả đủ mọi status. Những chỗ SRS khác baseline/source có decision bên dưới.

| ID | Trang/method path | Auth; request/query; success schema/status | Lỗi/headers và phép kiểm; owner/decision |
|---|---|---|---|
| CT-API-01 | 61 POST /auth/register | Anonymous; displayName,email,password required;201 AuthSession(user,AT,RT,expiresAt) | 400 field/409 email; Location /auth/me hiện tại; IT schema+DB+auto-login; GAP-004/021; phase07/22 |
| CT-API-02 | 61 POST /auth/login | Anonymous; email,password required;200 AuthSession | 400/401 generic/423 locked/429; no-store; IT auth matrix; phase07 |
| CT-API-03 | 61 POST /auth/google | Anonymous; idToken required;200 AuthSession | 400 invalid/unverified,409 link conflict; FR401/502 so source503 chưa đồng bộ OpenAPI; INV-G04; phase02/10/22 |
| CT-API-04 | 61–62 POST /auth/refresh | Anonymous; refreshToken required;200 rotated AuthSession | 400 body,401 invalid/expiry/reuse; inactive403 theo catalog chờ INV-G04; no-store; phase08 |
| CT-API-05 | 62 POST /auth/logout | Body RT; bearer optional theo GAP-007;204 | Owner match nếu valid bearer, expired AT được revoke; mismatch status chờ Phase02/09; missing body400; phase09 |
| CT-API-06 | 62 GET /auth/me | Bearer active; no body;200 UserDto | 401 token/404 deleted user; no-store; safe profile fields; phase07 |
| CT-API-07 | 62 PATCH /auth/me | Bearer; displayName?,avatarUrl?,bio?;≥1 field baseline;200 UserDto | 400 validation,401; omit giữ cũ/null clear nullable; no-store; phase07 |
| CT-API-08 | 62 GET /categories | Anonymous; no body;200 data Category[] | Empty data[]; NameASC/count Published; CT-HTTP cache; phase17 |
| CT-API-09 | 62 GET /categories/{slug} | Anonymous; page?,pageSize?, PDF sortBy?;200 data{category,recipes},meta | 404 missing,400 pagination; source không nhận sortBy; INV-G03; phase18/22 |
| CT-API-10 | 62 POST /categories | Admin; name required,description?,imageUrl?,orderIndex? baseline;201 Category | 400/401/403/409; Location /categories/{slug}; phase17 |
| CT-API-11 | 63 PUT /categories/{id} | Admin; name required,description?,imageUrl?,orderIndex?;200 Category | 400/401/403/404/409; stable slug, rollback; phase17 |
| CT-API-12 | 63 DELETE /categories/{id} | Admin; no body;204 soft-delete | 401/403/404/409 has recipes; count detail; phase17 |
| CT-API-13 | 63 GET /recipes | Anonymous; CT-QUERY;200 RecipePage | Invalid400; nonexisting category200 empty; public only; phase18 |
| CT-API-14 | 63 GET /recipes/{slug} | Anonymous;200 RecipeDetail gồm sorted arrays/nutrition/category/author | 404 public private/missing theo GAP-014, ETag; cache tags; phase17/22 |
| CT-API-15 | 63 GET /recipes/search | Anonymous; q required2–100, CT-QUERY;200 RecipePage+nullable relevanceScore | 400 empty/short/sort;200 empty; SQL-safe; phase18 |
| CT-API-16 | 63 POST /recipes | Author/Admin; title,description,categoryId,prepTime,cookTime,servings,difficulty required;instructions?,nutrition?,steps?,ingredients? | 201 Draft;400/401/403/409; OpenAPI có arrays nhưng DTO không nhận, INV-G02; phase17/22 |
| CT-API-17 | 63 PUT /recipes/{id} | Owner/Admin; PDF basic fields optional, runtime RecipeWriteRequest required như create;200 RecipeDetail | If-Match required400/stale409;401/403/404; INV-G01; phase17/22 |
| CT-API-18 | 63–64 PATCH /recipes/{id}/publish | Owner/Admin; no body;200 RecipeDetail | If-Match;400 incomplete/401/403/404/409; ingredient+step baseline; phase17 |
| CT-API-19 | 64 PATCH /recipes/{id}/unpublish | Owner/Admin; no body;200 RecipeDetail Draft | If-Match;401/403/404/409; invalidates public; phase17 |
| CT-API-20 | 64 PATCH /recipes/{id}/archive | Owner/Admin; no body;200 RecipeDetail Archived | If-Match;401/403/404/409 transition/version; phase17 |
| CT-API-21 | 64 DELETE /recipes/{id} | Owner/Admin; no body;204 soft-delete | If-Match;400/401/403/404/409; GAP-008 giữ child/object; phase17 |
| CT-API-22 | 64 POST /recipes/{id}/images | Owner/Admin; multipart file required,altText?,isPrimary?;201 ChildMutationEnvelope image | If-Match;400 MIME/size/signature,401/403/404/409/503; upload quota5; phase19 |
| CT-API-23 | 64 PATCH /recipes/{id}/images/{imageId} | Owner/Admin; altText?,isPrimary?,orderIndex?;200 ChildMutationEnvelope image | If-Match;400/401/403/404/409; primary atomic; GAP-012 không /primary riêng; phase19 |
| CT-API-24 | 64 DELETE /recipes/{id}/images/{imageId} | Owner/Admin; no body;204 | If-Match/ETag;401/403/404/409; object-delete outbox; phase19 |
| CT-API-25 | 65 POST /recipes/{id}/steps | Owner/Admin; title,description required,timerMinutes?,imageUrl?;server append number;201 ChildMutationEnvelope step | If-Match;400/401/403/404/409; GAP-017, PDF request stepNumber khác baseline; phase17 |
| CT-API-26 | 65 PUT /recipes/{id}/steps/{stepId} | Owner/Admin; PDF title?,description?,stepNumber?,timerMinutes?,imageUrl?;200 ChildMutationEnvelope step | If-Match;400/401/403/404/409; current StepWrite required title/description, INV-G01; phase17/22 |
| CT-API-27 | 65 DELETE /recipes/{id}/steps/{stepId} | Owner/Admin; no body;204, renumber | If-Match;401/403/404/409 last-step invariant; phase17 |
| CT-API-28 | 65 POST /recipes/{id}/ingredients | Owner/Admin; name required,quantity?,unit?,notes?,orderIndex?;201 ChildMutationEnvelope ingredient | If-Match;400/401/403/404/409; nullable quantity/unit theo Chương7/8; phase17 |
| CT-API-29 | 65 PUT /recipes/{id}/ingredients/{ingredientId} | Owner/Admin; PDF name?,quantity?,unit?,notes?,orderIndex?;200 ChildMutationEnvelope ingredient | If-Match;400/401/403/404/409; current name required, INV-G01; phase17/22 |
| CT-API-30 | 65 DELETE /recipes/{id}/ingredients/{ingredientId} | Owner/Admin; no body;204 | If-Match;401/403/404/409 last-ingredient invariant; phase17 |
| CT-API-31 | 65 GET /health | Anonymous root;200 Healthy hoặc503 Unhealthy | PDF status+entries; source status-only; INV-G05; dependency matrix; phase33 |
| CT-API-32 | 66 GET /health/live | Anonymous root;200 khi process sống | Không check dependency/không quota; response status JSON hiện tại; phase33 |
| CT-API-33 | 66 GET /health/ready | Anonymous root;200 DB+Redis up,503 otherwise | Kiểm traffic routing/health details privacy theo INV-G05; phase33 |

Các supporting/supplemental operations hiện có được giữ để không mất contract thực tế; **không thêm FR**. Chưa kiểm runtime; phải có endpoint matrix riêng ở phase24.

| ID | Nguồn/method path | Contract hiện có cần giữ và kiểm | Cách kiểm/owner |
|---|---|---|---|
| CT-SUP-01 | GAP-011 PATCH /recipes/{id}/unarchive | Owner/Admin+If-Match;200 Draft;400/401/403/404/409 | Transition+privacy IT; E-CONTENT; phase17 |
| CT-SUP-02 | GAP-014 GET /me/recipes | Author/Admin;status?,q?,page?,pageSize?;200 PageEnvelope chỉ của mình;private/no-store | Actor isolation/filter boundaries; phase17/24 |
| CT-SUP-03 | GAP-014 GET /me/recipes/{id} | Owner/Admin private detail;200 resource,401/403/404 | ID substitution/no cache; phase17/24 |
| CT-SUP-04 | SRS§2.3/baseline GET /admin/recipes | Admin;status?,authorId?,q?,page?,pageSize?;200 PageEnvelope mọi trạng thái | Guest/Author denied, Admin filter; phase17/24 |
| CT-SUP-05 | SRS§2.3 GET /admin/recipes/{id} | Admin;200 resource private;401/403/404 | Actor/missing matrix; phase17/24 |
| CT-SUP-06 | OBS/audit GET /admin/audit-logs | Admin;query filters hiện trong OpenAPI;data/meta/telemetry (seqConfigured,otlpConfigured,operationalNetworkRestricted) | Query/schema/redaction/auth tests; CQRS còn phase04, contract phase22/24 |
| CT-SUP-07 | GAP-022 GET /media/{imageId}/{variant} | variant original/medium/thumbnail;public Published,private owner/Admin;binary contentType,404 missing | Real storage/auth/cache state/fallback; phase19/24 |
| CT-SUP-08 | FR-JOB-003 GET /sitemap.xml (API) | XML latest;404 nếu chưa có;web `/sitemap.xml` và `/robots.txt` expose public | Parse generated artifacts/failure retain; phase20/21/24 |
| CT-SUP-09 | NFR-REL-001 GET /operations/maintenance | Anonymous API config read;data enabled/message/announcedAtUtc/startsAtUtc/endsAtUtc, meta | Time boundaries/banner48h; source Program; phase36/24 |
| CT-SUP-10 | Yêu cầu thêm POST /auth/change-password | Bearer;currentPassword,newPassword;204 revoke sessions;400/401 | PasswordLifecycleTests; phase07/24; không nằm34FR |
| CT-SUP-11 | Yêu cầu thêm POST /auth/forgot-password | Anonymous email;202 generic dù không tồn tại;400/503 SMTP | PasswordResetEmailTests; phase07/24 |
| CT-SUP-12 | Yêu cầu thêm POST /auth/reset-password | email,token,newPassword;204;token30phút/bound/single-use;400, revoke sessions | PasswordLifecycleTests; phase07/24 |
| CT-SUP-13 | §5.3 GET /hangfire dashboard | Admin+operational network policy,không public admin impersonation | AdminDashboardAuthorizationFilterTests + HTTP network; phase20/32 |

### Query và schema response chi tiết

| ID | Trang/nguồn | Expected có thể viết assertion | Cách kiểm; hiện trạng/evidence; owner |
|---|---|---|---|
| CT-QUERY-01 | 28; 37; 61 | page integer default1≥1;pageSize default12 trong1..50; số parse sai400; PDF API ví dụ10 còn mâu thuẫn | GAP-021/dictionary giữ12; INV-G03, IT omit/boundary; E-OPENAPI; phase18/22 |
| CT-QUERY-02 | 28; 37; 63 | categoryId UUID?,difficulty enum?,maxCookTime≥0?,minServings>0?,minPrepTime>0?,maxPrepTime>0?; AND; khoảng đảo400 | GAP-029; current thêm category slug,maxTime; IT mỗi input; phase18 |
| CT-QUERY-03 | 37; 61 | sort allowlist hai chiều; browse -createdAt; search relevance; PDF sortBy/sortOrder khác current sort | INV-G03; IT field/alias/default/tie-break; phase02/18/22 |
| CT-QUERY-04 | 36–37 | q trim/normalize2–100, không rỗng;ts_rank relevanceScore number nullable với browse | PostgreSQL FTS/rank/schema + query cache normalization; phase18 |
| CT-SCHEMA-01 | 61–62/baseline AuthSession | data required;accessToken/refreshToken strings response,expiresAt date-time UTC,user safe DTO;không nhầm expiresIn ví dụ PDF | IT response/typegen; OpenAPI hiện writeOnly AT/RT trong response **INV-G12**; phase22 |
| CT-SCHEMA-02 | 59; 62 User | id string serialized,email/displayName required,avatarUrl?/bio? nullable,roles Author/Admin[],emailConfirmed/isActive bool,createdAt UTC | IT safe projection và OpenAPI required fields; source DTO trườngbool hiện có nhưng schema emailConfirmed optional cần phase22 review |
| CT-SCHEMA-03 | 58; 62 Category | id UUID,name,slug,orderIndex,recipeCount required;description/imageUrl nullable;recipeCount≥0 | IT projection/computed count; phase17/22 |
| CT-SCHEMA-04 | 28–29; 63 Recipe | id/title/slug/description/prepTime/cookTime/servings/difficulty/status/category/author/createdAt/version;publishedAt/primaryImageUrl nullable | IT enum lowercase+int64 version; list source dùng RecipeDto chi tiết so OpenAPI summary cần field review, chưa kết luận lỗi |
| CT-SCHEMA-05 | 29; 63 detail | instructions?,nutrition? nullable;ingredients/steps/images arrays required,kể cả rỗng; nutrition six nullable decimals đúng đơn vị | IT all-null/object/decimal bounds, OpenAPI nutrition additionalProperties=true chưa mô tả sáu field **INV-G12**; phase22 |
| CT-SCHEMA-06 | 64–65 child write | ChildMutationEnvelope `{data:child,meta:{recipeVersion:int64≥1}}`, ETag cùng version | IT response/header/body; DELETE204 ETag không JSON body; phase17/19/22 |
| CT-SCHEMA-07 | 61 pagination | data[]+meta.page/pageSize/total/totalPages/hasNextPage/hasPreviousPage;category data.category+recipes[] | IT empty/nonempty/out-of-range/no metadata leak; phase18/22 |
| CT-SCHEMA-08 | 67–69 problem | type URI code ổn định,title,status,detail;field errors dictionary string[];baseline code/correlationId/traceId/instance | IT malformed/auth/domain/429/500 body/header; no stack/token/SQL; phase22/24 |

## Status và application errors — Phụ lục A/B

Nguồn tr.67–69; ưu tiên Must baseline; evidence E-OBS/E-OPENAPI/Error Catalog, **chưa kiểm HTTP runtime**. Với mỗi dòng chạy ít nhất case nêu trong ngữ cảnh, kiểm HTTP + `code` + suffix URI `type`; không dùng message string để branching. Owner chung phase22/24; auth-specific phase07–10, recipe/category phase17/19. 400/409 điều chỉnh phải giữ GAP reference.

| ID | Trang/code | Expected status / ngữ cảnh / phép kiểm |
|---|---|---|
| CT-ERR-01 | 68 AUTH_EMAIL_EXISTS | 409 normalized duplicate register; so DB không thêm user |
| CT-ERR-02 | 68 AUTH_INVALID_CREDENTIALS | 401 nonexistent email và wrong password generic cùng hình dạng |
| CT-ERR-03 | 68 AUTH_TOKEN_EXPIRED | 401 bearer expired, không stack hoặc raw JWT |
| CT-ERR-04 | 68 AUTH_TOKEN_INVALID | 401 malformed/signature/issuer/audience sai |
| CT-ERR-05 | 68 AUTH_REFRESH_TOKEN_EXPIRED | 401 RT quá7 ngày, không cấp token mới |
| CT-ERR-06 | 68 AUTH_REFRESH_TOKEN_REVOKED | 401 RT đã revoke/reuse; family revoke theo GAP-006 |
| CT-ERR-07 | 68 AUTH_GOOGLE_TOKEN_INVALID | 400 theo Chương8/catalog, khác FR401; INV-G04 |
| CT-ERR-08 | 68 AUTH_ACCOUNT_DISABLED | 403 inactive; mâu thuẫn refresh401 của FR cần INV-G04, không tự đổi |
| CT-ERR-09 | 68 RECIPE_NOT_FOUND | 404 missing/soft-deleted/non-Published public slug theo GAP-014 |
| CT-ERR-10 | 68 RECIPE_SLUG_EXISTS | 409 chỉ collision race không resolve; bình thường suffix theo GAP-031 |
| CT-ERR-11 | 68 RECIPE_PUBLISH_INCOMPLETE | 400 thiếu ingredient hoặc step; >=1 cả hai theo GAP-010 |
| CT-ERR-12 | 68 RECIPE_FORBIDDEN | 403 authenticated khác owner không Admin; không lộ private body |
| CT-ERR-13 | 68 RECIPE_CONCURRENCY_CONFLICT | PDF422 thay bằng409 theo GAP-003, client reload |
| CT-ERR-14 | 68 CATEGORY_NOT_FOUND | 404 slug/id thiếu/deleted |
| CT-ERR-15 | 68 CATEGORY_NAME_EXISTS | 409 normalized duplicate name |
| CT-ERR-16 | 69 CATEGORY_DELETE_HAS_RECIPES | 409 còn recipe active; detail count chính xác |
| CT-ERR-17 | 69 FILE_SIZE_EXCEEDED | 400 file>5MB trước buffering, không object/metadata |
| CT-ERR-18 | 69 FILE_MIME_INVALID | 400 ngoài allowlist; spoof signature code riêng baseline FILE_SIGNATURE_INVALID |
| CT-ERR-19 | 69 VALIDATION_ERROR | 400 và errors theo field, không side effects; GAP-002 thay422 |
| CT-ERR-20 | 69 RATE_LIMIT_EXCEEDED | 429+Retry-After giây và rate headers §5.2 |
| CT-ERR-21 | 67 200/201/204 | Success GET/update200/create201 có resource; DELETE/logout204 không body; Location khi FR yêu cầu |
| CT-ERR-22 | 67 400/401/403/404/409 | Malformed400/unauth401/no permission403/missing404/conflict409; catalog URI và body chính xác từng cause |
| CT-ERR-23 | 67 422 | Không dùng validation/concurrency422 trong baseline GAP-002/003; không xóa dấu vết nguyên văn PDF |
| CT-ERR-24 | 67 500/503 | 500 unhandled log+generic Problem Details;503 health/storage required down; Redis read có DB fallback không503 |
| CT-ERR-25 | baseline errors bổ sung | MALFORMED_REQUEST400/PRECONDITION_REQUIRED400/AUTH_ACCOUNT_LOCKED423/DEPENDENCY_UNAVAILABLE503; catalog còn child/state/sort codes phải dùng trong endpoint matrix |

## Thuật ngữ ảnh hưởng acceptance — Phụ lục C

Trang69–71 là từ điển, không tạo tính năng mới. Các dòng sau là kiểm nghĩa dùng xuyên inventory (Must baseline, chưa kiểm runtime), evidence baseline/Domain/DTO; owner phase02/22/37.

| ID | Trang/thuật ngữ | Nghĩa phải giữ / cách đối chiếu |
|---|---|---|
| CT-GLOSS-01 | 69 Access/Refresh Token | AT JWT15phút HS256 khác RT7ngày; kiểm claims/rotation trong FR-AUTH-004, không nhầm provider Google token |
| CT-GLOSS-02 | 69 Archive/Author | Archived chỉ ẩn không xóa; Author role mặc định; kiểm status/role enum không coi Guest persisted role |
| CT-GLOSS-03 | 69–70 CQRS/Clean Architecture | Read/write tách, dependency vào Domain; đối chiếu CONS-001/002 và CT-ARCH |
| CT-GLOSS-04 | 70 ISR/FTS | ISR tái tạo route theo chu kỳ khác Data Cache; FTS vector/query+unaccent khác substring; kiểm HTTP/PG thật |
| CT-GLOSS-05 | 70 optimistic concurrency | Version/If-Match phát hiện lost update; không phải chỉ timestamp hiển thị; GAP-015 và race test |
| CT-GLOSS-06 | 70–71 rotation/reuse | Dùng RT xong revoke/cấp mới; reuse revoke toàn family, không chỉ token bị reuse |
| CT-GLOSS-07 | 71 slug/soft-delete/UoW | Slug unique thân thiện URL; IsDeleted giữ dữ liệu; UoW transaction nhiều thao tác cùng commit/rollback |

## Sai khác và câu hỏi mới bàn giao, không tự chấp thuận

Đây là **source review ngày23/10/2026**, không phải assertion lỗi runtime đã tái hiện. Mỗi ID có expected/source, tác động, phase quyết định và phase thực thi; Phase01 chỉ ghi nhận. Không đổi GAP-001–033. D0-02/03/04 của Phase02 là decision reference cho các điểm chưa rõ; kết quả phải được gắn lại vào các acceptance ID liên quan trước khi viết test chốt contract.

| ID | Acceptance/nguồn và quan sát source hiện tại | Tác động / hướng bàn giao | Phase owner |
|---|---|---|---|
| INV-G01 | CT-API-17/26/29; tr.63/65 PUT recipe/step/ingredient field optional; ContentContracts.cs RecipeWriteRequest/StepWriteRequest/IngredientWriteRequest yêu cầu title/description/name; OpenAPI UpdateRecipeRequest allOf create | Chốt replacement hay partial update, omit vs null, schema/DTO/form/client tương ứng. Test PUT một field, không âm thầm xóa fields khác. Chưa có GAP cụ thể cho semantic này | 02 D0-02;17 D6-02/03;22 D6-13/15 |
| INV-G02 | FR-RCP-003/A06,CT-API-16; tr.29–30 cho tạo composition cùng request; OpenAPI CreateRecipeRequest có steps/ingredients, RecipeWriteRequest và create service không nhận arrays | Caller có thể tin đã lưu composition nhưng field bị bỏ qua. Cần giữ acceptance nested create/atomic rollback hoặc decision thay đổi được chấp thuận; không coi wizard save riêng là đã đủ API contract | 02 D0-02;17 D6-02/03;22 D6-15 |
| INV-G03 | CT-API-09,CT-QUERY-01/03; tr.61 sortBy/sortOrder+ví dụ pageSize10,tr.37 sort/pageSize12; source endpoints dùng sort và category không nhận sortBy | Baseline dictionary chọn sort/default12 nhưng cần ghi rõ xử lý aliases và category sort ở decision; test đúng contract chốt, không suy unknown query tự được hỗ trợ | 02 D0-02;18 D6-04;22 D6-15 |
| INV-G04 | FR-AUTH-003/A05–06,004/A07,CT-ERR-07/08; tr.20 provider502/invalid401,tr.61/68 invalid400,tr.21 refresh user deleted/locked401; catalog inactive403 | GlobalExceptionHandler map Google unavailable503, OpenAPI ghi502; Auth endpoints .Produces401 cho Google trong khi runtime map400. Chốt từng nguyên nhân provider/invalid/inactive, đồng bộ doc và test, giữ GAP-005 | 02 D0-02;08/10 D3-03/05;22 D6-15 |
| INV-G05 | FR-OBS-001/A04,CT-API-31; tr.65 /health có status+entries; HealthResponseWriter.cs chỉ status | Quyết định bỏ public dependency detail vì privacy hay trả safe entries có schema. Đo dependency failure status riêng; không gọi khác response này là khớp nguyên văn | 02 D0-02;22 D6-15;33 D9-05/06 |
| INV-G06 | FR-SRCH-001/A05; tr.37 zero results có items=[] và message gợi ý; PageEnvelope chỉ data/meta | Chốt gợi ý thuộc API hay UI theo envelope GAP-021; kiểm empty-state browser và response sau quyết định | 02 D0-02;18 D6-04;22 D6-15 |
| INV-G07 | NFR-SEC-006; SRS§2.3 tr.13 nói VerifiedAuthor email-confirmed; API DI chỉ ActiveUser/AuthorPolicy/AdminPolicy/RecipeOwnerPolicy, không thấy VerifiedAuthor | Không tự bật email-confirmation chặn auto-login. Chốt policy nào áp dụng use case nào và quan hệ v1 không confirmation endpoint. Rà product baseline trước sửa quyền | 02 D0-02;05 D2-03;07 D3-01 |
| INV-G08 | CT-DATA-USER-01/RCP-11/RT-01; SRS tr.55/58–59 và GAP-032 dùng Identity string key; ApplicationUser.cs IdentityUser<Guid>, Recipe.AuthorId/RefreshToken.UserId Guid; snapshot uuid | Sai khác schema/identity rõ từ source, không được đổi khóa/migrate trong inventory. Cần decision cùng migration/compatibility tác động dữ liệu hiện có và kiểu API serialized string | 02 D0-02;22 D6-13/14/15 |
| INV-G09 | CT-DATA-RCP-04/08/09; tr.55 Instructions text NOT NULL và enums smallint; Recipe nullable Instructions, config max10000/HasConversion<int>() | Nullability/default/storage-type chưa có GAP cụ thể; dictionary cho Instructions nullable là baseline hiện có cần trace approval. So catalog/migration rồi chốt, không tự siết null/backfill | 02 D0-02;22 D6-13/14 |
| INV-G10 | CT-DATA-IMAGE-02; tr.58 OriginalUrl NOT NULL; dictionary delivery projection nullable theo private bucket; source RecipeImageDto string nonnullable nhưng OpenAPI nullable | Phân biệt internal ObjectKey với delivery URL; test pending/private/ready/fail và sync nullable typegen, không public-read bucket để khớp PDF | 02 D0-02 với GAP-022;19 D6-05/06;22 D6-13/15 |
| INV-G11 | CT-DATA-RT-05/06; tr.60 CreatedByIp45, dictionary UserAgentHash64; RefreshTokenConfiguration CreatedByIp64, UserAgent512 raw | Chốt length/retention/hash UA là sửa schema hay approved extension; test migration không mất token, log không lộ dữ liệu nhạy cảm | 02 D0-02;22 D6-13/14 |
| INV-G12 | CT-SCHEMA-01/05; OpenAPI AuthEnvelope AT/RT writeOnly=true dù response trả token; nutrition object additionalProperties không field/precision schema | Tooling có thể bỏ token response hoặc không validate nutrition. Đây là gap contract cần kiểm bằng generated types/client, không tự sửa OpenAPI/types ở Phase01 | 22 D6-15; API schema semantics review thuộc02 D0-02 nếu cần thay baseline |

Các gap đã biết **vẫn giữ mở**, không ghi lại thành phát hiện mới: G01 coverage/browserE2E→23–26/29; G02 Google/revoke/owner→08–10; G03 HTTP404/ISR→11–13; G04 rate/shared headers→16; G05 cache mechanism/TTL/privacy→14–15; G06 metadata/sitemap/history→20–21; G07 CQRS/validation/logging→04–06; G08 format/toolchain→03/29; G09 production/UAT evidence→28/30–38. Ping Google, RedLock/thay thế, browser minimum, toolchain, uptime99.5% vs3.65h, backup03:00/timezone và production scaling đều có owner Phase02; không giả được quyết định trong inventory.

## Bằng chứng và cách dùng để viết test

Với từng ID, người nhận chọn đúng loại kiểm trong dòng, gắn method/test case cụ thể rồi lưu **cwd/runtime/config/dataset/HEAD-worktree/command/exit code/pass-fail-skip/expected-actual/log**. UT/domain không thay IT PostgreSQL; fake storage không thay MinIO adapter; JSDOM/source không thay browser; simulation provider không thay Google thật; production HTTP mock không thay mutation backend→Next thật. Không suy số test pass thành phần trăm SRS.

Lệnh **đã có trong repo**, chỉ là entry point cho lượt sau (không phải kết quả chạy lần này):

```powershell
# cwd backend/, SDK theo .mise.toml và lockfile
dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --configuration Release --collect 'XPlat Code Coverage'
dotnet test tests/CulinaryBlog.ArchitectureTests/CulinaryBlog.ArchitectureTests.csproj --configuration Release
dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj --configuration Release
# cwd frontend/, Node22.23.3 theo engines hiện tại
npm test
npm run build
npm run test:isr -- --skip-build
npm run budget:check
```

`--skip-build` chỉ khi build vừa tạo từ source hiện tại. Quality/restore/lint/typecheck/format đầy đủ theo [checklist mục6](../srs-completion-checklist.md). Không có thay đổi runtime lần này nên chỉ chạy verifier tài liệu và diff review, kết quả nằm trong [report](reports/phase-01-result.md). Node trên PATH lúc inventory là22.18.0, **không dùng nó để chứng nhận regression Node22.23.3**. Các phase phụ thuộc có thể dùng inventory ngay; acceptance runtime và decision của họ vẫn mở.
