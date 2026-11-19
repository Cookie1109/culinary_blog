# Kết quả gói G05 — Coverage, integration, E2E, UX và CI

**Checkpoint 29/10/2026 (Asia/Bangkok): 14/14 task đạt trong phạm vi local của G05.** Đã xử lý D7-04/D7-05 sau phê duyệt sửa diện rộng và review bốn diagnostic. Workflow đã nối đủ gate và chạy tương đương local; **GitHub Actions của diff G05 chưa chạy**, chưa có ủy quyền publish/ref. Phase29 yêu cầu pipeline thật khi môi trường/ủy quyền có; không gọi kết quả local là CI remote xanh. Không suy số task thành tỷ lệ SRS.

- Phạm vi: phase23/24/25/26/27/29; giữ công việc có sẵn và không thực hiện gói khác hoặc phase10/28/36. Đã đọc AGENTS, README gốc/điều phối, progress, report/checkpoint, task và dependency liên quan.
- Chuẩn PDF SRS tr.42–43, Application unit ≥80%, happy/error mỗi endpoint, năm E2E, UX/responsive/a11y và Sonar/StyleCop/Airbnb/Jest/Testing Library/Playwright. Giữ A/B/C; PDF SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`.
- Runtime: .NET SDK10.0.403 (CI10.0.402 với latestPatch), Node22.23.3 tại `tmp/srs-audit/runtime/node-v22.23.3-win-x64`, npm10.9.9, Python3.13.7, Docker29.7.2. Không dùng Node host22.18.0 để nghiệm thu.
- HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`; snapshot đầu `tmp/package-G05/before-status.txt`/`before-diff.txt`, snapshot trước sửa diện rộng `phase29-before.zip`. Working tree có nhiều thay đổi trước G05; không reset/clean/stage hàng loạt, không commit/push/deploy trong G05.
- Stack riêng: PostgreSQL16.10, Redis7.4.5, MinIO digest pin, MailHog1.0.1, port loopback ngẫu nhiên và label riêng. Không sửa `.env`, DB/Redis/storage của người dùng hoặc gửi SMTP ngoài MailHog.
- Evidence nguồn cuối: [manifest 675 source hashes và 27 log hashes](evidence/G05-final-gates-2026-10-29.json). Các file `G05-final-*` là kết quả hiện tại; `G05-gates-2026-10-29.json` và các artifact không có tiền tố final giữ snapshot lịch sử.

## Phase 23

| Task | Kết quả | Evidence |
|---|---|---|
| D7-01 | Đạt local | F01/F02: thêm 28 unit case cho partial update/omit/null, validation, dispatch/authorization/cache pipeline, owner logout, stale/not-found, failure không invalidation, metadata/cancellation và media actor. |
| D7-02 | Đạt local | **888/985 dòng Application = 90,15228%**, gate hiển thị **90,15%**, branch **68,35%**. Baseline 532/952 = 55,88%. Không gộp Domain/integration hoặc exclude nghiệp vụ để tăng tỷ lệ. |
| D7-03 | Đạt local | Gate dùng chung local/CI, đọc đúng package Application và executable lines; pass report thật, reject baseline/79%/79,99%, thiếu/empty package fail closed. F01/F02/F05. |

File mới: `ApplicationRequestTests.cs`, `PartialUpdateValidationTests.cs`, `scripts/check-application-coverage.py`, `scripts/test-application-coverage.py`. Phần chưa phủ chủ yếu getters/constructors DTO và một số nhánh sort/metadata/authorization. Chuyển type/file và documentation làm sequence points đổi từ952 sang985; không exclude nghiệp vụ hoặc gộp integration. Line coverage không phải branch coverage. [Cobertura unit](evidence/G05-final-unit-coverage.cobertura.xml) lưu bền vững; không nhận coverage integration làm unit evidence.

## Phase 24

