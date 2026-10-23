# Checklist và kế hoạch hoàn thiện toàn bộ SRS

Cập nhật phạm vi ngày **23/10/2026** theo yêu cầu người dùng: bổ sung tất cả điểm thiếu, sai khác và mục chưa đủ bằng chứng trong [báo cáo trạng thái](../reports/srs-acceptance-status.md), thay cho checklist chỉ gồm sáu nhóm khắc phục ban đầu. **Đây là kế hoạch thực hiện; việc thêm một mục vào đây không có nghĩa đã triển khai hoặc nghiệm thu mục đó.**

## 0. Giao từng phase nhỏ cho GPT 6.1 Sol

Kế hoạch D0–D10 bên dưới đã được chia thành **38 phase thực hiện**, mỗi phase sở hữu 1–4 task, phần lớn 1–3 task liên quan. Giữ file này làm nguồn yêu cầu và checkbox tổng; dùng các file phase để giao từng lượt, không đưa toàn bộ dự án vào một prompt.

- [Điều phối, thứ tự và quy tắc thực hiện](srs-completion-phases/README.md).
- [Phase 01 — Lập inventory acceptance và contract](srs-completion-phases/phase-01-acceptance-inventory.md): bắt đầu bằng prompt có sẵn trong file này.
- [Tiến độ và owner của đủ 85 task](srs-completion-phases/progress.md).
- [Mẫu report bàn giao mỗi phase](srs-completion-phases/reports/README.md).

Mỗi file phase có prompt cho GPT 6.1 Sol, đầu vào/phụ thuộc, task gốc, vùng mã cần đọc, trình tự, kiểm chứng và điều kiện hoàn thành. Chỉ giao **một phase mỗi lượt**; phase sau đọc report phase trước. Thiếu provider/thiết bị/staging thì giữ mục đó mở và làm phần độc lập; không tự chạy phase tiếp theo hoặc đánh đạt từ kết quả mô phỏng. Việc chia tài liệu chưa thực hiện các phase hay thay đổi chương trình.

## 1. Nguồn, phạm vi và quy tắc hoàn thành

- Nguồn chuẩn: [SRS v1.0.0](SRS_Culinary_Blog_v1.0.0.pdf), đủ **34 FR, 30 NFR, 10 CONS**, cùng yêu cầu UI, kiến trúc, dữ liệu và API ở Chương 5–8/phụ lục. Mục 5 bên dưới ánh xạ đủ 74 mã, không bỏ các mã đã có implementation.
- Tham chiếu [baseline giải mâu thuẫn](phase-0/00_SRS_Conformance_and_Resolution.md), [ma trận truy vết](phase-0/06_Traceability_Matrix.md) và [audit lịch sử](../reports/srs-and-course-audit-2026-10-23.md). Baseline trong repository không tự chứng minh mọi sai khác đã được người nghiệm thu chấp thuận.
- Giữ Next.js 15 và nền tảng đã chốt. Không thêm Next.js 16/PPR, 2FA/Keycloak, rating/comments, commerce hoặc các kỹ thuật tùy chọn của slide khi không có yêu cầu riêng. Trang chủ **đã có danh mục**; SRS không bắt buộc danh mục phải nằm trong sidebar.
- Đổi/quên/đặt lại mật khẩu là phần bổ sung người dùng đã yêu cầu; giữ và kiểm tra hồi quy riêng, không đổi tổng FR của PDF thành 36/37.
- Các ô bên dưới đang để trống vì là **công việc cần đóng trên bản cuối**. Có phần chỉ cần xác minh lại, không phải viết lại từ đầu. Những phần đã triển khai được giữ trong nhật ký lịch sử cuối file.
- Nhãn công việc: **Sửa** = đã biết thiếu/lệch cần sửa mã; **Kiểm chứng** = có mã/test nhưng thiếu bằng chứng cuối; **Quyết định** = yêu cầu mâu thuẫn/sai khác cần ghi rõ; **Môi trường** = phụ thuộc Docker, credential, thiết bị hoặc staging/production.
- Chỉ đánh `[x]` khi có: mã SRS/nguồn trang, thay đổi hoặc kết quả review, test/đo lường đạt, phiên bản chạy và link bằng chứng. Nếu yêu cầu có nhiều điều kiện, phải đủ các điều kiện bắt buộc; trường hợp điều chỉnh phải ghi quyết định và phạm vi được chấp thuận. Không đổi “chưa kiểm tra” thành “không áp dụng”.
- Không dùng số test pass hoặc số ô đã đánh dấu làm phần trăm hoàn thành SRS. Kết quả local, mock/simulation, browser thật và provider/production thật phải được ghi riêng.

## 2. Mốc xuất phát của kế hoạch

Theo lượt kiểm tra mới nhất trong báo cáo trạng thái, worktree có thay đổi chưa commit trên HEAD `32e203e0b558446c51d6379c71dbec60644319c9`:

| Hạng mục | Hiện trạng | Đợt xử lý |
|---|---|---|
| Build/lint/typecheck, backend format | Đạt ở lượt kiểm tra gần nhất | D1, D8 và D10 kiểm tra lại bản cuối |
| Unit/architecture/frontend | 75/3/104 test đạt; gồm source assertions/JSDOM, chưa thay E2E | D7–D8 |
| Application coverage | 62,56% line, dưới 80% | D7 |
| Integration | 41 đạt, 58 lỗi khởi tạo Docker / 99 tổng; chưa kết luận lỗi nghiệp vụ từ 58 lỗi môi trường | D1, D8 |
| Frontend Prettier | Lỗi sáu file nêu trong báo cáo | D1 |
| Google | Có provider/backend/ánh xạ avatar mới; refresh token bị revoke ngay ở callback | D3 |
| ISR/HTTP | Home/categories qua các assertion cache đầu; cả bài chưa đạt vì category không tồn tại trả 200 | D4 |
| Rate limit | Có quota 10/100/5, 429/Retry-After; còn fixed window auth và thiếu rate headers | D5 |
| Cache/CQRS/SEO | Đã có nhiều phần; còn G05/G07/G06 trong báo cáo | D2, D4–D6 |
| E2E/UAT/phi chức năng | Thiếu năm luồng browser thật; UAT Pending; chưa đủ evidence hiện tại | D8–D10 |

## 3. Thứ tự thực hiện và phụ thuộc

Vai trò dưới đây là trách nhiệm chuyên môn đề xuất, chưa phải phân công cho cá nhân. Ưu tiên trong bảng là thứ tự triển khai, không thay mức MoSCoW của từng FR trong SRS. Tất cả mục thuộc phạm vi vẫn phải được đóng trước khi kết luận hoàn thành toàn bộ.

| Đợt | Mục tiêu | Phụ thuộc | Vai trò | Đầu ra / điều kiện chuyển đợt |
|---|---|---|---|---|
| D0 | Chuẩn hóa acceptance và sai khác | Không | Phân tích + kỹ thuật | Danh sách acceptance có nguồn, cách xử lý mâu thuẫn và ID bằng chứng |
| D1 | Môi trường và kiểm tra chất lượng nền | D0 | Backend/frontend + QA | Docker/test environment hoạt động; format/build/test hiện tại được ghi chính xác |
| D2 | CQRS, validation, quyền, transaction, logging | D1 | Backend | Use case qua Application đúng trách nhiệm; test lỗi/quyền/cache đạt |
| D3 | Xác thực, Google, refresh/logout/profile | D2 | Backend/frontend + QA | Local/Google session và owner logout có test; provider thật ghi riêng |
| D4 | Rendering, 404, HTTP routes và revalidation | D1–D3 | Frontend/backend | TTL/ISR/SSR/status đúng; mutation làm nội dung public cập nhật |
| D5 | Redis/Output Cache, rate limit và nhiều instance | D2–D4 | Backend + hạ tầng | TTL/invalidation/privacy/headers/quota phân tán được chứng minh |
| D6 | FR còn lại, media/jobs, SEO và đồng bộ hợp đồng | D2–D5 | Backend/frontend | 34 FR có acceptance; media/jobs/SEO/schema/OpenAPI nhất quán |
| D7 | Coverage và CI bắt buộc | Bắt đầu từ D2, chốt sau D6 | Backend/frontend + QA | Application unit coverage ≥80%; gate local/CI thực sự chặn lỗi |
| D8 | Integration đầy đủ, E2E và UX/browser | D3–D7 | QA + frontend/backend | Mọi endpoint happy/error; năm browser E2E; a11y/responsive có phạm vi rõ |
| D9 | Hiệu năng, security, reliability, observability, scaling | D5–D8 | QA + hạ tầng | Bằng chứng đo trên artifact xác định và đúng môi trường |
| D10 | Tài liệu, UAT và kết luận nghiệm thu | D0–D9 | Kỹ thuật + QA + người nghiệm thu | Traceability không còn ô bỏ ngỏ; quyết định nghiệm thu có căn cứ |

