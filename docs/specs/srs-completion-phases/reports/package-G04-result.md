# Kết quả gói G04 — Nghiệp vụ, media, jobs, SEO, schema và API

Cập nhật27/10/2026, Asia/Bangkok. **Đạt G04 trong phạm vi nghiệm thu local:15/15 task đóng theo bằng chứng và các chỉ đạo đã ghi nhận.** E28 chốt HTTP/validation, giữ evidence không bị ảnh hưởng. Main e610783 (code a228817) và CI4 job xanh chỉ chứng nhận bản sửa đã publish; không coi đó là rollout toàn snapshot/migration G04 hoặc nghiệm thu toàn hệ thống production. Xem checkpoint mới nhất cuối report.

## Phạm vi, nguồn và ủy quyền

- Chỉ phase17–22, D6-01–15; tái dùng phần quyền/transaction/cache/revalidation đã có của G02/G03. Chạy lại shared regression không phải thực hiện task của gói khác. Không chạy phase10/28/36, không đổi Environment cloud hoặc migrate DB người dùng; push main/auto-deploy theo ủy quyền đã có.
- Đã đọc AGENTS.md, README gốc/điều phối, progress, work-packages, các phase17–22, inventory/register, dependency report và checkpoint của cùng report. Lượt tiếp tục đối chiếu lại diff/artifact/evidence; không làm lại phần còn hợp lệ.
- PDF71 trang SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063` là chuẩn. Giữ nguyên [đính chính A/B/C](../approvals/2026-10-23-abc-correction.md) và lịch sử baseline; không tạo waiver mới.
- Người dùng giao lựa chọn ba điểm còn thiếu; [quy tắc G04](../approvals/2026-10-25-g04-contract-clarifications.md) ghi nguyên văn ủy quyền. Omit giữ giá trị, null theo nullability, nutrition merge từng field; sort tương đương được nhận, mâu thuẫn400; Instructions NULL backfill thành chuỗi rỗng. Những chi tiết bổ sung không được gán thành nguyên văn PDF.
- Workspace chính giữ HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e` và index rỗng; không reset/clean/restore thay đổi đầu vào. Các bản sửa được chọn riêng/publish từ worktree cô lập, code `a22881794e24f7da656aa1f7c69819d99dee147d`, main hiện `e6107839283767594c598f84e70dbf139a626655` theo ủy quyền main/auto-deploy đã có. Không coi main là toàn bộ snapshot G04 local hoặc tự rollout các migration chưa duyệt. [Manifest G04](package-G04-source-manifest.json) tách delta so với snapshot đầu vào, thay vì nhận toàn bộ git diff là thay đổi G04.
- Runtime kiểm: .NET SDK10.0.402; Node22.23.3 tại `tmp/srs-audit/runtime/node-v22.23.3-win-x64`; PostgreSQL16.10, Redis7.4.5, MinIO theo digest Compose. Không dùng Node22.18 mặc định để kết luận gates.
- Áp dụng Postman API Engineer → Testing/Documentation/Simulation với contract/fixture local; PDF skill/pypdf6.21.2 để trích đúng trang. Không push Postman workspace hoặc gọi provider Google thật.

## Phase 17

POST recipe nhận Ingredients/Steps/Nutrition và lưu atomically cùng Draft; server gán step number theo thứ tự mảng. Invalid child không để lại recipe/composition. Giữ wizard lưu Draft trước rồi thêm composition. Category chặn HTML ở form và Application validator; E26 sửa input về2–50 theo FR-CAT-001, giữ cột varchar100 theo schema; đây là hai lớp khác nhau, không còn coi50/100 là mâu thuẫn phải chờ quyết định.

PUT recipe/step/ingredient dùng InputField phân biệt omit/null/value. Required data không nhận null; nullable child data có thể clear; Instructions null thành empty text; nutrition cập nhật từng chỉ số hoặc clear toàn khối. Empty object no-op vẫn kiểm quyền/If-Match. Nutrition-only write tăng aggregate Version. Sau khóa row phải đọc mới cả recipe và owned nutrition: test đã tái hiện root Reload giữ nutrition cũ và nay chứng minh cookTime-only không ghi đè cập nhật cạnh tranh.

Regression kiểm category Admin/duplicate/slug ổn định/soft delete conflict; recipe lifecycle/quyền/privacy/ETag race; ingredient limits/order; step append/renumber; primary-image race và rollback dùng G02 cùng bài mới. D6-01/02/03 đóng ở E28: validation/publish422, stale version409, malformed/If-Match400 theo FR và ủy quyền; category input2–50/storage100, ingredient input1–100/storage200. API real/rollback/version/quyền và UI có evidence; không suy PASS từ assert mã cũ.

## Phase 18

Search rỗng/nonmatching/ký tự đặc biệt có message gợi ý trong API và UI. Giữ PostgreSQL FTS/unaccent/rank, Published-only, AND filters, page1/pageSize12/max50, zero cookTime và tie-break ổn định. Canonical sort/cache key được xử lý tại Application pipeline, giữ sort và sortBy/sortOrder, nhận hai dạng tương đương, reject conflict; UI chuyển lựa chọn sẽ xóa aliases cũ. Category detail nhận sortBy/sortOrder, default category giữ publishedAt khi không truyền sort.

D6-04 đóng ở E28: q thiếu/ngắn và query semantic không hợp lệ trả422. Tìm chỉ trong tên món theo chỉ đạo E25; giữ FTS/simple/relevance/AND filters/URL/mobile/history. Không gọi title-only là hành vi tìm cả mô tả của nguyên văn PDF.

## Phase 19

Upload streaming multipart: precheck Content-Length, bounded64KiB read, giới hạn file5MiB trước khi ghi byte vượt mức; text parts cũng bounded, temp được dọn. MIME/magic/decode/pixel guard và GUID object key giữ bucket private. AVIF dùng Magick.NET15.0.0, các format cũ/variants dùng Skia; không chỉ nới allowlist mà không decode.

MinIO thật riêng kiểm đủ JPEG/PNG/WebP/AVIF, private object403, path traversal không làm đổi key, lưu original và WebP800×600/300×300. Canvas trắng giữ tỷ lệ được decode/assert pixel. Resize Failed giữ original dùng được qua API/UI/JSON-LD; retry/restart hồi phục variants. Delete soft-delete/outbox/idempotent/storage failure giữ metadata và ảnh recipe khác.

Media retry dùng Hangfire PostgreSQL worker thật: Scheduled60/300/1800, worker restart, Failed sau4 attempts, recovery từ Failed thành Succeeded. Test promote Scheduled thủ công để không chờ36 phút; không tuyên bố đã quan sát wall-clock đủ backoff. Linux container chạy actual AvifImage decode và Skia WebP encode/decode32×16 thành công; đây là codec smoke, không phải nghiệm thu toàn API Linux/production.

## Phase 20

Welcome HTML/encode/link tin cậy, outbox/persistent queue/concurrency/SMTP regression có thật. SMTP lỗi không rollback đăng ký; Hangfire Scheduled60/300/1800 qua worker restart và Failed đã được kiểm. Crash sau SMTP accept trước đánh dấu sent vẫn có thể gửi trùng; không hứa exactly-once. HTTP dashboard thực: Guest401, Author403, Admin200.

Sitemap02:00 UTC, retry2 và Hangfire lock giữ nguyên; bổ sung RedLock.net2.3.2 qua Redis multiplexer dùng chung. Hai factory RedLock riêng chứng minh lease contention; upload lỗi giữ artifact cũ. XML có public static/category/recipe URLs, lastmod dựa timestamp nội dung, changefreq/priority; robots trỏ sitemap. Không dùng sitemap/robots để miễn ping.

**D6-09 đã đóng theo chấp thuận thay thế riêng27/10/2026**: [approval robots.txt](../approvals/2026-10-27-sitemap-discovery.md) giữ job/XML/lịch/retry/lock/public URL và robots; không tuyên bố ping/index thành công. Ghi chú lịch sử trước chấp thuận: Google đã ngừng sitemap ping. [Thông báo chính thức](https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping) xác nhận endpoint không còn hỗ trợ. Không gửi request lỗi hoặc tự xóa yêu cầu PDF; cần chốt xử lý yêu cầu provider đã ngừng tại DEC-02. RedLock không còn là điểm thiếu triển khai.

## Phase 21

Metadata title hoàn chỉnh có hậu tố `| Culinary Blog` khi áp dụng và≤60 ký tự; giữ phần TrangN khi rút ngắn. Description150–160 có nội dung có nghĩa; OG/Twitter/canonical/noindex giữ đúng route, OG1200×630. Test thực thi helper và HTML Next production kiểm long/short content; không chỉ tìm string trong source.

RecipeSlugs giữ reservation slug hiện tại/lịch sử Draft. Published title không đổi slug; collision không chiếm slug recipe khác. Alias chỉ resolve khi current recipe Published, trả301 trực tiếp current slug; Draft/Archived/deleted không lộ qua alias. API và public Next đều có301. Canonical host trong fixture cấu hình localhost rõ ràng vì NextURL chuẩn hóa loopback127.0.0.1; assertion vẫn so Location chính xác.

JSON-LD dùng dữ liệu thật, duration/yield/ingredients/steps/nutrition và original fallback Pending/Failed, escape `<` chống script injection, không tạo rating/nutrition giả. **D6-12 đạt**: JSON-LD/data/escape/fallback có bằng chứng local; người dùng đã chạy Google Rich Results cho đúng URL HTTPS tạm của build G04, crawl thành công01:56:46 ngày26/10/2026 và1 Recipe hợp lệ. Xem evidence E19/checkpoint cuối. Có14 cảnh báo tùy chọn đã đối chiếu theo ảnh chi tiết: keywords/aggregateRating/recipeCuisine/video và url, image-or-video cho5 steps; không thêm dữ liệu giả. Không nhận Vercel cũ là deployment G04 hoặc bảo đảm Google lập chỉ mục.

## Phase 22

Ba migration mới: `20261025063821_ContentSlugsAndImageUrls`, `20261025064611_SrsIdentityAndDataTypes`, `20261025065808_SrsDefaultsAndConcurrency`. Up/down trên PostgreSQL riêng từ `20260922150000_RecipeSearchVector`, không apply vào DB người dùng. Capture/drop/recreate đúng FK/trigger khi đổi type; không bỏ trigger để ALTER chạy qua.