| Task | Kết quả | Evidence |
|---|---|---|
| D8-01 | Đạt local | [Matrix 47 operations](package-G05-endpoint-matrix.md), mỗi operation dẫn happy/error test cụ thể; verifier kiểm symbols và kết quả trong TRX. **237 integration có evidence pass:** full236/237, sau sửa readiness kiểm lại đúng bài MinIO1/1; 0 skip (F03/F04). F03/F04/F05. |
| D8-02 | Đạt local | Harness build, dựng dependency riêng, chờ readiness, test và cleanup thực chạy; F08 đạt trên source cuối. Không dùng backend mock cho năm critical journeys. |

Bổ sung 27 guest guards, 15 mutation bị Author khác từ chối và không đổi version, supporting GET/POST405, health 200/503 không leak, sitemap 200/404, Google HTTP adapter, delete child/version/race, category sort lỗi và categories quota429. Supporting endpoints không có business input dùng method405; không thêm ngoại lệ acceptance.

Google adapter chỉ thay Application handler để kiểm HTTP envelope/validation422; không chứng minh provider/signature/account linking hoặc đóng phase10. Controlled health là adapter evidence, không thay failure drill G06. Regression media/SMTP/jobs/migrations có F03/F04, không đổi chúng thành browser evidence.

Harness `frontend/scripts/run-e2e.mjs` copy API/Next standalone vào artifact của riêng run; proxy API tới backend thật, ép build offline không đọc deployment đang chạy, lưu BUILD_ID trước test, lock/API hashes và kiểm copy đúng BUILD_ID. API down/up trong E2E-05 chỉ dừng/restart child do run sở hữu. Cleanup kiểm label/path rồi dọn process/container/network/copy runtime/keys riêng. Traces và storageState tắt, không lưu auth tokens.

## Phase 25

| Task | Kết quả | Evidence |
|---|---|---|
| D8-03 | Đạt local | E2E-01: UI field validation → register → auto-login → profile edit → MailHog welcome capture; duplicate email cạnh field. F03/F04 giữ test SMTP fail không rollback user và retry schedule. |
| D8-04 | Đạt local | E2E-02: logout, login sai/đúng, private route, access expiry 65s, rotation, logout/private rejection và old refresh401. TTL access 1 phút chỉ ở stack thử, không đổi production TTL. |

Đã sửa race cookie thực: private middleware và Auth.js GET `/api/auth/session` không renew cookie, tránh response đọc đến muộn tái tạo phiên sau sign-out. Explicit POST sign-in/session/sign-out giữ handler gốc; GET csrf/OAuth callback vẫn giữ cookies. Jest kiểm ranh giới này, browser kiểm private/session reads không Set-Cookie và logout. Không giữ thử nghiệm hard navigation ở DashboardLayout. Google provider thật vẫn ngoài evidence của gói.

## Phase 26

| Task | Kết quả | Evidence |
|---|---|---|
| D8-05 | Đạt local | E2E-03: tạo Draft qua UI, ingredient/step/PNG upload vào MinIO thật, private preview popup/noindex/guest404; competing PUT thật gây409, giữ input rồi reload version mới. API chỉ gây competing write. |
| D8-06 | Đạt local | E2E-04: UI publish → home/list/category/search/detail; Author khác bị từ chối, Admin sửa qua UI; unpublish các view, detail404, republish/archive404. Poll đúng async invalidation, không tăng TTL production. |
| D8-07 | Đạt local | E2E-05: pho/phở, sort/filter/page/URL/history/detail/empty/invalid; delay RSC thật kiểm loading; dừng/restart API thử kiểm lỗi thân thiện và retry. API tạo ba fixture Phở vì seed50 không bảo đảm có Phở. |

Năm critical flows chạy **cùng một run trên source hiện tại**, cùng responsive/mobile ở F08; không ghép pass của các run thất bại để đóng task.

## Phase 27

| Task | Kết quả | Evidence |
|---|---|---|
| D8-08 | Đạt local | Inventory dưới đây; F06/F08. Catalog validation theo key, API message/fallback theo code; chỉ map field được biết và dùng message phía client, không render raw server detail/errors. Giữ input/rollback/retry/loading/empty và bổ sung feedback còn thiếu. |
| D8-09 | Đạt tự động | **72 tổ hợp**: 12 route × 320/767/768/1199/1200/1440; screenshot, axe AA và không overflow. **207 phép đo viền form**, minimum **5.43537:1**, đều ≥3:1. Mobile touch/Tab/Shift+Tab/Escape/Enter/focus restore, dashboard drawer/account menu đạt. |