Mỗi đợt kết thúc bằng review diff, chạy các phép kiểm tra liên quan và cập nhật bằng chứng. Nếu sửa sau khi đo, chạy lại phần bị ảnh hưởng rồi chốt lại artifact; không kéo nguyên kết quả cũ sang bản mới. Chỉ tối ưu query/cache hoặc thêm abstraction khi cần đóng yêu cầu hoặc có phép đo chỉ ra vấn đề.

Phụ thuộc trong bảng áp dụng cho phần kỹ thuật có liên quan. Việc chờ tài khoản Google, thiết bị, staging hoặc cửa sổ quan sát không ngăn công việc độc lập ở đợt sau; các mục đó vẫn để mở và vẫn ngăn kết luận nghiệm thu toàn bộ cho đến khi được đóng.

## 4. Kế hoạch công việc chi tiết

### D0. Chuẩn hóa tiêu chí và xử lý mâu thuẫn

Nguồn: toàn SRS, đặc biệt trang 14–15, 17–52, 54–68. Đầu ra: acceptance inventory/traceability và decision log có liên kết từ checklist.

- [x] **D0-01 · Kiểm chứng:** tách từng acceptance của 34 FR/30 NFR/10 CONS thành điều kiện kiểm tra được; ghi trang nguồn, mức Must/Should, API/job/screen, cách kiểm tra, hiện trạng và bằng chứng. Rà cả luồng thay thế/lỗi, không chỉ tiêu đề FR.
- [x] **D0-02 · Quyết định:** rà đủ GAP-001–033; giữ các quyết định đã được chốt trong phiên làm việc, ghi rõ phần còn cần người nghiệm thu xác nhận. Bao gồm 400/422, 409/If-Match, soft delete, body refresh/logout, public/private split, token entropy, FTS, bigint version, API/schema và private bucket. Không tự đảo baseline trong lúc sửa.
- [x] **D0-03 · Quyết định:** ghi riêng sai khác bổ sung: Output Cache hay cache-aside; ping sitemap Google; công cụ/ruleset SonarAnalyzer/StyleCop/Airbnb/Jest/Testing Library; browser minimum; lock singleton thay RedLock nếu có. Mỗi mục có nguyên văn, đề xuất, lý do, ảnh hưởng và trạng thái chấp thuận; chưa chốt thì chưa đánh đạt nguyên văn.
- [ ] **D0-04 · Quyết định:** làm rõ uptime 99,5% so với “3,65 giờ downtime/năm” không tương đương, khoảng thời gian đo, múi giờ backup 03:00 và cách nghiệm thu scaling/production. Lưu tiêu chí đo thống nhất trước khi D9 ghi kết quả.
- [x] **D0-05 · Kiểm chứng:** lập danh sách contract UI/API/data Chương 5–8/phụ lục: routes/redirects, status/header, request/response/nullable/enum, giới hạn field, relationships/index/concurrency/audit/soft delete. Mỗi sai khác mới phát hiện được thêm vào đợt tương ứng, không coi báo cáo hiện tại là audit hết mọi chi tiết nhỏ.

**Điều kiện đạt:** không còn acceptance bắt buộc bị bỏ khỏi inventory; mọi điểm mâu thuẫn có hướng xử lý và trạng thái rõ ràng. Việc đang chờ quyết định được ghi mở, không âm thầm miễn yêu cầu.

Bằng chứng D0-01/D0-05 ngày23/10/2026: [inventory](srs-completion-phases/acceptance-inventory.md) tách347 acceptance cho đủ34FR/30NFR/10CONS và207 hợp đồng UI/architecture/data/API/phụ lục; verifier đối chiếu45 operations OpenAPI. [Report kiểm chứng](srs-completion-phases/reports/phase-01-result.md) ghi source/hash/runtime/commands và12 nhóm sai khác bàn giao. Đây là hoàn thành sản phẩm tài liệu; **D0-02/03/04 và acceptance runtime vẫn mở**, không chứng nhận hệ thống đạt SRS.

Bằng chứng D0-02/D0-03 ngày23/10/2026: [decision register](srs-completion-phases/decision-register.md) review33 GAP/25 DEC/12 INV-G, phân biệt baseline hiện có và11 đề xuất chờ duyệt; [report](srs-completion-phases/reports/phase-02-result.md) ghi nguồn, lệnh kiểm và handoff. D0-03 đóng phần ghi nhận sai khác, **không miễn acceptance nguyên văn**. D0-04 vẫn mở vì DEC-09/11 (SLA window/load/scaling tolerances) chưa chốt; backup03:00 UTC đã có Test Strategy§4.

Cập nhật sau xác nhận ngày23/10/2026: [APP-2026-10-23-ABC](srs-completion-phases/approvals/2026-10-23-abc.md) ghi người dùng duyệt đủ ba góiA/B/C.11 quyết định đã có hiệu lực; D0-04 đóng phần tiêu chí thống nhất. Số liệu và ghi chú “chờ” của checkpoint trước ở trên là lịch sử; các phép đo/implementation vẫn do owner và chưa được chứng nhận.

Đính chính hiện tại: [SRS-2026-10-23-ABC-CORRECTION](srs-completion-phases/approvals/2026-10-23-abc-correction.md) thay hiệu lực A/B/C cũ theo yêu cầu lấy PDF làm chuẩn. Các kết luận duyệt/đóng ở checkpoint trước chỉ là lịch sử; D0-04 mở lại vì uptime và protocol đo chưa thống nhất. D0-02/D0-03 đóng phần review/ghi nhận, không miễn acceptance nguyên văn; tiến độ02=Chờ quyết định.

### D1. Khôi phục khả năng kiểm thử và chất lượng nền

File chính: `frontend/package.json`, `frontend/next.config.mjs`, `frontend/src/middleware.ts`, `frontend/tests/`, `.github/workflows/ci.yml`, `backend/tests/`. Không thay dữ liệu người dùng để làm test xanh.

- [ ] **D1-01 · Môi trường:** kiểm tra Docker Engine, các image PostgreSQL/Redis/MinIO/SMTP thử nghiệm và runtime Node 22.23.3/.NET 10; chạy test bằng database/container riêng. Phân biệt lỗi môi trường với assertion nghiệp vụ.
- [ ] **D1-02 · Sửa:** sửa Prettier của `next.config.mjs`, `scripts/check-isr.mjs`, `src/middleware.ts`, `tests/public-content-wiring.test.mjs`, `tests/rendering-cache-wiring.test.mjs`, `tests/visual-responsive-regression.test.mjs`; xác minh lại backend format và không sửa format lan sang file không liên quan.
- [ ] **D1-03 · Kiểm chứng:** clean/locked restore theo runtime/lockfile đã chốt; chạy build, lint, typecheck, unit, architecture, frontend và integration. Lưu exit code, số pass/fail/skip, nguyên nhân và phiên bản; không ghi 99/99 nếu container chưa khởi tạo.
- [ ] **D1-04 · Kiểm chứng:** ghi HEAD + trạng thái worktree, thời điểm, runtime, cấu hình test đã lược secrets. Sau này gắn release với commit/image digest để biết chính xác bản nào được đo.

**Điều kiện đạt:** môi trường tái chạy được, sáu lỗi format đã hết, lỗi regression hiện tại được tái hiện và gán ID. Nếu Docker chưa khả dụng, tiếp tục phần độc lập và giữ integration ở trạng thái chờ môi trường.

### D2. Hoàn thiện Application/CQRS và trách nhiệm từng tầng

Nguồn: CONS-001/002/008/010, NFR-SEC-004/006, NFR-MAINT-004, §6.2–6.3. File chính: `AuthEndpoints.cs`, `ContentEndpoints.cs`, `Application/Auth/`, `Application/Content/`, `Application/Behaviors/`, `Infrastructure/Content/ContentService.cs`.