| Nhóm inventory | Đối chiếu và thay đổi local |
|---|---|
| CT-DATA-BASE01–06 | UUID/default gen_random_uuid, timestamptz/NOW, soft delete false/filter, audit actor/timestamps; thêm bytea RowVersion NOT NULL/concurrency computed int8send(Version) cho5 bảng, giữ numeric aggregate ETag. UUID entity không chuyển string. |
| CT-DATA-RCP/NUT | Title/slug/description/time/servings/category/author/FK/index; Instructions text NOT NULL, không giữ limit10000 ở server hoặc form; difficulty/status smallint defaults1/0. Nutrition6 chỉ số decimal8,2 nullable, validation chặn overflow/precision. |
| CT-DATA-ING/STEP | Quantity decimal10,3 nullable, name/unit/notes/order; step title/description text/timer/image/number, FK/cascade/checks/renumber/aggregate version. Nullable clears và field limits có tests. |
| CT-DATA-IMAGE | OriginalUrl varchar500 NOT NULL, Medium/Thumbnail nullable, backfill từ controlled media path; object keys/MIME/status/order/primary và unique active-primary index giữ nguyên. Không mở bucket public. |
| CT-DATA-CAT | Name/NormalizedName/Slug unique indexes, Description text, ImageUrl500, order default0, soft delete và recipe delete-restrict. Input2–50/noHTML theo FR; response/cột100 cho dữ liệu cũ. E26/E28 đã kiểm hai lớp, không migration hoặc truncate. |
| CT-DATA-USER/Identity | IdentityUser<string>, Id và toàn bộ Identity/role/login/claim/token/UserId/Recipe.AuthorId/Welcome.UserId varchar450. DisplayName100/Avatar500/Bio text/IsActive true/CreatedAt NOW và normalized email index được rà. |
| CT-DATA-RT | TokenHash64 unique, FamilyId/expiry/rotation giữ dữ liệu; IP45 có guard không truncate; thêm/backfill UA SHA25664. Token mới chỉ ghi hash; giữ raw UA legacy cho bảo toàn upgrade/rollback, chưa tự đặt retention mới. |
| CT-SCHEMA/extension | RecipeSlugs; MediaOutbox/WelcomeEmailOutbox; PublicCacheState/SharedRateLimitState giữ PK/FK/default/index/transaction generation/quota. Catalog raw local:19 bảng gồm EF history,185 cột,57 indexes,49 constraints; không coi chỉ đếm bảng là assertion mọi field. |
| CT-API/ERR/HEADER/HTTP | Đối chiếu method/path/status/envelope/ETag/If-Match/Location/429-quota/Problem/multipart/MIME. Static OpenAPI có47 operations theo verifier; supporting routes/health có servers override phù hợp; runtime docs và generated types được kiểm lại E28. D6-13 đóng E28 theo ưu tiên FR422/409/422 đã chốt; malformed/If-Match400, quyền và lỗi riêng giữ nguyên. OpenAPI/typegen/runtime docs đồng bộ. Hai source issues còn mở của G01 là SLA/scaling ngoài G04. |

Upgrade bảo toàn user/role/claim/login/token/family/IP/UA/recipe/composition/image/media-outbox và dữ liệu Instructions có sẵn. NULL thành empty, không suy text từ steps. Down từ string sang UUID bị từ chối nếu có ID không cast được; EF commit **từng migration**, không hứa toàn chuỗi rollback atomic. Test ghi nhận migration trước đó đã down rồi reapply để về schema dùng được. IP legacy>45 phải xử lý bằng preflight được phép, không truncate dữ liệu.

Fresh seed dùng Bogus.Randomizer với seed cố định để chọn50 recipe từ curated Vietnamese templates,5 authors, ID mới đúng UUIDv4. Đây là lựa chọn dataset thực bằng Bogus; nội dung hướng dẫn và5 hồ sơ tác giả vẫn curated, không tuyên bố toàn bộ field/profile được Faker sinh tự động. D6-14 ghi rõ phương pháp/sai khác seed này, không miễn yêu cầu SRS. Không xóa100 seed cũ để ép count50; test upgrade/restart giữ100 IDs/titles, giữ user edits/soft deletions. Không seed đè user data.

OpenAPI static, XML docs, Scalar và generated types đã đồng bộ nested create/partial PUT/typed nutrition/non-null Instructions/OriginalUrl/sort aliases/message/301/Google400-403-409-503/multipart4 MIME/ETag/Location. XML generation bật ở API/Application; chỉ tắt warning1591 về member chưa có XML comment khi opt-in, không tắt analyzers/business gates. Các payload create/update của client dùng request schema generated để nhận nested create/partial PUT/nutrition merge đúng type; tsc/lint sau thay đổi type-only đều exit0, không đổi JavaScript runtime. Typegen và contract operation audit không chứng minh các status trái PDF đã được chấp thuận.

## Bằng chứng và gates

Cwd mặc định `D:/Culinary_Blog`; lệnh frontend dùng Node22.23.3 đã nêu trên. Raw log/TRX/HTTP/SQL catalog ở ignored `tmp/g04`, không chứa trong tracked report các credential hoặc request headers/bodies. Hash/log/artifact trong manifest cho phép đối chiếu cùng source. Không dùng run lỗi làm kết quả pass.

| ID | Lệnh đã chạy và cwd | Kết quả / artifact |
|---|---|---|
| E01 | root `dotnet test backend/CulinaryBlog.slnx --configuration Release --no-restore --logger "trx;LogFilePrefix=g04-release" --results-directory D:/Culinary_Blog/tmp/g04/release-regression` | **89unit+3architecture+135integration**,0fail/0skip, exit0; `backend-release.log`, TRX trong `release-regression`. Bao gồm docs9, media/jobs/migration/cache/auth và owned-nutrition race trên source cuối. |
| E02 | frontend `node --test tests/*.test.mjs` |113pass,0fail/0skip sau dependency patch và gateway; `preview/frontend-tests.log`. Lượt111 cũ giữ ở `frontend-verified.log`. |
| E03 | frontend `node scripts/check-isr.mjs --skip-build` |Build production mới sau dependency patch,40 HTTP checks pass/0fail, exit0; `preview/isr-run.log`, `preview/isr/evidence.json`, `preview/build.log`. TTL300/600/3600 dùng controlled clock, không tuyên bố chờ đủ wall-clock. |
| E04 | frontend `node scripts/check-public-stack.mjs --skip-build` |6 nhóm pass/exit0 sau dependency patch ở `preview/stack-run.log`, `preview/stack/evidence.json`; hai Kestrel + Next + PG/Redis/MinIO/Hangfire thật. Mọi cache/publish/private/profile/composition/media/unpublish/archive/delete và Next failure/retry/restart đạt trên build mới. |
| E05 | root `dotnet restore backend/CulinaryBlog.slnx --locked-mode`; `dotnet build backend/CulinaryBlog.slnx -c Release --no-restore`; `dotnet format backend/CulinaryBlog.slnx --verify-no-changes --no-restore` |Locked restore pass (`locked-restore-final.log`); source build cuối exit0/0warnings/0errors (`build-release.log`); format exit0 (`format-backend-release.log`). |
| E06 | frontend OpenAPI CLI generate; `tsc --noEmit`; ESLint `--max-warnings=0`; Prettier `--check .`; `node scripts/check-bundle-budget.mjs` |Sau generated types docs cuối: tất cả exit0; bundle mỗi route<200KB gzip. Declaration/docs thay đổi sau build không làm đổi JavaScript artifact Next. `generate-verified`, `typecheck-generated`, `lint-generated`, `format-generated`, `budget-verified`; bước binding request types cuối có `typecheck-client.log`, `lint-client.log` exit0. |
| E07 | root `docker compose config --quiet`; production template `docker compose --env-file infra/production/release.env.example -f infra/production/compose.production.yaml config --quiet` |Cả2 exit0, `compose-final.log`, `compose-production-final.log`; không deploy. |
| E08 | root Python verify-inventory.py/verify-decisions.py |Exit0;347 acceptance rows/209 contract rows/47 static operations/33 GAP/25 DEC/12 INV;3 source issues còn mở,11 phê duyệt cũ vẫn superseded,3 baseline hashes giữ nguyên. Đã kiểm lại sau progress/checklist cuối, kết quả ở cùng log/manifest. |
| E09 | frontend npm CLI/pinned Node: `npm ci`, `npm audit --audit-level=high --json` |Exit0, **0 vulnerabilities** mọi mức; `preview/npm-ci.log`, `preview/npm-audit.json`. Zod4.6.7 và compatible transitive patches trong lockfile, giữ Next15.5.28; không dùng force hoặc tắt gate. Lượt fail4high/1moderate cũ giữ ở `npm-audit-final.json` chỉ là lịch sử. |
| E10 | Docker build API local; publish Linux probe; `docker run --rm ... --entrypoint dotnet culinary-g04-api:local /probe/CulinaryBlog.IntegrationTests.dll` |Exit0; `docker-build.log`, `linux-publish.log`, `linux-codec.log`: actual AVIF decode + Skia WebP encode/decode32×16. Codec/dependency source không đổi sau probe; không gọi artifact này là release cuối của API. |
| E11 | MinIO/Hangfire retry, SMTP/legacy seed targeted và full E01 |`retry-media.log`:3pass/0skip; `owned-race.log` tái hiện fail, `owned-race-fixed.log` pass1; full E01 bao gồm toàn bộ. Retry được promote Scheduled có kiểm enqueue timestamp60/300/1800. |
| E13 | root `git diff --check`; `docker ps` readonly |Exit0; không lỗi whitespace;13 container người dùng còn nguyên, không còn container synthetic của các lượt này. |
| E12 | readonly SQL từ đúng PG run3a121ad778; static/generated operation audit |`schema-catalog.json`; `contract-operation-audit.json`; Static47 gồm44 versioned+3 health root; generated44 versioned khớp mọi method/path. Script audit và hashes trong manifest. Không đọc nội dung user data hoặc sửa DB. |

