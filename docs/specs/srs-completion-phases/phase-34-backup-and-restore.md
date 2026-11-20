# Phase 34 — Đóng backup schedule, durability và restore drill

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 34 theo docs/specs/srs-completion-phases/phase-34-backup-and-restore.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-07`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 22](phase-22-schema-migrations-openapi.md), [Phase 33](phase-33-observability-and-resilience.md)
- Loại công việc: **Backup/restore test riêng**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-REL-003`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `infra/operations/`
- `scripts/restore-drill.ps1`
- `docs/reports/phase-8-reliability-report.md`
- `docs/release/`

## Task gốc phải đóng

- [x] **D9-07 · Sửa/kiểm chứng:** backup PostgreSQL mỗi ngày 03:00 theo múi giờ D0, retention 30 ngày; kiểm tra WAL/durability, persistent DB/MinIO volumes và tokens DB survive restart. Restore vào môi trường sạch so đủ dữ liệu/schema/media; ghi RTO/RPO, checksum/storage độc lập và runbook. Không chạy restore đè DB người dùng.

Nghiệm thu10/11/2026 theo PDF trang43 và DEC-10: lịch03UTC/30ngày đã bật và thực chạy thành công, khôi phục30bảng/2044dòng/5migration/keyring mã hóa, WAL/rowhash/tokencontinuity và namedvolume đã có bằng chứng. Restore301ảnh/301SHA trước được đối chiếu với nội dung hiện tại; không backup ảnh mới. KhoDB Windows tách khỏi nguồnNeon. Điều kiện máy/user/Docker/mạng hoạt động được ghi trong runbook; PDF không bắt GoogleDrive, kho ảnh ngoàiPC, máy chạy khi đã tắt hoặc một cam kếtRTO/RPO mới. MinIOproduction4+node và bản váserver vẫn thuộc D9-04/D9-10 còn mở, không được suy đạt từ task này. Xem [review nghiệm thu](reports/package-G06-result.md#acceptance-review-10112026).

## Trình tự thực hiện trong phase

1. Chỉnh/kiểm backup 03:00 theo timezone đã chốt, retention30/WAL/persistent DB-MinIO/token restart.
2. Backup và restore vào DB/bucket sạch của test, kiểm checksums/schema/rows/media.
3. Ghi RTO/RPO/storage independence/runbook, thiếu môi trường thật ghi riêng thay vì pass từ script.

## Cách kiểm chứng

- Test retention/schedule/restore và dữ liệu user/recipe/composition/token/outbox/media còn đúng.
- Chỉ thao tác tài nguyên thử đã xác nhận, tuyệt đối không restore đè user DB.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [x] Restore drill mới tái lập và lịch/retention hợp đồng có evidence.
- [x] Phần production storage chưa kiểm được được giữ pending.
- [x] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [x] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-34-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-33-observability-and-resilience.md) · [Tiếp theo để tham khảo](phase-35-multi-instance-and-production-scaling.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
