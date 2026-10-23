# Sổ quyết định, mâu thuẫn và sai khác SRS

Ngày review: **23/10/2026, Asia/Bangkok**. Phạm vi **D0-02, D0-03, D0-04**. Đây là hồ sơ đối chiếu/quyết định cho các phase tiếp theo; chưa sửa API, schema, runtime hoặc môi trường đang chạy.

## Nguồn, thẩm quyền và trạng thái

- Nguồn chuẩn: [SRS 71 trang](../SRS_Culinary_Blog_v1.0.0.pdf), SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`. Trang bên dưới là trang PDF tính từ 1.
- Baseline hiện có: [Conformance](../phase-0/00_SRS_Conformance_and_Resolution.md) (C), [Decision Log](../phase-0/02_Decision_Log.md) (L), [Product Baseline](../phase-0/01_Product_Baseline.md) (P), [Data Dictionary](../phase-0/03_Data_Dictionary.md) (D), [Error Catalog](../phase-0/04_Error_Catalog.md) (E), [Routes](../phase-0/05_Routes_and_Wireframes.md) (R), [Test Strategy](../phase-0/09_Test_Strategy.md) (T), [ADR](../phase-0/adr/). [README baseline](../phase-0/README.md) ghi project directive ngày 11/09/2026 và ADR-010 amendment 04/10/2026. Giữ hiệu lực được ghi trong repo; không yêu cầu duyệt lại 33 GAP chỉ vì người duyệt cá nhân chưa được gán tên.
- Đầu vào: [inventory](acceptance-inventory.md), [report01](reports/phase-01-result.md), [checklist](../srs-completion-checklist.md), [điều phối](README.md). Source evidence `E-AUTH`…`E-OPENAPI` dùng định nghĩa trong inventory, **không phải test đã chạy**. Phiên bản/source fingerprint và lệnh kiểm mới nằm trong [report02](reports/phase-02-result.md).
- **Thẩm quyền hiện tại:** người dùng xác định PDF là tiêu chuẩn và yêu cầu “Thế chỉnh lại các gói ABC đi”. Theo [bản đính chính](approvals/2026-10-23-abc-correction.md), toàn bộ hiệu lực thay yêu cầu gốc của APP-2026-10-23-ABC được thay thế. Không dùng phê duyệt cũ để miễn acceptance PDF.
- **Theo SRS**: thực hiện yêu cầu gốc, chưa phải runtime pass. **Chờ làm rõ SRS**: thiếu quy tắc hoặc có mâu thuẫn trong nguồn; không tự chọn theo code đang có. **Giữ baseline/Làm rõ/Không đổi** ở các mục ngoài A/B/C là kết quả review lịch sử, không chứng nhận khớp nguyên văn PDF; nếu xung đột PDF phải ghi mở và đối chiếu trước nghiệm thu.
- Giữ lịch sử [APP-2026-10-23-ABC](approvals/2026-10-23-abc.md) và baseline cũ để truy vết, không xóa phản hồi người dùng. Các quyết định hiện tại nằm ở từng DEC bên dưới. Không thay runtime, migration hoặc triển khai ở lượt đính chính.

## Rà đủ GAP-001–033 (D0-02)

Mỗi dòng dưới là review mới đối với **quyết định cũ**. Trạng thái `Giữ` nghĩa giữ Accepted/Amended của baseline, **không phải pass implementation**. Chỉ rõ nơi có sai khác hoặc cần kiểm tiếp. Mã acceptance/CT dẫn về inventory; owner là phase thực thi, không đổi bảng phân công gốc.

| GAP / trang PDF | Quyết định hiện có và nguồn | Đối chiếu hiện trạng / ảnh hưởng / kiểm tiếp | Acceptance / owner / kết luận |
|---|---|---|---|
| GAP-001 /10,12,17–39 | C/L/P:34FR;27 là lỗi đếm | Inventory độc lập đã có34FR, không bỏ bảy mã; không đổi số FR do password bổ sung | Toàn FR;01/37; Giữ |
| GAP-002 /18–37,61–67 | C/L/ADR-002:validation/domain precondition400 | E-OBS GlobalExceptionHandler/catalog; test malformed/field/query/publish không 422 | CT-ERR-19/23;04/22/24; Giữ |
| GAP-003 /30–31,67–68 | C/L/ADR-002/003:missing If-Match400, stale409 | E-CONTENT ETag/expectedVersion; kiểm hai writer và child version, không dùng422/428 | FR-RCP-004/A03–04;17/22; Giữ |
| GAP-004 /17–18,58–59,61 | C/L/D:displayName, email, password;201 auto-login session | AuthContracts/Register endpoint có session; schema AT/RT response còn DEC-24 | FR-AUTH-001/A01;07/22; Giữ |
| GAP-005 /19–20,47,61 | C/L/ADR-005:Auth.js code+PKCE/callback, backend verify idToken | E-AUTH callback/service có; token lifecycle G02 còn mở; lỗi provider làm rõ DEC-16 | FR-AUTH-003;08/10; Giữ |
| GAP-006 /18–21,41,59–60 | C/L/ADR-004:RT512-bit, SHA256, FamilyId, rotation,7 ngày | Token/refresh source có; cần race/reuse/restart test, không chỉ kiểm entropy | NFR-SEC-002;08/09; Giữ |
| GAP-007 /21–22,47,62 | C/L/ADR-004:RT body, bearer optional; valid bearer phải khớp owner | Logout endpoint không đưa principal vào command; không coi anonymous route đủ owner check; DEC-16 chi tiết status | FR-AUTH-005/A04–05;09; Giữ |
| GAP-008 /32–33,43,54,64 | C/L/P/ADR-001:Recipe soft-delete, không purge v1 | DeleteRecipeCommand/global filters; giữ child/object khi recipe delete, khác image delete riêng | FR-RCP-007;17/19; Giữ |
| GAP-009 /26–27,54,63 | C/L/P/ADR-001:Category soft-delete khi không còn recipe chưa xóa | Category config/filter và service; thử Draft/Archived cũng cản xóa, không chỉ Published | FR-CAT-005;17; Giữ |
| GAP-010 /31,67–68 | C/L/P:publish cần ingredient+step, không cần ảnh | Domain.Publish/count/stepNumber; test thiếu từng loại, Published không xóa thành phần cuối | FR-RCP-005;17; Giữ |
| GAP-011 /32,64 | C/L:unarchive supporting endpoint→Draft | ContentEndpoints có unarchive; kiểm transition/ETag/privacy, không tự republish | FR-RCP-006, CT-SUP-01;17; Giữ |
| GAP-012 /33–34,64 | C/L:PATCH image metadata, không endpoint /primary riêng | ImageMetadataRequest/route có; test primary nguyên tử và gallery order | FR-RCP-008;19; Giữ |
| GAP-013 /23–37,40,44 | C/L/ADR-007:Redis-backed Output Cache; TTL30/5/15/1phút | Source RedisApplicationCache+IDistributedCache là cache-aside, chưa thấy OutputCache registration; DEC-01 giữ baseline cụ thể | NFR-PERF-003;14/15; Giữ, triển khai còn thiếu |
| GAP-014 /24–29,44,46 | C/L/ADR-007:public Published-only; private /me/recipes | E-CONTENT tách routes; cần actor/cache/ISR/media xuyên suốt, không làm public tùy identity | FR-RCP-001/002, CT-SUP-02/03;11–19; Giữ |
| GAP-015 /30–31,54 | C/L/ADR-003:Version bigint thay bytea Timestamp | BaseEntity/EF Recipe config concurrency token; cần atomic update/race, không chỉ body version | CT-DATA-BASE-05;17/22; Giữ |
| GAP-016 /36–37 | C/L/ADR-008:simple+unaccent, weighted tsvector/GIN, pg_trgm fallback | E-CONTENT search query/repository và migration; test pho/phở, prefix, rank, index, không raw SQL | FR-SRCH-001;18/30; Giữ |
| GAP-017 /35–36,57,65 | C/L/D:title, description, timerMinutes; server append number, PUT reorder | StepWriteRequest/config có; optional PUT semantics cần DEC-13, không đổi về DurationMinutes | FR-RCP-010;17/22; Giữ |
| GAP-018 /35,57,65 | C/L/D:orderIndex cho ingredient/image | DTO/config có; test sorting/default0/negative/normalize khi reorder | CT-DATA-ING-06/IMAGE-07;17/19; Giữ |
| GAP-019 /25,29,55,58 | C/L/D:category name2–100, cookTime≥0, prepTime>0 | Validators/check constraints có; test zero cookTime giữ qua UI/URL | CT-DATA-CAT-01/RCP-05/06;17/18; Giữ |
| GAP-020 /23,59,62 | C/L/D:profile displayName/avatarUrl/bio; không sửa email/username | Presence flags+DTO có; test omit vs null và invalid không partial update | FR-AUTH-007;07; Giữ |
| GAP-021 /28–29,47,61 | C/L/E:data resource, data/meta list | DTO PageEnvelope có; empty hint DEC-18, query defaults/alias DEC-15, không thêm items wrapper | CT-HTTP-04, CT-SCHEMA-07;18/22; Giữ |
| GAP-022 /13,29,38 | C/L/ADR-006:bucket private, ObjectKey identity, controlled delivery | Source object keys/media route có; URL nullability DEC-22, unpublish phải chặn public media | CT-DATA-IMAGE-02, CT-SUP-07;19; Giữ |
| GAP-023 /16,38–39,48 | C/L/ADR-009:HangfirePG+transactional outbox, idempotent dispatch | WelcomeEmailOutbox/MediaOutbox/job có; không hứa SMTP exactly-once; các outbox use case khác cần rà | FR-JOB-001/002;06/19/20/35; Giữ |
| GAP-024 /14,49 | C/L/T:Chrome112, Firefox113, Edge112, Safari16; mobile theo platform minimum | Source/browser tests không thay device thật; DEC-06 không đổi thành latest-only | CT-HW-02;28; Giữ |
| GAP-025 /1,9,13–14,50 | C/L/ADR-010:.NET10, Next15, Node22.23.3; pin patch/digest | package engines22.23.3 nhưng PATH22.18/.mise20.19.5; không nâng Next16; D1 xác minh toolchain | CONS-003, CT-ARCH-15;03/29; Giữ Amended04/10/2026 |
| GAP-026 /14–15,43–44 | C/L:Domain chỉ BCL; FluentValidation ở Application | E-ARCH project/DI; cần architecture và direct handler validation | CONS-001/008;04/29; Giữ |
| GAP-027 /40–42 | C/L:7NFR-SEC, số6 lỗi đếm | Inventory có SEC001–007, không bỏ secrets requirement | NFR-SEC-001–007;01/37; Giữ |
| GAP-028 /52,58 | C/L/D:RecipeImage bảng riêng1:N | EF RecipeImageConfiguration/snapshot có; test FK/unique primary/filter | CT-DATA-IMAGE-01;19/22; Giữ |
| GAP-029 /28,37,63 | C/L/D:hợp nhất category/difficulty/cook/servings/prep filters AND | Query DTO/validators có; sortBy aliases DEC-15 và testcase zero/boundaries | FR-SRCH-002;18; Giữ |
| GAP-030 /35,57 | C/L/D:ingredient name1–200 | DTO/validator/EF200; test1/200/201/blank, không tự siết100 | CT-DATA-ING-02;17; Giữ |
| GAP-031 /29–30,54–55,68 | C/L/D:slug collision suffix,409 race chưa resolve | Service allocate slug retry có; vẫn thiếu history/301 của slug Draft, không coi suffix đủ SEO | FR-RCP-003/A05, NFR-SEO-004;17/21; Giữ |
| GAP-032 /52,54,58–59 | C/L/D:Identity string key, User không BaseEntity, audit riêng | Source IdentityUser<Guid>/FKuuid khác PDF; DEC-20 trả về Identity string, ngoại lệ Guid hết hiệu lực | CT-DATA-USER-01/RCP-11/RT-01;22; Theo SRS/string tại DEC-20 |
| GAP-033 /7,44 | C/L:không rating giả ngoài scope | SEO/JSON-LD tests hiện có; dữ liệu thiếu phải omit, Rich Results chưa kiểm lại | NFR-SEO-001;21; Giữ |

Các quyết định bổ sung ở **C §3** cũng có hiệu lực: `/health` public aggregate-only, chi tiết ops riêng; không bật `VerifiedAuthor` trong v1 khi chưa có confirmation lifecycle; Admin audit view khác raw infrastructure access; retry sitemap2/welcome1-5-30phút/resize-delete3; Should giữ đúng MoSCoW. Đây là điểm inventory trước chưa dẫn chiếu đủ, không phải quyết định mới.

## Quyết định bổ sung và cách nghiệm thu

### DEC-01 — Output Cache và cache-aside

- **Task/acceptance/nguồn:** D0-03; NFR-PERF-003/A02–03, CT-ARCH-10; SRS tr.28 “Output Cache”, tr.40 “cache-aside pattern”; C GAP-013 ghi “Redis-backed Output Cache”.
- **Hiện trạng:** E-CACHE dùng IDistributedCache/RedisApplicationCache và CachingBehavior; source search không thấy AddOutputCache/UseOutputCache/AddStackExchangeRedisOutputCache. ADR-007 nói shared Redis chung hơn C, không tự phủ định cơ chế cụ thể.
- **Phương án ưu tiên:** giữ Redis-backed HTTP Output Cache cho public responses theo C. Query cache hiện có được giữ khi còn use case cần, nhưng phase14 phải chỉ rõ tầng nào sở hữu TTL/tag cho từng resource; tránh một entry stale phục hồi entry tầng khác. Không gọi cache-aside hiện tại là đã đạt Output Cache.
- **Lý do/tác động:** giữ accepted contract, TTL category1800/detail300/list900/search60 giây. Private/auth/no-store không shared-cache; error và response chứa user-state không được cache. Không đổi API/data schema; cấu hình/policy/invalidation thay ở phase14–15.
- **Kiểm tiếp:** HTTP/Redis TTL/hit, tag invalidation sau commit, header/privacy, Redis down, hai instance và backend→Next. Microsoft hướng dẫn dùng store OutputCache riêng, không đồng nhất AddStackExchangeRedisCache với AddStackExchangeRedisOutputCache [W2].
- **Trạng thái:** Giữ baseline; không cần duyệt lại để thực hiện đúng GAP-013. Nếu muốn chỉ giữ cache-aside, phải có CR riêng chưa có ở đây. Owner14/15/30.

### DEC-02 — Sitemap ping Google

- **Task/acceptance/nguồn:** D0-03; FR-JOB-003/A06, NFR-SEO-003/A04, CT-EXT-07; SRS tr.39/45/48 yêu cầu ping sau khi cập nhật sitemap.
- **Theo nguồn:** giữ yêu cầu ping trong danh mục nghiệm thu; sitemap/robots/Search Console không tự thay thế bước này.
- **Vướng mắc:** Google xác nhận endpoint đã ngừng và trả404 [W1], kiểm lại ngày23/10/2026. Không thể chứng nhận ping thành công; không gọi404 hoặc bỏ bước là đạt. Phương án thay thế trước đây đã hết hiệu lực.
- **Tác động/kiểm tiếp:** phase20/21/38 ghi rõ phần sitemap/XML/robots có thể kiểm độc lập và phần provider chưa thể đạt nguyên văn. Chỉ thay tiêu chuẩn khi có đính chính SRS riêng; không thêm request lỗi thời để làm checklist xanh.
- **Trạng thái:** Chờ làm rõ SRS — góiB; xử lý yêu cầu provider đã ngừng còn mở.

### DEC-03 — .NET static analysis

- **Task/acceptance/nguồn:** D0-03; NFR-MAINT-001/A01; SRS tr.43 “SonarAnalyzer, StyleCop, EditorConfig”.
- **Hiện trạng:** Directory.Build.props bật built-in analyzers/latest-recommended, TreatWarningsAsErrors, code style; Directory.Packages.props chưa kê SonarAnalyzer/StyleCop. Không có căn cứ gọi các bộ này là cùng ruleset.
- **Phương án ưu tiên:** đáp ứng các công cụ nguyên văn ở phase29; pin package/rules, cấu hình theo scope, review violation thực. Không thêm analyzer/refactor ở phase02. Không chấp nhận blanket suppressions chỉ để gate xanh.
- **Tác động/kiểm tiếp:** csproj/package/CI/rules có thể đổi, không đổi API/data. Build+format/static gate và negative proof gate phải chạy ở phase03/29; reviewer thật của PR vẫn là điều kiện riêng.
- **Trạng thái:** Theo SRS, không miễn toolchain. Muốn thay bằng built-in cần CR so sánh rules/risks có approval chưa có. Owner29.

### DEC-04 — ESLint ruleset

- **Task/acceptance/nguồn:** D0-03; NFR-MAINT-001/A02; SRS tr.43 “ESLint (Airbnb ruleset), Prettier”.
- **Hiện trạng:** eslint.config.mjs dùng JS/TS recommended+React/hooks/a11y và có các a11y rules disabled; không phải Airbnb. Prettier đã có script nhưng chưa chạy lần này.
- **Phương án ưu tiên:** giữ gate Airbnb theo SRS; phase29 kiểm khả năng cấu hình tương thích phiên bản hiện có và mapping rules, tránh ép downgrade framework ngoài scope. Nếu cần thay ruleset vì compatibility, đưa CR cụ thể ở checkpoint phase29; không được mặc nhiên miễn vì config hiện tại chạy được.
- **Tác động/kiểm tiếp:** lint config/lockfile/CI, rà exemptions có lý do và expiry; lint/format/build/browser a11y. Disabled lint không miễn WCAG/NVDA/VoiceOver.
- **Trạng thái:** Theo SRS; chưa có waiver Airbnb. Owner03/27/29.

### DEC-05 — Công cụ frontend test và E2E

- **Task/acceptance/nguồn:** D0-03; NFR-MAINT-002/A02–05; SRS tr.43 “Jest + Testing Library”,“Playwright”.
- **Hiện trạng:** npm test là Node runner+JSDOM, không có nghĩa đã có Jest/Testing Library hoặc5 browserE2E. Không bỏ các test hiện có.
- **Phương án ưu tiên:** giữ yêu cầu test tools nguyên văn; phase24/29 bổ sung cấu hình/gate phù hợp, tái dùng case behavior đang có, giữ source checks khi có giá trị. Không rewrite test chỉ đổi tên tool; không lấy integration coverage thay Application unit≥80%.
- **Tác động/kiểm tiếp:** package/CI/test harness; happy+error mỗi endpoint và5 Playwright flows trên backend riêng. Muốn công nhận Node/JSDOM thay Jest/TL phải có CR compatibility/equivalence riêng, chưa có.
- **Trạng thái:** Theo SRS; không công nhận test count cũ là E2E. Owner23–29.

### DEC-06 — Browser minimum

- **Task/acceptance/nguồn:** D0-03; CT-HW-02, NFR-USE-001/002; SRS tr.14 so49; GAP-024/T.
- **Quyết định giữ:** Chrome112+, Firefox113+, Edge112+, Safari16+; mobile Chrome90+/iOSSafari14+ theo platform; không IE. Khi cùng chạy trên platform đòi mức cao hơn thì dùng mức đó, không tự hạ Safari desktop16 vì mobile14.
- **Lý do/tác động:** đã có Accepted; không cần chốt lại thành latest-only. Chọn browser hiện đại cho smoke không chứng minh minimum cũ. Device thiếu ghi chờ môi trường, không giả PASS.
- **Kiểm tiếp:** phase28 ghi OS/browser version/viewport/device/keyboard/NVDA/VoiceOver thật, tách automated browser với Safari thật; không thêm BrowserStack subscription lúc inventory.
- **Trạng thái:** Giữ baseline. Owner28.

### DEC-07 — Singleton sitemap và RedLock

- **Task/acceptance/nguồn:** D0-03; NFR-SCALE-001/A03, FR-JOB-003/A05; SRS tr.44 nêu distributed lock (RedLock).
- **Theo nguồn:** dùng RedLock cho tác vụ singleton. PostgreSQL shared queue và Hangfire multiple workers vẫn là yêu cầu riêng; lock PostgreSQL/DisableConcurrentExecution không thay RedLock.
- **Hiện trạng/tác động:** source hiện có chưa chứng minh RedLock. Bỏ ngoại lệ lock PostgreSQL; không suy hiệu lực khóa từ attribute hoặc tên thư viện. Giữ bảo vệ artifact khi worker lỗi như yêu cầu correctness, không tự tuyên bố exactly-once.
- **Kiểm tiếp:** phase20/35 kiểm RedLock thực, hai worker, overlap/retry, crash/disconnect và publication không ghi đè artifact mới. Thay đổi code/hạ tầng thuộc lượt thực thi.
- **Trạng thái:** Theo SRS — góiB; ngoại lệ cũ hết hiệu lực, runtime còn mở.

### DEC-08 — Uptime và lỗi quy đổi

- **Task/acceptance/nguồn:** D0-04; NFR-REL-001/A01; SRS tr.43 ghi “uptime ≥99.5% (≈3.65 giờ downtime/năm)”.
- **Làm rõ:** 99,5% trong 365 ngày cho downtime tối đa **43,8 giờ**; năm 366 ngày là 43,92 giờ. Ngược lại, 3,65 giờ trong năm 365 ngày tương ứng uptime khoảng **99,958333%**. Không báo hai ngưỡng này tương đương.
- **Hướng xử lý:** giữ nguyên hai phát biểu ≥99,5% và ≈3,65 giờ/năm để đối chiếu tại DEC-09; chưa chọn target ưu tiên. Không tự đổi SLA thành 99,95% hoặc 99,99%.
- **Tác động/kiểm tiếp:** giải thích acceptance/report và kiểm số học bằng Decimal; chưa có phép đo uptime. Nếu chọn target 3,65 giờ thì phải ghi riêng target/cost mới.
- **Trạng thái:** Làm rõ số học; mâu thuẫn target/cửa sổ nghiệm thu còn mở tại DEC-09. Owner36/38.

### DEC-09 — Cửa sổ SLA, downtime và maintenance

- **Task/acceptance/nguồn:** D0-04; NFR-REL-001/A01–04; SRS tr.43 ghi ≥99,5% và ≈3,65 giờ/năm, probe10 giây, alert down>1 phút, maintenance báo trước48 giờ.
- **Theo nguồn:** giữ cả hai phát biểu uptime trong hồ sơ và chỉ rõ chúng không tương đương (DEC-08). Chưa chọn một số để bỏ số còn lại. Probe/alert/banner vẫn phải đáp ứng nguyên văn.
- **Rút tiêu chí tự thêm:** 30 ngày UTC, budget216 phút và cách tính maintenance/missing intervals không còn là protocol đã thống nhất. Các phép tính cũ chỉ là minh họa số học, không phải cửa sổ nghiệm thu SRS.
- **Tác động/kiểm tiếp:** phase36/38 cần chốt cách giải mâu thuẫn, kỳ đo, timezone và cách xử lý maintenance/thiếu evidence trước khi kết luận SLA. Giữ raw probes/version/timestamps; không biến thiếu dữ liệu thành pass hoặc vài ngày đo thành uptime năm.
- **Trạng thái:** Chờ làm rõ SRS — góiC; D0-04 mở, không tự chốt target/cửa sổ.

### DEC-10 — Backup 03:00 và múi giờ

- **Task/acceptance/nguồn:** D0-04; NFR-REL-003/A02–03; SRS tr.43 không nêu múi giờ nhưng **T §4 đã chốt03:00 UTC**, giữ30 ngày.
- **Quyết định giữ:** backup hằng ngày03:00 UTC, tức10:00 Asia/Bangkok; khác sitemap02:00 UTC. Không cần duyệt lại lựa chọn UTC.
- **Hiện trạng:** compose.reliability.yaml chạy backup rồi sleep86400 giây. Timestamp `date -u` không neo lịch; restart và thời gian job làm lịch drift.
- **Tác động/kiểm tiếp:** phase34 sửa scheduler đúng giờ UTC, kiểm restart/retry/retention/partial→complete/checksum và restore vào target riêng. Đo RPO/RTO thực; daily backup không tự tạo cam kết RPO/RTO chưa được chốt. Không restore stack người dùng.
- **Trạng thái:** Giữ baseline; implementation/restore evidence còn mở. Owner34.

### DEC-11 — Throughput, degradation và scaling tuyến tính

- **Task/acceptance/nguồn:** D0-04; NFR-PERF-001/002, NFR-SCALE-001–003; tr.40/44 yêu cầu ≥100 concurrent users, single instance2vCPU/4GB, không degradation và thêm instance tăng tuyến tính.
- **Theo nguồn:** giữ p50 cached GET≤150ms, p95 mọi API≤500ms, p99≤1000ms, cache warm hit≥80%; smoke→load→stress bằng k6, đo production với tải thực theo SRS§4.1. Giữ pool max100 connections/instance và topology production tại DEC-12.
- **Rút tiêu chí tự thêm:** workload80%GET/20%write, warmup5 phút+steady15 phút, unexpected errors≤1%, efficiency≥90%, B2≥1,8×B1/B3≥2,7×B1 không còn là điều kiện nghiệm thu. Không dùng các ngưỡng đó để kết luận đạt yêu cầu không degradation/tăng tuyến tính.
- **Tác động/kiểm tiếp:** phase30/35 ghi tải/dataset/tài nguyên/throughput và lỗi thực theo từng số instance; chốt protocol đo, thời lượng và cách đánh giá tuyến tính/không degradation trước khi nghiệm thu. Chưa tự đặt tolerance mới, staging không thay production evidence.
- **Trạng thái:** Chờ làm rõ SRS — góiC; protocol còn mở, các ngưỡng rõ trong PDF vẫn bắt buộc.

### DEC-12 — Phạm vi production scaling

- **Task/acceptance/nguồn:** D0-04; NFR-SCALE-003/A01–04, CT-HW-01; tr.44 có MinIO4+ nodes, CDN và upstream nhiều API.
- **Phương án ưu tiên:** giữ toàn bộ production acceptance: service containers riêng, nhiều API qua load balancer, MinIO distributed≥4 nodes, CDN cho `_next/static`. Initial scale nhỏ không tự miễn topology. Read replica và partition>1 triệu rows vẫn là optional/nâng cao.
- **Hiện trạng/tác động:** production Compose/Nginx mô tả service API/MinIO đơn; template không chứng minh deployment đã scale. Phase35 có thể kiểm topology riêng khi được giao; không mua cloud, provision hoặc deploy trong lượt này. Nếu bàn giao single-node cần CR riêng, chưa có waiver ở đây.
- **Kiểm tiếp:** topology/node count/failure/CDN response, nguồn lực thực và benchmark DEC-11. Tách “có thể scale”, “đã thử staging” và “đã triển khai production”.
- **Trạng thái:** Theo SRS; protocol DEC-11 còn chờ làm rõ, production evidence còn mở. Owner35/38.

### DEC-13 — PUT optional, omit và null (INV-G01)

- **Task/acceptance/nguồn:** D0-02; CT-API-17/26/29; SRS tr.63/65 đánh field PUT recipe/step/ingredient là optional; category Name không đánh optional.
- **Theo nguồn:** giữ method/path và tính optional đã ghi; không ép payload tạo mới cho PUT một field. Quyền, validation và concurrency vẫn phải kiểm.
- **Phần chưa rõ:** PDF không quy định đầy đủ omit/null, empty object và merge nutrition. Rút hiệu lực gói quy tắc omit-giữ-cũ/null-clear/empty400 trước đây; không gán những quy tắc đó thành nguyên văn SRS.
- **Tác động/kiểm tiếp:** phase17/22 chốt riêng bảng omit/null/value và validation sau cập nhật trước khi đồng bộ DTO/OpenAPI/types/test; không tự chọn cách làm mất dữ liệu. Chưa sửa code trong lượt này.
- **Trạng thái:** Chờ làm rõ SRS — góiA; optional theo PDF, chi tiết cập nhật còn mở.

### DEC-14 — Composition khi tạo Recipe (INV-G02)

- **Task/acceptance/nguồn:** D0-02; FR-RCP-003/A06, CT-API-16; tr.29–30 và OpenAPI có optional steps/ingredients.
- **Phương án ưu tiên:** thực hiện contract hiện có: tạo Recipe cùng steps/ingredients/nutrition trong một transaction; server gán step number; lỗi child rollback tất cả. Wizard lưu từng bước sau Draft vẫn giữ nguyên.
- **Hiện trạng/tác động:** RecipeWriteRequest không nhận arrays, service create chưa tạo composition. Phase17/22 bổ sung DTO/handler/test, không xóa field khỏi OpenAPI để che thiếu implementation.
- **Kiểm tiếp:** nested POST/readback, invalid child không Recipe/job/invalidation; arrays rỗng vẫn tạo Draft, publish invariant không đổi.
- **Trạng thái:** Theo SRS/OpenAPI; không cần waiver để thực hiện. Owner17/22.

### DEC-15 — Query sort và defaults (INV-G03)

- **Task/acceptance/nguồn:** D0-02; CT-QUERY-01/03, CT-API-09; tr.37 có sort và default page1/pageSize12/max50; tr.61/63 dùng sortBy/sortOrder, ví dụ pageSize10 không phải default.
- **Theo nguồn:** giữ các dạng tham số được PDF liệt kê theo endpoint; sort với tiền tố - và mặc định-createdAt theo tr.37, search theo relevance. Không bỏ sortBy/sortOrder chỉ vì code hiện tại dùng sort.
- **Phần chưa rõ:** cách nhận đồng thời hai kiểu sort, trường thiếu, allowlist từng endpoint chưa được PDF xác định đủ. Rút quy tắc alias phải đủ hai field và conflict400 đã thêm; chưa chọn precedence mới.
- **Tác động/kiểm tiếp:** phase18/22 chốt bảng query hợp lệ/xung đột, đồng bộ validation/cache keys/docs/types; kiểm default12, explicit10, filters AND và thứ tự query không gây kết quả ngầm khác nhau.
- **Trạng thái:** Chờ làm rõ SRS — góiA; giữ các tham số trong PDF, conflict policy còn mở.

### DEC-16 — Auth/provider/logout status (INV-G04)

- **Task/acceptance/nguồn:** D0-02; FR-AUTH-003/A05–06,004/A07,005/A05; CT-ERR-07/08; tr.20/21/61/68; ADR-002/E/GAP-007.
- **Làm rõ baseline:** Google idToken invalid/expired/unverified400 theo catalog; Culinary bearer missing/invalid401; dependency unavailable503 theo ADR-002. Inactive user403 theo catalog, refresh user deleted/locked401 theo FR; local login lockout423 giữ riêng. Không gộp disabled với lockout.
- **Logout:** valid bearerA+RTB trả403 theo quyền ADR-002, không revokeB. Access token expired vẫn cho revoke bằng RT hợp lệ; RT giá trị không tồn tại/revoked và request lặp vẫn204. Thiếu field body là400 validation, khác token có giá trị nhưng không tìm thấy. Bearer sai chữ ký không được coi như token chỉ hết hạn; phase09 phải kiểm authentication context phù hợp.
- **Hiện trạng/tác động:** runtime map Google unavailable503/invalid400; OpenAPI502/Produces401 chưa đồng bộ; logout chưa nhận principal. Owner08/09/10/22 sửa và kiểm ở phase thực thi.
- **Kiểm tiếp:** từng cause có status/code/body/side-effect assertion, provider fake/real tách riêng, browser không loop và không lộ credential/config.
- **Trạng thái:** Làm rõ theo ADR/catalog/GAP-007, không đảo accepted mapping; không coi anonymous route đủ owner check.

### DEC-17 — Health response công khai (INV-G05)

- **Task/acceptance/nguồn:** D0-02; FR-OBS-001/A04, CT-API-31; tr.65 có entries nhưng **C §3 chốt public aggregate-only**.
- **Quyết định giữ:** public `/health` chỉ status; chi tiết dependency qua internal/ops quyền/network phù hợp. Liveness kiểm process, readiness kiểm DB+Redis, giữ200/503/probe semantics.
- **Hiện trạng/tác động:** HealthResponseWriter đang trả status đúng shape baseline. Không thêm endpoint ops hoặc gọi dependency runtime đã pass chỉ từ source. Inventory chỉ bổ sung reference.
- **Kiểm tiếp:** schema/redaction, DB/Redis/MinIO failure matrix, health không quota, ops access khi có.
- **Trạng thái:** Giữ baseline; INV-G05 đã giải đáp quyết định, runtime còn mở. Owner22/33.

### DEC-18 — Search rỗng và gợi ý (INV-G06)

- **Task/acceptance/nguồn:** D0-02; FR-SRCH-001/A05; tr.37 A2 yêu cầu HTTP200 với items=[] và message gợi ý; tr.47/61 mô tả wrapper data/meta.
- **Theo nguồn:** response search rỗng phải có danh sách rỗng và message gợi ý; UI có thể hiển thị message nhưng không thay sự hiện diện của message trong API.
- **Tác động:** bỏ ngoại lệ chỉ có gợi ý ở UI. Khi đồng bộ wrapper data/meta phải bảo toàn ý nghĩa danh sách rỗng và message của tr.37; không coi response thiếu message là đạt.
- **Kiểm tiếp:** phase18/22 xác minh response schema/message và empty-state browser; nếu cần chốt vị trí field trong wrapper phải ghi rõ trước triển khai, không xóa yêu cầu message.
- **Trạng thái:** Theo SRS — góiA; API message bắt buộc, runtime chưa kiểm.

### DEC-19 — VerifiedAuthor (INV-G07)

- **Task/acceptance/nguồn:** D0-02; NFR-SEC-006, CT-DATA-USER-07; tr.13 yêu cầu email confirmed; **C §3 đã chốt chưa bật VerifiedAuthor v1**.
- **Quyết định giữ:** AuthorPolicy+active user+ownership; không chặn Author local vừa đăng ký vì chưa confirm email. Google email verification vẫn bắt buộc theo ADR-005. Confirmation lifecycle mới cần CR riêng.
- **Hiện trạng/tác động:** API DI chưa có VerifiedAuthor, đúng quyết định này; không có nghĩa mọi quyền Application đã đạt.
- **Kiểm tiếp:** local register→create/publish, non-owner/Admin/inactive, Google email unverified. Không tạo confirmation API trong phase02.
- **Trạng thái:** Giữ baseline; INV-G07 đã giải đáp quyết định. Owner05/07/10.

### DEC-20 — Identity string và khóa ngoại (INV-G08)

- **Task/acceptance/nguồn:** D0-02; CT-DATA-USER-01/RCP-11/RT-01; tr.55/58–59 yêu cầu `IdentityUser<string>`, Id/AuthorId/UserId varchar(450); GAP-032 cùng hướng.
- **Theo nguồn:** dùng `IdentityUser<string>` và khóa ngoại cùng kiểu varchar(450). Guid/uuid trong source hiện tại là sai khác cần sửa; JSON ID dạng chuỗi không chứng minh kiểu lưu trữ khớp PDF.
- **Tác động:** rút ngoại lệ giữ Guid/uuid. Phase22 phải lập kế hoạch chuyển đổi và kiểm bảo toàn user/role/login/token/recipe/FK, upgrade/rollback trên DB copy; không đổi UUID của các entity mà PDF quy định uuid.
- **Kiểm tiếp:** catalog schema, toàn bộ Identity FK/index và migration readback/upgrade; không migrate dữ liệu đang chạy trong lượt hồ sơ.
- **Trạng thái:** Theo SRS — góiB; giữ string theo GAP-032, runtime còn mở.

### DEC-21 — Instructions NOT NULL và enum smallint (INV-G09)

- **Task/acceptance/nguồn:** D0-02; CT-DATA-RCP-04/08/09; tr.55 yêu cầu Instructions text NOT NULL, Difficulty/Status smallint NOT NULL với default1/0.
- **Theo nguồn:** Instructions text NOT NULL; Difficulty/Status lưu smallint, giữ enum values1..4/0..2 và defaultsEasy1/Draft0. Không coi Instructions nullable theo dictionary cũ hoặc enum integer là khớp PDF.
- **Tác động:** rút ngoại lệ integer và phần giữ nullable đi kèm. Giới hạn10000 ký tự hiện tại không được coi là giới hạn của PDF. Phase22 rà schema/DTO/validation và dữ liệu NULL trước khi thiết kế migration; không tự thay NULL bằng chuỗi rỗng hoặc suy steps thay legacy Instructions.
- **Kiểm tiếp:** physical types/nullability/default/checks, dữ liệu legacy, migration bảo toàn và API contract; backfill cần quy tắc có căn cứ, không chạy trên DB người dùng.
- **Trạng thái:** Theo SRS — góiB; Instructions NOT NULL/smallint là expected, runtime còn mở.

### DEC-22 — OriginalUrl và delivery URL (INV-G10)

- **Task/acceptance/nguồn:** D0-02; CT-DATA-IMAGE-02; tr.58 yêu cầu OriginalUrl varchar(500) NOT NULL; MediumUrl/ThumbnailUrl nullable; tr.64 response upload có originalUrl.
- **Theo nguồn:** giữ OriginalUrl NOT NULL ở schema và originalUrl string trong response hợp lệ; không nới nullable để khớp OpenAPI hiện tại. Medium/thumbnail có thể NULL khi job chưa chạy theo PDF.
- **Tác động:** bỏ việc dùng approvalA để chứng nhận ObjectKey-only thay cột OriginalUrl. ObjectKey/private media là baseline kiến trúc hiện có, chưa đủ chứng minh physical schema tr.58; phase19/22 phải đối chiếu cả storage và API, giữ bảo vệ quyền khi xử lý URL.
- **Kiểm tiếp:** catalog cột/length/nullability và response upload/read/pending/fail/quyền; không tự đổi bucket public hoặc coi nonnullable DTO là migration đã đạt.
- **Trạng thái:** Theo SRS — góiA; giữ ràng buộc gốc, phần storage mapping chưa nghiệm thu.

### DEC-23 — Refresh-token IP và user-agent (INV-G11)

- **Task/acceptance/nguồn:** D0-02; CT-DATA-RT-05/06; tr.60 IPvarchar45, D UserAgentHash64; source IP64/UserAgent raw512.
- **Phương án ưu tiên:** theo D: IP≤45 và UA hash khi chỉ cần trace audit. Migration thêm/backfill hash trước, chỉ bỏ raw column sau khi upgrade/rollback đã review. Không cắt dữ liệu hoặc đổi FamilyId/expiry âm thầm.
- **Tác động/kiểm tiếp:** phase22 khảo sát dữ liệu legacy trên DB copy, không in PII; kiểm IPv4/IPv6/hash/rotation/FK/upgrade. Retention phải theo policy được duyệt, không tự đặt số ngày mới. Ops muốn rawUA/IP64 phải có CR use case/retention.
- **Trạng thái:** Giữ baseline D; chưa có waiver khớp source. Owner22.

### DEC-24 — Response tokens và nutrition schema (INV-G12)

- **Task/acceptance/nguồn:** D0-02; CT-SCHEMA-01/05; GAP-004/021, D có sáu nutrition fields; OpenAPI đánh writeOnly AT/RT response và nutrition mở.
- **Làm rõ:** response phải mô tả AT/RT mà client được nhận theo auto-login contract; bỏ writeOnly ở response schema. Request secrets vẫn writeOnly và không log. Annotation không tự chứng minh codegen đã bỏ token; cần test tool thực [W5].
- **Phương án ưu tiên:** typed nutrition calories/protein/carbohydrates/fat/fiber/sodium nullable, không âm, đơn vị đúng, DBdecimal8,2. Phase22 đồng bộ precision/rounding/validation và omit/null, không biến nutrition hoặc rating thành bắt buộc.
- **Tác động/kiểm tiếp:** OpenAPI/types/examples/contract tests và generated client; không đổi runtime contract đã chốt, không sửa spec/types trong phase02.
- **Trạng thái:** Không đổi hành vi; sửa mô tả để đáp ứng baseline. Owner22.

### DEC-25 — Routes UI và redirect slug

- **Task/acceptance/nguồn:** D0-02; CT-UI-06/07/14, NFR-SEO-004/A03; tr.46–47 liệt kê /auth/login,/auth/register,/search; tr.45 quy định301 khi slug thay đổi.
- **Theo nguồn:** cung cấp màn hình tại /auth/login và /auth/register theo CSR, /search theo SSR và quyền tương ứng. PDF không gọi các route này là legacy aliases.
- **Tác động:** bỏ ngoại lệ auth307/search308 thay route được yêu cầu. Redirect hiện có sang /login,/register,/recipes không tự chứng minh rendering/path theo PDF. Slug cũ→mới vẫn phải301 riêng.
- **Kiểm tiếp:** phase11/21 kiểm production HTTP tại route gốc, rendering, auth state, query/search và slug301; không đổi config hoặc route code trong lượt đính chính.
- **Trạng thái:** Theo SRS — góiA; alias redirects không thay acceptance route gốc.

## Đối chiếu 12 nhóm bàn giao từ inventory

| Nhóm | Decision reference | Kết luận và phần thực thi còn lại |
|---|---|---|
| INV-G01 | DEC-13 | Optional theo PDF; omit/null/empty/merge còn mở;17/22 |
| INV-G02 | DEC-14 | Nested create đã là yêu cầu;17/22 thực hiện nguyên tử |
| INV-G03 | DEC-15 | Giữ tham số PDF/default12; conflict policy còn mở;18/22 |
| INV-G04 | DEC-16 | Theo ADR/catalog/GAP-007;08/09/10/22 kiểm và sync |
| INV-G05 | DEC-17 | C§3 đã chốt;22/33 còn schema/dependency runtime |
| INV-G06 | DEC-18 | API phải có message gợi ý khi rỗng;18/22 |
| INV-G07 | DEC-19 | C§3 đã chốt chưa bật VerifiedAuthor;05/07 kiểm quyền |
| INV-G08 | DEC-20 | Identity string/varchar450, không miễn Guid;22 |
| INV-G09 | DEC-21 | Instructions text NOT NULL, enums smallint theo PDF;22 |
| INV-G10 | DEC-22 | OriginalUrl NOT NULL và response string; rà cả schema/storage;19/22 |
| INV-G11 | DEC-23 | Theo D IP45/UA hash;22 schema/upgrade/retention |
| INV-G12 | DEC-24 | Sync schema đúng contract; không claim codegen failure chưa đo;22 |

## Gói A/B/C sau đính chính

[SRS-2026-10-23-ABC-CORRECTION](approvals/2026-10-23-abc-correction.md) thay hiệu lực cả11 ID của [approval cũ](approvals/2026-10-23-abc.md). Không có miễn trừ PDF còn hiệu lực từ ba gói này. Bảng dưới là chỉ dẫn hiện tại; chi tiết/owner ở từng DEC.

| Gói | IDs | Hướng xử lý hiện tại | Nguồn chỉ dẫn |
|---|---|---|---|
| A — API/UI | DEC-13, DEC-15, DEC-18, DEC-22, DEC-25 | Giữ optional/query của PDF; message trong API; OriginalUrl NOT NULL; route gốc; omit/null và query conflict còn mở | SRS-2026-10-23-ABC-CORRECTION/góiA |
| B — Hạ tầng/dữ liệu | DEC-02, DEC-07, DEC-20, DEC-21 | RedLock; Identity string; Instructions NOT NULL/enums smallint; ping provider đã ngừng ghi vướng mắc | SRS-2026-10-23-ABC-CORRECTION/góiB |
| C — Tiêu chí vận hành | DEC-09, DEC-11 | Giữ ngưỡng rõ trong PDF; bỏ protocol/tolerance tự thêm; SLA và cách đo còn mở | SRS-2026-10-23-ABC-CORRECTION/góiC |

Đính chính không tự xác nhận các baseline ngoài A/B/C khớp nguyên văn PDF, không sửa mã, không nghiệm thu runtime và không cho phép tự chạy phase tiếp theo.

## Nguồn chính thức đã kiểm ngày23/10/2026

| Ref | Nguồn | Phần dùng và giới hạn bằng chứng |
|---|---|---|
| W1 | [Google: Sitemap ping deprecation](https://developers.google.com/search/blog/2023/06/sitemaps-lastmod-ping) | Bài26/06/2023 hiện ghi deprecation complete/404; không tự duyệt waiver SRS |
| W2 | [Microsoft: Output caching](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/output?view=aspnetcore-10.0) | Redis OutputCache khác IDistributedCache; không phải phép kiểm ứng dụng |
| W3 | [Google: Build and submit sitemap](https://developers.google.com/search/docs/crawling-indexing/sitemaps/build-sitemap) | Robots/SC submission chỉ là hint, không đảm bảo indexing; không dùng credential |
| W4 | [Hangfire: Concurrency and rate limiting](https://docs.hangfire.io/en/latest/background-processing/throttling.html) | Giới hạn khi mất connection, cần transaction/CAS/idempotency; không suy Throttling hỗ trợ PostgreSQL |
| W5 | [JSON Schema2020-12 readOnly/writeOnly](https://json-schema.org/draft/2020-12/json-schema-validation#name-readonly-and-writeonly), [OpenAPI3.1.0](https://spec.openapis.org/oas/v3.1.0.html) | Annotation và Schema Object; không khẳng định behavior codegen chưa chạy |

## Bàn giao và kiểm chứng

- D0-02: đã rà đủ33 GAP/12 INV-G; review/ghi nhận vẫn đóng. Đọc đính chính mới thay hiệu lực A/B/C cũ; các khoảng chưa rõ còn mở tại từng DEC.
- D0-03: ghi cache/ping/analyzers/ESLint/frontend tests/browser/lock, có nguồn nguyên văn, phương án, lý do, impact, test và status. Waiver chưa duyệt thì acceptance nguyên văn vẫn mở.
- D0-04: mở lại vì mâu thuẫn uptime, kỳ đo và protocol scaling chưa thống nhất theo PDF. Backup03:00UTC là quy ước T§4, PDF chỉ ghi03:00; không coi UTC là nguyên văn. Topology production giữ theo SRS.
- Owner đọc ID liên quan trước sửa contract; quyết định đang chờ không ngăn công việc độc lập. Không thực thi phase sau trong lượt này.
- Lệnh kiểm tài liệu: `python docs/specs/srs-completion-phases/verify-decisions.py`, verifier inventory và diff check. Application commands có thật ở checklist mục6/package/project nhưng chưa chạy khi chỉ đổi tài liệu; không claim runtime pass.