- [ ] **D2-01 · Sửa:** lập inventory từng use case HTTP và bổ sung Command/Query Handler còn thiếu, gồm audit/sitemap; xử lý HTTP/stream tại adapter, orchestration ở Application và kỹ thuật storage/EF/SMTP ở Infrastructure. Không gọi một lớp chuyển tiếp rỗng là đã chứng minh toàn bộ nghiệp vụ đúng tầng.
- [ ] **D2-02 · Sửa:** chuyển validation nghiệp vụ còn nằm trong endpoint/service vào FluentValidation/Application pipeline; giữ parse/binding HTTP ở endpoint. Rà body/query/ID/page/sort/ETag/file metadata; validation sai không được ghi DB, queue job hoặc xóa cache.
- [ ] **D2-03 · Sửa/kiểm chứng:** kiểm tra policy/quyền sở hữu tại Application, không chỉ route. Bao phủ Guest, Author-owner, Author khác, Admin và tài khoản inactive; sensitive mutation phải kiểm tra trước commit, không nhận userId/isAdmin do client tự khai.
- [ ] **D2-04 · Kiểm chứng:** rà Repository/UoW/transaction cho command cần thiết; create recipe đã có UoW phải được giữ. Chứng minh atomicity của composition, renumber, primary image, version và outbox; chỉ bổ sung abstraction/transaction ở nơi thiếu.
- [ ] **D2-05 · Kiểm chứng:** giữ thứ tự logging/validation/caching/handler/invalidation hợp lý; cache chỉ xóa sau ghi thành công, không xóa khi validation/quyền/handler lỗi; kiểm tra cả client disconnect và lỗi scheduler/cache, ghi rõ cơ chế retry.
- [ ] **D2-06 · Sửa/kiểm chứng:** bảo đảm structured log của các use case có CorrelationId, RequestPath và UserId khi có request xác thực; job có job/outbox ID để truy vết. Không log password/token/secret; kiểm tra slow query >100ms và pipeline >500ms theo SRS, cùng hành vi khi sink lỗi.
- [ ] **D2-07 · Kiểm chứng:** bổ sung architecture/unit/integration cho các đường vừa chuyển; Domain không phụ thuộc layer/package ngoài BCL theo GAP-026, Application không tham chiếu Infrastructure, read không âm thầm ghi nghiệp vụ.

**Điều kiện đạt:** inventory use case không còn bỏ qua Application trái hợp đồng; test validation/quyền/transaction/log/cache đạt; dependency tests xanh. Refactor từng use case, giữ contract công khai trừ thay đổi đã ghi rõ.

### D3. Xác thực, vòng đời phiên và hồ sơ

Nguồn: FR-AUTH-001–007, CONS-004, NFR-SEC-001/002/006, GAP-005/006/007/020. File chính: `frontend/src/auth.ts`, `AuthContext.tsx`, session-token store, `AuthRequests.cs`, `AuthService.cs`, `AuthLifecycleTests.cs` và password tests.

- [ ] **D3-01 · Kiểm chứng:** register Author mặc định/auto-login, email duplicate, password policy, PBKDF2 ≥100.000 iterations, generic login failure và lockout đúng acceptance; giữ đăng ký khi gửi email lỗi. Kiểm tra safe profile và phân quyền xem/sửa.
- [ ] **D3-02 · Sửa:** bỏ việc revoke refresh token ngay sau Google login thành công; hoàn thiện lưu/refresh/revoke theo hợp đồng đã chốt, không lộ refresh token qua session công khai/log và không tạo thêm cơ chế cookie/BFF ngoài phạm vi một cách ngầm định.
- [ ] **D3-03 · Sửa/kiểm chứng:** dùng được phiên sau access token hết hạn, refresh rotation một lần, chống cạnh tranh refresh và reuse revoke family; logout thu hồi đúng phiên Google/local, xử lý lỗi/expired session rõ ràng, không lặp refresh vô hạn.
- [ ] **D3-04 · Sửa:** hoàn thiện logout bearer optional theo GAP-007: bearer còn hạn phải cùng owner với refresh token, bearer hết hạn vẫn cho revoke bằng refresh hợp lệ, request lặp idempotent; quy định status khi bearer/token khác chủ trong contract rồi kiểm thử.
- [ ] **D3-05 · Kiểm chứng:** Google callback/code+PKCE, state/cancel/error, issuer/audience/signature/expiry/email verification/account linking và avatar. Kiểm thử có kiểm soát nhánh provider; bài mô phỏng không được ghi là đã đăng nhập Google thật.
- [ ] **D3-06 · Kiểm chứng:** giữ form profile RHF/Zod, sửa/xóa avatar, validation URL và không cập nhật một phần khi request sai; xác minh avatar thực sự hiển thị qua chính sách ảnh/CSP và tên/bio đồng bộ session/UI sau lưu.
- [ ] **D3-07 · Môi trường:** cấu hình Google client/callback hợp lệ và SMTP thử nghiệm khi cần; chạy smoke với provider/tài khoản thử đã được phép, lưu kết quả không chứa credential. Thiếu credential thì đánh chờ môi trường, tiếp tục test local độc lập.
- [ ] **D3-08 · Kiểm chứng bổ sung người dùng:** đổi/quên/reset password: current password, không tiết lộ email tồn tại, token 30 phút/bound/single-use, expiry, revoke sessions, lỗi SMTP, reset URL encode và thông báo UI. Không phát sinh FR mới của PDF.

**Điều kiện đạt:** local/Google không mất khả năng refresh vì chính callback; logout đúng owner và idempotent; test API/UI/session đạt, bằng chứng provider thật được ghi riêng.

### D4. Rendering, HTTP status, routes và invalidation xuyên suốt

Nguồn: §5.1 trang 46–47, FR-CAT-002/FR-RCP-002, NFR-PERF-005/NFR-SEO. File chính: public pages, `next.config.mjs`, `middleware.ts`, `public-content-server.ts`, `/api/revalidate`, refresh scheduler và `scripts/check-isr.mjs`.

- [ ] **D4-01 · Sửa:** thêm kiểm tra tái hiện rồi sửa category không tồn tại trả HTTP 200. Kiểm tra cả recipe không tồn tại, unknown URL, URL thừa segment và đường nội bộ; phân biệt trang nhìn như 404 với HTTP 404 thực sự, không dùng assertion source thay phép đo HTTP.
- [ ] **D4-02 · Sửa/kiểm chứng:** `/` và `/categories` ISR 3600 giây; `/recipes/[slug]` ISR 300; `/categories/[slug]` ISR 600. Kiểm tra Full Route Cache/headers/MISS-HIT và thời điểm hết hạn hoặc tái tạo, không chỉ có dòng `revalidate` hay fetch Data Cache.
- [ ] **D4-03 · Kiểm chứng:** list/search SSR theo query, các private page/media no-store và có auth. Public rewrite không gây redirect login, không làm lộ đường nội bộ hoặc cache nội dung người dùng; direct private URL vẫn bị chặn.
- [ ] **D4-04 · Kiểm chứng:** mapping `/search`, `/auth/login`, `/auth/register`, canonical và status redirect đúng quyết định D0; đăng nhập có redirect khi phù hợp, dashboard/new/edit/profile đúng quyền và loại rendering. Giữ home featured recipes + categories đã có.
- [ ] **D4-05 · Sửa/kiểm chứng:** chạy mutation create/update/delete/publish/unpublish/archive/category trên backend thật → pipeline → scheduler/job → callback Next → nội dung home/category/list/detail/search/media cập nhật; Draft/Archived biến mất khỏi public/cache.
- [ ] **D4-06 · Kiểm chứng:** callback đúng secret mới invalidation; thiếu/sai secret bị từ chối, không lộ secret. Kiểm tra retry khi Next tạm unavailable và tránh làm rollback nghiệp vụ đã commit; build không phụ thuộc API đang online nếu phương án ISR đã chọn yêu cầu vậy.
- [ ] **D4-07 · Kiểm chứng:** hoàn thiện bài HTTP production đang có, chạy hết assertion kể cả các bước đứng sau lỗi hiện tại; giữ Postman simulation từ OpenAPI cho test cô lập, bổ sung test stack thật cho D4-05. Hai loại kết quả có nhãn riêng.

**Điều kiện đạt:** bài HTTP xanh, missing resources trả 404, đủ TTL/rendering và mutation xuyên suốt. Không đánh hoàn thành từ riêng các bước đầu đã pass ở lần chạy cũ.

### D5. Distributed cache, rate limit và hành vi nhiều instance

Nguồn: NFR-PERF-003, NFR-SEC-003, NFR-SCALE-001, §5.2, GAP-013/014. File chính: cache abstractions/Redis/CachingBehavior, API DI/Program, OpenAPI và cấu hình Nginx.

- [ ] **D5-01 · Quyết định/sửa:** triển khai Redis-backed Output Cache theo GAP-013, hoặc lập thay đổi baseline cho cơ chế thay thế có đủ acceptance. Ghi rõ vai trò cache dữ liệu và cache HTTP để tránh hai tầng có TTL/invalidation mâu thuẫn; không báo cache-aside hiện có là Output Cache.
- [ ] **D5-02 · Kiểm chứng:** TTL category 30 phút, recipe detail 5 phút, list 15 phút, search 1 phút theo baseline; key bao phủ filter/sort/page/normalization. Test thời gian bằng clock/TTL query phù hợp thay vì chờ đủ 30 phút.
- [ ] **D5-03 · Kiểm chứng:** invalidation sau mọi mutation liên quan cả hai nhóm tags, kể cả primary image và composition; cache không lộ Draft/Archived hoặc token/user state giữa Guest/Author/Admin; hai instance cùng thấy dữ liệu mới.
- [ ] **D5-04 · Kiểm chứng:** Redis down/timeout vẫn đọc DB và không làm hỏng write đã commit; xác minh phục hồi/cache metrics, giới hạn và xử lý cạnh tranh fill cache nếu phát hiện vấn đề qua đo lường.
- [ ] **D5-05 · Sửa:** auth dùng sliding window 10 request/phút/IP; global fixed window 100 và upload 5 theo SRS. Chốt rõ ảnh hưởng khi nhiều limiter cùng áp dụng và phương án shared counter cho nhiều API instance, tránh mỗi instance cho thêm một quota độc lập.
- [ ] **D5-06 · Sửa:** phát `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` và `Retry-After` khi 429, thống nhất đơn vị/thời điểm và expose qua CORS khi UI cần. Cập nhật OpenAPI và test header thực.
- [ ] **D5-07 · Kiểm chứng:** test auth tại ranh giới cửa sổ trượt, request limit+1, IP độc lập, trusted proxy/header spoof, health không tiêu quota, upload quota và HTTP 429 Problem Details. Test luân phiên qua ít nhất hai API instance.

