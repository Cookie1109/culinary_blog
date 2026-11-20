# Kết quả gói G01 — Nguồn yêu cầu và môi trường

- **Trạng thái tổng: Chờ quyết định; baseline03 Chưa đạt.** Đã xử lý01/02/03 trong phạm vi gói; không thực thi gói khác hoặc phase10/28/36.
- Ngày chạy: **24/10/2026, Asia/Bangkok**, Codex; baseline từ13:03 đến13:13. HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`, nhánh `main`, index trống; đầu vào84 đường dẫn dirty/untracked. Không có ủy quyền commit/push mới trong lượt giao gói; chưa commit/push/deploy.
- Snapshot trước: `tmp/package-G01/before.json`,574 đường dẫn tracked/untracked, SHA256 `62766ccf17bbf559e2721dbe51b00e87b0ae5236de282a7a54ef13ec54edb65d`. Snapshot sau và kiểm bảo toàn ở E13. HEAD riêng lẻ không tái hiện được dirty source đã kiểm.
- Nguồn chuẩn: PDF SRS71 trang, SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`; giữ nguyên [đính chính A/B/C](../approvals/2026-10-23-abc-correction.md), không tạo ngoại lệ, protocol hay approval mới.
- Đã đọc AGENTS.md, README gốc/điều phối, work-packages, progress, phase01–03, report01/02, inventory/register và checklist phần D0/D1/mục6. Chưa có report/checkpoint G01 trước lượt này. Áp dụng PDF và Postman API Engineer → API Testing/API Simulation; tái dùng contract, test và simulation local, không publish.

## Phase 01

**Đạt ở phạm vi tài liệu; tái dùng inventory đã kiểm.** Không lập lại347 acceptance và không nhận test baseline làm nghiệm thu toàn bộ SRS.

| Task | Acceptance/trang PDF | Kết quả | Evidence/phần còn mở |
|---|---|---|---|
| D0-01 |34FR tr.17–39,30NFR tr.40–45,10CONS tr.14–15 | Giữ đóng | E01;347 acceptance, đủ74 mã. [Report01](phase-01-result.md) và [inventory](../acceptance-inventory.md) vẫn là sản phẩm tài liệu; runtime rows không tự chuyển Pass |
| D0-05 | UI/architecture/data/API/phụ lục tr.46–71 | Giữ đóng | E01;207 contract rows/45 operations, links/hash hợp lệ; các gap vẫn có owner |

So snapshot nguồn của report01 với đầu vào G01: sáu file frontend của D1-02 đã đổi bytes từ lần inventory. Đã kiểm lại cấu hình/script và các gate hiện tại ở E04–E09; không suy các thay đổi đó là đã đạt HTTP. API operations vẫn45; expected trong inventory không đổi. Không có căn cứ viết lại inventory hoặc đổi contract chỉ từ việc format/test source đã thay đổi.

## Phase 02

**Chờ quyết định.** Tái dùng [register](../decision-register.md) và [report02](phase-02-result.md); E01 kiểm lại cấu trúc/thẩm quyền, đối chiếu trực tiếp PDF tr.40/43/44 và đính chính hiện tại. Không xác minh lại provider bên ngoài trong lượt này; nguồn web ngày23/10 trong register là evidence lịch sử.

| Task | Acceptance/trang PDF | Kết quả | Evidence/phần còn mở |
|---|---|---|---|
| D0-02 | GAP-001–033; API/data/UI tr.17–71 | Giữ đóng phần rà/ghi nhận | E01;33 GAP/25 DEC/12 INV-G có mapping; các sai khác thực thi vẫn mở |
| D0-03 | Output Cache, ping, tools, browser, RedLock; tr.14/28/39–49 | Giữ đóng phần ghi nhận | E01; không dùng A/B/C cũ miễn ping/RedLock/string/NOT NULL/smallint/message/routes hoặc toolchain |
| D0-04 | NFR-REL-001/003, PERF-002, SCALE-001–003; tr.40/43–44 | **Pending, giữ mở** | DEC-08–12: chưa có tiêu chí nghiệm thu thống nhất theo PDF |