| Vùng UX | Đã đối chiếu/sửa | Evidence/giới hạn |
|---|---|---|
| Register/Login | RHF/Zod, validation catalog, duplicate/validation cạnh field, auth code/fallback, loading/duplicate guard, toast | Browser/Jest/Node; không gọi đó là provider Google thật |
| Forgot/Reset/Change password | RHF/Zod, code mapping, server fields/current-password inline, token recovery link, busy/success/error feedback | Node/JSDOM/integration; không thêm critical flow thứ sáu |
| Profile | RHF/Zod, map DisplayName/Bio/AvatarUrl bằng client message, giữ edits, avatar clear, toast | Browser + interaction; payload chứa fake stack/secret không hiện ra UI |
| CategoryAdmin | RHF/Zod, server fields/name conflict, optimistic update và rollback, giữ input/modal/busy, feedback | Interaction kiểm reject422 và rollback, field actionable, không lộ fake SQL/stack |
| RecipeEditor | RHF/Zod, server fields/nutrition, dirty guard, 409 giữ local/reload, publish lifecycle feedback | Browser + interaction server title422; fallback không lấy `problem.detail` |
| Ingredient/Step | RHF/Zod, server fields, giữ input sau failure, disabled khi pending, toast | Interaction reject ingredient422; browser tạo ingredient/step thật |
| Upload/image mutation | Accept và MIME guard JPEG/PNG/WebP/AVIF, 5MB, progress callback thật, disabled duplicate, giữ file/retry và feedback | JSDOM kiểm AVIF MIME/progress37%/failure/retry; browser PNG qua MinIO. Fake AVIF MIME không phải decoder proof; decoder thật có regression MediaStorageTests |
| Search/read/list/history | Filter RHF/Zod, URL state, loading/empty/error/retry và scoped authorization | Browser + Node/JSDOM + API regression; giữ phần read-only đã có, không thêm tính năng |

`forms/messages.ts` và code catalog trong `problem-details.ts` là đầu vào có key để địa phương hóa; không thêm framework/chế độ ngôn ngữ mới. `forms/server-errors.ts` chỉ nhận field mapping của từng form, so tên property không phân biệt hoa/thường, không render nội dung server hoặc unknown fields. Lỗi server không làm mất input.

Các lỗi a11y có reproduction đã sửa: chip từ khóa4,37:1, chữ wizard3,74:1, active sidebar4,1:1/mobile4,37:1; thiếu accessible name login mobile/main auth; viền form≈1,47:1. Chỉ sửa màu chữ/border thực sự fail, thêm aria-label/landmark, giữ separator trang trí. Viền input/select/textarea dùng muted foreground, giữ focus rule. Test đợi control enabled/transition hoàn tất trước khi đo. Đã xem screenshot, không dùng regex CSS để chứng nhận contrast.

Axe kiểm text contrast theo AA; 72 scan không có violation, riêng control contrast được đo từ computed border và nền kề. Đây là phạm vi tự động Chromium emulation; không chứng nhận toàn bộ WCAG, device hoặc screen reader thật của phase28.

Screenshot đã xem trên artifact cuối: [login320](evidence/G05-final-320-_auth_login.png), [editor1200](evidence/G05-final-1200-_dashboard_recipes_new.png), [mobile menu](evidence/G05-final-mobile-menu.png). Toàn bộ 72 screenshot còn ở artifact F08.

## Phase 29

| Task | Kết quả theo phạm vi local | Evidence |
|---|---|---|
| D7-04 | Đạt cấu hình và gates tương đương CI local. Locked restore, format/build/unit/architecture/coverage, API generation/drift/format/lint/typecheck/build/Node/Jest/audit, integration/matrix và browser có log/artifact; lệnh nonzero vẫn chặn. Remote chưa thực thi theo điều kiện ở đầu report. | F01–F10; `.github/workflows/ci.yml`, `scripts/verify.ps1`. |
| D7-05 | Đạt: Sonar/StyleCop strict0 error/0 warning, Airbnb thực trên ESLint9, Next plugin15.5.28, Jest/Testing Library; các rule kiểm soát vẫn bắt lỗi. Áp dụng đúng bốn diagnostic đã được người dùng duyệt. | F02/F06/F07/F09 và [review có phê duyệt](package-G05-analysis-review.md). |

