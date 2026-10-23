# Phase 06 — Kiểm invalidation pipeline và structured logging

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 06 theo docs/specs/srs-completion-phases/phase-06-pipeline-and-logging.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D2-05`, `D2-06`.
- Phụ thuộc kỹ thuật: [Phase 04](phase-04-cqrs-and-validation.md), [Phase 05](phase-05-authorization-and-transactions.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-OBS-002`, `NFR-PERF-004`, `NFR-MAINT-004`, `CONS-010`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Application/Behaviors/`
- `backend/src/CulinaryBlog.Api/Program.cs`
- `backend/src/CulinaryBlog.Infrastructure/Jobs/`
- `backend/tests/CulinaryBlog.UnitTests/ContentPipelineTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/AuditMutationMiddlewareTests.cs`

## Task gốc phải đóng

- [ ] **D2-05 · Kiểm chứng:** giữ thứ tự logging/validation/caching/handler/invalidation hợp lý; cache chỉ xóa sau ghi thành công, không xóa khi validation/quyền/handler lỗi; kiểm tra cả client disconnect và lỗi scheduler/cache, ghi rõ cơ chế retry.
- [ ] **D2-06 · Sửa/kiểm chứng:** bảo đảm structured log của các use case có CorrelationId, RequestPath và UserId khi có request xác thực; job có job/outbox ID để truy vết. Không log password/token/secret; kiểm tra slow query >100ms và pipeline >500ms theo SRS, cùng hành vi khi sink lỗi.

## Trình tự thực hiện trong phase

1. Kiểm thứ tự validation/cache/handler/invalidation và lỗi cancellation/scheduler/cache sau commit.
2. Bổ sung context CorrelationId/RequestPath/UserId và job/outbox ID phù hợp; không ghi token/password.
3. Kiểm slow query >100ms/pipeline >500ms và sink lỗi bằng đường test có ý nghĩa.

## Cách kiểm chứng

- Pipeline thật: thành công xóa cache, lỗi quyền/validation/handler không xóa; side effects đúng sau disconnect.
- Capture log asserts context/redaction/slow warning và sink failure policy.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Pipeline/logging đạt acceptance và có bằng chứng cho failure paths.
- [ ] Distributed TTL/Output Cache và vận hành exporter để phase 14–15/33 xử lý.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-06-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-05-authorization-and-transactions.md) · [Tiếp theo để tham khảo](phase-07-local-auth-profile-password.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