Các đầu vào thiếu quyết định tiếp tục được ghi mở:

- **DEC-09:**99,5% trong365 ngày cho43,8 giờ downtime;3,65 giờ tương ứng khoảng99,958333%. Cần giải mâu thuẫn target, kỳ đo, timezone và cách tính maintenance/thiếu probes. Không phục hồi protocol30 ngày cũ.
- **DEC-10:** PDF chỉ ghi backup03:00; Test Strategy§4 có03:00UTC. Giữ căn cứ baseline này nhưng không gọi UTC là nguyên văn PDF; implementation lịch/restore thuộc34.
- **DEC-11/12:** giữ ngưỡng latency/cache/hardware/concurrency và topology production trong PDF. Chưa có workload, thời lượng và cách đánh giá tăng tuyến tính/không degradation; không tự thêm tolerance. Không có production evidence từ G01.
- **DEC-13/15:** PUT optional đã rõ nhưng omit/null/empty/merge chưa đủ; query sort/sortBy/sortOrder đồng thời, field thiếu và allowlist chưa đủ. Phần validation/contract phụ thuộc phải làm rõ trước khi triển khai; không áp mặc định null/merge/conflict400 từ approval cũ.
- **DEC-02:** ping provider đã ngừng là vướng mắc trong register; sitemap/robots không thay waiver. Không chặn baseline03 hoặc công việc độc lập của G02.

## Phase 03

**Chưa đạt vì gate ISR và npm audit thất bại; môi trường container đã dùng được ở lượt cuối.**

| Task | Acceptance/trang PDF | Kết quả | Evidence/phần còn mở |
|---|---|---|---|
| D1-01 | Runtime/Docker; CONS-003/009 tr.14–15 | **Pass, đóng** | E02/E03/E10/E11: Node22.23.3/npm10.9.9, .NET10.0.402, Docker Engine29.7.2; PostgreSQL/Redis test riêng; MinIO/SMTP smoke riêng |
| D1-02 | NFR-MAINT-001 tr.43 | **Pass, đóng** | E04/E05: cả sáu file được giao đã đúng Prettier trước sửa, format toàn frontend/backend xanh; không cần format lại |
| D1-03 | Clean/locked restore và baseline; CONS-003/NFR-MAINT tr.14/43 | **Fail, giữ mở** | E03–E12: build/lint/typecheck/tests đạt ở lượt cuối, nhưng ISR trả200 thay404 và audit có4 high/1 moderate; không bỏ gate |
| D1-04 | Truy vết phiên bản/môi trường của phép kiểm | **Pass, đóng** | HEAD/snapshot/runtime/profile và lệnh/count/exit dưới đây; E13 bảo toàn worktree. Không tạo commit/image digest/CI URL giả |

### Thay đổi và profile

- Chỉ sửa cấu hình runtime [.mise.toml](../../../../.mise.toml): Node20.19.5→22.23.3 theo ADR-010/package engines/CI, .NET10.0.401→10.0.402 theo global.json/CI. Không đổi major Next hoặc lockfile.
- Node PATH của host là22.18.0; mọi npm/script frontend trong baseline dùng `D:\Culinary_Blog\tmp\srs-audit\runtime\node-v22.23.3-win-x64\node.exe` cùng npm-cli.js10.9.9 và prepend thư mục này vào PATH tiến trình. Không thay PATH hệ thống. Python3.13.7, PowerShell7.6.6.
- Backend Release trên source hiện tại. Integration dùng AuthApiFactory/Testcontainers, PostgreSQL database `culinary_blog_auth_test` với tài khoản synthetic, Redis/cổng ngẫu nhiên; migration chỉ chạy DB thử. Storage và password-reset mail trong fixture là in-memory/captured, **không chứng minh provider/storage/email nghiệp vụ thật**.
- MinIO/MailHog smoke dùng container tên `culinary-g01-*`, label `culinary.test.package=G01`, image digest của Compose, cổng loopback ngẫu nhiên; MinIO `/data` trên tmpfs, không mount volume người dùng. MinIO live200, MailHog HTTP200/SMTP EHLO+NOOP250; không gửi thư. Hai container đã stop/remove.
- Next build dùng `INTERNAL_API_BASE_URL=http://127.0.0.1:1/api/v1`, không gọi stack người dùng. ISR dùng đúng build này, standalone server/cổng ngẫu nhiên, secret ngẫu nhiên trong memory và Postman `public-isr.sim.yaml`/fixture synthetic có sẵn. Không browser/provider/deployment thật; không dùng build hôm trước.
-13 container người dùng có cùng ID/image/port trước/sau test; không chạy compose up/down, stop/restart/migrate stack đó. Testcontainers/Ryuk và container G01 không còn sau test.