**Điều kiện đạt:** đúng cơ chế, TTL, privacy, invalidation và headers; có bằng chứng chia sẻ trạng thái trên nhiều instance, không chỉ test một process.

### D6. Nghiệp vụ còn lại, media/jobs, SEO và contract dữ liệu/API

Nguồn: FR-CAT/RCP/SRCH/FILE/JOB, NFR-SEO-001–004, CONS-005/006/007, Chương 7–8/phụ lục. Các chức năng đã có được kiểm chứng trước; chỉ sửa phần acceptance còn thiếu.

- [ ] **D6-01 · Kiểm chứng:** category CRUD/quyền Admin, tên unique/giới hạn field, Name tăng dần public/count Published, quản trị orderIndex rõ nghĩa, detail pagination, xóa category còn recipe bị conflict, soft delete theo baseline.
- [ ] **D6-02 · Kiểm chứng:** public/owner/Admin recipe list/detail và wizard Draft; owner/Admin update với ETag/If-Match, stale race 409, missing header 400 theo baseline. Publish cần ingredient + step; unpublish/archive/unarchive/delete đúng transition và không lộ private content/media sau đó.
- [ ] **D6-03 · Kiểm chứng:** ingredient CRUD/nullability/limits/order; step CRUD/title/timer/append và renumber liên tục trong transaction; ảnh primary duy nhất kể cả concurrent write. Xác minh quyền và version cho từng mutation con, rollback khi một phần thao tác lỗi.
- [ ] **D6-04 · Kiểm chứng:** FTS có/không dấu, weighted rank/relevanceScore, q ≥2 sau trim, special characters/injection, empty result; AND filters category/difficulty/maxCookTime/minServings/prep bounds/maxTime, sort hai chiều/tie-break, pageSize 1–50 và metadata. UI/URL/default relevance đồng nhất API, `maxCookTime=0` không bị mất.
- [ ] **D6-05 · Kiểm chứng/sửa:** upload JPEG/PNG/WebP/AVIF ≤5 MB, magic bytes/decode/pixel/path traversal, kiểm tra size trước buffering theo SRS; filename GUID, bucket private, quyền tải/xem/xóa. Có ít nhất test adapter MinIO thật bên cạnh test storage giả, không suy kết quả fake thành storage thật.
- [ ] **D6-06 · Kiểm chứng:** resize decode thực medium 800×600, thumbnail 300×300, lưu bản gốc + variants/URL; retry 3, xử lý lỗi/restart/idempotency và fallback ảnh gốc khi job thất bại. Kiểm tra canvas/giữ tỷ lệ theo lựa chọn đã ghi, không đánh đủ fallback chỉ từ test kích thước.
- [ ] **D6-07 · Kiểm chứng:** delete object idempotent/retry 3/outbox, object không còn và metadata không trôi khi storage lỗi; không xóa ảnh recipe khác hoặc làm mất bản gốc khi resize thất bại.
- [ ] **D6-08 · Kiểm chứng:** welcome HTML có encode tên/link tin cậy, Hangfire persistent queue/outbox, retry 1/5/30 phút/Failed state/log, concurrency/restart không tạo job trùng thông thường; SMTP fail giữ đăng ký. Dashboard chỉ Admin; ghi giới hạn duplicate email nếu crash sau SMTP accept, không hứa exactly-once.
- [ ] **D6-09 · Sửa/kiểm chứng:** sitemap hàng ngày 02:00 UTC/retry 2/singleton/public URLs + static/category, `lastmod`, `changefreq`, `priority` theo hợp đồng; lỗi upload giữ artifact cũ; robots khai báo URL sitemap. Rà quyết định ping Google ở D0, không thêm request lỗi hoặc tự bỏ yêu cầu mà không ghi sai khác.
- [ ] **D6-10 · Sửa:** metadata title hoàn chỉnh, gồm hậu tố `| Culinary Blog` khi áp dụng, ≤60 ký tự; description 150–160 ký tự có ý nghĩa, OG 1200×630/Twitter/canonical/index-noindex phù hợp; kiểm tra long/short title/description và trang phân trang. Không chèn văn bản vô nghĩa chỉ để đủ độ dài.
- [ ] **D6-11 · Sửa:** lưu lịch sử draft slug khi đổi title/slug, 301 từ slug cũ sang slug hiện hành, xử lý collision và tránh loop/chain không cần thiết. Slug sau publish giữ ổn định; URL cũ không được dẫn tới Draft riêng tư hoặc phá public/private split.
- [ ] **D6-12 · Kiểm chứng:** Recipe JSON-LD đầy đủ field có dữ liệu thật, duration/yield/ingredients/steps/nutrition đúng; escape chống XSS, ảnh mặc định hợp lệ; chạy Rich Results Test trên bản triển khai phù hợp và lưu kết quả, không tạo rating/nutrition giả.
- [ ] **D6-13 · Kiểm chứng/sửa:** đối chiếu tất cả schema/data type/length/nullability/default/constraint/index/relationship/audit/soft-delete/Version và API method/path/status/error/envelope/header với inventory D0. Bao gồm refresh-token family/hash, media/outbox và tables mới; không chỉ kiểm OpenAPI có endpoint.
- [ ] **D6-14 · Kiểm chứng:** migration upgrade từ bản trước giữ user/recipe/composition/token/outbox/media, migration mới nullable/backfill đúng; restart/seed idempotent không ghi đè dữ liệu người dùng; kiểm tra dữ liệu seed theo SRS và ghi rõ sai khác dùng seed thủ công thay Bogus nếu còn. Test trên DB/container riêng, chưa tự áp dụng vào DB người dùng.
- [ ] **D6-15 · Sửa/kiểm chứng:** cập nhật OpenAPI/XML comments/Scalar và generate frontend types; kiểm diff để phát hiện contract drift. Bảo đảm docs auth/multipart/ETag/headers/error response khớp hành vi thật và môi trường được mở docs đúng quy định.

**Điều kiện đạt:** mọi acceptance FR trong inventory có implementation/test tương ứng, media/job có kiểm lỗi thật ở phạm vi cần thiết, SEO và schema/contract không còn sai khác ngầm.

### D7. Application coverage và gate CI

Nguồn: NFR-MAINT-001/002/004. File chính: `backend/tests/CulinaryBlog.UnitTests/`, architecture tests, `.github/workflows/ci.yml`, scripts kiểm tra và frontend test configuration.

- [ ] **D7-01 · Sửa:** đọc report uncovered Application commands/queries/validators/behaviors sau D2/D3; thêm unit test hành vi thành công, invalid input, forbidden/not-found/conflict, failure/cancellation/cache. Test phải phát hiện sai nghiệp vụ, không chỉ khớp cách viết handler.
- [ ] **D7-02 · Sửa:** đạt **Application unit line coverage ≥80%**, đo riêng layer từ bộ unit; không lấy Domain hoặc integration coverage thay thế, không loại trừ mã nghiệp vụ để tăng tỷ lệ. Giữ báo cáo line/branch và phân tích phần còn thiếu.
- [ ] **D7-03 · Sửa:** thêm gate đọc coverage XML và fail khi dưới 80%, dùng được local và CI; kiểm gate bằng một fixture/report dưới ngưỡng rồi khôi phục, lưu exit code. Gate phải nhắm đúng Application, không chỉ upload report.
- [ ] **D7-04 · Sửa/kiểm chứng:** CI chạy locked restore, backend format/build/test/architecture/coverage, frontend API generation/format/lint/typecheck/build/tests, integration và browser E2E theo thứ tự có artifact/log khi lỗi. Không bỏ test để làm pipeline xanh.
- [ ] **D7-05 · Quyết định/sửa:** đóng D0 về SonarAnalyzer/StyleCop/Airbnb/Jest/Testing Library; bổ sung công cụ/ruleset đúng yêu cầu hoặc ghi phương án thay thế được chấp thuận và scope kiểm tra tương đương. Rà compiler/analyzer warnings và cảnh báo Next ESLint, không tự tắt cảnh báo nghiệp vụ để đạt số 0.

