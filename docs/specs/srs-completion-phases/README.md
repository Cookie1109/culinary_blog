# Giao việc hoàn thiện SRS cho GPT 6.1 Sol

**38 phase nhỏ, ánh xạ đủ 85 task D0–D10 trong [checklist gốc](../srs-completion-checklist.md).** Mỗi task có đúng một phase chịu trách nhiệm; phần lớn phase gồm 1–3 task, tối đa 4. Không có phase nào đã được thực hiện chỉ vì tài liệu này được tạo.

## Cách dùng ngay

1. Chọn GPT 6.1 Sol trong môi trường làm việc có repository `D:\Culinary_Blog`.
2. Mở [phase 01](phase-01-acceptance-inventory.md) và gửi prompt trong file đó. Không cần dán cả checklist hơn 400 dòng vào một lượt.
3. Sau mỗi phase, đọc report và [progress.md](progress.md). Giao tiếp một phase tiếp theo có đủ phụ thuộc kỹ thuật.
4. Các phase Google thật/thiết bị/RUM/production/SLA có thể chờ điều kiện. Tiếp tục phase độc lập nhưng giữ các task chưa được xác minh ở trạng thái mở.
5. Dùng [mẫu report](reports/README.md) để agent sau đọc được chính xác việc đã làm/chưa làm, không dựa vào lịch sử chat.

Prompt dùng cho một phase bất kỳ:

```text
Thực hiện Phase NN trong docs/specs/srs-completion-phases/ theo file phase tương ứng.
Đọc AGENTS.md, README điều phối, progress.md, checklist gốc và report các phase phụ thuộc.
Chỉ làm phase được giao, sửa và kiểm chứng trong phạm vi đó; không tự làm toàn bộ dự án.
Cập nhật report, tiến độ và checkbox gốc theo bằng chứng; báo rõ phần đang chờ môi trường/quyết định.
```

Thay `NN` bằng số phase/file cụ thể; mỗi file phase đã có prompt ghi đầy đủ đường dẫn.

## Quy tắc chung cho từng lượt thực hiện

- Tuân thủ `AGENTS.md` và chỉ dẫn người dùng đang có hiệu lực. Tài liệu này không thay quyền hạn hoặc quy tắc dự án.
- Đọc `git status`/diff trước sửa, giữ mọi thay đổi đang có. Không reset/clean/restore, không sửa/xóa PDF hoặc gom các file người dùng đang chuyển thư mục vào công việc khác.
- Checklist gốc là nguồn task và điều kiện đóng; SRS/baseline/decisions là nguồn acceptance. Báo cáo trạng thái là bằng chứng của đợt trước, phải đối chiếu mã hiện tại. Không tự nâng Next.js 15 hoặc thêm tính năng ngoài phạm vi.
- Mỗi task D có một owner phase trong bảng; phase khác được dùng kết quả nhưng không đánh task đó đạt thay owner. Khi phát hiện gap mới, ghi vào report/checklist và gán phase phù hợp, không tự bỏ hoặc refactor ngoài phạm vi.
- Nêu giả định và kế hoạch ngắn trước sửa. Giữ diff theo use case, tái dùng abstraction/test/contract đang có, chỉ thêm phần còn thiếu. Scope của task rộng có thể làm theo nhiều checkpoint trong cùng phase, không phải viết lại mọi thứ trong một lần.
- Công việc API không đơn giản áp dụng skill `postman:api-engineer` và skill nhánh phù hợp khi có trong phiên. Dùng lại OpenAPI/requests/test suite; HTTP simulation local chỉ chứng minh caller với dependency mô phỏng, không thay browser/backend/provider thật của nghiệm thu.
- Unit, integration, React/JSDOM/source, production HTTP, browser thật và provider/production thật là các loại evidence khác nhau. Ghi runtime/cwd/command/exit code/count/expected/actual; không biến skip/lỗi Docker thành pass hoặc lỗi nghiệp vụ chưa được xác nhận.
- Môi trường thử riêng: DB/container/SMTP/storage/dataset synthetic, port không xung đột. Không stop/restore/migrate đè dữ liệu stack người dùng để test. Không gửi email/thông báo tới người khác hoặc publish/deploy chỉ để làm phase xanh.
- Không tự commit/push/deploy/publish/gửi thông báo nếu chưa có yêu cầu/ủy quyền phù hợp; nếu đã được yêu cầu áp dụng cho hành động đó thì thực hiện theo phạm vi được giao. Commit khi có yêu cầu dùng mô tả tiếng Việt không dấu.
- Thiếu quyết định/credential/thiết bị: đọc quyết định và nguồn sẵn có trước, tiếp tục phần độc lập, ghi chính xác điều kiện cần từ người dùng khi thật sự cần. Không suy chấp thuận từ thời gian chờ, không tự ký UAT hoặc tạo số đo/CI URL/digest giả.
- Bằng chứng cũ chỉ dùng tham khảo. Sửa source sau build/đo phải chạy lại phần bị ảnh hưởng; `--skip-build` chỉ dùng đúng build của mã hiện tại. Không dùng test count/checkbox count để suy phần trăm SRS.
- Tạo `reports/phase-NN-result.md`, cập nhật dòng phase trong progress và checkbox gốc tương ứng. Chỉ đóng checkbox D khi toàn bộ điều kiện của nó đạt hoặc có điều chỉnh hợp lệ, không đóng từ riêng một phần local.
- Dừng tại kết quả/handoff của phase được giao. Không tự chạy phase tiếp theo hoặc gọi sub-agent chỉ vì README liệt kê chúng.

