# Phase 23 — Nâng unit coverage Application và thêm gate

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 23 theo docs/specs/srs-completion-phases/phase-23-unit-coverage-gate.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D7-01`, `D7-02`, `D7-03`.
- Phụ thuộc kỹ thuật: [Phase 04](phase-04-cqrs-and-validation.md), [Phase 05](phase-05-authorization-and-transactions.md), [Phase 06](phase-06-pipeline-and-logging.md), [Phase 09](phase-09-logout-token-owner.md), [Phase 22](phase-22-schema-migrations-openapi.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-MAINT-002`, `NFR-MAINT-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/tests/CulinaryBlog.UnitTests/`
- `backend/tests/CulinaryBlog.UnitTests/CulinaryBlog.UnitTests.csproj`
- `.github/workflows/ci.yml`
- `scripts/`

## Task gốc phải đóng

- [ ] **D7-01 · Sửa:** đọc report uncovered Application commands/queries/validators/behaviors sau D2/D3; thêm unit test hành vi thành công, invalid input, forbidden/not-found/conflict, failure/cancellation/cache. Test phải phát hiện sai nghiệp vụ, không chỉ khớp cách viết handler.
- [ ] **D7-02 · Sửa:** đạt **Application unit line coverage ≥80%**, đo riêng layer từ bộ unit; không lấy Domain hoặc integration coverage thay thế, không loại trừ mã nghiệp vụ để tăng tỷ lệ. Giữ báo cáo line/branch và phân tích phần còn thiếu.
- [ ] **D7-03 · Sửa:** thêm gate đọc coverage XML và fail khi dưới 80%, dùng được local và CI; kiểm gate bằng một fixture/report dưới ngưỡng rồi khôi phục, lưu exit code. Gate phải nhắm đúng Application, không chỉ upload report.

## Trình tự thực hiện trong phase

1. Đọc uncovered commands/queries/validators/behaviors trên mã hiện tại.
2. Thêm unit test hành vi/nhánh lỗi/quyền/cancellation/cache thật cần thiết; đo riêng Application.
3. Thêm local/CI gate fail dưới 80%, chứng minh cả pass và report fixture dưới ngưỡng.

## Cách kiểm chứng

- Application unit line coverage ≥80%; giữ line/branch XML và cách tính.
- Không thay bằng integration/Domain hoặc exclude mã nghiệp vụ; gate dưới ngưỡng phải exit nonzero.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Coverage mục tiêu đạt và gate đã được thực thi, không chỉ có YAML.
- [ ] Không thêm test phản chiếu implementation mà không bảo vệ acceptance.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-23-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-22-schema-migrations-openapi.md) · [Tiếp theo để tham khảo](phase-24-integration-matrix-and-playwright.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
