# Kết quả gói G03 — Routes, rendering, ISR, cache và rate limit

- Trạng thái tổng: **Chưa đạt đủ gates** vì `npm audit` còn 4 high/1 moderate. Phạm vi chức năng local của phase11–16 đạt; đóng 14 task D4-01–07/D5-01–07 theo evidence dưới đây. Không chứng nhận toàn bộ SRS.
- Chốt ngày **25/10/2026, Asia/Bangkok**, agent Codex. HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e` + worktree có sẵn thay đổi G01/G02/người dùng; chưa commit/push/deploy. Không thực hiện gói khác hoặc phase10/28/36.
- Đã đọc AGENTS, README điều phối, work-packages/progress/checkpoint và dependency G01/G02. Giữ PDF làm chuẩn, giữ [đính chính A/B/C](../approvals/2026-10-23-abc-correction.md), không bổ sung waiver. Google handoff G02 vẫn chờ theo người dùng; không cần provider thật cho phần local G03 này.
- Người dùng đã chọn **“Bổ sung counter dự phòng PostgreSQL”** để giữ quota khi Redis lỗi. Quyết định này đã được triển khai/kiểm, không còn là mục chờ quyết định trong G03.
- Runtime: Node22.23.3, .NET SDK10.0.402, Next15.5.28; không nâng Next hoặc dependency frontend. PostgreSQL/Redis/MinIO synthetic riêng, SMTP/exporter tắt; không migrate/stop/reload stack người dùng.

## Phase 11

**Đạt ở phạm vi task local.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D4-01 | HTTP404 resource/unknown/extra/internal, tr.26–29,46 | E02 kiểm HTTP404 thật cho missing category/recipe, category page2, unknown URL, extra segment và `/render-public` exact/descendant. Lỗi category200≠404 đầu vào đã sửa |
| D4-03 | List/search SSR theo query; private auth/no-store, tr.46–47 | E02: query phản ánh nội dung; no-store và anonymous redirect/private media401; quyền Author/Admin. E03/E04 kiểm privacy API/media thật, public rewrite không đi qua login |
| D4-04 | `/search`, `/auth/login`, `/auth/register`, canonical/redirect/quyền, tr.46–47 | E02 đo HTTP200 tại route SRS, canonical công khai, signed session login→dashboard và direct private URL theo quyền. Không suy ra Google/provider đã đạt |

Root loading boundary từng làm stream trang lỗi thành200; chuyển loader vào private group, giữ server auth/private layout dynamic và Suspense nhỏ tại NavLink đọc query. Middleware chặn đường nội bộ trước Auth.js, kiểm anonymous/role và trả private no-store. Form login giữ returnTo cùng origin; alias cũ còn hoạt động.

## Phase 12

**Đạt ở phạm vi task local.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D4-02 | Full Route Cache: home/categories3600, recipe300, category detail600 giây, tr.46 | E02 kiểm MISS→HIT, cache-control đúng TTL, HIT tại TTL−30, STALE tại TTL+10 và revision mới sau tái tạo; có category pagination. Đồng hồ Date.now/performance.now chỉ điều khiển trong process Next thử; TTL production giữ nguyên |
| D4-07 | Bài HTTP production chạy hết; simulation và stack thật tách riêng | E02 **38 checks/0 fail**, E03 **6 nhóm/0 fail**; runner gom assertion thay vì dừng sau lỗi đầu. Simulation dựa OpenAPI local chỉ chứng minh caller với dependency mô phỏng |

Recipe/category detail dùng static params rỗng, category pagination rewrite sang internal path; build production không cần API online. E01 build Next với API unreachable đạt. Bài TTL không được ghi là đã chờ thực3600 giây.

## Phase 13

**Đạt ở phạm vi task local.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D4-05 | Mutation backend→pipeline→scheduler/job→callback→Next/public/media, §5.1 tr.46–47 và FR-CAT/RCP/FILE/JOB | E03: category create/update/delete; Draft; publish; recipe description/profile tác giả/ingredient/step/primary/variants/image delete; unpublish/publish/archive/unarchive/publish/delete. Đọc home/category/list/search/detail/media và hai API instance sau mutation |
| D4-06 | Secret/retry/không rollback/build độc lập API, §5.1 và FR-JOB | E02 thiếu/sai secret401, đúng secret200 và revision mới. E03 Next dừng: write vẫn commit; Hangfire retry thật sau60 giây, restart Next cùng cache rồi nội dung mới xuất hiện. E01 build API unreachable đạt |

`ImageProcessingJob` trước đây thiếu refresh khi variants Ready; bổ sung evict cả hai nhóm tags và scheduler sau save, kể cả nhánh retry đã Ready. Refresh nằm ngoài catch xử lý ảnh để lỗi callback không làm ảnh đã Ready thành Failed. Dùng lại pipeline/scheduler G02, không viết lại nghiệp vụ đã có.

## Phase 14

**Đạt ở phạm vi task local.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D5-01 | Redis-backed HTTP Output Cache, §5.2/NFR-SCALE-001, GAP-013 | Package chính thức `Microsoft.AspNetCore.OutputCaching.StackExchangeRedis10.0.13`, endpoint policies/UseOutputCache và adapter fail-open. E03/E04 có Age hit qua hai instance, không nhận IDistributedCache là Output Cache |
| D5-02 | TTL/key/filter/sort/page/normalization; FR-CAT tr.23–24, FR-RCP-002 tr.28, NFR-PERF-003 tr.40 | E04 đọc TTL thực trong Redis: HTTP category1800/list900/search60/detail3600; data detail300. Unit key/filter/normalization regression E04 đạt; query khác không dùng nhầm entry |

**Phân biệt hai tầng theo PDF, không tạo ngoại lệ:** FR-RCP-002 tr.28 yêu cầu HTTP Output Cache detail60 phút; NFR-PERF-003 tr.40 yêu cầu data cache-aside detail5 phút. Giữ HTTP3600 và data300, cùng invalidation sau write. Con số detail5 phút trong checklist/baseline được chứng minh ở tầng data, không dùng nó để hạ HTTP TTL rõ trong PDF. Next ISR recipe300 là tầng Full Route Cache riêng.

HTTP store Redis chính thức có namespace riêng; data cache-aside vẫn dùng IDistributedCache. HTTP body tối đa1MiB; policy bỏ authenticated/error/private. Ghi correlation ID và quota headers tại OnStarting để cache hit không phát lại metadata request cũ. Căn cứ cơ chế: [Microsoft Output caching .NET10](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-10.0).

## Phase 15

**Đạt ở phạm vi task local.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D5-03 | Tags/invalidation/privacy/cross-instance, tr.40,43,46–47 | E03 mọi nhóm mutation kể trên; E04 Guest/Author/Admin/private/error/no-store và invalidation hai nhóm tags. Reader instance không thực hiện write cũng thấy trạng thái mới |
| D5-04 | Redis down/timeout→DB; commit/recovery/metrics/concurrent fill, NFR-PERF-003/NFR-SCALE-001 | E04 container Redis thật down/pause/timeout: đọc DB, write đã commit, failure metrics, phục hồi Age hit. Test unpublish lúc Redis pause rồi đọc từ instance thứ hai sau resume trả404; không hồi sinh Published payload cũ |

Đã tái hiện lỗi privacy thật: mất invalidation khi timeout khiến recipe vừa unpublish trả200 sau Redis phục hồi. Chỉ thống nhất tag provider không sửa được lỗi; đã bỏ thử nghiệm đó. Bản cuối dùng **cache generation trong PostgreSQL**, tăng trong cùng transaction mutation bằng statement triggers trên Categories/Recipes/RecipeIngredients/RecipeSteps/RecipeImages. Data key và HTTP vary đều chứa generation; mỗi request giữ một snapshot lấy trước fallback/fill. Rollback không tăng generation. E04 có regression commit/rollback và reader thứ hai sau recovery.

Migration `20261025050000_PublicCacheGeneration` tạo state/function/triggers. Chi phí: thêm một metadata SELECT theo PK/request dùng cache; vẫn cần PostgreSQL dù cache ấm. Không thêm distributed fill lock theo suy đoán; lỗi stale fill quan sát được xử lý bằng generation. Không lấy kết quả local này chứng nhận throughput/p95 hoặc production scaling.


Rà dependency cuối phát hiện profile tác giả cũng nằm trong public recipe DTO/HTML. `profile-repro` tái hiện PATCH200 nhưng cache qua instance thứ hai vẫn trả tên cũ (1fail,0skip). Sửa duy nhất marker `IContentMutation` trên `UpdateProfileCommand` để dùng pipeline evict/scheduler có sẵn; trigger UPDATE trên AspNetUsers tăng generation khi DisplayName/AvatarUrl/Bio/Email/EmailConfirmed/IsActive thực sự đổi, không tăng vì cập nhật lockout/security stamp đơn thuần. E04 cuối kiểm tên/avatar/bio qua detail/list/search ở hai instance cả Redis healthy và pause/recovery; E03 cuối kiểm profile→Next thật. Không đổi contract/validation hoặc luồng profile G02.

## Phase 16

**Đạt ở phạm vi task local sau lựa chọn PostgreSQL của người dùng.**

| Task | Acceptance/nguồn PDF | Kết quả và evidence |
|---|---|---|
| D5-05 | Sliding auth10/phút/IP; fixed global100/upload5; shared counter, FR-AUTH-002/NFR-SEC-003 tr.41, §5.2 | ASP.NET rate-limiting middleware dùng limiter async; SQL reservation atomic theo IP trước Redis mirror. E04/E05: auth/global/upload đúng giới hạn qua hai instance cả healthy, Redis timeout và recovery; không có quota mới khi đổi instance/phục hồi |
| D5-06 | Limit/Remaining/Reset/Retry-After, CORS/429/OpenAPI | E04/E05 đo header thật; Reset Unix giây, Retry-After giây1–60 khi429; RFC7807/CORS exposure. OpenAPI/type generation E06 đạt; quota headers không bị output cache replay |
| D5-07 | Boundary/IP/proxy/health/upload/concurrent/hai instance | `AuthRateLimitTests` **12 cases**: sliding boundary/limit+1, IP riêng, spoof/trusted proxy, health exemption, upload, global compose, CORS, outage/recovery3 quotas, cạnh tranh20 requests khi Redis pause và cleanup state. E04 full regression đạt |

Migration `20261025053000_SharedRateLimitFallback` tạo SQL ledger/function với row lock theo IP, đồng hồ PostgreSQL dùng chung. Auth giữ timestamps60 giây; global/upload fixed theo phút Unix. Global vẫn tiêu quota khi endpoint limiter áp dụng; auth/upload không nhận thêm quota độc lập theo API instance. Headers phản ánh quota đang ràng buộc.

Để failover không reset quota, PostgreSQL reservation chạy cả khi Redis healthy; Redis mirror dùng cùng timestamp. **Không fail-open quota khi Redis lỗi.** PostgreSQL lỗi không cấp permit vượt quota. Chi phí thêm một SQL round-trip cho mỗi request có rate limit; health miễn quota. Hangfire cleanup state hết hạn theo batch1000 mỗi phút, giữ state còn hiệu lực. Bảng quota không làm tăng public cache generation.

Bỏ trust-all forwarded headers, giữ KnownProxies/KnownNetworks/ForwardLimit1. Local Compose có CIDR nội bộ cấu hình; production bắt buộc `TRUSTED_PROXY_CIDR` rõ trong release env. Nginx đã overwrite XFF bằng peer thật, được giữ nguyên. E04 kiểm middleware trusted/untrusted peer thật trên TestServer; E07 chỉ chứng minh Compose parse được, không nhận là đã nghiệm thu nginx/TLS deployment.

## Evidence dùng chung

Cwd viết tương đối từ `D:\Culinary_Blog`; log/JSON/TRX nằm trong `tmp/package-G03/` được ignore, chưa publish. Mỗi `<label>.json` lưu executable đầy đủ, cwd, command, thời điểm +07, duration và exit; `<label>.log` giữ stdout/stderr. Mốc E04 cuối2026-10-25T13:14:45.751543+07:00 (194.25s), E03 cuối2026-10-25T13:14:46.700493+07:00 (334.2s). Không cộng các lần chạy trùng thành số acceptance hoặc phần trăm SRS.

| ID | Cwd/lệnh thực tế | Loại, expected→actual và artifact |
|---|---|---|
| E01 | `frontend`: `node scripts/check-public-stack.mjs` (lượt `public-stack4`) | Next production build không API online đạt, BUILD_ID `m4n9EgTHT1mMvf-IVntmn`. Chỉ tái dùng artifact frontend còn hợp lệ từ lượt này; cache/backend evidence cũ bị thay bởi E03/E04 |
| E02 | `frontend`: `node scripts/check-isr.mjs --skip-build` | HTTP Next production + **Postman simulation**; expected38/actual38 checks pass,0fail, exit0; `isr-final.json/log`, `isr-final/evidence.json` và upstream/Next logs |
| E03 | `frontend`: `node scripts/check-public-stack.mjs --skip-build` | **Stack thật**: hai Kestrel + Next + Docker PostgreSQL/Redis/MinIO + Hangfire; expected6/actual6 nhóm pass, exit0; `public-stack-profile-final.json/log`, `stack-profile-final/evidence.json`, api/job/Next logs |
| E04 | `backend`: `dotnet test CulinaryBlog.slnx -c Release --no-build --no-restore --logger trx --results-directory ../tmp/package-G03/test-results/profile-final` | Full regression:85 unit +3 architecture +124 integration,0fail/0skip, exit0; `backend-regression-profile-final.json/log`, TRX tại `test-results/profile-final`. IT có DB/Redis containers riêng |
| E05 | `backend`: `dotnet test tests/CulinaryBlog.IntegrationTests -c Release --no-build --no-restore --filter 'FullyQualifiedName~AuthRateLimitTests\|FullyQualifiedName~OutputCacheApiTests\|FullyQualifiedName~MigrationUpgradeTests\|FullyQualifiedName~SecurityHardeningTests\|FullyQualifiedName~ApiDocumentationTests' --logger trx --results-directory ../tmp/package-G03/test-results/fallback` | Targeted trước bổ sung profile:31pass/0fail/0skip, exit0, `fallback-rate-tests.json/log`; quota evidence còn hợp lệ. Cache/migration bị ảnh hưởng đã được E04 cuối chạy lại toàn bộ, gồm profile healthy và Redis timeout/recovery |
| E06 | `frontend`: `npm run generate:api`, `npm run typecheck`, `npm run format` | Exit0, `generate-fallback`, `frontend-typecheck-fallback`, `frontend-format-profile`. Schema/type thay đổi cuối chỉ là mô tả clock/fallback; không thay runtime Next đã build |
| E07 | root: `docker compose config --quiet`; `docker compose --env-file infra/production/release.env.example -f infra/production/compose.production.yaml config --quiet` | Cả hai exit0, `compose`, `compose-production`; syntax/template validation, chưa deploy |
| E08 | `backend`: `dotnet restore CulinaryBlog.slnx --locked-mode`; `dotnet build CulinaryBlog.slnx -c Release --no-restore`; `dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore` | Cả ba exit0, build0warnings/0errors; `locked-restore-fallback`, `profile-build-final`, `backend-format-profile-final`. root `dotnet tool restore` exit0, EF10.0.13 (`tool-restore`) |
| E09 | `frontend`: `npm ci`; `npm test`; `npm run lint`; `npm run budget:check` | Exit0:515 packages;106 tests/0fail/0skip; lint sạch; mọi route<200KB gzip, max190.4KB. Labels `npm-ci`, `frontend-tests`, `frontend-lint`, `frontend-budget` |
| E10 | `frontend`: `npm audit --audit-level=high` | **Exit1**,4high/1moderate; `frontend-audit.json/log`. Cùng frontend lockfile đầu vào, không được bỏ gate hoặc dùng waiver để ghi toàn gói Đạt |
| E11 | root: `python docs/specs/srs-completion-phases/verify-inventory.py`; `python docs/specs/srs-completion-phases/verify-decisions.py`; `git diff --check -- <reviewed-scope paths>` | Cả ba exit0; inventory347 acceptance/207 contracts/45 operations/82 links; decisions33 GAP/25 DEC/12 INV/11 superseded/5 source issues/3 baseline hashes giữ nguyên. Kết quả/lệnh tại `docs-final`, `decisions-final`, `diff-final` JSON/log; không thay bằng chứng runtime hoặc tự đóng task owner khác |

Native runner dùng cổng ngẫu nhiên loopback, credentials/dataset synthetic, hai process API với Host cố định qua node:http để cache key qua instance thực sự giống nhau. Test orchestration nâng quota **chỉ cấu hình test** lên global10000/auth100/upload100 và debounce1 để polling không làm sai bài mutation; quota production100/10/5 được kiểm riêng trong E04/E05. MinIO dùng digest đã pin của Compose `sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e`. Tài nguyên sở hữu có run label, cleanup theo ID; không dùng fake storage thay evidence MinIO thật.

## Artifact, bảo toàn và hiệu lực evidence

- PDF SRS SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`, đính chính A/B/C và report G01/G02 giữ byte hash ban đầu. PDF extraction để tra trang tại `tmp/package-G03/srs-source.txt`; không sửa/move PDF.
- Snapshot đầu595 paths `before.json`, SHA256 `9d0c32bc20aeba44171140415bf1f092b86dd635ae3fdf2228eca5490fdb93b4`; snapshot tiếp tục606 paths `resume.json`, SHA256 `793861e35fa8f407e9bb9643c7695876a7ae10f1fbf4160d7dd2c88a60bd3c87`. Source manifest cuối loại ba tài liệu kết quả để tránh tự tham chiếu: `source-manifest.json`, SHA256 **99d09a553b7897aa8a315414b6cf6d945cb3ee59b4c0a8695e69e727e9ee4797**.
- `reviewed-scope.json` liệt kê đúng64 paths có delta G03; `preservation-final.json` đối chiếu snapshot đầu và source cuối. 549 paths đầu vào ngoài phạm vi giữ nguyên hash/trạng thái tồn tại, gồm các PDF giáo trình đã xóa có sẵn. Không reset/clean/gom thay đổi khác.
- `artifact-hashes.json` lưu SHA256 ba DLL API/Infrastructure/Application Release, Next BUILD_ID và frontend lockfile. E03 dùng DLL từ E08 cuối, Next standalone buildID nêu trên. E02, unit/lint frontend còn hợp lệ vì các sửa sau đó thuộc backend, comment generated types và runner stack thật; routes/React/ISR runner không đổi. Generated types đã typecheck/format lại; runner mới format và ESLint riêng exit0 (`frontend-format-profile`, `frontend-lint-profile`: `node node_modules/eslint/bin/eslint.js scripts/check-public-stack.mjs --max-warnings=0`). Không dùng backend test cũ trước hai migration để đóng task.
- `runtime-final.json`/`docker-final.jsonl` lưu đối chiếu13 container người dùng đã quan sát cùng ID; `docker-before.txt` đầu rỗng khi Docker chưa sẵn sàng, không dùng file rỗng làm baseline PASS, dùng các ID ghi nhận trong phiên tiếp tục;22 container hiện tại đều tạo trước24/10 (9 container dừng có sẵn); không còn container/network mang nhãn runner G03 sau cleanup. Không migrate DB stack người dùng; không có test process G03 còn chạy.
- Checkpoint cũ giữ tại `tmp/package-G03/checkpoint-before-final.md`; log fail giữ lịch sử: soft404, private callback redirect, fetch bỏ custom Host, clock chỉ Date.now, Retry-After61, cache resurrection sau Redis timeout và profile tác giả cũ sau PATCH. Evidence candidate/stack trước generation, trước counter PostgreSQL và trước sửa profile đã được thay bằng E03/E04 cuối; không dùng lại để đóng phần bị ảnh hưởng.