## Thứ tự giao việc và phụ thuộc

Các số phase là thứ tự mặc định; cột phụ thuộc là các đầu vào kỹ thuật cần đọc/đủ để sửa phần liên quan. Khi phase phụ thuộc có một phần provider/môi trường pending, report phải chỉ rõ phần code nào đã sẵn; chỉ làm phần độc lập của phase sau. Phase 38 có thể chuẩn bị gói handoff với mục pending, nhưng không chứng nhận toàn bộ SRS nếu còn điều kiện bắt buộc mở.

| Phase | Phạm vi | Task gốc do phase sở hữu | Phụ thuộc | Loại công việc |
|---|---|---|---|---|
| 01 | [Lập inventory acceptance và contract](phase-01-acceptance-inventory.md) | D0-01, D0-05 | — | Tài liệu |
| 02 | [Chốt cách xử lý mâu thuẫn và sai khác](phase-02-decisions-and-conflicts.md) | D0-02, D0-03, D0-04 | 01 | Tài liệu/quyết định |
| 03 | [Chuẩn hóa môi trường, format và baseline test](phase-03-environment-and-baseline.md) | D1-01, D1-02, D1-03, D1-04 | 01 | Mã/test local |
| 04 | [Hoàn thiện handlers và validation pipeline](phase-04-cqrs-and-validation.md) | D2-01, D2-02, D2-07 | 01, 02, 03 | Mã/test local |
| 05 | [Kiểm quyền và tính nguyên tử của command](phase-05-authorization-and-transactions.md) | D2-03, D2-04 | 04 | Mã/test local |
| 06 | [Kiểm invalidation pipeline và structured logging](phase-06-pipeline-and-logging.md) | D2-05, D2-06 | 04, 05 | Mã/test local |
| 07 | [Hồi quy local auth, hồ sơ và mật khẩu](phase-07-local-auth-profile-password.md) | D3-01, D3-06, D3-08 | 04, 05 | Mã/test local |
| 08 | [Sửa vòng đời phiên Google và refresh](phase-08-google-session-lifecycle.md) | D3-02, D3-03 | 02, 04, 07 | Mã/test local |
| 09 | [Sửa kiểm tra owner khi logout](phase-09-logout-token-owner.md) | D3-04 | 02, 04, 08 | Mã/test local |
| 10 | [Kiểm Google callback và provider thật](phase-10-google-provider-verification.md) | D3-05, D3-07 | 08, 09 | Provider/credential ngoài local |
| 11 | [Sửa HTTP 404 và bảo vệ routes](phase-11-http-status-and-routes.md) | D4-01, D4-03, D4-04 | 03, 08, 09 | Mã/test local |
| 12 | [Hoàn thiện ISR và bài HTTP production](phase-12-isr-ttl-production.md) | D4-02, D4-07 | 11 | Mã/test local |
| 13 | [Kiểm invalidation từ mutation tới Next](phase-13-mutation-revalidation.md) | D4-05, D4-06 | 06, 12 | Mã/test local |
| 14 | [Đóng cơ chế Output Cache và TTL](phase-14-output-cache-and-ttl.md) | D5-01, D5-02 | 02, 06, 13 | Mã/test local |
| 15 | [Kiểm cache privacy, invalidation và Redis failure](phase-15-cache-isolation-and-failure.md) | D5-03, D5-04 | 14 | Mã/test local |
| 16 | [Sửa rate limiter, headers và quota chia sẻ](phase-16-rate-limit-and-headers.md) | D5-05, D5-06, D5-07 | 02, 03, 15 | Mã/test local |
| 17 | [Nghiệm thu category, recipe và composition](phase-17-recipe-category-composition.md) | D6-01, D6-02, D6-03 | 05, 13, 15 | Mã/test local |
| 18 | [Nghiệm thu tìm kiếm và khám phá công thức](phase-18-search-filters-and-pagination.md) | D6-04 | 15, 17 | Mã/test local |
| 19 | [Nghiệm thu upload, variants và xóa media](phase-19-media-storage-and-jobs.md) | D6-05, D6-06, D6-07 | 05, 15, 17 | Mã/test local |
| 20 | [Nghiệm thu welcome email và sitemap job](phase-20-welcome-and-sitemap-jobs.md) | D6-08, D6-09 | 02, 06, 17 | Mã/test local |
| 21 | [Hoàn thiện metadata, JSON-LD và lịch sử slug](phase-21-seo-metadata-and-slugs.md) | D6-10, D6-11, D6-12 | 02, 11, 17, 20 | Mã/test local |
| 22 | [Đồng bộ schema, migration và OpenAPI/types](phase-22-schema-migrations-openapi.md) | D6-13, D6-14, D6-15 | 16, 18, 19, 20, 21 | Mã/test local |
| 23 | [Nâng unit coverage Application và thêm gate](phase-23-unit-coverage-gate.md) | D7-01, D7-02, D7-03 | 04, 05, 06, 09, 22 | Mã/test local |
| 24 | [Lập endpoint matrix và dựng Playwright](phase-24-integration-matrix-and-playwright.md) | D8-01, D8-02 | 22, 23 | Mã/test local |
| 25 | [E2E đăng ký và vòng đời đăng nhập](phase-25-e2e-registration-and-login.md) | D8-03, D8-04 | 07, 08, 09, 24 | Mã/test local |
| 26 | [E2E tạo, xuất bản và tìm kiếm công thức](phase-26-e2e-recipes-publish-search.md) | D8-05, D8-06, D8-07 | 17, 18, 19, 21, 24, 25 | Mã/test local |
| 27 | [Rà UX, responsive và accessibility tự động](phase-27-ux-responsive-accessibility.md) | D8-08, D8-09 | 17, 18, 19, 21, 26 | Mã/test local |
| 28 | [Kiểm browser matrix và screen reader thật](phase-28-browser-and-screen-reader.md) | D8-10 | 02, 27 | Thiết bị/browser/screen reader thật |
| 29 | [Hoàn thiện CI, analyzers và các quality gate](phase-29-ci-and-static-analysis.md) | D7-04, D7-05 | 02, 23, 24, 25, 26, 27 | Mã/test local |
| 30 | [Đo tải API và review query/index/pooling](phase-30-load-and-database-plans.md) | D9-01, D9-02 | 15, 16, 22, 26, 29 | Benchmark/container |
| 31 | [Đo bundle, Core Web Vitals và INP](phase-31-frontend-performance-and-inp.md) | D9-03 | 12, 21, 27, 29 | Browser/benchmark/RUM |
| 32 | [Kiểm security scans và HTTPS deployment](phase-32-security-and-https.md) | D9-04 | 16, 19, 22, 29 | Scans/HTTPS staging |
| 33 | [Kiểm telemetry, health và dependency failure](phase-33-observability-and-resilience.md) | D9-05, D9-06 | 06, 15, 19, 20, 29 | Observability/container test |
| 34 | [Đóng backup schedule, durability và restore drill](phase-34-backup-and-restore.md) | D9-07 | 02, 22, 33 | Backup/restore test riêng |
| 35 | [Kiểm nhiều instance và production scaling](phase-35-multi-instance-and-production-scaling.md) | D9-09, D9-10 | 15, 16, 20, 30, 33, 34 | Multi-instance/production topology |
| 36 | [Theo dõi SLA, alert và maintenance](phase-36-sla-alerts-and-maintenance.md) | D9-08 | 02, 33, 34 | Thời gian quan sát/receiver được phép |
| 37 | [Chốt docs, artifact và traceability cuối](phase-37-docs-artifacts-and-traceability.md) | D10-01, D10-02, D10-03 | 22, 23, 26, 29 | Tài liệu/artifact |
| 38 | [UAT, tổng kết và bàn giao nghiệm thu](phase-38-uat-and-final-handoff.md) | D10-04, D10-05, D10-06, D10-07 | Review 01–37 | UAT/người nghiệm thu |