Lỗi trung gian giữ trong tmp để truy vết: trigger chặn ALTER TYPE, slug race/deadlock, nutrition-only không tăng Version, owned nutrition snapshot cũ, exporter default struct, test EnqueueAt milliseconds, MinIO tag phải dùng digest, loopback host assertion, include paths formatter, compile predicate/analyzer và Windows DLL lock khi stack đang chạy. Đã sửa và kiểm lại sau đúng resource cleanup; không đánh pass từ các lượt lỗi.

## Task và phần còn mở

| Task | Trạng thái checkpoint | Căn cứ / điều kiện còn thiếu |
|---|---|---|
| D6-01 | Mở, chức năng local đã kiểm |Category CRUD/quyền/HTML/cache/unique; còn400/422 và Name50/100, không đóng từ baseline tests. |
| D6-02 | Mở, chức năng local đã kiểm |Nested atomic create/lifecycle/quyền/ETag/partial PUT/race; semantic validation/publish thiếu steps400 còn khác422. |
| D6-03 | Mở, chức năng local đã kiểm |Child null/limits/order/renumber/quyền/rollback/media race; semantic validation400/422. |
| D6-04 | Mở, chức năng local đã kiểm |FTS/filter/sort/message/page/API/UI/cache; q invalid400 còn khác422. |
| D6-05 | Đạt local |E01/E11/E10:streaming4 MIME/real MinIO/private/path/magic/decode/pixel/size. |
| D6-06 | Đạt local |E01/E11:actual resize/canvas/Hangfire retries/Failed/recovery/original fallback. |
| D6-07 | Đạt local |E01/E11:delete actual object/outbox/idempotency/storage failure/isolation/recovery. |
| D6-08 | Đạt local |E01/E11:HTML/SMTP/outbox/Hangfire/worker restart/Failed/dashboard; duplicate crash window đã nêu. |
| D6-09 | Mở |Sitemap/RedLock local có evidence; Google ping provider đã ngừng, DEC-02 chưa chốt. |
| D6-10 | Đạt local |E02/E03 metadata helper và HTML production. |
| D6-11 | Đạt local |E01/E03 reservation/collision/privacy/stability/one-hop301 API+Next. |
| D6-12 | Đạt trên môi trường kiểm tạm |E14–E19 + E02/E03: dữ liệu/SSR JSON-LD/escape/fallback/ảnh đúng; Google URL crawl thành công01:56:46 ngày26/10,1 Recipe hợp lệ. 14 warning đều là optional fields theo ảnh chi tiết; không fake rating/nutrition. |
| D6-13 | Mở |Schema/types/migration/cross-contract audit; còn source conflicts/status400/422, Category50/100 và không nhận legacy extension thành waiver. |
| D6-14 | Đạt local |E01/E11 migration preservation/guards/restart/seed50 mới và100 legacy. |
| D6-15 | Đạt local |E01/docs9, E06/E12:static/XML/Scalar/types; media4 MIME, ETag/Location, auth/multipart/errors;44 versioned methods/paths khớp,3 health ngoài prefix có server override. |

## Checkpoint ngày25/10/2026 (lịch sử; xem checkpoint26/10 cuối report)

Đã xử lý phần độc lập của cả6 phase. **8/15 task đóng**: D6-05/06/07/08/10/11/14/15; **7 task mở**: D6-01/02/03/04/09/12/13. Progress17–22 và checkbox gốc/mirror task đã cập nhật theo cùng bằng chứng; package G04 giữ Chưa đạt. Không còn lượt test đang được dùng như kết quả chờ.

Bản tiếp tục không làm lại Linux codec, MinIO/retry/seed hoặc ISR còn nguyên source/dependency; đã hoàn tất full backend bị ngắt và kiểm lại phần docs/typegen bị ảnh hưởng. Bộ cuối89+3+135 backend,111 frontend,40 HTTP và6 nhóm stack không có fail/skip; npm audit vẫn exit1. Stack/script synthetic đã tự cleanup;13 container người dùng giữ nguyên. Chỉ giữ image codec local làm artifact, không deploy.

Source/code/migration/test/contract độc lập còn đủ đầu vào của G04 đã xử lý. Không tự chọn400/422 hoặc50/100 để đóng nghiệm thu PDF; không dùng A/B/C cũ hay trạng thái baseline Giữ làm waiver. Đính chính và frontend lockfile giữ nguyên hash; không xóa file nào có trong snapshot đầu vào. Manifest tách delta và hash các bằng chứng, report/progress chỉ là metadata sau lượt kiểm.

Khi có đầu vào mới: chốt mâu thuẫn status/limit theo đúng trang PDF và register ở17/18/22; DEC-02 giải quyết provider ping đã ngừng ở20; nối backend public/proxy với URL Vercel đã có, đối chiếu artifact và chạy Rich Results ở21. Chỉ chạy lại tests/artifact liên quan phần thay đổi đó; giữ chứng cứ còn hợp lệ. npm audit vẫn là gate fail cần xử lý theo owner/dependency phù hợp, không tự miễn. RUM/production/SLA và E2E/coverage đầy đủ không được suy từ G04.

Không tự chạy gói khác, Google thật, deploy, phase10/28/36 hoặc commit/push. Nếu mở phiên mới, bắt đầu từ bảng7 task mở này, kiểm manifest trước khi tái dùng evidence; không khởi động lại toàn G04.


### Cập nhật môi trường Vercel ngày25/10/2026

Người dùng cung cấp `https://culinary-blog.vercel.app/` và xác nhận backend vẫn chạy local. Đã điền SITE_URL/AUTH_URL vào file import riêng `tmp/g04/vercel-import.env`, giữ nguyên AUTH_SECRET/REVALIDATION_SECRET đã tạo; file được Git ignore và không đưa giá trị secret vào report/manifest. INTERNAL_API_BASE_URL còn chờ endpoint HTTPS thực tế; không suy địa chỉ backend từ domain frontend.

Probe readonly `tmp/g04/vercel-url-probe.json`: GET `/` HTTP200 HTML shell; GET `/api/v1/categories` HTTP404 HTML. Metadata trang chủ có canonical `http://localhost:8080`; HTTP200 không chứng minh trang recipe render được. Cần import lại biến frontend/redeploy, chuẩn bị backend public và proxy `/api/v1`, rồi đối chiếu SHA/artifact cloud trước Rich Results. Chưa nhận deployment người dùng tự tạo này là artifact G04 đã nghiệm thu. Không chạy Rich Results trên trang lỗi để ghi pass, không tự mở backend local ra Internet hoặc thay đổi container người dùng. Task/checkbox D6-12 vẫn mở.


### Rich Results trang chủ do người dùng cung cấp

Ảnh người dùng gửi: Google Rich Results Test cho `https://culinary-blog.vercel.app/`, crawl thành công lúc21:57:47 ngày25/10/2026 (Asia/Bangkok), kết quả “Không phát hiện thấy mục nào”. Đây là ảnh bài kiểm do người dùng chạy, không phải agent đã chạy Google test hoặc Recipe pass. URL là trang chủ; Recipe JSON-LD trong mã G04 được render ở trang chi tiết `/recipes/{slug}`. D6-12 giữ mở.

Preflight readonly mới (`tmp/g04/vercel-rich-results-preflight.json`): `/` HTTP200 nhưng0 JSON-LD block và canonical vẫn `http://localhost:8080`; `/api/v1/categories`404; `/sitemap.xml`503. Cấu hình SITE_URL chưa có hiệu lực trên artifact đang serve; backend public/proxy còn thiếu. Cần áp dụng env trên Vercel Production, xác minh deployment/source, kết nối backend rồi kiểm URL recipe Published có dữ liệu thật. Không thêm Recipe giả vào trang chủ chỉ để công cụ phát hiện item, không coi crawl thành công là structured data hợp lệ.


## Checkpoint tiếp tục ngày26/10/2026 — môi trường G04 tạm, miễn phí

Người dùng chọn: “Kiểm G04 tạm, ưu tiên miễn phí”. Chuẩn bị môi trường seed riêng trên máy và gateway chỉ đọc; không cần tạo Render/Railway hoặc cloud database lúc này. Đây là môi trường kiểm D6-12, không phải triển khai production hoặc nghiệm thu gói khác.

Dependency gate đã được sửa bằng patch tương thích: Zod4.6.2→4.6.7, fast-uri3.1.8→3.1.9, undici7.30.0→7.30.1 và Puppeteer/CDP/BiDi trong range của dependency hiện có. Giữ Next15.5.28. `npm ci`/audit0, tsc/lint/format/build/budget và113 frontend tests đạt; source/lockfile frontend đã thay đổi có chủ đích, thay mô tả “lockfile giữ nguyên” của checkpoint cũ. A/B/C không đổi. Build hiện tại `78imeCK0oGhzbx6zw2eDK`; lock SHA256 `6a164582818a5420bdfe8a7e0efb0395f9eaed1f47c7f476241323c32163e8c2`.

| Evidence mới | Phạm vi và kết quả |
|---|---|
| E14 | `node scripts/prepare-g04-preview.mjs` (frontend, Node22.23.3), exit0; `tmp/g04/preview/preflight-final.log`, `preview/9f8ffb0259/evidence.json`: PG/Redis/MinIO riêng + API G04 + Next production;50 Published seed, trang `/recipes/com-chien-hai-san` HTTP200, Recipe JSON-LD SSR/canonical theo origin, ảnh HTTP200,20 CSS/JS assets HTTP200, public recipe/category GET200. Không dùng DB/secret stack người dùng. Lượt này chỉ loopback, đã tự cleanup. |
| E15 | Gateway HTTP tests2pass trong tổng113pass: chặn write/private/auth/admin/traversal, không chuyển tiếp Cookie/Authorization/middleware headers, trusted301 và external redirect bị chặn. `preview/gateway-tests.log`, `preview/frontend-tests.log`; lint/format exit0 ở `preview/lint-final.log`, `preview/format-final.log`. |
| E16 | Sau dependency patch: tsc/build exit0 ở `preview/typecheck.log`, `preview/build.log`; ISR40 pass `preview/isr/evidence.json`; shared real-stack regression6 nhóm pass/exit0 ở `preview/stack/evidence.json`, bundle mọi route<200KB gzip pass ở `preview/budget.log`. Backend C# không đổi nên giữ E01/E05/E10/E11 còn hợp lệ, không chạy lại89+3+135. |