**Phê duyệt và sửa backend:** người dùng cho phép sửa diện rộng để giữ strict: “Cho phép sửa diện rộng, giữ gate strict”, sau preview638KB/383 đường dẫn. Đã tách type theo file, thêm XML docs/header theo provenance repo, sửa `this.`, layout/member order/nullable và compiler warnings; [type moves](evidence/G05-final-type-moves.json). Bốn migration đổi tên file theo class, giữ MigrationId/SQL; [mapping](evidence/G05-final-migration-file-moves.json). Sửa link inventory và verifier health theo đường dẫn mới; acceptance347/contracts209/operations47 giữ nguyên. Không thay PDF/A/B/C.

Các sửa Sonar thực gồm temp directory riêng thay file tạm dùng chung; cancellation khi ghi problem response; timeout1s cho regex chạy trong .NET; bỏ param/biểu thức thừa, tách ternary và giữ lazy initialization của seed. SMTP dùng factory chung cho welcome/password reset: plaintext chỉ cho capture local, external phải TLS, production bật email phải `Email:EnableSsl=true`. Đây là điều kiện cấu hình production mới cần giữ khi triển khai; không sửa cấu hình deployment trong lượt này. Npgsql command timeout30s được đặt rõ, expression search vẫn dịch sang SQL `~*`; thêm7 SMTP policy cases,3 production TLS cases và1 SQL translation case, đều pass (không gửi email ngoài).