**Điều kiện đạt:** coverage ≥80%, gate thật sự chặn report dưới ngưỡng, CI và local dùng cùng tiêu chí; có kết quả kiểm tra chứ không chỉ cấu hình YAML.

### D8. Integration toàn diện, năm E2E browser và UX

Nguồn: NFR-MAINT-002, NFR-USE-001–004, §5.1/5.4 và GAP-024. Môi trường: stack thử riêng, browser thật, SMTP/storage test; thiết bị/screen reader khi kiểm thủ công.

- [ ] **D8-01 · Kiểm chứng:** lập bảng **mỗi endpoint ít nhất một happy path và một error case**, gắn với FR/acceptance; chạy full integration sau D6. Bao phủ toàn role matrix, invalid/body/query/header, not-found/duplicate, ETag race, privacy/cache, jobs, storage và migration; không suy từ tổng 99 test là đã đủ endpoint.
- [ ] **D8-02 · Sửa:** thêm Playwright config/fixtures và script một lệnh dựng/chờ/test/dọn tài nguyên thuộc test; dữ liệu synthetic được cô lập, không dùng DB người dùng. Đặt tên script và ghi README sau khi triển khai, không quảng cáo lệnh chưa tồn tại là đã chạy được.
- [ ] **D8-03 · Sửa/kiểm chứng E2E-01:** đăng ký từ UI → Author/auto-login → profile → welcome email capture; kiểm lỗi field/duplicate và hồ sơ sửa được. SMTP failure được kiểm riêng không rollback user.
- [ ] **D8-04 · Sửa/kiểm chứng E2E-02:** local login → route riêng → hết access token/refresh → logout; token/session cũ không dùng được. Google smoke/provider test D3 được bổ sung riêng, không thay luồng local hoặc đánh provider thật từ mock.
- [ ] **D8-05 · Sửa/kiểm chứng E2E-03:** tạo Draft bằng wizard với thông tin, ingredient, step, image; kiểm preview/private visibility, validation và save/error/conflict recovery theo SRS.
- [ ] **D8-06 · Sửa/kiểm chứng E2E-04:** publish từ UI → home/list/detail/category/search thấy nội dung → unpublish/archive mất khỏi public/cache; Author khác không được sửa, Admin đúng quyền; kiểm cả nội dung vừa mutation và invalidation.
- [ ] **D8-07 · Sửa/kiểm chứng E2E-05:** tìm “pho/phở” → relevance/filter/sort/page/URL/back-forward → mở đúng detail; có empty/error/loading và query không hợp lệ rõ ràng.
- [ ] **D8-08 · Kiểm chứng/sửa:** rà mọi form/async/write flow dùng RHF/Zod hoặc cách đã chốt, inline/server error actionable, error code i18n-ready, không lộ stack; skeleton/loading/empty, toast thành công/thất bại, optimistic rollback phù hợp và upload progress % thật.
- [ ] **D8-09 · Kiểm chứng:** responsive 320–767/768–1199/≥1200 trên public + auth + profile + dashboard; không tràn ngang, thao tác touch/keyboard, focus/escape/modal/menu, contrast text ≥4,5:1 và UI ≥3:1 theo WCAG AA. Lưu screenshot/axe/browser results, không chỉ regex class CSS.
- [ ] **D8-10 · Môi trường/kiểm chứng:** chạy browser matrix minimum Chrome/Firefox/Edge/Safari theo GAP-024, iOS/Android theo quyết định đã chốt; ghi phiên bản và giới hạn kiểm tra. Kiểm NVDA trên Windows và VoiceOver trên macOS/iOS, lưu người thực hiện/kịch bản/kết quả; không coi JSDOM là screen reader thật.

**Điều kiện đạt:** năm luồng browser thật chạy lại được, bảng endpoint không còn thiếu happy/error, lỗi UX/a11y bắt buộc được đóng. Trace/screenshots/report không chứa credential/token hoặc dữ liệu người dùng thật.

### D9. Hiệu năng, security, vận hành, reliability và scaling

Nguồn: NFR-PERF/SEC/REL/SCALE và FR-OBS. Tái dùng `scripts/performance.ps1`, failure/restore drill, `infra/observability/`, `infra/operations/`, production templates và reports hiện có; không ghi báo cáo cũ thành phép đo mới.

- [ ] **D9-01 · Kiểm chứng:** load smoke/load/stress trên artifact có định danh; profile tối thiểu 2 vCPU/4 GB cho API, ≥100 concurrent users; ghi dataset/request mix/duration/network/TLS, cold/warm và write traffic. Đạt warm GET p50 ≤150ms, API p95 ≤500ms, p99 ≤1000ms và Redis steady-state hit rate ≥80%; nới rate limit chỉ trong profile đo, ghi rõ và không sửa cấu hình production để né D5.
- [ ] **D9-02 · Kiểm chứng/sửa theo đo:** review EXPLAIN ANALYZE truy vấn chính, N+1/projection/batch và B-tree/GIN cho điều kiện WHERE/ORDER BY; chứng minh slow log >100ms; kiểm pool Npgsql max 100 connections/instance. Không thêm read replica/partitioning chỉ vì slide có ví dụ; các phần này được SRS ghi tùy chọn/nâng cao.
- [ ] **D9-03 · Kiểm chứng:** production bundle tất cả route ≤200 KB gzip, Lighthouse/CWV LCP ≤2,5s, CLS ≤0,1; lấy INP ≤200ms từ phép đo tương tác/RUM phù hợp và ghi cửa sổ mẫu. TBT là lab proxy, không thay thế chứng nhận INP; lưu audit meta/ảnh/code splitting và môi trường.
- [ ] **D9-04 · Kiểm chứng:** quét dependencies backend/frontend, secret history/worktree, images/config và dynamic security trên staging HTTPS đúng artifact; ghi scan version/report/findings/remediation, không suy “0 High/Critical” từ báo cáo cũ. Đóng Critical/High; finding còn lại được triage theo release gate hiện có. Kiểm CORS exact origins, TLS ≥1.2, HTTP redirect, HSTS 31536000, CSP/security headers/cookie policy và production fail-fast secrets.
- [ ] **D9-05 · Kiểm chứng/sửa:** live/ready/aggregate health đúng dependency matrix và không lộ chi tiết công khai; xác minh HTTP/EF/custom trace/span/metrics xuất tới OTLP/collector/dashboard thật, correlation nối request → handler → job khi có. Kiểm alerts/log sink lỗi không làm lộ dữ liệu hoặc gây crash ngoài hợp đồng.
- [ ] **D9-06 · Kiểm chứng:** failure drill PostgreSQL/Redis/MinIO/SMTP/worker/restart, global 500 Problem Details không lộ stack, DB reconnect/timeout 30s và Redis fallback, queue persistence/retry; ghi thời điểm lỗi/hồi phục và dữ liệu còn nguyên.
- [ ] **D9-07 · Sửa/kiểm chứng:** backup PostgreSQL mỗi ngày 03:00 theo múi giờ D0, retention 30 ngày; kiểm tra WAL/durability, persistent DB/MinIO volumes và tokens DB survive restart. Restore vào môi trường sạch so đủ dữ liệu/schema/media; ghi RTO/RPO, checksum/storage độc lập và runbook. Không chạy restore đè DB người dùng.
- [ ] **D9-08 · Môi trường/kiểm chứng:** probe readiness mỗi 10s, alert down >1 phút tới receiver thử được chỉ định và maintenance công bố ≥48h/banner đúng; đo uptime ≥99,5% theo cửa sổ D0. Thời gian chờ quan sát không được tự coi là đã đạt SLA.
- [ ] **D9-09 · Môi trường/kiểm chứng:** ít nhất hai API instance qua Nginx, JWT stateless/shared Redis/Hangfire PostgreSQL, multi-worker/singleton sitemap đúng lock; test rate/cache/job không nhân đôi quota hoặc rò nội dung. So throughput/latency trước và sau thêm instance theo tiêu chí D0; ghi rõ cơ chế lock so với RedLock trong PDF.
- [ ] **D9-10 · Môi trường/kiểm chứng:** production scaling theo SRS: MinIO distributed 4+ nodes, CDN cho `_next/static`, upstream load balancing, persisted services và multi-stage/non-root images. Nếu môi trường nghiệm thu không có, ghi chưa kiểm chứng hoặc quyết định phạm vi được chấp thuận, không báo đạt từ file Compose đơn node.

**Điều kiện đạt:** đủ ngưỡng bắt buộc và report mới trên môi trường phù hợp; tách kết quả local khỏi staging/production. Mục cần thời gian quan sát, credential hoặc thiết bị vẫn mở đến khi có bằng chứng/điều chỉnh chính thức.

### D10. Tài liệu cuối, UAT và nghiệm thu