Chia theo nhóm để dễ tìm: **01–03** nguồn/môi trường; **04–06** kiến trúc; **07–10** auth; **11–13** rendering; **14–16** cache/rate limit; **17–22** FR/media/jobs/SEO/contract; **23–29** coverage/E2E/UX/CI; **30–36** phi chức năng/vận hành; **37–38** tài liệu/artifact/UAT.

## Trạng thái và điều kiện chuyển phase

| Trạng thái | Ý nghĩa |
|---|---|
| Chưa bắt đầu | Chưa kiểm tra/thực hiện phase này |
| Đang làm | Có công việc đang thực hiện, report checkpoint ghi rõ |
| Đạt | Toàn bộ task/acceptance của phase được chứng minh ở đúng phạm vi/môi trường |
| Chưa đạt | Có assertion/acceptance thất bại; report nêu lỗi và bước tiếp theo |
| Chờ môi trường | Thiếu Docker/provider/device/staging/cửa sổ quan sát; phần đã kiểm vẫn ghi riêng |
| Chờ quyết định | Thiếu lựa chọn/chấp thuận ảnh hưởng contract, không tự chọn ngầm |
| Cần kiểm tra lại | Mã/artifact thay đổi làm evidence phase cũ không còn đủ |

File task/phase có checkbox trống là kế hoạch, không phải kết luận thiếu code. `Đạt` của một phase hẹp không tự chứng nhận cả FR/NFR liên quan. Sau cùng vẫn phải đối chiếu toàn bộ 34 FR/30 NFR/10 CONS và acceptance UI/data/API trong checklist gốc.
