# Phase 33 — Kiểm telemetry, health và dependency failure

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 33 theo docs/specs/srs-completion-phases/phase-33-observability-and-resilience.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-05`, `D9-06`.
- Phụ thuộc kỹ thuật: [Phase 06](phase-06-pipeline-and-logging.md), [Phase 15](phase-15-cache-isolation-and-failure.md), [Phase 19](phase-19-media-storage-and-jobs.md), [Phase 20](phase-20-welcome-and-sitemap-jobs.md), [Phase 29](phase-29-ci-and-static-analysis.md)
- Loại công việc: **Observability/container test**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-FILE-002`, `FR-JOB-001`, `FR-JOB-002`, `FR-OBS-001`, `FR-OBS-002`, `FR-OBS-003`, `NFR-REL-002`, `CONS-010`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Api/Telemetry/`
- `backend/src/CulinaryBlog.Api/Health/`
- `infra/observability/`
- `scripts/failure-drill.ps1`
- `docs/reports/`

## Task gốc phải đóng

- [ ] **D9-05 · Kiểm chứng/sửa:** live/ready/aggregate health đúng dependency matrix và không lộ chi tiết công khai; xác minh HTTP/EF/custom trace/span/metrics xuất tới OTLP/collector/dashboard thật, correlation nối request → handler → job khi có. Kiểm alerts/log sink lỗi không làm lộ dữ liệu hoặc gây crash ngoài hợp đồng.
- [ ] **D9-06 · Kiểm chứng:** failure drill PostgreSQL/Redis/MinIO/SMTP/worker/restart, global 500 Problem Details không lộ stack, DB reconnect/timeout 30s và Redis fallback, queue persistence/retry; ghi thời điểm lỗi/hồi phục và dữ liệu còn nguyên.

## Trình tự thực hiện trong phase

1. Kiểm health dependency matrix không lộ nội bộ và trace HTTP/EF/custom metrics thật tới exporter/dashboard.
2. Chạy fault/restart drill trên stack chuyên dụng cho PostgreSQL/Redis/MinIO/SMTP/worker.
3. Chứng minh global handler/no stack leak/reconnect/timeout30s/retry/persistence/correlation và phục hồi.

## Cách kiểm chứng

- Lưu trace/metric/log liên kết được với request/job, không chỉ có AddOtlpExporter trong source.
- Dữ liệu và queue còn sau restart; ảnh/media/error behavior đúng contract khi dependency fail.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Health/telemetry/failure behavior có evidence runtime, ghi rõ scope.
- [ ] Không stop dependency thuộc stack người dùng đang chạy.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-33-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-32-security-and-https.md) · [Tiếp theo để tham khảo](phase-34-backup-and-restore.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