## Evidence dùng chung

Cwd backend=`D:\Culinary_Blog\backend`, frontend=`D:\Culinary_Blog\frontend`, root=`D:\Culinary_Blog`. Thời gian là24/10/2026 UTC+7. Mọi lệnh dưới đây **đã chạy**, không phải kế hoạch. Lệnh npm được gọi qua Node/npm-cli.js pin ở trên. Logs và command/start/duration/exit JSON tại `tmp/package-G01/<label>.log/.json` (ignored); summary bền vững là report này.

| ID | Cwd/lệnh thực tế | Expected / actual / exit | Loại evidence và log |
|---|---|---|---|
| E01 | root: `python docs/specs/srs-completion-phases/verify-inventory.py`; `python docs/specs/srs-completion-phases/verify-decisions.py`; PdfReader tr.14/15/40/43/44 | Exit0/0;74 mã,347 acceptance,207 CT,45 operations,82 links;33 GAP/25 DEC/12 INV,11 quyết định bị thay hiệu lực,5 vấn đề nguồn chưa rõ,226 links,3 baseline hashes giữ nguyên | Documentation/source; excerpt `srs-source.txt`. Không chứng nhận runtime bằng count |
| E02 | root: `docker version`; `docker image ls --format ...`; `docker ps --format ...`; `docker compose config --quiet` | Docker Desktop4.90.0/Engine29.7.2 linux/amd64, API1.55; images PG16.10/Redis7.4.5/MinIO/MailHog có sẵn; Compose exit0 | Environment/config; `docker-version`, `docker-images`, `docker-running-before`, `compose-config` |
| E03 | backend: `dotnet tool restore`; `dotnet restore CulinaryBlog.slnx --locked-mode` | Exit0/0;dotnet-ef10.0.13,7 projects locked restore thành công | Restore; `backend-tool-restore`, `backend-restore-authorized`,13:05 |
| E04 | backend: `dotnet format CulinaryBlog.slnx --verify-no-changes --no-restore --report ../tmp/package-G01/backend-format-report.json`; `dotnet build CulinaryBlog.slnx --configuration Release --no-restore` | Exit0/0; build0 warning/0 error | Format/build; `backend-format`, `backend-build`,13:05–13:06 |
| E05 | frontend: `npm ci`; `npm run generate:api`; `npm run format` | Exit0/0/0;515 packages installed/516 audited; codegen không đổi bytes schema; toàn frontend đúng Prettier | Clean install/codegen/format; `frontend-ci-authorized`, `frontend-generate`, `frontend-format-before`,13:06–13:07 |
| E06 | backend: `dotnet test tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj --configuration Release --no-build --no-restore --collect 'XPlat Code Coverage' --logger 'trx;LogFileName=unit.trx' --results-directory ../tmp/package-G01/test-results/unit`; tương tự ArchitectureTests, không collect, log architecture.trx | Exit0/0; unit75 pass/0 fail/0 skip, architecture3/0/0 | Unit/architecture,13:06; `backend-unit`, `backend-architecture`, TRX dưới `test-results/`; Application line coverage60,65%, chưa đạt ngưỡng80% của owner23 |
| E07 | frontend: `npm run lint`; `npm test`; `npm run build`; `npm run typecheck` | Exit0 cả4; Node/JSDOM/source104 pass/0 fail/0 skip; build Next15.5.28 thành công, có cảnh báo chưa phát hiện Next ESLint plugin | `frontend-lint`, `frontend-tests`, `frontend-build`, `frontend-typecheck`,13:07–13:09. Không gọi Node/JSDOM là Jest/Testing Library/Playwright |
| E08 | frontend: `npm run test:isr -- --skip-build` | **Exit1**, expected `/categories/missing-category`404, actual200 tại check-isr.mjs:213 | Local production HTTP + Postman simulation; `frontend-isr`, `tmp/srs-audit/isr/{simulation,next}.log`,13:09–13:11. Không có evidence.json mới để claim cả bài pass |
| E09 | frontend: `npm run budget:check` | Exit0; mọi entry≤200KiB gzip, max190,3KiB | Build artifact check; `frontend-budget`,13:09; không phải browser Core Web Vitals/RUM |
| E10 | backend: `dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj --configuration Release --no-build --no-restore --logger 'trx;LogFileName=integration.trx' --results-directory ../tmp/package-G01/test-results/integration-retry` | **Exit0;99 pass/0 fail/0 skip**,166,69s tổng/2m39s test;13:09:18–13:12:05 | Integration với PG/Redis thật riêng; `backend-integration-retry`, `test-results/integration-retry/integration.trx` |
| E11 | root: `python tmp/package-G01/dependency-smoke.py`; Docker đọc tài nguyên trước/sau | Exit0; MinIO200, MailHog200/EHLO250/NOOP250; không container G01/Testcontainers còn lại;13 ID người dùng giữ nguyên | Dependency smoke, không nghiệm thu upload/welcome job; `dependency-smoke`, `dependency-smoke-results.json`, `docker-running-after`, `test-resources-after`, `g01-resources-after` |
| E12 | frontend: `npm audit --audit-level=high --json`; backend: `dotnet list CulinaryBlog.slnx package --vulnerable --include-transitive` | Npm **exit1**,4 high (`chromium-bidi`, `puppeteer-core`, `undici`, `zod`),1 moderate (`fast-uri`); NuGet exit0,7 projects không vulnerable theo nguồn hiện tại | Existing audit gates; `frontend-audit`, `backend-vulnerable`,13:07/13:12; không tự update dependency hoặc miễn gate |
| E13 | root: snapshot SHA256 các file, `git diff --check`, hai verifier và so bảo toàn trước/sau | Kết quả kiểm cuối ghi ở mục kiểm bảo toàn bên dưới | Repository/documentation; không thay build/test từ source khác |