File chính: README/CHANGELOG/ADR/OpenAPI, [release checklist](../release/environment-and-secrets-checklist.md), [UAT](../release/uat-signoff.md), [go/no-go và rollback](../release/go-no-go-and-rollback.md), [báo cáo trạng thái](../reports/srs-acceptance-status.md).

- [ ] **D10-01 · Kiểm chứng/sửa:** fresh setup theo README bằng Docker Compose <5 phút trong điều kiện package/image đã được nêu; kiểm env sample/ports/migrations/seed/Scalar. Cập nhật ADR, CHANGELOG/SemVer, API docs, test commands và known deviations đúng bản cuối.
- [ ] **D10-02 · Kiểm chứng:** feature/code freeze, commit SHA và API/Web image digest xác định; build/release manifest/SBOM/provenance và `/api/v1/release` khớp khi dùng release pipeline hiện có. Chạy lại regression/gates bị ảnh hưởng trên cùng artifact, không suy kết quả worktree sang GitHub/container cũ.
- [ ] **D10-03 · Kiểm chứng:** điền evidence cho đủ bảng 74 mã bên dưới và acceptance chi tiết D0, gồm ID test/case, pass/fail/pending, run URL/file, thời điểm, runtime/dataset/môi trường. Cập nhật báo cáo trạng thái và tài liệu Phase 8 cũ để không còn câu pass/deferred/deviation mâu thuẫn với bản mới.
- [ ] **D10-04 · Môi trường/nghiệm thu:** UAT đủ CJ-01–05/UX trên staging đúng artifact, triage defects, có người nghiệm thu/giảng viên/QA thực hiện đúng vai trò; không tự ký thay, không đổi Pending thành Accept chỉ vì test tự động xanh.
- [ ] **D10-05 · Kiểm chứng:** nếu nằm trong phạm vi triển khai, hoàn tất checklist secrets/TLS/DNS/backup/alert/maintenance, go/no-go và rollback/recovery theo tài liệu hiện có. Kế hoạch này không tự cho phép gửi thông báo, publish workspace, deploy production hoặc thực hiện restore dữ liệu thật.
- [ ] **D10-06 · Kiểm chứng:** review diff theo từng nhóm, ghi commit mô tả rõ chức năng bằng tiếng Việt không dấu khi thực hiện commit theo yêu cầu người dùng. Không gộp các PDF người dùng đang chuyển thư mục hoặc thay đổi ngoài phạm vi; phân biệt việc lập kế hoạch hiện tại với lần commit/push triển khai sau.
- [ ] **D10-07 · Nghiệm thu:** chỉ kết luận “hoàn thành toàn bộ SRS” khi các acceptance bắt buộc đều đạt hoặc có điều chỉnh rõ ràng được chấp thuận, không còn pending không giải thích. Nếu mới đạt local/chức năng cốt lõi, kết luận đúng mức đó và liệt kê điều kiện còn mở; không đưa phần trăm tự suy đoán.

**Điều kiện đạt:** người đọc truy từ yêu cầu → mã → test → kết quả đúng artifact; UAT/quyết định sai khác không còn bị giả định. Mọi mục chưa đóng có trạng thái/lý do/bước tiếp theo cụ thể.

## 5. Ma trận bao phủ để không bỏ sót yêu cầu

Các bảng này là bản đồ đến công việc và điều kiện đóng, **không phải bảng pass hiện tại**. Bằng chứng từng acceptance được điền khi D0/D10 thực hiện. Một FR có thể liên quan nhiều NFR/CONS; đóng chức năng không tự đóng kiến trúc hoặc hiệu năng.

### 5.1. Đủ 34 FR

| Mã | Công việc | Điều kiện phải có khi đóng |
|---|---|---|
| FR-AUTH-001 | D3-01, D6-08, D8-03 | Register Author, auto-login, input/duplicate, welcome job không rollback đăng ký |
| FR-AUTH-002 | D3-01, D5-05–07, D8-04 | Login/error/lockout, rate limit, UI/session |
| FR-AUTH-003 | D3-02–05, D3-07 | Google callback/token/account/avatar, refresh/logout, nguồn bằng chứng rõ |
| FR-AUTH-004 | D3-03, D8-04 | Rotation/reuse/expiry/race, cả nhánh Google/local |
| FR-AUTH-005 | D3-04, D8-04 | Logout owner/bearer optional/idempotent/expired access và session bị revoke |
| FR-AUTH-006 | D3-01, D8-01 | Profile an toàn, đúng user, unauthenticated/error |
| FR-AUTH-007 | D3-06, D8-03 | Sửa/xóa avatar/tên/bio, validation/atomicity/session sync |
| FR-CAT-001 | D4-02, D5-01–03, D6-01 | Name A–Z/count Published, empty/cache/ISR |
| FR-CAT-002 | D4-01–02, D6-01 | Detail/paging/privacy và HTTP 404/rendering đúng |
| FR-CAT-003 | D2-02–04, D6-01 | Admin create, duplicate/validation/unique slug |
| FR-CAT-004 | D2-03, D6-01 | Admin update, slug ổn định, not-found/duplicate |
| FR-CAT-005 | D6-01, D8-01 | Admin soft delete, chặn category còn recipe theo baseline |
| FR-RCP-001 | D4-03, D6-02, D6-04 | List/filter/sort/page/public-private đúng API/UI |
| FR-RCP-002 | D4-01–03, D6-02, D6-12 | Detail/composition/privacy/404/rendering/JSON-LD |
| FR-RCP-003 | D2-04, D6-02, D8-05 | Draft wizard/transaction/UoW/validation/owner |
| FR-RCP-004 | D6-02, D6-11, D8-01 | Update owner/Admin/ETag/race và slug contract |
| FR-RCP-005 | D4-05, D6-02, D8-06 | Publish/unpublish invariants và public/cache visibility |
| FR-RCP-006 | D4-05, D6-02, D8-06 | Archive/unarchive đúng trạng thái/quyền/visibility |
| FR-RCP-007 | D2-04, D6-02, D8-01 | Soft delete/version/privacy/retained data theo baseline |
| FR-RCP-008 | D6-03, D6-05–07 | Upload/primary/delete/ownership/concurrency/media privacy |
| FR-RCP-009 | D6-03, D8-05 | Ingredient CRUD/nullability/order/limits/quyền |
| FR-RCP-010 | D6-03, D8-05 | Step CRUD/timer/append/renumber liên tục/transaction |
| FR-SRCH-001 | D6-04, D8-07 | FTS accent/rank/score/q/input/empty/Published |
| FR-SRCH-002 | D6-04, D8-07 | AND filters đầy đủ API/UI/URL/cache keys |
| FR-SRCH-003 | D6-04, D8-07 | Allowlist/hai chiều/default relevance/tie-break |
| FR-SRCH-004 | D6-04, D8-07 | Offset/page bounds/pageSize/meta/empty |
| FR-FILE-001 | D6-05, D8-01 | 5 MB/allowlist/signature/MinIO/quyền/private object |
| FR-FILE-002 | D6-07, D9-06 | Delete object/idempotency/retry/storage lỗi |
| FR-JOB-001 | D6-08, D9-06 | HTML/Hangfire/outbox/1-5-30/Failed/restart/concurrency |
| FR-JOB-002 | D6-06, D9-06 | 800×600/300×300/3 versions/retry/fallback thật |
| FR-JOB-003 | D6-09, D9-09 | 02:00 UTC/retry 2/public sitemap/singleton/sai khác ping |
| FR-OBS-001 | D9-05–06 | Aggregate/live/ready/dependency matrix không lộ nội bộ |
| FR-OBS-002 | D2-06, D9-05 | Structured correlated request/pipeline/audit/slow logs |
| FR-OBS-003 | D9-05 | HTTP/EF/custom traces/metrics/export/dashboard thật |

### 5.2. Đủ 30 NFR

