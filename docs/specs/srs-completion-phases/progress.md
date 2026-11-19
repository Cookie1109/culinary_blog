# Tiến độ thực hiện các phase SRS

Mốc tạo: 23/10/2026. **Chưa phase nào được chạy trong lượt chia tài liệu.** Những kết quả test lịch sử ở checklist gốc không tự điền thành `Đạt` tại đây.

Agent thực hiện cập nhật đúng các dòng phase đã xử lý trong gói hoặc phase được giao. Khi giao gói, evidence dẫn tới mục tương ứng của một report `reports/package-GNN-result.md` sau khi tạo thật; giữ report phase cũ làm lịch sử. Chưa có report để `—`, không tạo link PASS giả. Nếu cần chạy lại do source/artifact thay đổi, giữ log cũ và đánh `Cần kiểm tra lại`.

**Điều chỉnh kế hoạch demo05/11:** [bỏ các bước bổ sung G06/G07](demo-scope-2026-11-05.md). Chỉ thay kế hoạch/hồ sơ; không đóng task, không chạy G07 hoặc đổi tiêu chuẩn PDF.

## Tổng quan 7 gói giao việc

Phạm vi/prompt ở [work-packages.md](work-packages.md), thay đổi và kiểm chứng trong [biên bản điều phối](reports/work-packages-setup.md). Bảng này tổng hợp các dòng phase bên dưới; không thay owner85 task. Ngày chuyển cách điều phối23/10/2026 chưa chạy thêm gói/phase nào, không tự nâng trạng thái. Sau khi thực hiện, cập nhật cả dòng gói và những phase/task đã xử lý; phase chưa đụng tới giữ nguyên.