### Lỗi môi trường và giới hạn lần chạy

-13:03–13:07: Docker CLI không thấy named pipe Engine, kể cả ngoài sandbox. Lượt integration đầu exit1:41 pass/58 fail/0 skip;58 lỗi fixture là DockerUnavailableException, chưa chạy assertion nghiệp vụ của các case đó. Giữ log/TRX đầu ở `backend-integration`/`test-results/integration/`; không cộng chúng với kết quả99/99.13:09 Engine phản hồi; chạy lại toàn integration E10 trên cùng source thay evidence môi trường cũ. Agent không khởi động Docker Desktop/stack người dùng.
- Restore đầu bị sandbox chặn NuGet.Config; npm ci đầu gặp EACCES tới registry và `Exit handler never called`. Hai lần retry được công cụ chấp thuận đã thành công; log lỗi vẫn giữ. Không dùng cài dependencies dở dang làm baseline.
- Codegen thực tế exit0 và file schema không đổi. Helper in log lần đầu lỗi console cp1252 khi gặp ký tự Unicode sau khi lưu exit0; đã sửa helper stdout UTF-8. Đây không phải codegen fail.
- ISR đã đi qua các check MISS/HIT/header TTL3600/revalidation/detail trước assertion404. Không suy bài tổng pass hoặc đã chờ đủ TTL3600 giây. Khi assertion xảy ra, cleanup trong sandbox treo chờ child exit; đã xác nhận PID/parent/commandline và dọn đúng Next29784/Postman26404 cùng descendants bằng taskkill ngoài sandbox. Runner kết thúc với assertion200≠404, không sửa assertion/source để xanh.
- Chẩn đoán HTTP riêng cùng server thấy `/recipes/missing-recipe` cũng200. Cả hai có API upstream404 trong simulation.log; **owner11/G03** cần kiểm/sửa HTTP not-found. Không thực thi phase11 trong G01.
- Application coverage60,65%, Next ESLint-plugin warning và audit được chuyển bằng evidence cho owner23/29/32 khi được giao. Không thêm analyzer/Jest/Playwright hoặc đổi ruleset trong G01; baseline này không tự đóng NFR-MAINT.