| Mã | Công việc | Điều kiện phải có khi đóng |
|---|---|---|
| NFR-PERF-001 | D9-01 | Warm GET p50 ≤150ms, API p95 ≤500ms/p99 ≤1000ms |
| NFR-PERF-002 | D9-01, D9-09 | ≥100 concurrent users trên profile yêu cầu và scaling có phép đo |
| NFR-PERF-003 | D5-01–04, D9-01 | TTL/cơ chế/invalidation đúng, Redis hit ≥80% |
| NFR-PERF-004 | D2-06, D9-02 | Không N+1, projection/index/EXPLAIN/slow query review |
| NFR-PERF-005 | D4-02–03, D9-03 | CWV/INP/JS gzip/ISR/image optimization đúng ngưỡng |
| NFR-SEC-001 | D3-01 | Identity/PBKDF2/iterations/password complexity |
| NFR-SEC-002 | D3-02–05, D8-04 | JWT claims/signature/15 phút, refresh hash/7 ngày/rotation/family |
| NFR-SEC-003 | D5-05–07 | Sliding auth/fixed global/quota/headers/proxy/multi-instance |
| NFR-SEC-004 | D2-02, D6-05, D9-04 | Input pipeline/parameterization/CSP/MIME/prebuffer size/path safety |
| NFR-SEC-005 | D9-04 | TLS/HSTS/redirect/CORS/cookie policy thực trên deployment |
| NFR-SEC-006 | D2-03, D6-02–03, D8-01 | Application policies/ownership/sensitive write/audit |
| NFR-SEC-007 | D9-04, D10-05 | Secrets ngoài Git, env/User Secrets, scans/rotation policy |
| NFR-USE-001 | D8-09–10 | Viewports và thiết bị/browser đúng phạm vi |
| NFR-USE-002 | D8-09–10 | WCAG AA/semantic/ARIA/keyboard/contrast/screen reader |
| NFR-USE-003 | D8-08 | Field/server errors, Problem Details, error codes/i18n-ready |
| NFR-USE-004 | D8-08 | Loading/skeleton/optimistic rollback/toast/upload progress |
| NFR-REL-001 | D0-04, D9-08 | SLA có cửa sổ đo, probe/alert/maintenance 48h |
| NFR-REL-002 | D5-04, D6-06–09, D9-06 | Graceful handler/dependency failure/retry/recovery |
| NFR-REL-003 | D6-14, D9-07 | Backup 03:00/30 ngày/persistent data/token DB/restore |
| NFR-MAINT-001 | D1-02–03, D7-04–05, D10-06 | Static analysis/format/build warnings/review PR theo quyết định |
| NFR-MAINT-002 | D7-01–04, D8-01–07 | Application unit ≥80%, mỗi endpoint happy/error, năm browser E2E |
| NFR-MAINT-003 | D6-15, D10-01 | Setup <5 phút, XML/Scalar/OpenAPI, ADR/CHANGELOG |
| NFR-MAINT-004 | D2-01–07, D7-01 | Dependency/CQRS/trách nhiệm đúng tầng và architecture test |
| NFR-SCALE-001 | D5-03–07, D9-09 | Stateless/shared state/queue/lock/multi-worker |
| NFR-SCALE-002 | D9-02 | Pool max 100/index strategy; phần tùy chọn ghi rõ |
| NFR-SCALE-003 | D9-09–10 | Containers/Nginx upstream/MinIO distributed/CDN |
| NFR-SEO-001 | D6-12 | Recipe JSON-LD/Rich Results, dữ liệu thật, GAP-033 |
| NFR-SEO-002 | D4-01, D6-10 | Title/description/OG/Twitter/canonical/robots/404 |
| NFR-SEO-003 | D0-03, D6-09 | Sitemap schedule/fields/public URLs/robots/ping decision |
| NFR-SEO-004 | D6-11 | Slug unique/không dấu/stable published/301 draft history |

### 5.3. Đủ 10 CONS và yêu cầu không chỉ nằm trong mã FR/NFR

| Mã | Công việc | Điều kiện phải có khi đóng |
|---|---|---|
| CONS-001 | D2-01, D2-07 | Bốn layer và Domain độc lập |
| CONS-002 | D2-01 | Mỗi use case Command/Query Handler đúng trách nhiệm |
| CONS-003 | D1-03, D10-01 | .NET 10 Minimal APIs, Next App Router/runtime đã chốt |
| CONS-004 | D3-01–05 | Stateless JWT/Identity/PBKDF2/access-refresh TTL |
| CONS-005 | D6-13, D6-15, D8-01 | REST/version path/Problem Details/method/status/header |
| CONS-006 | D6-13–14, D9-02 | PostgreSQL/EF Code First/parameterization/migrations |
| CONS-007 | D6-05 | ≤5 MB/JPEG-PNG-WebP-AVIF/MIME checks |
| CONS-008 | D2-02 | FluentValidation/Application pipeline, endpoint chỉ binding |
| CONS-009 | D1-01, D9-10 | Multi-stage Docker/Compose và runtime packaging |
| CONS-010 | D2-06, D9-05 | Serilog/CorrelationId/RequestPath/UserId/context |

- [ ] **UI/§5.1/5.4:** D4 và D8 bao phủ từng route/rendering/auth/redirect/browser, không chỉ public homepage.
- [ ] **Kiến trúc/§6:** D2/D5/D6/D9 bao phủ pipeline, layers, stack, queue/storage/cache và interfaces.
- [ ] **Dữ liệu/API/Chương 7–8/phụ lục:** D0-05/D6-13–15/D8-01 đối chiếu từng field/constraint/endpoint/error/header và migration, không suy đủ schema từ bảng 74 mã.
- [ ] **Giả định/phụ thuộc/§2.6:** D1/D3/D6/D9 xác minh seed, provider fallback, SMTP/MinIO/PostgreSQL/Redis và điều kiện chạy; sai khác so với PDF được ghi riêng.

## 6. Cách chạy và thu thập bằng chứng

Các lệnh dưới đây đã có công cụ/script trong repository; chạy đúng thư mục và runtime D1. Các gate coverage/Playwright mới chỉ được thêm theo D7/D8, chưa được xem là có sẵn chỉ vì xuất hiện trong kế hoạch.

```powershell
# backend/
dotnet restore CulinaryBlog.slnx --locked-mode
dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore
dotnet build CulinaryBlog.slnx --configuration Release --no-restore
dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --configuration Release --collect 'XPlat Code Coverage'
dotnet test tests/CulinaryBlog.ArchitectureTests/CulinaryBlog.ArchitectureTests.csproj --configuration Release
dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj --configuration Release

# frontend/
npm ci
npm run generate:api
npm run format
npm run lint
npm run build
npm run typecheck
npm test
npm run test:isr -- --skip-build
npm run budget:check
```

`test:isr --skip-build` chỉ dùng khi build vừa tạo đúng mã hiện tại; sau khi sửa source phải build lại. Kiểm thử stack thật D4-05/D8 dùng backend test riêng, không được thay bằng mock. Truy vấn API/test dùng lại OpenAPI và các test/request hiện có; mô phỏng HTTP dependency dùng Postman simulation local khi phù hợp, không publish workspace/mock ra ngoài trong quá trình thu thập evidence local.

Performance/security/restore/failure phải dùng profile và môi trường D9 đã ghi; không tự chạy lệnh gây downtime hoặc restore trên stack người dùng. Kết quả chi tiết có thể lưu dưới `tmp/srs-audit/`/`artifacts/` được ignore; báo cáo tổng hợp phải chứa summary và vị trí lưu evidence bền vững khi nghiệm thu.

Mẫu bản ghi để đóng từng công việc:

| Trường | Bắt buộc ghi |
|---|---|
| Task / requirement / acceptance | Ví dụ D4-01 / FR-CAT-002 / missing slug HTTP 404, kèm trang PDF |
| Phân loại / trạng thái | Sửa, Kiểm chứng, Quyết định, Môi trường / Chưa làm, Đang làm, Chờ điều kiện, Đạt, Không đạt |
| Mã nguồn / quyết định | File/line/commit hoặc decision ID, tác động API/data/migration nếu có |
| Kiểm tra | Tên test/case/lệnh, expected/actual, exit code, pass/fail/skip và thời gian |
| Phiên bản / môi trường | SHA/worktree, image digest nếu chạy container, runtime/config/dataset đã lược secrets |
| Loại bằng chứng | Unit, integration, JSDOM/source, browser thật, simulation, provider/staging/production thật |
| Link evidence / người xác nhận | TRX/coverage/trace/report/run URL và người thực hiện/xác nhận khi có |
| Điều kiện còn mở | Credential/thiết bị/cửa sổ đo/decision, ảnh hưởng và bước tiếp theo; không thay bằng dấu pass |

## 7. Tổng kiểm trước khi kết luận hoàn thành

- [ ] G01 được đóng bởi D7/D8: Application unit ≥80%, coverage gate và năm browser E2E.
- [ ] G02 được đóng bởi D3: Google/local refresh/logout/owner/provider evidence rõ.
- [ ] G03 được đóng bởi D4: HTTP 404/rendering/TTL và revalidation xuyên suốt.
- [ ] G04/G05 được đóng bởi D5: sliding/headers/multi-instance và cache theo contract.
- [ ] G06 được đóng bởi D6: metadata/sitemap/slug redirects/JSON-LD và quyết định sai khác.
- [ ] G07 được đóng bởi D2: CQRS/validation/quyền/transaction/log đủ mọi use case.
- [ ] G08 được đóng bởi D1/D7: format và toàn bộ quality gate bản cuối xanh.
- [ ] G09 được đóng bởi D8–D10: phi chức năng/artifact/UAT/deployment evidence đúng phạm vi.
- [ ] Đủ 74 mã, UI/architecture/data/API và các acceptance chi tiết D0; sai khác mới phát hiện không bị bỏ ngoài kế hoạch.
- [ ] Phân biệt rõ “đã viết mã”, “đạt local”, “đạt provider/browser”, “đã nghiệm thu toàn bộ”; không lấy lịch sử phía dưới thay kết quả hiện tại.