| Gói | Phase | Trạng thái hiện tại | Kết quả/điều kiện còn mở | Report gói |
|---|---|---|---|---|
| G01 | 01,02,03 | Chờ quyết định |01 giữ Đạt tài liệu;02 còn D0-04;03 Chưa đạt vì ISR HTTP200≠404 và npm audit; D1-01/02/04 đóng | [Report G01](reports/package-G01-result.md) |
| G02 | 04,05,06,07,08,09 | Chưa đạt |05/06/07/09 đạt local;04 còn orchestration/validation;08 chờ Google handoff theo người dùng; audit gate còn fail | [Report G02](reports/package-G02-result.md) |
| G03 | 11,12,13,14,15,16 | Chưa đạt | 14 task chức năng local đóng; HTTP38/stack thật6 nhóm/backend85+3+124/frontend106 đạt,0skip; npm audit4 high/1 moderate còn fail | [Report G03](reports/package-G03-result.md) |
| G04 | 17,18,19,20,21,22 | Đạt |15/15 task; E28 HTTP422/409/422, category2–50/storage100 và ingredient1–100/storage200; sitemap/robots theo chấp thuận. Main e610783 (code a228817) CI4 job xanh,Render/Vercel live8/8; không rollout toàn snapshot/migration G04 | [Report G04](reports/package-G04-result.md) |
| G05 | 23,24,25,26,27,29 | Đạt | 14/14 task local; strict0/0, coverage90,15%, matrix47,134 Node/21 Jest,7 browser/72 axe. Remote CI chưa chạy; phạm vi/MinIO targeted rerun ghi rõ trong report, không suy toàn SRS đạt | [Report G05](reports/package-G05-result.md) |
| G06 | 30,31,32,33,34,35 | Đã đóng cho báo cáo môn học | 9/9 task đã đóng theo phạm vi miễn phí. 5 task đạt theo bằng chứng hiện có, 4 task có điều kiện SRS còn thiếu: D9-01/04/09/10. | [Report G06](reports/package-G06-result.md#free-demo-closeout-20261111) |
| G07 | 37,38 | Đã bàn giao báo cáo môn học | D10-01/02/03/04/06/07 hoàn tất theo phạm vi ghi và UAT được người dùng chấp nhận. D10-05 còn điều kiện vận hành production. | [Report G07](reports/package-G07-result.md) |

Hàng chờ riêng: **10** (D3-05/07, Google thật), **28** (D8-10, browser/device/screen reader thật), **36** (D9-08, SLA/monitor/thời gian quan sát). Trạng thái vẫn theo các dòng phase gốc; chưa kiểm điều kiện thì không tự đổi sang Chờ môi trường. Các phần môi trường trong31/32/35/38 và các task khác vẫn nằm đúng owner gốc. Gói có task bắt buộc pending không được ghi Đạt.

## Chi tiết 38 phase

| Phase | Phạm vi | Trạng thái | Task còn mở / điều kiện chờ | Phiên bản/ngày chạy | Evidence đã tạo |
|---|---|---|---|---|---|
| 01 | [Lập inventory acceptance và contract](phase-01-acceptance-inventory.md) | Đạt | D0-01/D0-05 giữ đóng tài liệu; verifier347 acceptance/207 CT/45 operations; runtime không tự nghiệm thu | 24/10/2026; HEAD7ed7a58 + dirty source, fingerprint trong report gói | [G01/01](reports/package-G01-result.md#phase-01), [inventory](acceptance-inventory.md) |
| 02 | [Chốt cách xử lý mâu thuẫn và sai khác](phase-02-decisions-and-conflicts.md) | Chờ quyết định | D0-02/D0-03 giữ đóng review; D0-04 mở; giữ đính chính A/B/C, ping/PUT/query/SLA/protocol còn mở | 24/10/2026; HEAD7ed7a58 + dirty source | [G01/02](reports/package-G01-result.md#phase-02), [register](decision-register.md), [đính chính](approvals/2026-10-23-abc-correction.md) |
| 03 | [Chuẩn hóa môi trường, format và baseline test](phase-03-environment-and-baseline.md) | Chưa đạt | D1-01/02/04 đóng; D1-03 mở: ISR missing category trả200 thay404, npm audit4 high/1 moderate; unit75/architecture3/IT99/frontend104 pass, không skip | 24/10/2026; HEAD7ed7a58 + dirty source; Node22.23.3/.NET10.0.402 | [G01/03](reports/package-G01-result.md#phase-03) |
| 04 | [Hoàn thiện handlers và validation pipeline](phase-04-cqrs-and-validation.md) | Chưa đạt | D2-07 đóng; D2-01/02 mở: forwarding orchestration, validation/source400–422 và DEC-13/15 |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/04](reports/package-G02-result.md#phase-04) |
| 05 | [Kiểm quyền và tính nguyên tử của command](phase-05-authorization-and-transactions.md) | Đạt | D2-03/04 đóng local: trusted actor/role/owner, fault rollback composition/version/outbox và race |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/05](reports/package-G02-result.md#phase-05) |
| 06 | [Kiểm invalidation pipeline và structured logging](phase-06-pipeline-and-logging.md) | Đạt | D2-05/06 đóng local: post-commit failure paths, request/job context, slow100/500ms, sink failure |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/06](reports/package-G02-result.md#phase-06) |
| 07 | [Hồi quy local auth, hồ sơ và mật khẩu](phase-07-local-auth-profile-password.md) | Đạt | D3-01/06/08 đóng điều kiện task local; API/React/Chrome profile + SMTP/password; không đóng toàn FR/status conflict |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/07](reports/package-G02-result.md#phase-07) |
| 08 | [Sửa vòng đời phiên Google và refresh](phase-08-google-session-lifecycle.md) | Chờ quyết định | D3-02/03 mở: người dùng giữ chờ handoff Google; local single-flight/rotation/reuse/locked account đã kiểm |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/08](reports/package-G02-result.md#phase-08) |
| 09 | [Sửa kiểm tra owner khi logout](phase-09-logout-token-owner.md) | Đạt | D3-04 đóng: owner403, optional/expired signed204, malformed401/idempotent; OpenAPI/types đồng bộ |24/10/2026; HEAD7ed7a58 + dirty source; .NET10.0.402/Node22.23.3 | [G02/09](reports/package-G02-result.md#phase-09) |
| 10 | [Kiểm Google callback và provider thật](phase-10-google-provider-verification.md) | Chưa bắt đầu | — | — | — |
| 11 | [Sửa HTTP 404 và bảo vệ routes](phase-11-http-status-and-routes.md) | Đạt | D4-01/03/04 đóng local:404 thật, SSR/private no-store, routes/canonical/quyền; HTTP38 checks đạt | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/11](reports/package-G03-result.md#phase-11) |
| 12 | [Hoàn thiện ISR và bài HTTP production](phase-12-isr-ttl-production.md) | Đạt | D4-02/07 đóng local: ISR3600/3600/300/600, clock trước/sau TTL; simulation HTTP38 và stack thật6 nhóm đạt | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/12](reports/package-G03-result.md#phase-12) |
| 13 | [Kiểm invalidation từ mutation tới Next](phase-13-mutation-revalidation.md) | Đạt | D4-05/06 đóng local: mutation/profile/composition/image/visibility→Next thật; secret/retry60s/không rollback/build offline | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/13](reports/package-G03-result.md#phase-13) |
| 14 | [Đóng cơ chế Output Cache và TTL](phase-14-output-cache-and-ttl.md) | Đạt | D5-01/02 đóng local: Redis HTTP Output Cache; HTTP detail3600 và data300 theo PDF; TTL/key hai instance đạt | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/14](reports/package-G03-result.md#phase-14) |
| 15 | [Kiểm cache privacy, invalidation và Redis failure](phase-15-cache-isolation-and-failure.md) | Đạt | D5-03/04 đóng local: privacy/tag/cross-instance/profile và Redis timeout/recovery; generation transactional chặn cache resurrection | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/15](reports/package-G03-result.md#phase-15) |
| 16 | [Sửa rate limiter, headers và quota chia sẻ](phase-16-rate-limit-and-headers.md) | Đạt | D5-05/06/07 đóng local: quota100/10/5, SQL ledger được người dùng chọn; headers/proxy/failover/concurrency12 cases đạt | 25/10/2026; HEAD7ed7a58 + source manifest G03; Node22.23.3/.NET10.0.402 | [G03/16](reports/package-G03-result.md#phase-16) |
| 17 | [Nghiệm thu category, recipe và composition](phase-17-recipe-category-composition.md) | Đạt |D6-01/02/03: FR validation/publish422, version409, malformed/If-Match400; category2–50/storage100, ingredient1–100/storage200; atomic CRUD/quyền/lifecycle/rollback/UI đạt |27/10/2026; E28 Root3 architecture+101 unit+185 integration,126 frontend; main a228817 CI3+79+122/110; không ghi DB người dùng | [G04/17](reports/package-G04-result.md#phase-17) |
| 18 | [Nghiệm thu tìm kiếm và khám phá công thức](phase-18-search-filters-and-pagination.md) | Đạt |D6-04: chỉ khớp tên món theo chỉ đạo E25; tiếng Việt/relevance/AND filters/paging/URL/mobile/history; query semantic422 đã kiểm E28 |27/10/2026; E28 regression Root185/main122 và frontend126/110; giữ evidence tìm kiếm E25, không gọi title-only là tìm cả mô tả | [G04/18](reports/package-G04-result.md#phase-18) |
| 19 | [Nghiệm thu upload, variants và xóa media](phase-19-media-storage-and-jobs.md) | Đạt |D6-05/06/07: giữ MinIO/private/stream4 MIME/AVIF/variants/Hangfire/outbox/race; lỗi media riêng không đổi; Linux codec smoke E21 còn hiệu lực |27/10/2026; E28 full Root185 integration bao gồm media/jobs; giữ evidence E19/E21 không bị ảnh hưởng; không đổi MinIO người dùng | [G04/19](reports/package-G04-result.md#phase-19) |
| 20 | [Nghiệm thu welcome email và sitemap job](phase-20-welcome-and-sitemap-jobs.md) | Đạt |D6-08/09: SMTP/outbox/retry, sitemap02UTC/XML/lock/public/robots; người dùng chấp thuận thay ping E27, không tuyên bố Google index |27/10/2026; E28 Root185 integration/frontend126; source sitemap/robots không đổi, không bật job production | [G04/20](reports/package-G04-result.md#phase-20) |
| 21 | [Hoàn thiện metadata, JSON-LD và lịch sử slug](phase-21-seo-metadata-and-slugs.md) | Đạt |D6-10/11/12: metadata/301/privacy/JSON-LD; giữ Google URL crawl E19 thành công1 Recipe,14 warning optional; không dùng thay sitemap |27/10/2026; E28 Root185 integration/frontend126/build đạt; E19 là bằng chứng Google lịch sử của source JSON-LD không đổi, không nhận đã test Google lại | [G04/21](reports/package-G04-result.md#phase-21) |
| 22 | [Đồng bộ schema, migration và OpenAPI/types](phase-22-schema-migrations-openapi.md) | Đạt |D6-13/14/15: schema/data/API, upgrade/backfill/retention/seed50+legacy100, errors/headers/docs/typegen đạt local; input limits khác storage hợp lệ |27/10/2026; E28 Root3+101+185/frontend126, inventory347/209/47, verifier giữ3 baseline hash và chặn tamper; CI main a228817 xanh; không áp migration lên Neon | [G04/22](reports/package-G04-result.md#phase-22) |
| 23 | [Nâng unit coverage Application và thêm gate](phase-23-unit-coverage-gate.md) | Đạt | D7-01/02/03:129 unit; Application888/985=90,15%, branch68,35%; gate thật +3 tests/7 controls, chọn report gốc không đếm TRX copy | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/23](reports/package-G05-result.md#phase-23) |
| 24 | [Lập endpoint matrix và dựng Playwright](phase-24-integration-matrix-and-playwright.md) | Đạt | D8-01/02:47 operations/matrix TRX pass;237 integration distinct có pass (full236/237, sửa MinIO readiness rồi targeted1/1); harness thật/cô lập/cleanup | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/24](reports/package-G05-result.md#phase-24) |
| 25 | [E2E đăng ký và vòng đời đăng nhập](phase-25-e2e-registration-and-login.md) | Đạt | D8-03/04:browser nguồn cuối register/profile/SMTP/duplicate và login/expiry65s/rotation/logout/private guards; Node middleware; không provider thật | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/25](reports/package-G05-result.md#phase-25) |
| 26 | [E2E tạo, xuất bản và tìm kiếm công thức](phase-26-e2e-recipes-publish-search.md) | Đạt | D8-05/06/07:5 critical flows chung run8f73ac1ff4f6; wizard/409/private preview, publish/roles/invalidation và pho/phở/filter/history/loading/retry | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/26](reports/package-G05-result.md#phase-26) |
| 27 | [Rà UX, responsive và accessibility tự động](phase-27-ux-responsive-accessibility.md) | Đạt | D8-08/09:134 Node +21 Jest;72 responsive/axe,207 phép đo UI min5,43537:1; modal xóa/refs/labels/JSON-LD/carousel; không phase28 | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/27](reports/package-G05-result.md#phase-27) |
| 28 | [Kiểm browser matrix và screen reader thật](phase-28-browser-and-screen-reader.md) | Chưa bắt đầu | — | — | — |
| 29 | [Hoàn thiện CI, analyzers và các quality gate](phase-29-ci-and-static-analysis.md) | Đạt | D7-04/05:workflow và local gates đạt; strict Sonar/StyleCop0/0; Airbnb/Next lint0/0, clean build; negative controls +4 diagnostic user duyệt; remote CI chưa chạy theo điều kiện phase29 | 29/10/2026; HEAD7ed7a58 + G05-final manifest; .NET10.0.403/Node22.23.3 | [G05/29](reports/package-G05-result.md#phase-29) |
| 30 | [Đo tải API và review query/index/pooling](phase-30-load-and-database-plans.md) | Đóng cho báo cáo, SRS đạt một phần | D9-02 giữ đạt. D9-01 có k6 lab 100 VU/10 phút. Chưa đo production với profile 2 vCPU/4 GB. | 11/11/2026; lab API 49163d6, Render 41f619b | [G06/30](reports/package-G06-result.md#free-demo-closeout-20261111) |
| 31 | [Đo bundle, Core Web Vitals và INP](phase-31-frontend-performance-and-inp.md) | Đạt | D9-03[x] trênproductiondf0: Lighthouse7×3/42assertions đạt,58assets20pagesbudget và18acceptedINP/CWV đúngwindow; khôngTBT/organicRUMclaim | 10/11/2026; primarydpl_CzWEgVuacUVkQFs9oKSweUPb1YHM/df0dbb9 | [G06/31](reports/package-G06-result.md#acceptance-review-10112026) |
| 32 | [Kiểm security scans và HTTPS deployment](phase-32-security-and-https.md) | Đóng cho báo cáo, SRS đạt một phần | D9-04 có kiểm HTTPS staging và smoke demo. MinIO server còn finding High và HSTS công khai khác cấu hình SRS. | 11/11/2026; MinIO 14cea493, Render 41f619b | [G06/32](reports/package-G06-result.md#free-demo-closeout-20261111) |
| 33 | [Kiểm telemetry, health và dependency failure](phase-33-observability-and-resilience.md) | Đạt | D9-05/06 giữ đạt; backend/telemetry không đổi, labbổsung đãdọn đúngproject, giữ13containerIDs | 10/11/2026; Renderdep-dbpj3c6/same41f619b | [G06/33](reports/package-G06-result.md#acceptance-review-10112026) |
| 34 | [Đóng backup schedule, durability và restore drill](phase-34-backup-and-restore.md) | Đạt | D9-07[x]: lịchDB03UTC/30ngày/action0,30tables2044rows/301mediarestoreSHA/WAL/tokencontinuity; khoDBtáchNeon,runbook/RTO/phạmviRPO rõ | 10/11/2026; scheduledrun160716Z; PGtool941fc536 | [G06/34](reports/package-G06-result.md#acceptance-review-10112026) |
| 35 | [Kiểm nhiều instance và production scaling](phase-35-multi-instance-and-production-scaling.md) | Đóng cho báo cáo, SRS đạt một phần | D9-09/10 có kiểm hai API và MinIO bốn node trong lab, cùng CDN Vercel trên demo. Chưa đạt topology và đo scaling production theo PDF. | 11/11/2026; Render 41f619b, Vercel df0dbb9 | [G06/35](reports/package-G06-result.md#free-demo-closeout-20261111) |
| 36 | [Theo dõi SLA, alert và maintenance](phase-36-sla-alerts-and-maintenance.md) | Chưa bắt đầu | — | — | — |
| 37 | [Chốt docs, artifact và traceability cuối](phase-37-docs-artifacts-and-traceability.md) | Đạt phạm vi bàn giao | D10-01/02/03: setup 224,07 giây với image MinIO đã nạp, định danh hai artifact demo và tổng hợp traceability. | 11/11/2026; Render 41f619b, Vercel df0dbb9 | [G07/37](reports/package-G07-result.md#phiên-bản-và-khởi-tạo) |
| 38 | [UAT, tổng kết và bàn giao nghiệm thu](phase-38-uat-and-final-handoff.md) | Đạt một phần | D10-04 UAT demo được chấp nhận, D10-06/07 đã review và kết luận No-Go production. D10-05 còn gate vận hành production. | 11/11/2026; UAT localhost:18080, PR #31 | [G07/38](reports/package-G07-result.md#cập-nhật-uat-và-hồi-quy-ngày-11112026) |

## Owner của đủ 85 task gốc

Bảng này dùng để tránh bỏ sót hoặc hai phase cùng đánh hoàn thành một task. Không chuyển owner khi chưa cập nhật bảng điều phối và prompt/file phase tương ứng.

| Task trong checklist gốc | Phase chịu trách nhiệm |
|---|---|
| `D0-01` | [01](phase-01-acceptance-inventory.md) |
| `D0-05` | [01](phase-01-acceptance-inventory.md) |
| `D0-02` | [02](phase-02-decisions-and-conflicts.md) |
| `D0-03` | [02](phase-02-decisions-and-conflicts.md) |
| `D0-04` | [02](phase-02-decisions-and-conflicts.md) |
| `D1-01` | [03](phase-03-environment-and-baseline.md) |
| `D1-02` | [03](phase-03-environment-and-baseline.md) |
| `D1-03` | [03](phase-03-environment-and-baseline.md) |
| `D1-04` | [03](phase-03-environment-and-baseline.md) |
| `D2-01` | [04](phase-04-cqrs-and-validation.md) |
| `D2-02` | [04](phase-04-cqrs-and-validation.md) |
| `D2-07` | [04](phase-04-cqrs-and-validation.md) |
| `D2-03` | [05](phase-05-authorization-and-transactions.md) |
| `D2-04` | [05](phase-05-authorization-and-transactions.md) |
| `D2-05` | [06](phase-06-pipeline-and-logging.md) |
| `D2-06` | [06](phase-06-pipeline-and-logging.md) |
| `D3-01` | [07](phase-07-local-auth-profile-password.md) |
| `D3-06` | [07](phase-07-local-auth-profile-password.md) |
| `D3-08` | [07](phase-07-local-auth-profile-password.md) |
| `D3-02` | [08](phase-08-google-session-lifecycle.md) |
| `D3-03` | [08](phase-08-google-session-lifecycle.md) |
| `D3-04` | [09](phase-09-logout-token-owner.md) |
| `D3-05` | [10](phase-10-google-provider-verification.md) |
| `D3-07` | [10](phase-10-google-provider-verification.md) |
| `D4-01` | [11](phase-11-http-status-and-routes.md) |
| `D4-03` | [11](phase-11-http-status-and-routes.md) |
| `D4-04` | [11](phase-11-http-status-and-routes.md) |
| `D4-02` | [12](phase-12-isr-ttl-production.md) |
| `D4-07` | [12](phase-12-isr-ttl-production.md) |
| `D4-05` | [13](phase-13-mutation-revalidation.md) |
| `D4-06` | [13](phase-13-mutation-revalidation.md) |
| `D5-01` | [14](phase-14-output-cache-and-ttl.md) |
| `D5-02` | [14](phase-14-output-cache-and-ttl.md) |
| `D5-03` | [15](phase-15-cache-isolation-and-failure.md) |
| `D5-04` | [15](phase-15-cache-isolation-and-failure.md) |
| `D5-05` | [16](phase-16-rate-limit-and-headers.md) |
| `D5-06` | [16](phase-16-rate-limit-and-headers.md) |
| `D5-07` | [16](phase-16-rate-limit-and-headers.md) |
| `D6-01` | [17](phase-17-recipe-category-composition.md) |
| `D6-02` | [17](phase-17-recipe-category-composition.md) |
| `D6-03` | [17](phase-17-recipe-category-composition.md) |
| `D6-04` | [18](phase-18-search-filters-and-pagination.md) |
| `D6-05` | [19](phase-19-media-storage-and-jobs.md) |
| `D6-06` | [19](phase-19-media-storage-and-jobs.md) |
| `D6-07` | [19](phase-19-media-storage-and-jobs.md) |
| `D6-08` | [20](phase-20-welcome-and-sitemap-jobs.md) |
| `D6-09` | [20](phase-20-welcome-and-sitemap-jobs.md) |
| `D6-10` | [21](phase-21-seo-metadata-and-slugs.md) |
| `D6-11` | [21](phase-21-seo-metadata-and-slugs.md) |
| `D6-12` | [21](phase-21-seo-metadata-and-slugs.md) |
| `D6-13` | [22](phase-22-schema-migrations-openapi.md) |
| `D6-14` | [22](phase-22-schema-migrations-openapi.md) |
| `D6-15` | [22](phase-22-schema-migrations-openapi.md) |
| `D7-01` | [23](phase-23-unit-coverage-gate.md) |
| `D7-02` | [23](phase-23-unit-coverage-gate.md) |
| `D7-03` | [23](phase-23-unit-coverage-gate.md) |
| `D8-01` | [24](phase-24-integration-matrix-and-playwright.md) |
| `D8-02` | [24](phase-24-integration-matrix-and-playwright.md) |
| `D8-03` | [25](phase-25-e2e-registration-and-login.md) |
| `D8-04` | [25](phase-25-e2e-registration-and-login.md) |
| `D8-05` | [26](phase-26-e2e-recipes-publish-search.md) |
| `D8-06` | [26](phase-26-e2e-recipes-publish-search.md) |
| `D8-07` | [26](phase-26-e2e-recipes-publish-search.md) |
| `D8-08` | [27](phase-27-ux-responsive-accessibility.md) |
| `D8-09` | [27](phase-27-ux-responsive-accessibility.md) |
| `D8-10` | [28](phase-28-browser-and-screen-reader.md) |
| `D7-04` | [29](phase-29-ci-and-static-analysis.md) |
| `D7-05` | [29](phase-29-ci-and-static-analysis.md) |
| `D9-01` | [30](phase-30-load-and-database-plans.md) |
| `D9-02` | [30](phase-30-load-and-database-plans.md) |
| `D9-03` | [31](phase-31-frontend-performance-and-inp.md) |
| `D9-04` | [32](phase-32-security-and-https.md) |
| `D9-05` | [33](phase-33-observability-and-resilience.md) |
| `D9-06` | [33](phase-33-observability-and-resilience.md) |
| `D9-07` | [34](phase-34-backup-and-restore.md) |
| `D9-09` | [35](phase-35-multi-instance-and-production-scaling.md) |
| `D9-10` | [35](phase-35-multi-instance-and-production-scaling.md) |
| `D9-08` | [36](phase-36-sla-alerts-and-maintenance.md) |
| `D10-01` | [37](phase-37-docs-artifacts-and-traceability.md) |
| `D10-02` | [37](phase-37-docs-artifacts-and-traceability.md) |
| `D10-03` | [37](phase-37-docs-artifacts-and-traceability.md) |
| `D10-04` | [38](phase-38-uat-and-final-handoff.md) |
| `D10-05` | [38](phase-38-uat-and-final-handoff.md) |
| `D10-06` | [38](phase-38-uat-and-final-handoff.md) |
| `D10-07` | [38](phase-38-uat-and-final-handoff.md) |

## Gap mới / thay đổi làm mất hiệu lực evidence

Ngày23/10/2026: review inventory phát hiện **INV-G01–12**: PUT optional/replacement, nested composition, query aliases/defaults, auth/provider status, health schema, empty search hint, VerifiedAuthor, Identity Guid/string, Instructions/enums storage, image URL nullable, RT IP/UA, OpenAPI response/schema. Nguồn/acceptance/impact/owner chi tiết trong [report01](reports/phase-01-result.md) và cuối [inventory](acceptance-inventory.md). Phase02 tiếp nhận quyết định; phase05/08/10/17/18/19/22/33 tiếp nhận phần liên quan, không đánh họ đã chạy. Không sửa runtime hay làm mất evidence phase khác. Các gap G01–G09 cũ vẫn mở.

Runtime bổ sung cho phase03 D1-01: host Node22.18.0/.NET10.0.402; .mise.toml còn Node20.19.5/.NET10.0.401, khác Node22.23.3 của ADR-010/package engines. Chưa đổi cấu hình trong lượt tài liệu.

Khi phát hiện tiếp: ghi mã/acceptance/nguồn, phase phụ trách, impact và link report; không coi khoảng trống là “không còn lỗi”.

Ngày23/10/2026 — đối chiếu quyết định: [register](decision-register.md) giữ33 GAP, có25 DEC và mapping12 INV-G. INV-G05/07 đã có baseline tại Conformance§3; backup03:00 UTC đã có Test Strategy§4. Phần approval của11 đề xuất còn mở theo [report02](reports/phase-02-result.md); không thay trạng thái phase03–38 hoặc tự đóng runtime acceptance.

Sau phản hồi ngày23/10/2026: người dùng xác nhận “Duyệt gói A”, “Duyệt gói B”, “Duyệt gói C”; [APP-2026-10-23-ABC](approvals/2026-10-23-abc.md) ghi đủ11 ID. D0-04 đã đóng và dòng02 chuyển Đạt ở phạm vi quyết định; checkpoint chờ trước đó giữ làm lịch sử. Không nâng trạng thái nghiệm thu runtime hoặc phase03–38.

Đính chính theo chỉ dẫn mới “PDF là tiêu chuẩn” và “Thế chỉnh lại các gói ABC đi”: [SRS-2026-10-23-ABC-CORRECTION](approvals/2026-10-23-abc-correction.md) thay hiệu lực11 quyết định trong approval cũ. Các ghi chú duyệt/Đạt trước đó là lịch sử. D0-04 mở lại; dòng02 chuyển Chờ quyết định. Không dùng ngoại lệ cũ để đóng nguyên văn; runtime và tiến độ01/03–38 không đổi.


Cập nhật G04 ngày25/10/2026: chi tiết DEC-13/15/21 được người dùng giao lựa chọn tại [quy tắc G04](approvals/2026-10-25-g04-contract-clarifications.md), giữ [đính chính A/B/C](approvals/2026-10-23-abc-correction.md). Register hiện còn3 source issues ở trạng thái Chờ làm rõ; các ghi chú đếm trước đây giữ làm lịch sử. Source/runtime/evidence mới và7 task mở xem [checkpoint G04](reports/package-G04-result.md#checkpoint-tiếp-tục). Không nâng trạng thái gói khác hoặc phase10/28/36.
