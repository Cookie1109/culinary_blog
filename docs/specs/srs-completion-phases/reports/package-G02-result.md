# Kết quả gói G02 — Kiến trúc và auth

- **Trạng thái tổng: Chưa đạt, có checkpoint tiếp tục.** Đã xử lý phase04–09; còn D2-01/D2-02 và D3-02/D3-03. Không thực thi gói khác hoặc phase10/28/36.
- Ngày24/10/2026, Asia/Bangkok, Codex; HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`, nhánh main, worktree có sẵn89 đường dẫn dirty/untracked. Không có report/checkpoint G02 trước lượt này. Không có ủy quyền commit/push cụ thể được tìm thấy trong đầu vào; chưa commit/push/publish/deploy.
- Snapshot đầu vào: `tmp/package-G02/before.json`, SHA256 `25e7b56269857e4b533b468a4373c90a635dd3e772629a813044d3c23865a047`; `status-before.txt` giữ cả các PDF đang được chuyển thư mục. Manifest cuối và bảo toàn thay đổi: E09 bên dưới.
- Đã đọc AGENTS.md, README gốc/điều phối, work-packages, progress, phase04–09, checklist D2/D3/mục6, reportG01 và phần inventory/register/ADR-004/005 liên quan. PDF SRS71 trang SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`; đọc tr.14–15,17–23,40–45,47–50,61–62,67–69. Giữ nguyên đính chính A/B/C, không thêm waiver.
- Runtime: .NET10.0.402; Node22.23.3/npm10.9.9 tại `tmp/srs-audit/runtime/node-v22.23.3-win-x64`; Python3.13.7, PowerShell7.6.6. Node PATH chỉ đổi cho tiến trình runner. Backend Release; frontend Next15.5.28 production build; PostgreSQL16.10/Redis7.4.5/SMTP/storage synthetic Testcontainers, cổng ngẫu nhiên. Chrome155.0.8059.85, profile riêng, API Postman HTTP simulation riêng; không Google thật.
- Dependency: inventory/register và môi trường G01 đủ cho phần code local; G01 còn ISR404/audit và D0-04 không được nhận là pass. Phần handler/validation hiện có đủ để làm quyền/rollback/logging/auth độc lập; chưa coi toàn phase04 đạt. Áp dụng Postman API Engineer → API Testing/Simulation/Mocking; dùng contract local, không push workspace.

## Phase 04

**Chưa đạt.** Tái dùng named requests có sẵn; bổ sung audit/sitemap, policy, metadata/query/file validation và regression. Chưa gọi các forwarding handler là kiến trúc nghiệp vụ đã đạt.

| Task | Acceptance/nguồn | Kết quả | Evidence/phần còn mở |
|---|---|---|---|
| D2-01 | CONS-001/002, NFR-MAINT-004; tr.14,44–45,49–50 | Partial, mở | E01/E02; audit/sitemap đã qua MediatR; DeleteRecipe có orchestration/UoW ở Application. Auth và phần lớn category/recipe/composition handler vẫn chuyển tiếp nghiệp vụ sang Infrastructure; phải tiếp tục từng use case |
| D2-02 | CONS-008, NFR-SEC-004; tr.14–15,17–23,41 | Partial, mở | E01/E02; body validators hiện có, metadata ID/version, managed paging/query và upload metadata đã chặn trước service/cache/job. `ValidatePage`/managed search guard và business precondition vẫn còn trong ContentService. Giữ mở mâu thuẫn PDF422/400 và DEC-13/15, không tự tạo precedence/query/PUT exception |
| D2-07 | Domain BCL, hướng phụ thuộc, read không ghi nghiệp vụ; tr.14,44–45,49 | Đạt phạm vi task | E02: architecture3, unit85, IT112; siết test Domain không miễn toàn bộ Microsoft.*. Integration interceptor chứng minh các read public/profile/managed/audit/sitemap không SaveChanges; pipeline tests invalid input không gọi mutation/cache/job |

### Inventory use case HTTP → request/handler

