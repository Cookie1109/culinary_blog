# Phase 02 — Chốt cách xử lý mâu thuẫn và sai khác

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

Kết quả sau xác nhận ngày23/10/2026: **Đạt trong phạm vi tài liệu/quyết định**. Người dùng duyệt đủ góiA/B/C,11 quyết định có hiệu lực theo [APP-2026-10-23-ABC](approvals/2026-10-23-abc.md). D0-02/D0-03/D0-04 đã đóng; xem [register](decision-register.md) và [report](reports/phase-02-result.md). Implementation/runtime vẫn thuộc các phase owner.

## Giao việc bằng prompt này

```text
Thực hiện Phase 02 theo docs/specs/srs-completion-phases/phase-02-decisions-and-conflicts.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D0-02`, `D0-03`, `D0-04`.
- Phụ thuộc kỹ thuật: [Phase 01](phase-01-acceptance-inventory.md)
- Loại công việc: **Tài liệu/quyết định**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-REL-001`, `NFR-SEO-003`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `docs/specs/phase-0/00_SRS_Conformance_and_Resolution.md`
- `docs/specs/phase-0/02_Decision_Log.md`
- `docs/specs/srs-completion-phases/acceptance-inventory.md`

## Task gốc phải đóng

- [x] **D0-02 · Quyết định:** rà đủ GAP-001–033; giữ các quyết định đã được chốt trong phiên làm việc, ghi rõ phần còn cần người nghiệm thu xác nhận. Bao gồm 400/422, 409/If-Match, soft delete, body refresh/logout, public/private split, token entropy, FTS, bigint version, API/schema và private bucket. Không tự đảo baseline trong lúc sửa.
- [x] **D0-03 · Quyết định:** ghi riêng sai khác bổ sung: Output Cache hay cache-aside; ping sitemap Google; công cụ/ruleset SonarAnalyzer/StyleCop/Airbnb/Jest/Testing Library; browser minimum; lock singleton thay RedLock nếu có. Mỗi mục có nguyên văn, đề xuất, lý do, ảnh hưởng và trạng thái chấp thuận; chưa chốt thì chưa đánh đạt nguyên văn.
- [x] **D0-04 · Quyết định:** làm rõ uptime 99,5% so với “3,65 giờ downtime/năm” không tương đương, khoảng thời gian đo, múi giờ backup 03:00 và cách nghiệm thu scaling/production. Lưu tiêu chí đo thống nhất trước khi D9 ghi kết quả.

## Trình tự thực hiện trong phase

1. Rà GAP-001–033; giữ quyết định đã được người dùng chốt, phân biệt proposal và quyết định được chấp thuận.
2. Ghi vấn đề Output Cache, ping sitemap, toolchain/ruleset, browser, lock singleton, uptime và múi giờ backup thành từng decision có ảnh hưởng/test.
3. Tạo decision-register.md; nêu phương án thực hiện ưu tiên và phần còn chờ, liên kết acceptance liên quan.

## Cách kiểm chứng

- Kiểm không sửa ngầm contract/HTTP status/schema và không tự đánh Accepted.
- Nếu xác minh thông tin bên ngoài, ghi nguồn chính thức và ngày kiểm tra; thiếu quyết định chỉ chặn phần phụ thuộc.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [x] Mỗi mâu thuẫn có quyết định hiện có hoặc đề xuất cụ thể/trạng thái cần chốt.
- [x] Uptime, backup time zone và phạm vi scaling có tiêu chí đo hoặc được ghi mở.
- [x] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [x] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-02-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-01-acceptance-inventory.md) · [Tiếp theo để tham khảo](phase-03-environment-and-baseline.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