## Điểm chặn và cập nhật tiến độ

- D0-01/02/03/05 giữ đóng phạm vi tài liệu; D0-04 giữ mở. Đóng D1-01/02/04, D1-03 mở vì gates thất bại. Checkbox trong checklist gốc và file phase03 khớp kết quả này.
- Progress:01=Đạt tài liệu;02=Chờ quyết định;03=Chưa đạt. Dòng G01 dẫn report này và ghi cả hai điều kiện; không sửa dòng gói/phase chưa thực thi.
- Đầu vào độc lập cho G02 đã có: inventory/current register, runtime/restore/build/format và backend test baseline trên dirty source hiện tại. D0-04 vận hành không chặn handlers/auth độc lập; omit/null/query conflict và mọi contract chưa rõ vẫn cần giữ mở ở phần phụ thuộc. G02 chỉ thực hiện khi được giao.

### Kiểm bảo toàn cuối (E13)

- `git diff --check` exit0; cảnh báo LF→CRLF của Git không phải whitespace failure. Hai verifier chạy lại sau cập nhật tài liệu exit0/0; inventory82 links, decisions228 links, counts/hash/đính chính giữ nguyên.
- Snapshot sau tại `tmp/package-G01/after.json`. Manifest nguồn `tmp/package-G01/after-source.json` gồm571 đường dẫn, loại riêng bốn tài liệu kết quả/theo dõi để không tự tham chiếu hash report; SHA256 **`6f8bcb32dcbb3b59111c9c806603ddabf00bedc7ce15ee9a16abe1cfaba4dd6d`**. Gồm `.mise.toml` đã sửa; các file source/lockfile/PDF/Postman đầu vào giữ bytes.
- Trong574 đường dẫn ban đầu,570 giữ hash,4 thay đổi đúng phạm vi: `.mise.toml`, checklist, progress, file phase03. Chỉ thêm một file tracked-candidate là report gói này; tmp/logs bị ignore. Cả84 đường dẫn dirty/untracked có sẵn đều giữ hash và trạng thái; không sửa/xóa PDF hoặc thay đổi người dùng.
- Docker ps trước/sau giống bytes,13 container người dùng giữ cùng ID/image/port; truy vấn label G01/Testcontainers sau test trả rỗng. Không có container thử còn chờ dọn.

## Checkpoint tiếp tục

- G01 đã hết phần độc lập có thể làm theo đầu vào hiện tại. Chưa ghi toàn gói/SRS Đạt; không xin giao lại từng phase.
- Khi tiếp tục: đọc report này và so HEAD/worktree fingerprint. Nếu source không đổi, tái dùng E03–E07/E09–E11; chỉ chạy lại phần bị ảnh hưởng hoặc lỗi chưa giải quyết. Không tạo lại inventory347 acceptance.
- Cần làm rõ DEC-09/11 và phần thiếu của DEC-13/15; không dùng việc chưa trả lời làm approval. Múi giờ backup giữ phân biệt baseline/PDF. Ping provider giữ vướng mắc, không tự thay yêu cầu.
- ISR404 giao đúng owner11/G03; audit giao owner29/32 khi được giao. Sau khi source/dependency liên quan được sửa, chạy lại build→typecheck→ISR/budget và audit trên đúng lockfile/artifact; đây là bước chưa thực hiện, không chạy gói khác từ checkpoint này.
- Tài nguyên thử đã dọn; stack người dùng còn nguyên13 container. Logs/TRX/coverage/command JSON ở `tmp/package-G01/`, ISR logs ở `tmp/srs-audit/isr/`, chưa upload artifact ngoài máy. Dùng summary/hash report này nếu tmp bị xóa; cần chạy lại nếu muốn xem raw evidence không còn.
