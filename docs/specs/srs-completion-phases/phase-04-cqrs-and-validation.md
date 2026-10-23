# Phase 04 — Hoàn thiện handlers và validation pipeline

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 04 theo docs/specs/srs-completion-phases/phase-04-cqrs-and-validation.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D2-01`, `D2-02`, `D2-07`.
- Phụ thuộc kỹ thuật: [Phase 01](phase-01-acceptance-inventory.md), [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 03](phase-03-environment-and-baseline.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-003`, `NFR-SEC-004`, `NFR-MAINT-004`, `CONS-001`, `CONS-002`, `CONS-008`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Api/Presentation/AuthEndpoints.cs`
- `backend/src/CulinaryBlog.Api/Presentation/ContentEndpoints.cs`
- `backend/src/CulinaryBlog.Application/`
- `backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs`
- `backend/tests/CulinaryBlog.ArchitectureTests/`

## Task gốc phải đóng

- [ ] **D2-01 · Sửa:** lập inventory từng use case HTTP và bổ sung Command/Query Handler còn thiếu, gồm audit/sitemap; xử lý HTTP/stream tại adapter, orchestration ở Application và kỹ thuật storage/EF/SMTP ở Infrastructure. Không gọi một lớp chuyển tiếp rỗng là đã chứng minh toàn bộ nghiệp vụ đúng tầng.
- [ ] **D2-02 · Sửa:** chuyển validation nghiệp vụ còn nằm trong endpoint/service vào FluentValidation/Application pipeline; giữ parse/binding HTTP ở endpoint. Rà body/query/ID/page/sort/ETag/file metadata; validation sai không được ghi DB, queue job hoặc xóa cache.
- [ ] **D2-07 · Kiểm chứng:** bổ sung architecture/unit/integration cho các đường vừa chuyển; Domain không phụ thuộc layer/package ngoài BCL theo GAP-026, Application không tham chiếu Infrastructure, read không âm thầm ghi nghiệp vụ.

## Trình tự thực hiện trong phase

1. Inventory named requests và nơi endpoint/service còn xử lý nghiệp vụ; ưu tiên audit/sitemap còn bypass.
2. Chuyển từng use case còn thiếu qua Application, đặt validation đúng tầng; giữ binding/stream adapter ở HTTP và kỹ thuật EF/storage ở Infrastructure.
3. Thêm test đường thay đổi và kiểm dependency; làm từng lát nhỏ, không viết lại ContentService hàng loạt nếu không cần.

## Cách kiểm chứng

- Unit invalid input phải chứng minh không gọi ghi DB/queue/cache invalidation.
- Architecture tests và integration liên quan; kiểm OpenAPI/client nếu contract buộc đổi.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có đối chiếu mỗi use case đến handler/validator và đúng trách nhiệm tầng.
- [ ] Tests chứng minh code mới, không chỉ kiểm tên class hoặc source regex.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-04-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-03-environment-and-baseline.md) · [Tiếp theo để tham khảo](phase-05-authorization-and-transactions.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