## Nhật ký bằng chứng

Các đợt dưới đây là **lịch sử triển khai**, giữ để truy vết, không phải kết quả mới của kế hoạch D0–D10. Các mục chưa đánh dấu bên trên vẫn thuộc phạm vi phải hoàn thiện. Các tài liệu bài giảng người dùng đang chuyển thư mục được giữ nguyên. Lượt cập nhật checklist này chỉ lập kế hoạch, chưa sửa chương trình hoặc chạy lại các test lịch sử.

### Đợt triển khai form, avatar, pipeline và job — 23/10/2026

- Avatar: form hồ sơ cho phép đặt hoặc xóa URL HTTP/HTTPS, validation Zod, lỗi theo trường từ API, giữ nội dung khi lưu thất bại. `AuthLifecycleTests.ProfileAvatarCanBeUpdatedAndClearedAndInvalidUrlsDoNotPartiallyUpdateProfile` chứng minh API không cập nhật một phần khi URL sai; test React chứng minh thao tác giao diện.
- Form: RHF/Zod cho đăng ký, đăng nhập, hồ sơ, đổi/quên/đặt lại mật khẩu, danh mục, thông tin công thức, nguyên liệu và bước nấu. Giữ validation backend, cảnh báo rời trang, ETag và các bước wizard. Dùng `useWatch` cho dữ liệu form cần hiển thị phản ứng.
- UX: toast ở hồ sơ/danh mục/lưu và xuất bản công thức; sửa danh mục có optimistic update và rollback. Test React xác nhận danh sách khôi phục và nội dung đang sửa được giữ nguyên khi API lỗi.
- Pipeline: auth/category/recipe/composition đã chuyển sang named requests; các public query category/detail dùng CachingBehavior. CacheInvalidationBehavior chạy sau ghi thành công, không chạy sau lỗi quyền/validation/handler và vẫn hoàn tất sau client disconnect. Bộ 6 `ContentPipelineTests` chạy qua DI + MediatR thật với dependency nghiệp vụ giả lập. Chưa đánh dấu hoàn thành toàn bộ CQRS vì audit/sitemap query và rà soát validation toàn diện còn lại.
- Welcome email: outbox dispatcher đưa job vào Hangfire PostgreSQL, khóa hàng SKIP LOCKED và lưu JobId; job gửi HTML có tên được encode và link cấu hình tin cậy. `WelcomeEmailJobTests` chứng minh queue đúng job một lần, bỏ qua retry chưa đến hạn, SMTP thực qua MailHog, cạnh tranh hai worker, lịch retry và việc giữ dữ liệu đăng ký khi SMTP lỗi. Không dùng SMTP hoặc tài khoản Google thật. SMTP không có bảo đảm exactly-once nếu tiến trình chết sau khi máy chủ nhận thư nhưng trước khi transaction commit.
- Ảnh: canvas medium 800x600 và thumbnail 300x300 giữ tỷ lệ bằng viền trắng; assertion decode ảnh WebP đầu ra trong `RecipeCompositionApiTests` kiểm tra kích thước thực. Bộ test vẫn kiểm tra MIME/signature/size, quyền media và retry job.
- Migration `20261023061828_WelcomeEmailHangfire` thêm JobId nullable; test upgrade bổ sung dữ liệu outbox cũ và kiểm tra bảo toàn. Chưa áp dụng migration này vào stack dữ liệu người dùng đang chạy.
- Kết quả frontend: 102/102 test đạt với Node 22.23.3, Next 15.5.28; bao gồm 29 test tương tác JSDOM, chưa phải 5 E2E browser thật. Build production đúng runtime đã đạt; route `/` và `/categories` vẫn dynamic nên ISR chưa hoàn tất. Đã đồng bộ node_modules theo package-lock; Node tạm tải từ nodejs.org và xác minh SHA256, nằm trong tmp được ignore.
- Backend: 73/73 unit và 3/3 architecture đạt. Full integration cuối có **76/76 đạt**, gồm kiểm tra nâng cấp database giữ dữ liệu outbox cũ và cả 3 WelcomeEmailJobTests. Đã sửa test kỳ vọng tên migration cũ thay vì bỏ assertion.
- Coverage Application mới đo: **60,77% line; 65,30% branch**, chưa đạt 80%. Các handler mới làm tăng tổng mã cần được kiểm thử; không dùng integration coverage hoặc loại trừ handler để thay thế yêu cầu unit coverage.
- Backend format và frontend Prettier đã đạt. TRX, XML coverage và log frontend tại `tmp/srs-audit/` (Git ignore). Chưa commit/push các thay đổi của đợt hoàn thiện này.


### Đợt tìm kiếm và danh mục — 23/10/2026

- Bổ sung `categoryId`, `maxCookTime`, `minServings`, `minPrepTime`, `maxPrepTime` cho tìm kiếm. Các điều kiện kết hợp AND với category slug, difficulty và tổng thời gian; cả categoryId và slug cùng được gửi thì phải cùng khớp. `maxCookTime=0` là giá trị có nghĩa và được giữ trong URL/cache.
- Giao diện có form RHF/Zod cho bộ lọc số, báo lỗi khoảng chuẩn bị đảo ngược, giữ các điều kiện khi đổi sort/trang, đưa về trang 1 khi thay bộ lọc/pageSize, phục hồi form khi URL thay đổi. Có các chiều tăng/giảm theo tên, ngày tạo, thời gian nấu, chuẩn bị, khẩu phần; tổng thời gian nhanh nhất; pageSize 12/24/50 và giá trị hợp lệ từ URL. Các alias `createdAt`, `-createdAt`, `title`, `-title` hiển thị đúng lựa chọn tương ứng.
- Khi có từ khóa, mặc định `relevance`; trả `relevanceScore` ts_rank từ PostgreSQL với trọng số title/description. Khi duyệt không từ khóa, frontend dùng `GET /recipes`, mặc định ngày tạo mới trước và score null. Đã mở rộng list với category slug/tổng thời gian để giữ đầy đủ bộ lọc ở luồng duyệt.
- **Điều chỉnh contract:** `/recipes/search` yêu cầu q có ít nhất 2 ký tự sau trim, đúng điều kiện của FR-SRCH-001. Request thiếu/rỗng/quá ngắn trả 400 theo baseline GAP-002; không dùng endpoint search để duyệt tất cả nữa. API browse không yêu cầu q. Các caller trong repository đã được rà soát và frontend được đổi theo contract này.
- Allowlist sort, hướng tăng/giảm, phân trang và tie-break bằng ID được dùng nhất quán cho list/search. Cache key bao gồm toàn bộ bộ lọc mới; normalize search/category thống nhất với key. Không trả Draft/Archived qua các public endpoint.
- OpenAPI và `frontend/src/lib/api/schema.d.ts` đã cập nhật/tạo lại. Trường score thêm dạng nullable, không bắt buộc cho các resource khác. Chưa đánh dấu hoàn thành toàn bộ OpenAPI vì các nhóm rate limit/SEO còn có thể đổi contract.
- Danh mục public theo Name tăng dần, không phụ thuộc orderIndex. Giao diện quản trị giữ thứ tự orderIndex để quản lý, đổi nhãn thành “Thứ tự quản trị” và giải thích public A–Z. Integration test dùng tên Aaa/Zzz với orderIndex đảo ngược để xác nhận API.
- Kiểm thử cuối: **75/75 unit**, **3/3 architecture**, **99/99 integration**, **104/104 frontend** đều đạt. 29 integration cases của RecipeDiscoveryApiTests bao phủ bộ lọc, score/rank, từ khóa không hợp lệ, sort/hướng, cache/privacy/outage, danh mục và migration/index. 31 bài React/JSDOM trong bộ frontend, chưa thay thế 5 E2E browser thật.
- ESLint, TypeScript, Prettier và backend format đạt. Build production với Node 22.23.3/Next 15.5.28 đạt. Bộ kiểm ngân sách JS trên build mới đạt mọi route; trang recipes khoảng 155 KB gzip (giới hạn 200 KB). Đây là kiểm tra bundle, chưa phải bằng chứng tải/Lighthouse/accessibility trên browser thật.
- Coverage Application sau thay đổi: **62,56% line; 67,24% branch**, vẫn dưới 80%. XML tại `tmp/srs-audit/coverage-discovery-final/`; TRX tại `tmp/srs-audit/backend-discovery-final/`; log frontend tại `tmp/srs-audit/frontend-discovery-final.txt`.
- Chưa commit/push hoặc cập nhật container ứng dụng người dùng. Các nhóm Google session, ISR/Output Cache, rate limiter nhiều instance, SEO, CQRS/validation còn lại, coverage và browser E2E/nghiệm thu vẫn giữ trong phạm vi công việc.
