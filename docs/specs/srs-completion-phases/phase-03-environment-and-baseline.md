# Phase 03 — Chuẩn hóa môi trường, format và baseline test

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 03 theo docs/specs/srs-completion-phases/phase-03-environment-and-baseline.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D1-01`, `D1-02`, `D1-03`, `D1-04`.
- Phụ thuộc kỹ thuật: [Phase 01](phase-01-acceptance-inventory.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-MAINT-001`, `CONS-003`, `CONS-009`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Không fix Google/ISR/CQRS chỉ vì baseline tái hiện lỗi; giao đúng phase phía sau.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/package.json`
- `frontend/next.config.mjs`
- `frontend/src/middleware.ts`
- `frontend/scripts/check-isr.mjs`
- `frontend/tests/`
- `backend/tests/`
- `.github/workflows/ci.yml`

## Task gốc phải đóng

- [ ] **D1-01 · Môi trường:** kiểm tra Docker Engine, các image PostgreSQL/Redis/MinIO/SMTP thử nghiệm và runtime Node 22.23.3/.NET 10; chạy test bằng database/container riêng. Phân biệt lỗi môi trường với assertion nghiệp vụ.
- [ ] **D1-02 · Sửa:** sửa Prettier của `next.config.mjs`, `scripts/check-isr.mjs`, `src/middleware.ts`, `tests/public-content-wiring.test.mjs`, `tests/rendering-cache-wiring.test.mjs`, `tests/visual-responsive-regression.test.mjs`; xác minh lại backend format và không sửa format lan sang file không liên quan.
- [ ] **D1-03 · Kiểm chứng:** clean/locked restore theo runtime/lockfile đã chốt; chạy build, lint, typecheck, unit, architecture, frontend và integration. Lưu exit code, số pass/fail/skip, nguyên nhân và phiên bản; không ghi 99/99 nếu container chưa khởi tạo.
- [ ] **D1-04 · Kiểm chứng:** ghi HEAD + trạng thái worktree, thời điểm, runtime, cấu hình test đã lược secrets. Sau này gắn release với commit/image digest để biết chính xác bản nào được đo.

## Trình tự thực hiện trong phase

1. Ghi git status/HEAD và runtime trước sửa, bảo toàn thay đổi đang có; kiểm Docker bằng thao tác đọc trước.
2. Sửa đúng sáu file Prettier được checklist nêu; chạy locked restore/build/lint/typecheck/test bằng runtime đã chốt.
3. Lưu baseline mới và phân loại assertion fail với Docker/font/network/config fail; không sửa nghiệp vụ ở phase này.

## Cách kiểm chứng

- Chạy các lệnh có sẵn ở mục 6 checklist, lưu exit code và số pass/fail/skip.
- Không thay dữ liệu stack người dùng; integration dùng container/database thử riêng.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Format liên quan xanh; baseline có đủ version/command/result.
- [ ] Nếu thiếu môi trường: ghi Chờ môi trường, không ghi test pass; phần kiểm được vẫn hoàn tất.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-03-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-02-decisions-and-conflicts.md) · [Tiếp theo để tham khảo](phase-04-cqrs-and-validation.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
