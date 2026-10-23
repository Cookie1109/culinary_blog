# Tiến độ thực hiện các phase SRS

Mốc tạo: 23/10/2026. **Chưa phase nào được chạy trong lượt chia tài liệu.** Những kết quả test lịch sử ở checklist gốc không tự điền thành `Đạt` tại đây.

Agent thực hiện cập nhật đúng các dòng phase đã xử lý trong gói hoặc phase được giao. Khi giao gói, evidence dẫn tới mục tương ứng của một report `reports/package-GNN-result.md` sau khi tạo thật; giữ report phase cũ làm lịch sử. Chưa có report để `—`, không tạo link PASS giả. Nếu cần chạy lại do source/artifact thay đổi, giữ log cũ và đánh `Cần kiểm tra lại`.

## Tổng quan 7 gói giao việc

Phạm vi/prompt ở [work-packages.md](work-packages.md), thay đổi và kiểm chứng trong [biên bản điều phối](reports/work-packages-setup.md). Bảng này tổng hợp các dòng phase bên dưới; không thay owner85 task. Ngày chuyển cách điều phối23/10/2026 chưa chạy thêm gói/phase nào, không tự nâng trạng thái. Sau khi thực hiện, cập nhật cả dòng gói và những phase/task đã xử lý; phase chưa đụng tới giữ nguyên.

| Gói | Phase | Trạng thái hiện tại | Kết quả/điều kiện còn mở | Report gói |
|---|---|---|---|---|
| G01 | 01,02,03 | Chờ quyết định |01 đã có inventory/report;02 còn D0-04;03 chưa thực hiện, phần độc lập có thể bắt đầu | — |
| G02 | 04,05,06,07,08,09 | Chưa bắt đầu | Cần đầu vào liên quan từ G01 | — |
| G03 | 11,12,13,14,15,16 | Chưa bắt đầu | Cần đầu vào liên quan từ G01/G02 | — |
| G04 | 17,18,19,20,21,22 | Chưa bắt đầu | Cần đầu vào liên quan từ G02/G03 | — |
| G05 | 23,24,25,26,27,29 | Chưa bắt đầu | Cần contract G04 và auth G02 | — |
| G06 | 30,31,32,33,34,35 | Chưa bắt đầu | Cần gates liên quan G05; protocol/môi trường thực kiểm khi giao | — |
| G07 | 37,38 | Chưa bắt đầu | Tổng hợp evidence; nghiệm thu cuối gồm cả hàng chờ | — |

Hàng chờ riêng: **10** (D3-05/07, Google thật), **28** (D8-10, browser/device/screen reader thật), **36** (D9-08, SLA/monitor/thời gian quan sát). Trạng thái vẫn theo các dòng phase gốc; chưa kiểm điều kiện thì không tự đổi sang Chờ môi trường. Các phần môi trường trong31/32/35/38 và các task khác vẫn nằm đúng owner gốc. Gói có task bắt buộc pending không được ghi Đạt.

## Chi tiết 38 phase