User đã duyệt riêng: “Duyệt 4 vị trí có kiểm chứng”. Chỉ pragma S5332 quanh một initializer SMTP, S6444 quanh một database expression, hai `no-script-url` quanh literal input xấu bị từ chối. [Review](package-G05-analysis-review.md) ghi kiểm soát, test và căn cứ [Npgsql translator](https://github.com/npgsql/efcore.pg/blob/v10.0.0/src/EFCore.PG/Query/ExpressionTranslators/Internal/NpgsqlRegexTranslator.cs). Negative probes ngoài các vị trí đó vẫn fail theo rule; không tắt rule trên toàn project.

**Frontend:** cài `eslint-config-airbnb19.0.4`, dùng FlatCompat/compat trên ESLint9.39.5/Hooks7.1.1, import resolver TS, Prettier và Next plugin15.5.28. Npm overrides chỉ peer của preset Airbnb; `npm ci` thật pass, không legacy-peer-deps/downgrade Next15. Cấu hình tích hợp được ghi rõ: named public exports, native for-of Node22, sequential await trong journey/fixture, dev dependencies chỉ tool/test, typed JSX/RHF forwarding, TypeScript equivalents cho shadow/use-before-define, Node ESM extensions và typed Button default/type union. Các rule Hooks refs/set-state-in-effect và unescaped entities đã tắt từ trước vẫn giữ phạm vi cũ. Năm a11y disables đã bật lại; label kiểm association theo HTML for hoặc nesting. Đây là preset Airbnb có cấu hình tích hợp repo, không tuyên bố mọi tùy chọn mặc định Airbnb được giữ nguyên.

Sửa lỗi frontend thực: label readonly thành text, nút validation header native không lồng button, event carousel qua presentation wrapper; callback auth cập nhật immutable, context callbacks/memo ổn định; ref-as-prop React19 cho Button/Input, tách error description khỏi accessible name; modal xác nhận xóa ingredient/step/image có busy/duplicate guard/error recovery; JSON-LD qua script children và kiểm closing-script injection trên output SSR của page thật. Các ảnh chuyển sang Next Image `unoptimized` giữ nguồn private/blob. Node/JSDOM kiểm cả ba delete actions/cancel/pending/error/version, mouse/touch carousel và profile input xấu; Jest kiểm refs/type/labels và auth callback/security/cookie boundaries.

Rà build phát hiện Jose CompressionStream/DecompressionStream không phù hợp Edge. Middleware dùng `runtime: 'nodejs'` trên standalone Node22 hiện có, tính năng ổn định của [Next15.5](https://nextjs.org/blog/next-15-5#nodejs-middleware-stable); giữ Next15 và toàn bộ matcher/role/cookie guards. Build cuối “Compiled successfully”, không còn cảnh báo compiler Edge hoặc Next ESLint plugin. Browser chạy lại đủ7/7 sau đổi runtime.

**Sửa gate thực:** TRX có thể copy Cobertura dưới `test-run/In/host`; gate chọn đúng một report gốc trực tiếp dưới run-id, từ chối nhiều report gốc. Local/CI dùng chung selector và thêm negative controls. Sau full integration, chỉ fixture MinIO thiếu readiness thất bại trước khi tạo bucket; thêm HTTP `/minio/health/ready`, giữ assertions/timeout mặc định. Kiểm lại đúng bài đã lỗi1/1; production backend không đổi sau full suite. Không gọi đó là một full integration run237/237 xanh.

## Evidence nguồn cuối và lệnh tái lập

Cwd `D:/Culinary_Blog` trừ chỗ ghi frontend/backend; mọi lệnh dùng runtime ở đầu report. `G05-final-*.log/json/xml` lưu trong [evidence](evidence/); raw artifacts có đường dẫn và SHA trong manifest. Expected: gate pass với mã sạch, fail với lỗi kiểm soát, không skip để xanh.

| ID | Lệnh đã chạy / cwd | Actual / exit | Evidence |
|---|---|---|---|
| F01 | Root: `dotnet test backend/tests/CulinaryBlog.UnitTests -c Release --no-build --no-restore --collect "XPlat Code Coverage" --results-directory tmp/package-G05/unit-phase29-verified --logger "trx;LogFileName=unit.trx"`; `python scripts/check-application-coverage.py tmp/package-G05/unit-phase29-verified` |129/129,0 skip;888/985=90,15%, branch0.6835; exit0/0 | [Unit](evidence/G05-final-unit-results.json), [Cobertura](evidence/G05-final-unit-coverage.cobertura.xml), coverage-gate-final.log |
| F02 | Root: `python scripts/test-application-coverage.py`; `python scripts/test-srs-analyzers.py`; frontend `npm test` includes lint contract |3 coverage tests/7 controls; clean analyzer pass, S5332/S6444/SA1633/SA1600/missing DLL reject;6 ESLint rule probes reject; exit0 | coverage-controls-final.log, analyzer-controls.log, Node log |
| F03 | Root: `dotnet test backend/CulinaryBlog.slnx -c Release --no-restore --logger "trx;LogFilePrefix=g05-final" --results-directory tmp/package-G05/phase29-regression` |129 unit +3 architecture pass; integration236/237,0 skip; **exit1**: MinIO “Server not initialized yet” before bucket creation | [Architecture](evidence/G05-final-architecture-results.json), [initial integration](evidence/G05-final-integration-first-results.json), phase29-regression.log |
| F04 | Root: `dotnet test backend/tests/CulinaryBlog.IntegrationTests -c Release --no-build --no-restore --filter FullyQualifiedName~MediaStorageTests.RealPrivateMinioAcceptsAllFormatsAndPreservesOriginalAcrossResizeAndDeletionFailures --logger "trx;LogFileName=media-readiness.trx" --results-directory tmp/package-G05/media-readiness` |1/1 pass,0 skip; exit0 after fixture readiness fix. Combined distinct integration evidence237; initial failure retained | [Targeted rerun](evidence/G05-final-media-readiness-results.json), media-readiness.log |
| F05 | Root: `python scripts/verify-endpoint-matrix.py --trx tmp/package-G05/phase29-regression/g05-final_net10.0_20261029143820.trx` |47 operations; all happy/error symbols exist and passed supplied TRX; exit0. Does not claim failed media case passed in that run | matrix-final.log |
| F06 | Frontend: `npm test`; `npm run test:components` |134/134 Node;3 Jest suites/21 tests;0 fail/skip; exit0/0 | frontend-final-test.log, frontend-final-test-components.log |
| F07 | Frontend: `npm ci`; `npm run generate:api`; `npm run format`; `npm run lint`; `npm run typecheck`; `npm audit --audit-level=high --json` |exit0; generated hash unchanged; app/E2E typecheck; lint0/0; audit0 high/critical/moderate,5 low. Npm reports tool deprecations | frontend-final-exits.json, api-type-drift-phase29.json, npm-ci-phase29.log, npm-audit-phase29.json |
| F08 | Frontend: `npm run test:e2e` (full production backend/Next builds, real disposable stack) |7/7,0 skip/retry;72 axe scans0 violation,207 border measurements min5.43537:1, all≥3:1; exit0 | [Browser](evidence/G05-final-browser.json), [run](evidence/G05-final-browser-run.json), frontend-build.log/backend-build.log |
| F09 | Root: `dotnet build backend/CulinaryBlog.slnx -c Release --no-restore -warnaserror -t:Rebuild -p:CustomAfterMicrosoftCommonTargets=D:/Culinary_Blog/scripts/srs-analyzers.targets` |**0 error/0 warning**, exit0; Sonar10.35.0.4138/StyleCop1.3.151-beta loaded with XML doc output | analyzers-final.log |
| F10 | Backend tool restore, solution/tool locked restore, `dotnet format backend/CulinaryBlog.slnx --verify-no-changes --no-restore`; workflow YAML/PowerShell parse; `docker compose config --quiet`; inventory/decisions verifiers; diff check |exit0;7 CI jobs retained;347 acceptances/209 contracts/47 ops,33 GAP/25 DEC/12 INV;3 historical baseline hashes unchanged | restore/format/workflow/inventory/decisions logs, coordination-final.log, manifest |

TRX summaries retain original counters/names/outcomes and raw SHA/path, omit verbose test output; full raw integration TRX (~52MB) remains under tmp. Initial failure and targeted resolution are separate records. Workflow backend/frontend/browser/analyzers publish logs when fail; security/compose/dependency-review jobs remain. `verify.ps1` stops on nonzero and verifies API type drift. Remote CI/publish/deploy not performed.

Final browser run **8f73ac1ff4f6**, BUILD_ID **udSIAiglXzYjkNWlQo18G**, API SHA **e53fd625b1916ed348d51e6d5ea787426fca8b7d2c2fd96940d40682037c9fc0**, lock SHA **d95534a4f7326b82ec82aac2f3ba005572b3fa7312cea6a646810e14e50f85d6**. Harness copied and checked build before testing; no rebuild during run. Cleanup removed4 containers/network/runtime/keys; final label/process check0/0/0, [cleanup](evidence/G05-final-cleanup-final.json). Screenshots viewed: [login320](evidence/G05-final-320-_auth_login.png), [editor1200](evidence/G05-final-1200-_dashboard_recipes_new.png), [mobile menu](evidence/G05-final-mobile-menu.png).

## Lịch sử và checkpoint bàn giao

Trước phê duyệt:12/14 task, strict325 Domain errors và inventory5041 warnings; các diagnostic được sửa theo source, không lấy inventory với warnings-as-errors=false làm acceptance. Unit858/952, browser `4eb88b02faf8` từng7/7 là snapshot cũ. Run `e3e3332d93e9`7/7 trước đổi middleware runtime có210 measurements nhưng còn compiler warning, chỉ giữ lịch sử. Run `44d147218888` bị gián đoạn môi trường, không tính pass; các failure selector/logout/contrast/fixture được giữ tại tmp. Snapshot cũ và manifest cũ không chứng nhận source cuối.

G05 không còn task local mở. Tiếp tục nếu source/artifact thay đổi: đối chiếu manifest nguồn cuối rồi chỉ kiểm lại phần bị ảnh hưởng; giữ regression chung/gate bắt buộc. Chạy GitHub Actions trên ref được phép khi có ủy quyền publish, giữ rõ trạng thái remote chưa thực thi. Provider Google thật, device/screen reader, production/SLA và các phần ngoài gói thuộc owner riêng; không tự chạy gói khác hoặc phase10/28/36. Không commit/push/deploy từ checkpoint này khi chưa có ủy quyền phù hợp.
