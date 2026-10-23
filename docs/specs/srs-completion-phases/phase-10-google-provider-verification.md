# Phase 10 — Kiểm Google callback và provider thật

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 10 theo docs/specs/srs-completion-phases/phase-10-google-provider-verification.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D3-05`, `D3-07`.
- Phụ thuộc kỹ thuật: [Phase 08](phase-08-google-session-lifecycle.md), [Phase 09](phase-09-logout-token-owner.md)
- Loại công việc: **Provider/credential ngoài local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-003`, `NFR-SEC-002`, `CONS-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/auth.ts`
- `backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs`
- `backend/tests/`
- `frontend/tests/`

## Task gốc phải đóng

- [ ] **D3-05 · Kiểm chứng:** Google callback/code+PKCE, state/cancel/error, issuer/audience/signature/expiry/email verification/account linking và avatar. Kiểm thử có kiểm soát nhánh provider; bài mô phỏng không được ghi là đã đăng nhập Google thật.
- [ ] **D3-07 · Môi trường:** cấu hình Google client/callback hợp lệ và SMTP thử nghiệm khi cần; chạy smoke với provider/tài khoản thử đã được phép, lưu kết quả không chứa credential. Thiếu credential thì đánh chờ môi trường, tiếp tục test local độc lập.

## Trình tự thực hiện trong phase

1. Kiểm callback/code+PKCE/state/cancel/errors, verification claims/account linking/avatar bằng bài có kiểm soát.
2. Kiểm cấu hình client/callback chỉ qua tên biến/trạng thái, không in secrets.
3. Nếu có credential/tài khoản thử được phép, chạy smoke provider thật; nếu không, viết hướng dẫn và ghi đúng điều kiện chờ.

## Cách kiểm chứng

- Negative cases issuer/audience/expiry/signature/email và callback cancel/fail.
- Smoke login → refresh → logout với provider thật được ghi tách khỏi simulation.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Bằng chứng cô lập/provider thật phân biệt rõ, không đánh pass Google thật từ mock.
- [ ] Thiếu credential không chặn phase 11 trở đi khi phần code phụ thuộc đã đạt.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-10-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-09-logout-token-owner.md) · [Tiếp theo để tham khảo](phase-11-http-status-and-routes.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
