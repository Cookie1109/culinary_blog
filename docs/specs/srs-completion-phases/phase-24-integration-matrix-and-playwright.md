# Phase 24 — Lập endpoint matrix và dựng Playwright

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 24 theo docs/specs/srs-completion-phases/phase-24-integration-matrix-and-playwright.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D8-01`, `D8-02`.
- Phụ thuộc kỹ thuật: [Phase 22](phase-22-schema-migrations-openapi.md), [Phase 23](phase-23-unit-coverage-gate.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-006`, `FR-CAT-005`, `FR-RCP-004`, `FR-RCP-007`, `FR-FILE-001`, `NFR-SEC-006`, `NFR-MAINT-002`, `CONS-005`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/tests/CulinaryBlog.IntegrationTests/`
- `frontend/package.json`
- `frontend/tests/`
- `compose.yaml`
- `scripts/`

## Task gốc phải đóng

- [ ] **D8-01 · Kiểm chứng:** lập bảng **mỗi endpoint ít nhất một happy path và một error case**, gắn với FR/acceptance; chạy full integration sau D6. Bao phủ toàn role matrix, invalid/body/query/header, not-found/duplicate, ETag race, privacy/cache, jobs, storage và migration; không suy từ tổng 99 test là đã đủ endpoint.
- [ ] **D8-02 · Sửa:** thêm Playwright config/fixtures và script một lệnh dựng/chờ/test/dọn tài nguyên thuộc test; dữ liệu synthetic được cô lập, không dùng DB người dùng. Đặt tên script và ghi README sau khi triển khai, không quảng cáo lệnh chưa tồn tại là đã chạy được.

## Trình tự thực hiện trong phase

1. Lập endpoint → happy/error test matrix từ OpenAPI, bổ sung test còn thiếu và chạy full integration.
2. Thêm Playwright config/fixtures/script dựng-chờ-chạy-dọn test stack riêng, pin dependency phù hợp repo.
3. Tạo smoke thực thi chứng minh harness chạy browser thật; luồng chi tiết giao phase25–26.

## Cách kiểm chứng

- Mọi endpoint có ít nhất happy+error phù hợp; no silent skips.
- Một lệnh chạy harness và cleanup tài nguyên thuộc nó, không dùng DB/port stack user.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Harness có browser thực chạy và endpoint matrix có evidence.
- [ ] Không đánh đủ năm E2E ở phase scaffold này.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-24-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-23-unit-coverage-gate.md) · [Tiếp theo để tham khảo](phase-25-e2e-registration-and-login.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