Gateway chỉ cho GET/HEAD trang recipe, API public recipes/categories, ảnh và CSS/JS/fonts phục vụ trang; auth/admin/jobs/OpenAPI/private-media và mọi phương thức ghi bị chặn. Upstream cố định loopback, bỏ credentials/header điều khiển từ caller. PG/Redis/MinIO cũng chỉ bind loopback, dữ liệu seed tổng hợp, email/admin seed tắt. Robots cho crawler vào đúng tài nguyên cần kiểm Recipe. Không tạo dữ liệu JSON-LD giả hoặc đưa Recipe vào trang chủ.

Cloudflared2026.10.3 tải từ GitHub chính thức, đối chiếu SHA256 asset digest; mới chạy `--version`, **chưa bật tunnel**. Metadata/hash ở `preview/tools/cloudflared.json`, không chứa credential. `--public` là hành động riêng, tự dừng sau20 phút từ lúc mở tunnel và dọn tài nguyên riêng; cần máy/Docker còn chạy trong lúc Google kiểm. URL tạm sẽ thay đổi mỗi lượt.

Bước kế tiếp: xin một lần ủy quyền cụ thể để công khai môi trường seed đọc trong20 phút theo giới hạn ở [work-packages.md](../work-packages.md): “Gom gói không cấp quyền deploy, gửi email/alert, dùng tài khoản người khác hoặc ký UAT.” Khi được phép, chạy `node scripts/prepare-g04-preview.mjs --public` bằng pinned Node, lấy URL Recipe/giờ hết hạn từ stdout và evidence, xác minh HTTP qua URL HTTPS rồi chạy Google Rich Results cho URL Recipe đó. Preflight HTML không thay kết quả Google; D6-12 và checkbox gốc/mirror vẫn mở. Không cần người dùng tự deploy toàn backend lên Render để tiếp tục cách kiểm này.

Trạng thái vẫn8/15 task đóng,7 mở như bảng trên. Không commit/push, không sửa DB người dùng, không tự chạy phase10/28/36 hoặc gói khác. Npm audit không còn là gate fail hiện tại; các quyết định400/422,Name50/100 và Google ping vẫn mở. Các report gói cũ giữ nguyên lịch sử, không nhận dependency patch là chứng nhận nghiệm thu những gói đó.


### URL HTTPS tạm đã được cho phép —26/10/2026