| Phase | Phạm vi | Trạng thái | Task còn mở / điều kiện chờ | Phiên bản/ngày chạy | Evidence đã tạo |
|---|---|---|---|---|---|
| 01 | [Lập inventory acceptance và contract](phase-01-acceptance-inventory.md) | Đạt | D0-01/D0-05 đóng ở phạm vi tài liệu; runtime chưa nghiệm thu; INV-G01–12 chuyển owner theo report | 23/10/2026; HEAD32e203e + dirty source, fingerprint trong report | [Report](reports/phase-01-result.md), [inventory](acceptance-inventory.md) |
| 02 | [Chốt cách xử lý mâu thuẫn và sai khác](phase-02-decisions-and-conflicts.md) | Chờ quyết định | D0-02/D0-03 đóng review; D0-04 mở lại; PDF là chuẩn, ngoại lệ A/B/C hết hiệu lực; ping/PUT/query/SLA/protocol còn mở | 23/10/2026; đính chính sau HEAD8663943 | [Report](reports/phase-02-result.md), [register](decision-register.md), [đính chính](approvals/2026-10-23-abc-correction.md) |
| 03 | [Chuẩn hóa môi trường, format và baseline test](phase-03-environment-and-baseline.md) | Chưa bắt đầu | — | — | — |
| 04 | [Hoàn thiện handlers và validation pipeline](phase-04-cqrs-and-validation.md) | Chưa bắt đầu | — | — | — |
| 05 | [Kiểm quyền và tính nguyên tử của command](phase-05-authorization-and-transactions.md) | Chưa bắt đầu | — | — | — |
| 06 | [Kiểm invalidation pipeline và structured logging](phase-06-pipeline-and-logging.md) | Chưa bắt đầu | — | — | — |
| 07 | [Hồi quy local auth, hồ sơ và mật khẩu](phase-07-local-auth-profile-password.md) | Chưa bắt đầu | — | — | — |
| 08 | [Sửa vòng đời phiên Google và refresh](phase-08-google-session-lifecycle.md) | Chưa bắt đầu | — | — | — |
| 09 | [Sửa kiểm tra owner khi logout](phase-09-logout-token-owner.md) | Chưa bắt đầu | — | — | — |
| 10 | [Kiểm Google callback và provider thật](phase-10-google-provider-verification.md) | Chưa bắt đầu | — | — | — |
| 11 | [Sửa HTTP 404 và bảo vệ routes](phase-11-http-status-and-routes.md) | Chưa bắt đầu | — | — | — |
| 12 | [Hoàn thiện ISR và bài HTTP production](phase-12-isr-ttl-production.md) | Chưa bắt đầu | — | — | — |
| 13 | [Kiểm invalidation từ mutation tới Next](phase-13-mutation-revalidation.md) | Chưa bắt đầu | — | — | — |
| 14 | [Đóng cơ chế Output Cache và TTL](phase-14-output-cache-and-ttl.md) | Chưa bắt đầu | — | — | — |
| 15 | [Kiểm cache privacy, invalidation và Redis failure](phase-15-cache-isolation-and-failure.md) | Chưa bắt đầu | — | — | — |
| 16 | [Sửa rate limiter, headers và quota chia sẻ](phase-16-rate-limit-and-headers.md) | Chưa bắt đầu | — | — | — |
| 17 | [Nghiệm thu category, recipe và composition](phase-17-recipe-category-composition.md) | Chưa bắt đầu | — | — | — |
| 18 | [Nghiệm thu tìm kiếm và khám phá công thức](phase-18-search-filters-and-pagination.md) | Chưa bắt đầu | — | — | — |
| 19 | [Nghiệm thu upload, variants và xóa media](phase-19-media-storage-and-jobs.md) | Chưa bắt đầu | — | — | — |
| 20 | [Nghiệm thu welcome email và sitemap job](phase-20-welcome-and-sitemap-jobs.md) | Chưa bắt đầu | — | — | — |
| 21 | [Hoàn thiện metadata, JSON-LD và lịch sử slug](phase-21-seo-metadata-and-slugs.md) | Chưa bắt đầu | — | — | — |
| 22 | [Đồng bộ schema, migration và OpenAPI/types](phase-22-schema-migrations-openapi.md) | Chưa bắt đầu | — | — | — |
| 23 | [Nâng unit coverage Application và thêm gate](phase-23-unit-coverage-gate.md) | Chưa bắt đầu | — | — | — |
| 24 | [Lập endpoint matrix và dựng Playwright](phase-24-integration-matrix-and-playwright.md) | Chưa bắt đầu | — | — | — |
| 25 | [E2E đăng ký và vòng đời đăng nhập](phase-25-e2e-registration-and-login.md) | Chưa bắt đầu | — | — | — |
| 26 | [E2E tạo, xuất bản và tìm kiếm công thức](phase-26-e2e-recipes-publish-search.md) | Chưa bắt đầu | — | — | — |
| 27 | [Rà UX, responsive và accessibility tự động](phase-27-ux-responsive-accessibility.md) | Chưa bắt đầu | — | — | — |
| 28 | [Kiểm browser matrix và screen reader thật](phase-28-browser-and-screen-reader.md) | Chưa bắt đầu | — | — | — |
| 29 | [Hoàn thiện CI, analyzers và các quality gate](phase-29-ci-and-static-analysis.md) | Chưa bắt đầu | — | — | — |
| 30 | [Đo tải API và review query/index/pooling](phase-30-load-and-database-plans.md) | Chưa bắt đầu | — | — | — |
| 31 | [Đo bundle, Core Web Vitals và INP](phase-31-frontend-performance-and-inp.md) | Chưa bắt đầu | — | — | — |
| 32 | [Kiểm security scans và HTTPS deployment](phase-32-security-and-https.md) | Chưa bắt đầu | — | — | — |
| 33 | [Kiểm telemetry, health và dependency failure](phase-33-observability-and-resilience.md) | Chưa bắt đầu | — | — | — |
| 34 | [Đóng backup schedule, durability và restore drill](phase-34-backup-and-restore.md) | Chưa bắt đầu | — | — | — |
| 35 | [Kiểm nhiều instance và production scaling](phase-35-multi-instance-and-production-scaling.md) | Chưa bắt đầu | — | — | — |
| 36 | [Theo dõi SLA, alert và maintenance](phase-36-sla-alerts-and-maintenance.md) | Chưa bắt đầu | — | — | — |
| 37 | [Chốt docs, artifact và traceability cuối](phase-37-docs-artifacts-and-traceability.md) | Chưa bắt đầu | — | — | — |
| 38 | [UAT, tổng kết và bàn giao nghiệm thu](phase-38-uat-and-final-handoff.md) | Chưa bắt đầu | — | — | — |

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