## Điểm chặn và checkpoint tiếp tục

- **Còn gate E10:**4high/1moderate ở fast-uri/undici/zod và chuỗi dependency chromium-bidi/puppeteer-core. Đây là failure còn thật từ baseline G01/G02; không tự upgrade/audit fix hoặc thay Next15 ngoài phạm vi. Gói G03 ghi Chưa đạt đủ gates; owner D1-03/quality-security tiếp nhận khi được giao, không tự chạy gói khác.
- Các task chức năng D4-01–07/D5-01–07 đã đóng theo E02–E05; progress cập nhật đúng phase11–16 và dòng G03. Giữ nguyên phase03 cùng báo cáo G01 lịch sử; missing-category gate đã có evidence sửa tại G03, audit vẫn fail. D0-04, Google handoff và task ngoài G03 giữ trạng thái cũ.
- Hai migration mới phải áp dụng trước binary mới khi được phép triển khai; chỉ đã migrate/upgrade trên DB synthetic. Các instance đã kiểm cùng binary mới, chưa kiểm rolling deployment trộn phiên bản cũ/mới. Cache generation và quota ledger thêm SQL I/O; chưa chứng nhận tải, p95, RUM, production proxy/TLS/scale, SLA, browser/device/provider thật. Không đổi các điều kiện đó thành ngoại lệ hoặc tự ký nghiệm thu.
- Không còn bước chức năng G03 đang chờ Docker/quyết định. Lượt tiếp tục chỉ đối chiếu source/artifact với manifest và xử lý gate thuộc phạm vi được giao; chỉ chạy lại phần bị ảnh hưởng. Không cần chạy lại mutation/TTL/quota còn hợp lệ. Log/artifact đang ở máy local, chưa upload vào kho nghiệm thu bền vững.