Bảng là inventory source hiện tại, không phải endpoint coverage matrix của phase24. `Handler` tương ứng named request; các request validation dùng body validator, query validator và `RequestMetadataValidator<T>` khi có ID/version. HTTP binding, If-Match parse, multipart/stream và Results nằm ở adapter. Ownership/policy được kiểm ở Application pipeline; kỹ thuật EF/storage/Identity/SMTP ở Infrastructure. Cột cuối chỉ rõ phần nghiệp vụ chưa di chuyển.

| HTTP | Application request → handler | Trách nhiệm hiện tại / còn thiếu |
|---|---|---|
| `POST /api/v1/auth/register` | `RegisterCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/login` | `LoginCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/google` | `GoogleLoginCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/refresh` | `RefreshSessionCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/logout` | `LogoutCommand` | Application đối chiếu owner; Infrastructure tra hash/revoke |
| `POST /api/v1/auth/change-password` | `ChangePasswordCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/forgot-password` | `ForgotPasswordCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/auth/reset-password` | `ResetPasswordCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `GET /api/v1/auth/me` | `GetCurrentUserQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `PATCH /api/v1/auth/me` | `UpdateProfileCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `GET /api/v1/categories` | `GetCategoriesQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/categories/{slug}` | `GetCategoryQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `POST /api/v1/categories` | `CreateCategoryCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PUT /api/v1/categories/{id:guid}` | `UpdateCategoryCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `DELETE /api/v1/categories/{id:guid}` | `DeleteCategoryCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `GET /api/v1/sitemap.xml` | `GetSitemapQuery` | Application Exists/Open; Infrastructure storage; adapter stream |
| `GET /api/v1/recipes` | `ListPublishedRecipesQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/recipes/search` | `SearchPublishedRecipesQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/recipes/{slug}` | `GetPublishedRecipeQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `POST /api/v1/recipes` | `CreateRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PUT /api/v1/recipes/{id:guid}` | `UpdateRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PATCH /api/v1/recipes/{id:guid}/publish` | `PublishRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PATCH /api/v1/recipes/{id:guid}/unpublish` | `UnpublishRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PATCH /api/v1/recipes/{id:guid}/archive` | `ArchiveRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PATCH /api/v1/recipes/{id:guid}/unarchive` | `UnarchiveRecipeCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `DELETE /api/v1/recipes/{id:guid}` | `DeleteRecipeCommand` | Application owner/version/domain/Delete/UoW; post-commit pipeline |
| `POST /api/v1/recipes/{id:guid}/ingredients` | `CreateIngredientCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PUT /api/v1/recipes/{id:guid}/ingredients/{ingredientId:guid}` | `UpdateIngredientCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `DELETE /api/v1/recipes/{id:guid}/ingredients/{ingredientId:guid}` | `DeleteIngredientCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/recipes/{id:guid}/steps` | `CreateStepCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PUT /api/v1/recipes/{id:guid}/steps/{stepId:guid}` | `UpdateStepCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `DELETE /api/v1/recipes/{id:guid}/steps/{stepId:guid}` | `DeleteStepCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `POST /api/v1/recipes/{id:guid}/images` | `UploadImageCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `PATCH /api/v1/recipes/{id:guid}/images/{imageId:guid}` | `UpdateImageCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `DELETE /api/v1/recipes/{id:guid}/images/{imageId:guid}` | `DeleteImageCommand` | Application validation/policy; orchestration service còn ở Infrastructure (D2-01) |
| `GET /api/v1/media/{imageId:guid}/{variant}` | `OpenMediaQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/me/recipes` | `ListMyRecipesQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/me/recipes/{id:guid}` | `GetMyRecipeQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/admin/recipes` | `ListAdminRecipesQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/admin/recipes/{id:guid}` | `GetAdminRecipeQuery` | Application query/policy; Infrastructure projection/storage; không Save |
| `GET /api/v1/admin/audit-logs` | `ListAuditLogsQuery` | Application Admin/query; Infrastructure đọc log/audit projection |

`Program.cs` còn các adapter vận hành `/api/v1`, `/api/v1/release`, `/api/v1/operations/maintenance`, `/health`, `/health/live`, `/health/ready`; OpenAPI/Scalar/Hangfire dashboard là adapter/framework. Không nhận chúng là command nghiệp vụ bị thiếu, cũng không nghiệm thu FR-OBS bằng inventory này. Mã nghiệp vụ và các request đã có trước lượt được giữ, không viết lại ContentService hàng loạt.

## Phase 05

**Đạt trong phạm vi kiểm quyền và atomicity local của task.** Chưa đóng các FR hay contract khác chỉ bằng các test này.

| Task | Acceptance/nguồn | Kết quả | Evidence |
|---|---|---|---|
| D2-03 | Application policies/ownership/active user trước commit; tr.25–31,41–42,49 | Đạt | E02: Guest/owner/other/Admin/inactive/spoofed UserId/spoofed IsAdmin, không gọi handler/UoW/cache/job khi từ chối; HTTP role/owner/Admin regression hiện có; media lấy actor thực thay trường tự khai |
| D2-04 | UoW/transaction/composition/renumber/primary/version/outbox; tr.28–34,49–50 | Đạt atomicity | E02: giữ create UoW; fault **sau SQL save đầu**, trước commit, rollback step renumber/primary/image deletion, version và outbox. ETag race owner/Admin/primary hiện có pass. Runtime stale409 theo baseline GAP-003; không lấy task atomicity làm waiver cho sai khác422 trong PDF, phần contract thuộc17/22 vẫn cần đối chiếu |

`IApplicationActor` đọc principal server và user/role hiện tại qua Identity, không nhận client userId/isAdmin làm nguồn quyền. `AuthorizationBehavior` chạy sau validation, trước handler/cache; đối chiếu actor, role active và owner qua UoW. Các guard Domain và concurrency commit hiện có được giữ. Không thêm repository chỉ để đủ tên pattern.

## Phase 06

**Đạt phạm vi pipeline/logging local.** TTL/Output Cache, failure distributed và vận hành exporter vẫn thuộc14/15/33.

| Task | Acceptance/nguồn | Kết quả | Evidence |
|---|---|---|---|
| D2-05 | Validation/quyền/handler fail không invalidation; sau commit kể cả disconnect; tr.14–15,40,49 | Đạt | E02 pipeline thật qua ISender: commit → recipes tag → categories tag → schedule; cancellation sau commit không hủy. Lỗi cache từng tag vẫn thử tag khác/scheduler; lỗi scheduler không biến commit thành HTTP lỗi để client ghi lại |
| D2-06 | CorrelationId/RequestPath/UserId; job/outbox ID; slow query>100ms/pipeline>500ms; tr.15,39–40,44 | Đạt | E02: request enricher lấy sub/NameIdentifier, Hangfire filter JobId+scope dispose; existing jobs log outbox/message IDs. SQL pg_sleep(0.15) cảnh báo CommandId/duration, không SQL/params; pipeline550ms cảnh báo; không serialize password/email từ LoginCommand; Serilog sink throw không lan ra caller |

Thứ tự hiện tại: Logging → Validation → Authorization → Performance → Caching → CacheInvalidation → Handler; invalidation ở chiều quay về, sau handler đã commit. Không có retry ghi command trong behavior. Redis implementation ghi metric/log rồi fail-open; cache cũ hết hạn bằng TTL. Scheduler có Redis debounce fallback enqueue, Hangfire refresh job retry theo cấu hình hiện có; nếu không enqueue được, job sitemap định kỳ hằng ngày là fallback. **Không có durable per-mutation retry khi scheduler/cache cùng lỗi**; không tuyên bố invalidation tức thời/distributed đã đạt. Chỉ bảo đảm commit không bị báo thành thất bại và các đường fallback được ghi rõ.

## Phase 07

**Đạt các điều kiện local của ba task; không chứng nhận toàn FR-AUTH hoặc provider/production.** Status422/400 vẫn mở đúng D2-02/22.

| Task | Acceptance/nguồn | Kết quả | Evidence |
|---|---|---|---|
| D3-01 | Author mặc định/auto-login/duplicate/PBKDF2≥100000/15 phút/7 ngày/generic/5 lần lockout15 phút/safe profile; tr.17–19,22,41,47 | Đạt chức năng task | E02: registration/token/hash/duplicate409/TTL/generic error/safe profile; lockout có deadline, đúng password vẫn423 trong lockout, hết lockout đăng nhập được; welcome SMTP failure không rollback user/token/outbox. Body validation không side effect ở unit |
| D3-06 | RHF/Zod, sửa/xóa avatar, URL validation/atomicity/CSP/session UI; tr.23 | Đạt chức năng task | E02 API không partial update; E03 React; E04 Chrome thật trên frontend production + API simulation: URL sai giữ profile, ảnh HTTPS ngoài miền naturalWidth>0, name/bio/avatar trong Auth.js session và UI sau lưu, xóa avatar giữ name/bio. Không coi simulation là backend/provider thật |
| D3-08 | Yêu cầu bổ sung người dùng, không tạo FR mới | Đạt local | E02/E03: current password, reset bound/single-use/expiry, cấu hình30 phút, revoke mọi AT/RT, known/unknown cùng202 kể cả SMTP connection refused, reset link fragment encode qua MailHog thử riêng, thông báo form |

Hai gap browser đã sửa: nút mở form ngăn default submit khi React đổi nút thành Save; `update({})` gửi payload để Auth.js chạy callback jwt `trigger=update` đọc lại `/auth/me`. CSP Next và hai Nginx config cho HTTP/HTTPS avatar đã được API/Zod chấp nhận; không thêm script/connect source. Browser chỉ chứng minh HTTPS avatar synthetic qua CSP của frontend production; chưa kiểm Nginx TLS deployment thật hoặc mọi HTTP avatar bị nâng HTTPS bởi production policy.

## Phase 08

**Chờ quyết định cho Google handoff; phần local đã tiếp tục và kiểm.**

| Task | Acceptance/nguồn | Kết quả | Evidence/phần còn mở |
|---|---|---|---|
| D3-02 | Google RT sống sau login, không lộ session/log, không ngầm thêm BFF/cookie; tr.19–21,47,61–62; ADR-004/005 | Pending, mở | `frontend/src/auth.ts` vẫn revoke RT ngay sau exchange. Không chỉ bỏ revoke để tạo token mồ côi. Chưa có đường handoff hợp lệ từ callback server đến sessionStorage; người dùng đã chọn **“Giữ chờ quyết định; làm phần G02 độc lập”** trong lượt này |
| D3-03 | Rotation/expiry/race/reuse/logout cả local và Google; tr.20–22,41,47 | Partial, mở | E02/E03 local: refresh endpoint và callback được kiểm độc lập, server race tối đa1 success và family revoke, RT expired, inactive/locked reject401 và family revoke; frontend cùng tab share1 in-flight promise, logout đợi rotation rồi revoke token mới nhất, lỗi clear/signOut không loop. Chưa có browser chạy qua mốc hết hạn AT thật và Google còn phụ thuộc D3-02; không gọi callback unit/local pass là Google lifecycle pass |

Backend bổ sung warning reuse chỉ có FamilyId/UserId, không raw token. Lockout/disabled sau cấp token không refresh được. Không thay đổi contract body/sessionStorage hay Auth.js JWT để giữ RT, không chạy phase10 hoặc dùng Google credential.

## Phase 09

**Đạt task owner/logout local, độc lập với thiết kế handoff Google còn mở.**

| Task | Acceptance/nguồn | Kết quả | Evidence |
|---|---|---|---|
| D3-04 | Bearer optional/owner/expired/idempotent; tr.21–22,47,62; GAP-007/DEC-16 | Đạt | E02: same owner204, other owner403 không revoke, missing bearer204, correctly signed expired bearer204, invalid/tampered cả expired401 không revoke, repeated204/no-store, old RT401. Application handler đối chiếu owner qua hash lookup, adapter phân biệt expired hợp lệ với bearer giả. E01 OpenAPI/types thêm401/403 và mô tả |

## Thay đổi và giới hạn

- Application: audit/sitemap requests; metadata/query/file validators; validation context riêng cho từng validator; authoritative actor + authorization behavior; logout ownership orchestration; post-commit invalidation failure isolation. Tái dùng body validators và handler hiện có.
- Infrastructure/API: Identity actor adapter, owner lookup, refresh locked/inactive check và reuse warning/lockout deadline; request log enricher, Hangfire scope/filter, slow SQL interceptor; sitemap config adapter; auth bearer expiry parse ở HTTP adapter. Không đổi schema, migration hoặc dependency/lockfile.
- Frontend: single-flight refresh/logout coordination; profile mở form và update-session fix; CSP avatar Next/Nginx; logout OpenAPI/types. Thêm React lifecycle test và browser runner/Postman fixture từ contract local. Không sửa Google token storage khi chưa chốt.
- Mâu thuẫn nguồn giữ mở: FR-AUTH001/002/007 tr.18/19/23 yêu cầu422 nhưng bảng HTTP/error catalog tr.67/68 quy định FluentValidation/VALIDATION_ERROR400. Runtime hiện400 theo GAP-002. Không tự chọn override mới hoặc dùng D3 task để nghiệm thu toàn FR; D2-02/22 phải xử lý cùng nguồn quyết định. Stale ETag409/422 giữ truy vết GAP-003 cho17/22. A/B/C correction không bị đổi.
- Audit5 findings và ISR404 còn từ G01; không sửa dependency/ISR để làm gói xanh. Google provider thật vẫn thuộc10, browser/device matrix28, SLA36.

## Evidence dùng chung

Mọi lệnh chạy thật qua `python tmp/package-G02/run.py <label> <cwd> ...`; JSON cùng tên giữ command, cwd, start(+07:00), seconds, exit; log/TRX ở `tmp/package-G02`. Dưới đây ghi lệnh con, không nhận lệnh dự kiến là kết quả.

| ID | Cwd/runtime/artifact | Lệnh thực tế / label | Loại evidence | Expected/actual/exit |
|---|---|---|---|---|
| E01 | root/backend/frontend, contract worktree | đọc PDF bằng pypdf ở `tmp/pdf-tools`; `npm run generate:api` (`generate`) | Source/contract |45 operations contract hiện có được giữ; logout thêm401/403; generate exit0, không đổi endpoint/body |
| E02 | backend, .NET10.0.402 Release | `dotnet build CulinaryBlog.slnx --configuration Release --no-restore` (`build-password`); `dotnet test CulinaryBlog.slnx -c Release --no-build --no-restore --logger trx --results-directory ../tmp/package-G02/test-results/regression-final` (`backend-regression-final`) | Unit/architecture/IT TestServer + PostgreSQL/Redis/SMTP/storage thử riêng | Build0 warnings/errors, exit0; **85 unit +3 architecture +112 integration**, fail0/skip0, exit0; backend test161.39s |
| E03 | frontend, Node22.23.3, source cuối | `npm test` (`frontend-regression-final`) | React/JSDOM/source |106 pass, fail0/skip0, exit0; gồm2 test lifecycle mới, không nhận JSDOM là browser |
| E04 | frontend production build hiện tại + Chrome riêng + Postman simulation | `node scripts/check-profile-browser.mjs --skip-build` (`profile-browser-finalcheck`) | Browser thật/caller với API HTTP mô phỏng | Chrome155.0.8059.85;6 cases pass, avatar request1/naturalWidth>0, exit0; `profile-browser/result.json`, `avatar-saved.png`, `simulation.log` có login→PATCH→GETme; không provider/backend thật |
| E05 | frontend production, API build-time unreachable như G01 | `npm run build` (`frontend-build-profile-final`); `npm run typecheck`, `npm run budget:check` (`frontend-typecheck-last`, `frontend-budget-last`) | Build/static gates | Build exit0/27.34s; typecheck0; mọi route<200KB gzip, max190.3KB, budget0. Build sau source profile/refresh cuối; browser dùng bản sao standalone mới từng lần |
| E06 | backend/frontend, worktree | `dotnet tool restore`; `dotnet restore CulinaryBlog.slnx --locked-mode`; `dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore`; `npm run format`; `npm run lint` (`tool-restore-authorized`, `restore`, `backend-format-check-last`, `frontend-format-check-last`, `frontend-lint-last`) | Tool/format/lint | Tool/locked restore0; format backend/frontend0; lint0. Không có dependency/lockfile đổi; tái dùng node_modules từ npm ci đã kiểm trong G01 với lockfile không đổi |
| E07 | frontend, đúng lockfile đầu vào | `npm audit --audit-level=high` (`frontend-audit-authorized`) | Dependency gate | **Exit1:4 high/1 moderate**, giống G01; không bỏ gate, không chạy audit fix ngoài scope |
| E08 | root, Docker | `docker compose config --quiet` (`compose`) | Config | Exit0; không start/rebuild/reload stack người dùng; browser kiểm cùng img-src ở hai file Nginx bằng source assertion, không nhận là nginx TLS deployment |
| E09 | root, snapshot/manifest | snapshot before/after và đối chiếu paths ngoài scope; Docker/process inventory sau test | Source/resource preservation | Exit0:46 paths đổi trong phạm vi;72/89 paths dirty đầu vào giữ nguyên hash/trạng thái,17 paths được sửa tiếp đúng G02; không path ngoài scope đổi, không file đầu vào bị mất |

### Các lượt fail và bằng chứng bị thay thế

- `build`/`build2`: ambiguous ValidationFailure rồi namespace JwtOptions trong test mới; đã sửa. `build5`/`build7`: analyzer constant array và public helper test; đã sửa trước build/test cuối. Không tắt analyzer.
- `integration-final`:108 pass/1 fail/0 skip vì fixture Hangfire dùng JobCancellationToken.Null; đổi thành instance hợp lệ. `backend-regression`110 và `backend-regression-auth`111 pass là các checkpoint trước; E02 **112** mới nhất thay chúng sau bổ sung SMTP test.
- `backend-format` báo EOL/BOM/import ở các file vừa viết; format theo repo rồi kiểm lại. Không format file ngoài danh sách thay đổi G02.
- Build frontend đầu bị sandbox chặn Google Fonts; rerun được phép tải fonts rồi pass. npm audit đầu/tool restore đầu bị sandbox network/profile chặn; retry được phép đã có kết quả riêng E06/E07. Không có auto-review rejection còn chặn.
- Các runner browser trước chưa đạt: thiếu public directory trong runner, navigation/network-idle/locator/fill không phù hợp, và bản sao standalone tái dùng giữ HTML cũ. Đã bỏ copy directory không có, dùng locator đúng, bản sao mới từng run và selection+Backspace khi clear. Các lượt này **không** được tính pass. Browser cũng tái hiện gap mở form và session-sync thật; E04 chạy sau hai sửa source và production build cuối. Raw log failure giữ ở tmp, không dùng screenshot cũ làm bằng chứng cuối.

## Điểm còn mở và checkbox

- Đóng task theo điều kiện của chính task: **D2-03,D2-04,D2-05,D2-06,D2-07,D3-01,D3-04,D3-06,D3-08**. Không suy những checkbox này là toàn bộ FR/NFR liên quan đã đạt.
- Giữ mở **D2-01,D2-02,D3-02,D3-03**. Không tự đóng bằng named class/test count. Runtime400/422,409/422 và PUT/query ambiguities giữ truy vết nguồn, không thêm ngoại lệ hoặc sửa owner.
- Người dùng chưa chốt handoff Google; chỉ dẫn hiện tại là giữ chờ và làm phần độc lập. Không cần người dùng giao lại phase05/06/07/09: các phần độc lập đó đã tiếp tục trong lượt này.
- Các dòng04–09 và dòngG02 trong progress trỏ đúng mục report. Phase/gói khác và các checkbox ngoài G02 được bảo toàn; các file phase gốc là tài liệu kế hoạch, report gói này là bằng chứng.

### Manifest và bảo toàn sau kiểm

- `tmp/package-G02/after-source.json`: SHA256 `9d99ed444ed7e12e732aaa064c0ffdc2416293cdc6da1bc9be43886232104c9f`; manifest gồm toàn bộ tracked/untracked không ignore, loại riêng report G02/progress/checklist để tránh hash tự tham chiếu. `preservation.json` giữ46 path thay đổi và hash artifact.
- Đầu vào568 file hiện hữu và89 path dirty/untracked; trong89 path đó72 giữ hash/trạng thái,17 được chỉnh tiếp trong G02. Không có file đầu vào bị xóa thêm; các PDF đã xóa/chuyển từ trước giữ nguyên trạng thái, PDF SRS giữ nguyên hash. Các path trước không dirty nhưng được sửa đều thuộc code/test/header G02; không có thay đổi ngoài allowlist scope.
- Next build ID `2kqXDwPFeXZ9CTrOOxVG6`; backend4 DLL và BUILD_ID có SHA256 tại preservation.json. Backend E02 chạy trên build-password sau mọi sửa C#; frontend E03/E04/E05 chạy sau profile/session fixes. E04 cuối dùng `profile-browser-finalcheck` exit0,6.07s; runner chỉ format sau lần pass trước, đã chạy lại trên bytes cuối.
- Docker đối chiếu inventory đầu/cuối:13 container người dùng giữ cùng ID/image/port; không dừng/migrate/reload chúng. Resource check cuối giữ trong `resources-after.log`, `testcontainers-after.log`, `processes-after.log`; không còn Testcontainers hoặc process owned G02 sau cleanup. Browser screenshot/result/log là synthetic, không có credential thật.
- Tài liệu verifier sau cập nhật: `verify-inventory.py` exit0 (347 acceptance/207 contracts/45 OpenAPI operations/82 links); `verify-decisions.py` exit0 (33 GAP/25 DEC/12 INV/11 superseded/5 source issues/235 links/3 baseline hashes giữ nguyên). Chỉ chứng minh cấu trúc/link, không chứng nhận runtime.

## Checkpoint tiếp tục

1. Đọc report này, progress và snapshot cuối; so HEAD/worktree/artifact trước khi dùng lại evidence. Backend112/frontend106/browser6 cases đang xanh trên source ghi trong manifest; audit vẫn fail. Nếu source/dependency tương ứng đổi, build và chạy lại phần bị ảnh hưởng; không dùng --skip-build với artifact cũ.
2. **D2-01 còn việc code độc lập:** chuyển orchestration business trong AuthService/ContentService về Application theo từng lát. Bắt đầu category create/update/delete (duplicate/slug/save), rồi recipe create/update/lifecycle và composition. Tái dùng Domain/UoW/transaction/projection/storage; không chuyển EF/SMTP/HTTP vào Application hoặc viết wrapper rỗng. Giữ các behavior và regression hiện tại. Đây là việc chưa làm tiếp trong checkpoint này, không phải thiếu môi trường.
3. **D2-02:** đối chiếu inventory trên, đưa các guard page/search/body/business validation còn ở service sang pipeline phù hợp; giữ HTTP parse và file signature/stream adapter đúng tầng. Cần quyết định giải mâu thuẫn nguồn400/422 và các phần omit/query tại DEC-13/15 trước khi thay expected phụ thuộc; không tự đặt rule mới.
4. **D3-02/03:** giữ quyết định chờ. Khi có thiết kế handoff server callback→sessionStorage được cho phép, triển khai Google token lifecycle qua boundaries, không lộ RT vào public session/log/URL; kiểm race/rotation/reuse/logout cho nhánh đó. Chưa chạy provider thật; phase10 cần được giao riêng.
5. Giữ source/tests/fixture/runner/báo cáo hiện tại, chưa commit/push. Chưa chạy gói khác. Tiếp tục cùng report G02; không tạo lại inventory hoặc report cho từng phase trùng evidence. Chưa tuyên bố toàn gói/SRS đạt.