Người dùng xác nhận: “Cho phép bật URL kiểm thử 20 phút”. Đã chạy pinned Node `scripts/prepare-g04-preview.mjs --public` tại frontend; session27303 đang chạy có timer, không ghi exit0 cho toàn phiên trước khi đóng. Run `623dcc3a9a`, build/lock giữ đúng checkpoint đã kiểm. Recipe [Cơm chiên hải sản](https://processors-names-holder-affiliate.trycloudflare.com/recipes/com-chien-hai-san) hoạt động qua HTTPS; tự hết hạn **02:04:50 ngày26/10/2026, Asia/Bangkok** (2026-10-25T19:04:50.184Z). Chỉ dữ liệu seed riêng và phạm vi đọc đã nêu, không migrate/chạm DB người dùng; không cần deploy Render/Vercel mới.

E17: `python -X utf8 tmp/g04/preview/probe-public.py`, exit0, **11 HTTP assertions qua HTTPS công khai**: Recipe200, Recipe JSON-LD khớp seed/canonical public, ảnh200+image MIME, CSS200, API public200, robots200; login/jobs/session/me-recipes/health404; POST405. Artifact `tmp/g04/preview/623dcc3a9a/public-http-evidence.json`; preflight đầy đủ/20assets ở cùng `evidence.json`, log `preview/public-run.log`; thời hạn/ủy quyền ở `preview/public-session.json`. Đây là public HTTPS evidence, chưa phải Google Rich Results pass.

Đã gửi đúng URL Recipe và hướng dẫn Google Rich Results cho người dùng. **D6-12 vẫn mở** đến khi có kết quả tool Google cho chính URL Recipe và đối chiếu lỗi/cảnh báo thực tế. Task gốc/mirror chưa đổi checkbox; package vẫn8/15. Source/backend/dependency không đổi ở lượt bật URL, không chạy lại regression đã hợp lệ. Mốc “chưa bật tunnel” trong checkpoint chuẩn bị ở trên là lịch sử trước ủy quyền này.

Tiếp tục: nhận link chia sẻ/ảnh kết quả Google, đối chiếu schema và lưu evidence; sửa đúng phần lỗi nếu có, kiểm lại phần bị ảnh hưởng. URL chỉ còn đến hạn trên; không tự mở lượt công khai mới ngoài20 phút đã được phép. Script tự đóng tunnel và dọn đúng resources có label run; không dừng container người dùng. Khi cần đối chiếu cleanup, kiểm session27303 và Docker label `culinary.g04.preview=623dcc3a9a`. Không coi timer chưa hết là cleanup đã hoàn tất.


### Checkpoint Google crawl và URL thay thế —26/10/2026

Ảnh người dùng gửi cho URL Quick Tunnel lúc **01:48:06 ngày26/10**: Google không thể lập chỉ mục/thu thập dữ liệu không thành công; đồng thời phát hiện **1 Recipe hợp lệ**, có vấn đề không nghiêm trọng. Chưa mở chi tiết crawl hoặc tên warning; không suy warning là rating/video và không tạo dữ liệu giả để loại cảnh báo. Ảnh nằm trong chat, chưa có file screenshot hoặc link kết quả được lưu ở workspace. D6-12 chưa đạt.

Đối chiếu HTTP E18 (`preview/623dcc3a9a/crawl-diagnosis.json`): ứng dụng local200, meta index/follow, không X-Robots-Tag; Quick Tunnel200 nhưng edge thêm **X-Robots-Tag: none**. [Google](https://developers.google.com/search/docs/crawling-indexing/robots-meta-tag) định nghĩa none=noindex/nofollow và áp dụng chỉ thị hạn chế nhất. Đây là blocker indexability đã xác minh; ảnh Google chưa cho exact crawl-detail reason nên không nhận đã đọc reason trong dropdown. Preflight E17 trước đó thiếu kiểm header indexability: pass HTTP/JSON-LD không có nghĩa đủ điều kiện nghiệm thu URL. Đã bổ sung kiểm response HTTPS và chặn none/noindex vào runner; syntax/Prettier/ESLint targeted đều exit0. `prepare-g04-preview.mjs --public` với Cloudflare sẽ fail/cleanup nếu edge còn header này, không gửi lại URL ấy để nghiệm thu.

Đã thay đường truy cập bằng [localhost.run SSH miễn phí](https://localhost.run/docs/forever-free/), không dùng tài khoản, SSH key hoặc agent cá nhân (`nokey`, config riêng, IdentityFile/Agent none). Scope và thời hạn công khai **không tăng**: vẫn cùng API/storage seed riêng và whitelist gateway, hết hạn02:04:50.184 ngày26/10. Phiên SSH probe đầu đã đóng đúng PID22456; phiên thay thế có watchdog theo hạn ban đầu ở session51900. Helper `tmp/g04/preview/ssh-preview.mjs` tạo riêng Next production từ đúng build và SITE_URL mới, để canonical/JSON-LD/ảnh không giữ origin Cloudflare. API fixture cũ ở session27303 được giữ đến hạn để phục vụ dữ liệu; không đụng stack người dùng.

URL Recipe thay thế: **https://7c16068d5822a3.lhr.life/recipes/com-chien-hai-san**. Đã kiểm qua HTTPS200, không có header none/noindex, JSON-LD Recipe/canonical đúng URL mới, ảnh200, auth/jobs/me/session404, POST405; evidence `preview/623dcc3a9a/ssh/evidence.json` và đối chiếu headers E18. Đã gửi URL mới cho người dùng chạy Google, **chưa có kết quả Google cho URL mới**. Pointer `preview/latest.json` và `preview/public-session.json` đã cập nhật, giữ original URL để truy vết. Log/helper/hash được lưu trong manifest; lỗi race tạo log SSH đầu chỉ là lượt lỗi, không tính pass.

Tiếp tục từ checkpoint này: nhận Google result URL mới và chi tiết warning Recipe. Không tự thêm rating/review/video/field không có dữ liệu thật; chỉ đóng D6-12 khi đủ điều kiện gốc. Nếu hết hạn, kiểm cleanup cả27303/51900; không tự gia hạn/mở thêm ngoài cửa sổ được phép. App/dependency không đổi nên giữ regression89+3+135/113/40/6 còn hợp lệ, chỉ kiểm targeted runner/HTTP bị ảnh hưởng. Package vẫn8/15, task gốc/mirror D6-12 mở.


## Checkpoint Google Rich Results đạt —26/10/2026

**E19 — bằng chứng Google thật do người dùng chạy:** ảnh mới cho `https://7c16068d5822a3.lhr.life/recipes/com-chien-hai-san` hiển thị “Đã phát hiện 1 mục hợp lệ”; crawl thành công **01:56:46 ngày26/10/2026, Asia/Bangkok**; mục Công thức có1 item hợp lệ và cảnh báo không nghiêm trọng. Không còn thông báo URL không thể lập chỉ mục của lần Cloudflare. Không nhận là agent tự chạy Google. Bản ghi [G04-rich-results-2026-10-26.json](evidence/G04-rich-results-2026-10-26.json) lưu nguyên trạng quan sát, provenance ảnh trong chat, URL, timestamp, build/lock và hash HTTP/JSON-LD fixture; chưa có file ảnh gốc hay share-result URL trong workspace.

Kết hợp E02/E03 kiểm duration/yield/ingredients/steps/nutrition/escape/original fallback và E14/E17/E18 kiểm API/storage/SSR/ảnh/CSS/JS/HTTPS đúng build, đủ điều kiện gốc **đóng D6-12**. [Tài liệu Google](https://support.google.com/webmasters/answer/7445569?hl=en) phân biệt valid item và cảnh báo; checklist không yêu cầu xóa mọi cảnh báo không nghiêm trọng. Tên cảnh báo chưa được mở nên giữ điều đó là giới hạn chưa xác minh; không tự đoán, không fake rating/review/nutrition/video, không tạo ngoại lệ PDF. Kết quả này không chứng nhận production, website được index hoặc hiển thị rich result trên Search.

**Phase21 Đạt trong phạm vi gói**, D6-10/11/12 đóng. **G04 vẫn Chưa đạt:9/15 task đóng**, D6-05/06/07/08/10/11/12/14/15; **6 task mở** D6-01/02/03/04/09/13 do status400/422, Category50/100 và Google ping provider đã ngừng. Không chạy lại các suite source/dependency không đổi; kiểm tài liệu/checklist/provenance/manifest sau cập nhật. A/B/C giữ nguyên, không commit/push hoặc chạy gói khác/phase10/28/36.

Các checkpoint8/15, “D6-12 mở” và “chưa có Google result” phía trên là lịch sử trước ảnh thành công này. Điểm tiếp tục là6 task mở vừa liệt kê; không làm lại D6-12 hoặc mở URL mới chỉ để kiểm lại kết quả còn hợp lệ. Môi trường URL tạm được đóng/cleanup theo evidence cleanup bổ sung; không gia hạn public window. Nếu người dùng cung cấp chi tiết warning hoặc link chia sẻ, bổ sung cùng E19 và chỉ sửa khi có dữ liệu thật/yêu cầu liên quan.


### Bổ sung chi tiết14 cảnh báo của E19

Người dùng gửi thêm4 ảnh chi tiết cùng item “Cơm chiên hải sản”, ID/canonical/ảnh trùng URL SSH và JSON-LD đã lưu. Google hiển thị **14 vấn đề không nghiêm trọng**, tất cả ghi “không bắt buộc”:4 ở Recipe (`keywords`, `aggregateRating`, `recipeCuisine`, `video`) +2 cho mỗi5 `HowToStep` (thiếu `url`; cần `image` hoặc `video`). Ảnh còn xác nhận author Phạm Gia Huy, các steps có position/name/text và6 nutrition values khớp fixture. Bản ghi E19 đã cập nhật danh sách/count/provenance.

[Google Recipe documentation](https://developers.google.com/search/docs/appearance/structured-data/recipe) xếp những field này trong recommended properties; HowToStep URL là link đến bước nếu có sẵn. Không có critical error hoặc điều kiện checklist yêu cầu zero optional warnings. Không thêm cuisine/keywords khi chưa có dữ liệu nguồn tương ứng, không giả đánh giá/video, không dùng ảnh món ăn làm ảnh minh họa từng bước. Không sửa code chỉ để xóa optional warnings. Trạng thái D6-12/phase21 Đạt giữ nguyên, không đặt waiver PDF mới. Các câu “chưa biết warning” ở checkpoint trước là lịch sử, nay đã có chi tiết từ người dùng.


### Xác nhận cleanup cuối lượt Google

Đến hạn02:04:50 ngày26/10, cả session27303 và51900 tự kết thúc **exit0**. Đã kiểm readonly:0 container/0 network có label `culinary.g04.preview=623dcc3a9a`, không còn process preview/API/Next/SSH/cloudflared của lượt này; các thư mục copy API/Next/data-protection riêng đã dọn; **13 container `culinary-blog-*` của người dùng còn nguyên**. Không gia hạn public window. Evidence `tmp/g04/preview/623dcc3a9a/cleanup-evidence.json`, trạng thái closed ở `preview/public-session.json`; log/JSON-LD/HTTP/Google observation được giữ, không giữ các runtime copies/DP keys.

Checkpoint cuối: D6-12 vàphase21 đã đóng theo E19 cùng14 warning tùy chọn có chi tiết. G04 vẫn9/15,6 task mở D6-01/02/03/04/09/13. Không cần giữ máy/Docker chạy để xem kết quả Google đã lưu; không tự mở lại URL tạm. Report/checklist gốc/mirror/progress/manifest đồng bộ; các kiểm tài liệu và diff whitespace đạt. Không commit/push.


### Bàn giao chuẩn bị Render theo yêu cầu mới —26/10/2026

Sau kiểm G04, người dùng muốn deploy backend lên Render và hỏi file.env/PostgreSQL/Redis/kho ảnh; chưa xác định đã tạo dependency nào. Đã đối chiếu appsettings/options/ProductionConfigurationGuard/Dockerfile và tài liệu Render hiện tại. Không tạo cloud service, không đọc/gửi credential tài khoản, không deploy hoặc commit/push; task G04 vẫn9/15 và6 mục mở, không tự thực hiện owner phase32/35 hoặc gói khác.

Đã chuẩn bị ignored `tmp/render/backend-render.env.draft`, hướng dẫn `tmp/render/README.md`, trạng thái không-secret `tmp/render/draft-status.json`. File nháp giữ Production guard, tạo JWT riêng, tái dùng đúng revalidation secret từ file frontend riêng; không in giá trị secret vào chat/report/manifest. Chưa thể import/deploy: còn API hostname, DB/Redis/storage endpoint+credentials và release artifact metadata thực. Không dán `.env` Docker local hoặc file frontend vào Render; không tự dùng SHA/digest giả, tắt guard hay chạy migrations lên DB chưa xác định. Chỉ kiểm cấu trúc23 env entries/unique keys, JWT length, blanks bắt buộc và migration disabled; không gọi đó là deploy/runtime pass. Secret file không đưa vào manifest.

Nguồn [Render Web Services](https://render.com/docs/web-services) xác nhận domain onrender.com tự cấp; tài nguyên CPU/RAM/SSD khác DNS. [Render Postgres](https://render.com/docs/postgresql-creating-connecting) cho chọn16, cần cùng region API; Free DB hết hạn30 ngày theo [Free limits](https://render.com/docs/free). [Render Key Value](https://render.com/docs/key-value) hiện dùng **Valkey8** cho instance mới, không phải Redis7 của PDF; không tự nhận tương thích giao thức là đã đúng công nghệ SRS. MinIO cần storage persistent; Free Web Service không có persistent disk, không dùng local filesystem ephemeral làm nơi lưu ảnh lâu dài. SMTP/persistence/data-protection/proxy và artifact/source Vercel+Render còn cần cấu hình/đối chiếu, ghi đúng trong hướng dẫn nháp. Không sửa acceptance hoặc tạo ngoại lệ PDF/A/B/C để làm Render Free thành production đạt.

Tiếp tục khi có resource inventory thật: chuẩn bị PostgreSQL16, Redis7 và MinIO persistent theo phương án được chọn; điền file.env riêng, đối chiếu source/artifact và kế hoạch migration, rồi triển khai trong phạm vi ủy quyền mới. Quyền mở URL20phút đã hết, không dùng lại cho triển khai lâu dài. Bản G04 Google đã đạt không cần chạy lại chỉ vì đang chuẩn bị Render.


## Checkpoint Cloudinary — 26/10/2026

**Lịch sử đã bị thay thế:** người dùng đã rút yêu cầu Cloudinary và yêu cầu giữ nguyên MinIO. Adapter/config/CLI/test Cloudinary ở checkpoint này đã được gỡ; không tiếp tục theo các hướng dẫn triển khai/copy bên dưới. Xem checkpoint MinIO cuối report cho trạng thái hiện hành. E20 chỉ là bằng chứng kiểm của phiên bản tạm trước khi hoàn tác.

**E20 — đã triển khai và kiểm chứng adapter Cloudinary theo yêu cầu mới.** [Phạm vi ủy quyền](../approvals/2026-10-26-cloudinary-storage.md), [bằng chứng có hash](evidence/G04-cloudinary-2026-10-26.json), [vận hành/copy dữ liệu](../../../operations/cloudinary-storage.md). Đây là thay đổi provider do người dùng yêu cầu, không sửa PDF hoặc đính chính A/B/C, không miễn các gate production khác. Các checkpoint MinIO/Render ở trên là lịch sử trước lựa chọn Cloudinary.

CloudinaryDotNet1.29.3 được đăng ký qua ObjectStorage:Provider=Cloudinary; credentials có validation theo provider, không yêu cầu MinIO cho API cloud. Assets dùng raw/authenticated để giữ nguyên byte và quyền đọc qua API; tên object GUID/logic, URL API, Hangfire Skia/Magick resize, sitemap và outbox không đổi. Download có chữ ký chỉ dùng nội bộ và được loại khỏi download-client logger/HTTP tracing; không gửi secret về frontend. SDK destroy không hỗ trợ CancellationToken: hủy dừng chờ, request provider có thể còn tiếp tục; delete/outbox vẫn idempotent. List/ping Admin API và upload/download chịu quota tài khoản, không chứng nhận tải production/free tier.

**Provider thật:** credentials trong ảnh được người dùng yêu cầu điền vào ignored `tmp/cloudinary/runtime.env`, không đưa vào report/Git/manifest. Xác thực200; Windows probe và image API Linux đều trả verifiedObjects=5 sau roundtrip JPEG/PNG/WebP/AVIF/XML byte-exact, XML overwrite, anonymous access denied, list, delete hai lần và kiểm không còn asset. Chỉ tạo prefix probe ngẫu nhiên và tự dọn dữ liệu mẫu; không đụng asset người dùng. Docker API build thành công, container/image test riêng đã dọn, 13 container local còn nguyên. Không tuyên bố probe là nghiệm thu toàn API Linux/production hoặc Rich Results của deployment Cloudinary.

**Phase19:** test SDK qua Postman simulation kiểm upload/read/list phân trang/xóa, lỗi503/404/credential/key; API với PostgreSQL/Redis riêng kiểm AVIF owner/guest draft privacy, decode800×600/300×300, publish/read và delete/outbox. Bằng chứng MinIO thật/retry/restart/canvas/fallback trước vẫn thuộc baseline MinIO; không gán thành test Cloudinary thật. Probe Cloudinary thật bổ sung bằng chứng provider, không thay kiểm API/HTTP fixture.

**Phase20:** SitemapGenerationJob chạy RedLock và lưu XML bằng adapter Cloudinary qua HTTP fixture, full health dùng Cloudinary. D6-09 vẫn mở do Google ping đã ngừng; không thêm request lỗi hoặc miễn yêu cầu. Welcome/queue/retry regression giữ nguyên.

**Phase17/18/21/22:** không đổi DTO/schema/database/JSON-LD/frontend. Chạy lại toàn backend do DI/guard/storage thay đổi: **89 unit +3 architecture +141 integration =233 passed,0 failed,0 skipped**. Riêng SDK/transfer/guard qua Postman simulation5 test đạt. Locked restore, format verify, Release build0 warning/error, dependency audit0 vulnerable, Compose config và API Docker build đạt. Frontend113/HTTP ISR40/public-stack6 và E19 Google giữ trong phạm vi cũ, không chạy lại khi source/contract không đổi. Test OpenAPI/schema/migration trong full backend đạt; không phát sinh migration database cho provider này.

Có CLI storage-probe và storage-migrate chỉ chạy khi gọi rõ, trước startup migrations/jobs. Copy MinIO→Cloudinary giữ key/DB, verify SHA-256 đọc lại, resumable, không xóa source hoặc overwrite target khác byte; test dùng MinIO container thật và Cloudinary HTTP fixture đã đạt. **Chưa chạy copy dữ liệu MinIO người dùng**. Cần backup và cửa sổ tạm dừng ghi trước cutover như hướng dẫn; không xóa volume hoặc tự chuyển stack local hiện tại (vẫn mặc định MinIO).

`tmp/render/backend-render.env.draft` đã chọn Cloudinary và điền credentials riêng, nhưng **chưa sẵn sàng import/deploy**: còn AllowedHosts, PostgreSQL/Redis connections và Release CommitSha/ApiImageDigest/WebImageDigest thật. Không tự tạo cloud resources, bypass Production guard, fake metadata, deploy hoặc commit/push. Secret đã xuất hiện trong ảnh cần xoay trước sử dụng lâu dài. Render/Vercel/persistence/SMTP/resources chưa được nghiệm thu bởi lượt này.

Checkpoint tiếp tục: phần code/config/test Cloudinary đã xong; chuyển dữ liệu thật và Render deployment còn chờ tài nguyên/metadata thật, không mở lại URL kiểm20phút. G04 vẫn **9/15**,6 task mở D6-01/02/03/04/09/13; checkbox gốc/mirror không nâng do đổi provider. Không chạy gói khác hoặc phase10/28/36. Kiểm tài liệu/provenance/manifest sau cập nhật; giữ thay đổi ngoài phạm vi.

Bổ sung file cấu hình theo yêu cầu tiếp theo: đã tạo `backend/.env` từ nháp Render, chọn Cloudinary và điền credentials có sẵn; kiểm các key duy nhất/giá trị khớp nguồn mà không in secret. File được Git ignore và loại khỏi Docker build context qua `backend/.dockerignore`. Còn6 giá trị deployment chưa cấp như draft-status; không tự điền giả. `.env` ở gốc dùng Compose được giữ nguyên, backend/.env không tự nạp vào ASP.NET Core hoặc đổi stack local. Không đổi runtime source nên không chạy lại233 test/probe; chỉ kiểm file cấu hình/ignore/tài liệu/manifest.


## Checkpoint giữ nguyên MinIO — 26/10/2026

**E21 — đã hoàn tác phương án Cloudinary theo chỉ đạo mới:** “Giữ nguyên MinIO đi. Ban nãy quyết định chuyển sang Cloudinary là một sai lầm”. [Yêu cầu trước đã rút](../approvals/2026-10-26-cloudinary-storage.md), [bằng chứng hiện hành](evidence/G04-minio-retained-2026-10-26.json). E20 và checkpoint Cloudinary chỉ giữ lịch sử bản code tạm; không còn là hướng dẫn triển khai hiện hành. PDF và đính chính A/B/C giữ nguyên, không tạo ngoại lệ mới.

Đã gỡ adapter/options/health/CLI probe-copy Cloudinary, SDK NuGet và dependency lock tương ứng, HTTP fixture/Postman simulation/test riêng và guard/telemetry/provider selector vừa thêm. Khôi phục IFileStorageService→MinioFileStorageService, required MinIO options, MinIO full health và production credential checks. Xóa cấu hình Cloudinary ở Compose/.env.example; README trở lại PostgreSQL16/Redis7/MinIO. Giữ các thay đổi G04/G02/G03 khác (AVIF, retry/outbox/sitemap/RedLock/cache/schema/validation/HTTP/docs), không reset/restore toàn worktree.

`backend/.env` và `tmp/render/backend-render.env.draft` đã bỏ Cloudinary credentials/provider, trở lại ObjectStorage__Endpoint/AccessKey/SecretKey/BucketName. Giữ JWT/revalidation và cấu hình không liên quan; MinIO cloud endpoint/credentials chưa có nên để trống, cùng các giá trị deploy chưa được cấp khác. Hai file đều Git ignore; giữ backend/.dockerignore loại `.env*` khỏi build context vì file backend vẫn chứa secret riêng. Đã bỏ file credential/provisioning helpers Cloudinary do agent tạo; giữ log/evidence kiểm cũ để truy vết, không gọi tài khoản Cloudinary thêm.

**Không có chuyển dữ liệu người dùng sang Cloudinary trước đó**, nên không cần migration ngược. `.env` gốc/volume/bucket MinIO local được giữ nguyên; không restart/recreate stack, không sửa DB hoặc xóa ảnh. Không deploy Render, commit/push, tạo cloud service hoặc chạy gói khác/phase10/28/36.

Kiểm hiện hành: **89 unit +3 architecture +135 integration =227 passed,0 failed,0 skipped**; có test MinIO thật với4 MIME, quyền/private, resize/retry/fallback và xóa/outbox. Locked restore, format verify, Release build0 warning/error, dependency audit0 vulnerable và Compose config đều đạt. Không chạy lại frontend113/HTTP ISR40/public-stack6 hoặc Google E19 vì frontend/DTO không đổi; các bằng chứng đó giữ phạm vi ban đầu, không dùng để chứng nhận production.

Trạng thái gói giữ **9/15**: phase19/21 Đạt theo phạm vi local/evidence đã có; D6-01/02/03/04/09/13 còn mở do400/422,Category50/100 và Google ping ngừng. Source manifest/progress/checkbox gốc-mirror đồng bộ; chưa nâng task hoặc nhận production đạt. Điểm tiếp tục là MinIO cùng các dependency thực và6 task mở; nếu tiếp tục Render cần cấp MinIO persistent endpoint/credentials, PostgreSQL/Redis, hostname và artifact metadata thật, không dùng file nháp như cấu hình hoàn chỉnh.

## Checkpoint27/10/2026 — sửa tìm kiếm từ ngắn và đối chiếu deployment

**E22:** [evidence](evidence/G04-short-search-2026-10-27.json). Người dùng yêu cầu sửa kết quả “gà” và push GitHub. Đã tái hiện query tiền tố `ga:*` khớp gạo/gấc/gan; regression mới thất bại2/2, expected3 nhưng actual7 món trong fixture. Bản sửa ưu tiên nguyên từ đơn2 ký tự khi có trong Published, tắt mở rộng trigram cho nhánh đó; nếu chưa có nguyên từ vẫn giữ prefix (“ph”→phở). Query nhiều từ giữ ví dụ PDF `pho:* & bo:*`, các trường hợp khác giữ hành vi trước. Xôi xéo có mỡ gà và súp cua có gà xé vẫn hợp lệ. Không đổi DTO/schema/FTS config/credentials.

Kiểm PostgreSQL16/Redis7 Testcontainers riêng: **8/8** RecipeDiscoveryApiTests trên source tách từ main; **32/32** cùng class trên workspace G04,0fail/skip. Build test đạt; pre-push chạy đủ dotnet format và frontend Prettier. Commit `9719c8bff462c910ba1586bdf8ae4e70271bf118` (“Sua tim kiem tu ngan tieng Viet”) đã fast-forward lên main, đúng3 file service/test/tài liệu. Không mang các thay đổi G04 khác hoặc migration chưa deploy vào commit. Giữ nguyên working tree chính và index; mirror bản sửa/test vào source G04 hiện tại. Bằng chứng cũ ngoài tìm kiếm chỉ được dùng đúng phạm vi cũ, không nhận tổng test cũ là lượt chạy mới.

Deploy frontend/backend đã hoạt động ở checkpoint vận hành; việc push này kích hoạt auto-deploy theo yêu cầu mới. Trạng thái quan sát commit Render mới: **đã thấy; API ga/gà cùng18 kết quả, Vercel12+6 thẻ hai trang khớp API, không còn xôi gấc/cháo lòng/nem lụi**; chi tiết timestamp/count/commit ở E22. CI mới tại [run40370676637](https://github.com/Cookie1109/culinary_blog/actions/runs/40370676637): backend/compose thành công, security filesystem scan và frontend npm audit thất bại; giữ nguyên gates/dependencies và không coi push/Ready hay format xanh là toàn bộ CI đạt. Không đổi cloud Environment, chạy seed/migration hoặc sửa Neon/Upstash/MinIO.

**G04 vẫn9/15.** D6-01/02/03/04/09/13 và checkbox gốc/mirror giữ mở:400/422, Name50/100, Google ping retired/DEC-02. Bản cloud giữ5 migration tương thích snapshot Neon;6 migration cùng phần G04 chưa commit trong workspace chưa được rollout. Ba cờ migration/seed/background jobs của cấu hình Render vẫn false; deploy đọc được không chứng minh toàn bộ media/job/SEO runtime trên cloud đã nghiệm thu. Không nâng phase17/20/22, không dùng deploy làm waiver, không chạy gói khác hoặc phase10/28/36. Bước tiếp theo là giải quyết đúng các contract còn mở theo PDF, lập kế hoạch nâng schema/đưa source G04 lên cloud dưới ủy quyền phù hợp; không migrate production để đóng checkbox.

Kiểm tài liệu mới: `verify-inventory.py` exit0 (347 acceptance/209 contract/47 operation); `verify-decisions.py` exit1 vì `02_Decision_Log.md` khác hash baseline, do dòng bổ sung ADR-011 ngoài commit tìm kiếm. Giữ nguyên file đó và gate/checksum; ghi mở provenance thay vì tái dùng E08 cũ như kết quả mới. Không có lỗi inventory/link mới được lượt kiểm này phát hiện.

## Checkpoint27/10/2026 — dependency audit đã sửa trên main

**E23:** [evidence audit/CI](../../../operations/frontend-audit-fix-result.json). Bản vá dependency trước chỉ có trong workspace G04, chưa nằm trong các commit metadata/search tách từ main. Vì vậy CI main tiếp tục thấy Zod4.6.2 cùng Zod3.25.76 dưới chromium-bidi, undici7.30.0 và fast-uri3.1.8; đây là nguyên nhân hai job frontend/security còn đỏ, không phải lỗi tìm kiếm mới.

Đã push đúng2 file package.json/package-lock.json trong commit `d512494dd9c7876d305f6df8b946cf84a1ec9853` (“Va lo hong frontend va dong bo lockfile”). Zod4.6.7; Puppeteer25.16.0 kéo chromium-bidi157.0.8090-0 theo range upstream và dùng chung Zod4.6.7, loại Zod3 cũ; undici7.30.1, fast-uri3.1.9. Không dùng force/new override, không nâng Next15.5.28, không sửa workflow/severity/license policy, schema/migrations hoặc dữ liệu. Hai file package ở workspace chính vốn đã có bản vá nên được giữ nguyên, không chép đè graph chứa thay đổi G04 khác.

`npm ci` sạch đạt; npm audit **5→0 vulnerabilities** mọi mức. Generate API/format/lint/typecheck/build đạt; **97/97 frontend test**,0fail/skip; mọi route dưới200KB gzip. Trivy0.70.0 FS vuln/secret/misconfig và license HIGH/CRITICAL đều exit0 trên snapshot file Git; Lighthouse13.5.0 chạy headless với trang loopback/profile riêng đạt, không dùng làm nghiệm thu NFR. Hook backend/frontend format vẫn chạy khi push.

**CI của chính commit này đã PASS:** [run40374398464](https://github.com/Cookie1109/culinary_blog/actions/runs/40374398464), backend/frontend/compose/security đều success; dependency-review skip đúng điều kiện push vì chỉ chạy pull_request. Các run đỏ trước là lịch sử, không thay kết quả của commit mới. Render release đã trả đúng SHA trên và readiness200; Vercel home200 render12 recipe links, không còn digest lỗi localhost cũ. Không đổi cloud Environment hoặc chạy seed/jobs/migration.

G04 vẫn **9/15**, sáu task contract/ping/source rollout giữ mở. Gate provenance `verify-decisions.py` ở workspace đã ghi E22 vẫn mở; commit dependency không sửa baseline/ADR hoặc nâng checkbox. Không nhận CI xanh là toàn G04 hay production SRS đạt. Giữ checkpoint E22 về tìm kiếm và các bằng chứng local khác theo đúng phạm vi của chúng.


## Checkpoint27/10/2026 — tìm kiếm tiếng Việt và UX đã deploy

**E24:** [evidence có hash](evidence/G04-vietnamese-search-ux-2026-10-27.json), [quy tắc tìm kiếm](../../../operations/search-short-terms.md). Người dùng yêu cầu tối ưu tìm kiếm và UX khi kiểm trên Vercel. Đã push main commit `62bc66d` (“Toi uu tim kiem tieng Viet va trai nghiem loc cong thuc”) từ checkout tách, giữ nguyên workspace/index G04. Không đưa6 migration hoặc phần G04 khác chưa publish lên cloud.

Ưu tiên khớp đủ từ, đúng dấu khi có tập kết quả tương ứng; sau đó mới prefix và tên gần đúng theo từng từ. `gà` không mở rộng thành gạo/gấc/gan khi đã có nguyên từ; `pho bo` không trộn món chỉ chứa một từ. Giữ FTS/unaccent/trigram/GIN/rank/Published/filter; search cache đổi namespace v2, giữ TTL/tag, không flush Redis. Khi không có khớp đúng dấu, vẫn có fallback không dấu theo hợp đồng; không diễn giải mọi kết quả “bơ” thành món bơ. UI tô từ khóa và giải thích khớp mô tả/không dấu/gần đúng.

UX mặc định Phù hợp nhất khi có từ khóa, chuẩn hóa Unicode/khoảng trắng, xử lý IME và lỗi2–100 ký tự; bỏ từng filter/từ khóa, giữ lịch sử Back/Forward, tải/retry, phân biệt lỗi kết nối với rỗng, phân trang gọn và phục hồi page ngoài phạm vi. Mobile giữ bộ lọc mở khi chọn nhiều điều kiện; không tràn ngang với từ khóa100 ký tự. Các món như xôi xéo/súp cua có gà trong mô tả vẫn hợp lệ, được xếp sau các tên món phù hợp và có nhãn giải thích.

CI đầu của feature phát hiện test sitemap khởi tạo AutomaticRetryAttribute, chạm LoggerFactory toàn cục đã dispose. Commit tiếp `3d46833` chỉ sửa1 test đọc CustomAttributeData, vẫn assert retry đúng2, không đổi job. Lượt cuối [CI40389122105](https://github.com/Cookie1109/culinary_blog/actions/runs/40389122105) **backend/frontend/compose/security success**; backend3 architecture+67 unit+107 integration, frontend111/111,0fail/skip; npm audit0. Dependency-review skip đúng điều kiện push. Generate API/format/lint/typecheck/build/bundle budget và pre-push format giữ đủ, không hạ gate.

Kiểm local source main:21/21 search riêng,107/107 full integration,67/67 unit. Workspace G04:45/45 search,3/3 sitemap sau sửa metadata,127/127 frontend và typecheck đạt; không nhận build root cũ là build mới. Chrome thật/profile riêng trên Vercel đạt8/8 luồng desktop1440×960/mobile390×844; đã xem ảnh chụp. Render release trả đúng `3d468338c2c2c063a61a90131b8c99c3dcb44a1e`, readiness200.13 truy vấn live đạt: ga/gà/GÀ/NFD cùng18, ga nuong2, ga nuongg1, pho bo1, ph22, cá12/ca17, bò12/bơ14, từ không tồn tại0. Bằng chứng JSON ghi rõ bơ là fallback không dấu, không tuyên bố tách dấu tuyệt đối.

Lưu trữ E23: một số log local audit-* bị helper tái sử dụng khi kiểm feature. Giữ nguyên hash/kết quả lịch sử97 test; đánh dấu đúng các file đã bị ghi thay và dẫn log CI gốc d512494/job121753384511 đã tải về, thay vì coi111 test của source mới là97 test cũ. Không thay kết quả audit/Trivy hoặc giả phục hồi log Windows gốc.

**G04 vẫn9/15**, D6-01/02/03/04/09/13 và checkbox gốc/mirror giữ mở.400/422, Name50/100, ping Google/DEC-02 và rollout source/schema khác chưa giải quyết. PDF/đính chính A/B/C giữ nguyên. Không đổi Neon/Upstash/MinIO/Environment, chạy seed/migration hoặc mở gói khác/phase10/28/36. Gate provenance verify-decisions.py đã ghi E22 còn mở, không sửa checksum để qua gate. Bước tiếp theo vẫn là xử lý các contract còn mở theo PDF và lập kế hoạch rollout G04 có kiểm soát; không nhận deploy tìm kiếm là hoàn tất gói.

Kiểm tài liệu sau checkpoint: inventory exit0,347 acceptance/209 contract/47 operation/84 link; decisions exit1 chỉ tại baseline `02_Decision_Log.md` đã biết (3 source issues còn mở), không sửa gate hoặc checksum. Diff whitespace phần tác động exit0, index root vẫn rỗng; ảnh visible desktop/mobile đều tải được trong lượt Chrome cuối.


## Checkpoint27/10/2026 — chỉ khớp tên món, bỏ tô từ và nhãn

**E25:** [bằng chứng hiện hành](evidence/G04-title-only-search-2026-10-27.json). Người dùng trực tiếp yêu cầu bỏ tô từ khóa/nhãn giải thích, rồi chốt “các kết quả khớp mô tả thì không tính, chỉ tính nếu khớp tên món”. E24 là lịch sử trước thay đổi này, không phải chính sách tìm kiếm hiện hành.

Đã push main `9486cb2` bỏ mark/nhãn và helper không còn dùng; DOM test vẫn kiểm tên/mô tả/filter, chuyển sang assert không có mark/nhãn. Commit `d5eca46` giới hạn mọi nhánh exact/prefix/dictionary/trigram vào tên món; dùng weight A đã có của SearchVector, kiểm dấu trên Title. Giữ trigger/vector/index, dữ liệu, sort/filter/privacy và fallback tiếng Việt; cache namespace v3 tránh kết quả cũ, không flush Redis. Hint/API/OpenAPI/types/tài liệu đã đồng bộ chỉ tìm tên. Không đổi schema/migration/seed/Environment/MinIO.

Test thay đổi phạm vi tái hiện8 fail/17 trước sửa; sau sửa109/109 full integration và67/67 unit main đạt,47/47 search cùng cache-key test G04 đạt. Frontend109/109 main và125/125 G04 đạt; hai test helper của tính năng hiển thị bị bỏ đã được loại cùng helper, không hạ gate. Generate/format/lint/typecheck/build/bundle đạt. Hook format chặn CRLF ở file C# vừa ghi; đã chuẩn hóa LF và push lại qua đủ hook, không bypass.

**[CI40406039643](https://github.com/Cookie1109/culinary_blog/actions/runs/40406039643) cả4 job success**: backend3 architecture+67 unit+109 integration, frontend109, audit0; dependency-review skip đúng sự kiện push. Render trả đúng SHA `d5eca462390b501098c34c102bc5a58ab2ebfd21`, readiness200;13 query live đạt. “ga/gà” hiện14 kết quả đều có nguyên từ ga trong tên, loại xôi xéo/súp cua/xôi mặn/bún thang; “ga nuong”1 và “ga nuongg”1, “pho bo”1. Chrome thật/profile riêng kiểm Vercel desktop1440/mobile390 đều không mark/nhãn, hint mới đúng và không tràn ngang; đã xem screenshot. Backend/SSR cùng áp dụng phạm vi mới, không chỉ lọc trang đang hiển thị ở frontend.

PDF/A/B/C giữ nguyên; đây là thay đổi phạm vi theo chỉ đạo trực tiếp, không tự nhận là waiver hoặc toàn bộ FR-SRCH đạt. **G04 vẫn9/15**, sáu checkbox cũ giữ mở;400/422, Name50/100, Google ping/DEC-02, rollout phần G04 khác và provenance baseline tài liệu chưa được giải quyết. Không mở phase/gói khác. Giữ các bằng chứng E22/E23/E24 theo source/thời điểm cũ; E25 là checkpoint tiếp tục cho phần tìm kiếm hiện tại.

Kiểm tài liệu E25: inventory exit0 (347/209/47); decisions exit1 chỉ do hash baseline `02_Decision_Log.md` đã ghi mở, không bypass. Root cache-key4/4, diff whitespace phần tác động đạt và index root vẫn rỗng.


## Checkpoint27/10/2026 — giới hạn tên danh mục theo đúng hai lớp

**E26:** [evidence](evidence/G04-category-name-2026-10-27.json), [quy tắc](../../../operations/category-name-validation.md). Đã push main f6ec7bf: API/Application/Domain/UI đều2–50, không HTML cho tạo/cập nhật; CategoryWrite OpenAPI/types50, response/model/DB vẫn100 để giữ dữ liệu cũ. Không migration hoặc cắt/sửa tên hiện có. Đã đính chính nhận định trước: validation50 và sức chứa cột100 có thể đồng thời đúng PDF, không cần waiver giới hạn tên.

Unit trước sửa4fail/7; sau sửa74/74 unit main,116/116 integration main,110 frontend main; Root G04 category unit7/7,full integration179/179 và126 frontend đạt. Hai tên fixture dài hơn50 được rút ngắn, giữ nguyên assert audit/dataset/ngưỡng hiệu năng. Regression Root đầu thiếu schema Hangfire ở3 test manual-worker; bổ sung khởi tạo schema trong Testcontainers ở AuthApiFactory, vẫn tắt worker tự động và không đổi production. Lượt cuối179/179,không skip.

[CI40412485400](https://github.com/Cookie1109/culinary_blog/actions/runs/40412485400) backend/frontend/security/compose success; backend3 architecture+74unit+116integration,frontend110/audit0. Render release đúng f6ec7bf,ready200; Vercel home200;20 danh mục public đọc được,tên dài nhất19. Không đăng nhập/POST/PUT vào production để thử validation. Form create/edit được kiểm React DOM; không nhận đó là test admin browser live.

G04 vẫn9/15 tại checkpoint E26 vì các mã HTTP và ping còn mở lúc ghi bằng chứng này. Lượt tiếp tục sau đã nhận chấp thuận thay ping qua robots.txt và giao lựa chọn ưu tiên mã FR422/409/422; xem các approval27/10, phải kiểm/ghi kết quả mới trước khi nâng checkbox. PDF/A/B/C và dữ liệu cloud giữ nguyên.


## Checkpoint27/10/2026 — sitemap được chấp thuận thay thế

**E27:** [approval riêng](../approvals/2026-10-27-sitemap-discovery.md), người dùng trả lời “Chấp thuận phương án thay thế qua robots.txt”. Giữ nguồn PDF/ABC; sinh sitemap02UTC/retry2/singleton/URL public/lastmod/changefreq/priority và phục vụ XML, khai báo robots được kiểm bởi source/test G04 đang có. Root179/179 integration và126 frontend E26 là bằng chứng mới sau sửa môi trường test; sitemap/robots source không đổi. Không gọi ping cũ/không claim Google index, Search Console tùy chọn, không dùng Rich Results thay cho sitemap. D6-09 và mirror đóng theo đúng phê duyệt, tổng10/15.

Người dùng tiếp tục giao lựa chọn hợp lý và triển khai khi được trình xung đột400/422,409/422,publish400/422 trong PDF. [Quy tắc mới](../approvals/2026-10-27-content-http-statuses.md) chọn ưu tiên FR422/409/422, giữ malformed/If-Match400 và các mapping riêng. Các task D6-01/02/03/04/13 chưa nâng checkbox: đang sửa/test/docs cho đúng lựa chọn này. Không áp migration lên Neon hoặc rollout toàn source G04 để đóng task.

## Checkpoint27/10/2026 — chốt HTTP và nghiệm thu G04 local

**E28:** [evidence có hash/source/artifact](evidence/G04-content-http-2026-10-27.json). G04 đạt **15/15 task trong phạm vi local**, đóng thêm D6-01/02/03/04/13; đã cập nhật checkbox gốc, mirror các phase17–22 và từng dòng tiến độ. Không chạy gói khác hoặc phase10/28/36.

Ưu tiên FR cụ thể theo [ủy quyền chọn quy tắc](../approvals/2026-10-27-content-http-statuses.md): dữ liệu/query semantic sai422, version conflict409, publish chưa đủ ingredient/step422. JSON sai cấu trúc/If-Match thiếu hoặc hỏng400; quyền401/403/404, unique conflict409, media riêng và sort alias conflict400 giữ nguyên. CategoryId không tồn tại khi tham chiếu từ recipe trả422 với field errors và không ghi dở dang; GET resource không tồn tại vẫn404. Shared FluentValidation của auth đổi422 phù hợp FR, các AuthProblem có mã riêng giữ mapping.

Sửa thêm giới hạn nguyên liệu bị phát hiện khi rà D6-03: input1–100 theo FR-RCP-009, cột/response cũ200. Category input2–50/storage100 giữ E26. Domain/Application/UI/OpenAPI/typegen đồng bộ, không migration/truncate/đổi dữ liệu. API kiểm POST/PUT ở100/101/200; invalid không tăng version/không tạo hoặc sửa một phần, model vẫn200. Malformed JSON được kiểm400. Các test stale409/publish422/quyền/cache/quota/atomic composition vẫn chạy.

**Lệnh và kết quả thực chạy:**

| Cwd | Kiểm | Kết quả |
|---|---|---|
| worktree cô lập | dotnet test UnitTests/IntegrationTests/ArchitectureTests -c Release --no-restore |79/79 unit,122/122 integration,3/3 architecture; TRX dưới artifacts/content-http, không skip |
| workspace G04 | dotnet test tương ứng -c Release --no-restore |101/101 unit,185/185 integration,3/3 architecture; TRX dưới tmp/g04/content-http, không skip |
| cả hai source | dotnet format backend/CulinaryBlog.slnx --no-restore --verify-no-changes |exit0; giữ đầy đủ gate, không thay cấu hình analyzer |
| frontend cả hai source | npm run generate:api, format, lint, typecheck, build, test, budget:check |exit0; main110/110, G04 local126/126; mọi route dưới200KB gzip; Node22.23.3 |
| workspace | python verify-inventory.py; python verify-decisions.py tại thư mục điều phối |347 acceptance/209 contracts/47 operations;3 checksum phần baseline gốc giữ nguyên;2 source issues SLA/scaling của G01 còn mở ngoài G04 |
| workspace | kiểm tamper in-memory của verifier |sửa nội dung baseline hoặc dòng dẫn ADR-011 đều bị từ chối; không hạ gate để bỏ qua file thay đổi |
| worktree cô lập | git diff --cached --check, pre-push backend/frontend format |exit0; chỉ stage21 file được review; root index giữ rỗng |

Lượt đầu integration bị sandbox chặn Docker pipe, chưa chạy assertion API; chạy lại với quyền Docker vào test containers riêng. Lượt main sau đó121/122 do test biên mới dùng chung quota auth với nhóm quyền; tách fixture của test mới, không tăng permit limit, không thay ngưỡng hoặc skip. Lượt cuối122/122. Lint G04 phát hiện getter trùng và helper history không dùng trong harness UX do các lượt trước tạo; chỉ bỏ hai phần dư này,126 test giữ nguyên. Các log integration thất bại được giữ để truy vết, không báo PASS thay chúng.

Commit [a22881794e24f7da656aa1f7c69819d99dee147d](https://github.com/Cookie1109/culinary_blog/commit/a22881794e24f7da656aa1f7c69819d99dee147d) — `Dong bo ma loi va rang buoc nghiep vu theo SRS` đã push main theo ủy quyền. [CI40420863738](https://github.com/Cookie1109/culinary_blog/actions/runs/40420863738): backend/frontend/security/compose success;3 architecture+79unit+122integration,110frontend/audit0. dependency-review là job chỉ chạy PR nên skipped ở push; không phải bỏ qua test thất bại. Không thay workflow hoặc dependency để vượt gate.

Giữ bằng chứng đã có của E19/E21/E25/E26/E27 cho media/private/codec/Google Rich Results/sitemap/SEO/search; HTTP regression chung được gom ở trên. Search chỉ khớp tên món và thay ping qua robots là chỉ đạo đã ghi nhận của người dùng, không tự gọi chúng là nguyên văn PDF. Ba quyết định PUT/sort/Instructions và đính chính A/B/C giữ nguyên. Không test CUD/login admin trên production, không đổi Render Env hoặc chạy migration/seed trên Neon. Các migration/schema của snapshot G04 local chưa được tự rollout cùng commit main này; CI/main và snapshot local được ghi riêng trong evidence/manifest.

**Xác minh publish cuối:** đính chính số trang quy tắc nguyên liệu là **trang35** trong một dòng tài liệu, commit [e6107839283767594c598f84e70dbf139a626655](https://github.com/Cookie1109/culinary_blog/commit/e6107839283767594c598f84e70dbf139a626655); backend/frontend code không đổi so a228817. [CI40422043271](https://github.com/Cookie1109/culinary_blog/actions/runs/40422043271) của đúng main này cũng backend/frontend/security/compose success, conditional dependency-review skipped đúng trigger push. Vercel status success.

Public GET sau auto-deploy xác nhận Render release đúng **e610783**,ready200. **8/8 probe đạt**: q ngắn422, sort không hợp lệ422, maxTime sai kiểu400, tìm ga14 kết quả đều khớp tên, Vercel home/search200 không có error page. Các lần probe trước được giữ: một lượt dùng nhầm route list thay route `/api/v1/recipes/search`; lượt đúng route nhưng Render còn f6ec7bf trả400 cũ. Không gọi hai lượt đó là PASS của source mới; artifact cuối mới chứng minh mapping đang chạy. Không thực hiện authenticated POST/PUT/DELETE trên production. Không cần thao tác Render thủ công cho bản sửa này.

G04 local15/15 không tự đóng G01/G05/G06/G07 hoặc SLA/scaling còn mở. Snapshot root vẫn giữ thay đổi G04 chưa publish/migration chưa được duyệt; commit main chỉ gồm bản sửa đã chọn riêng. Manifest ghi rõ source local, source publish, BUILD_ID và phạm vi evidence Google cũ; không gán Google crawl cũ cho build mới. Không còn bước local độc lập nào đang mở trong G04 theo các chỉ đạo đã ghi nhận.
