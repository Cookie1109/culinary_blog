# Phase 07 — Hồi quy local auth, hồ sơ và mật khẩu

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 07 theo docs/specs/srs-completion-phases/phase-07-local-auth-profile-password.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D3-01`, `D3-06`, `D3-08`.
- Phụ thuộc kỹ thuật: [Phase 04](phase-04-cqrs-and-validation.md), [Phase 05](phase-05-authorization-and-transactions.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-001`, `FR-AUTH-002`, `FR-AUTH-006`, `FR-AUTH-007`, `NFR-SEC-001`, `CONS-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs`
- `frontend/src/features/culinary/pages/Profile.tsx`
- `frontend/src/features/culinary/pages/auth/`
- `backend/tests/CulinaryBlog.IntegrationTests/AuthLifecycleTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/PasswordLifecycleTests.cs`

## Task gốc phải đóng

- [ ] **D3-01 · Kiểm chứng:** register Author mặc định/auto-login, email duplicate, password policy, PBKDF2 ≥100.000 iterations, generic login failure và lockout đúng acceptance; giữ đăng ký khi gửi email lỗi. Kiểm tra safe profile và phân quyền xem/sửa.
- [ ] **D3-06 · Kiểm chứng:** giữ form profile RHF/Zod, sửa/xóa avatar, validation URL và không cập nhật một phần khi request sai; xác minh avatar thực sự hiển thị qua chính sách ảnh/CSP và tên/bio đồng bộ session/UI sau lưu.
- [ ] **D3-08 · Kiểm chứng bổ sung người dùng:** đổi/quên/reset password: current password, không tiết lộ email tồn tại, token 30 phút/bound/single-use, expiry, revoke sessions, lỗi SMTP, reset URL encode và thông báo UI. Không phát sinh FR mới của PDF.

## Trình tự thực hiện trong phase

1. Kiểm local register/login/lockout/password hashing/TTL/auto-login; chưa sửa provider Google tại đây.
2. Kiểm sửa/xóa avatar, validate URL, CSP/ảnh hiển thị và đồng bộ session; giữ dữ liệu khi lỗi.
3. Chạy hồi quy đổi/quên/reset mật khẩu theo yêu cầu bổ sung, email capture/SMTP test và revoke phiên.

## Cách kiểm chứng

- API happy/error, React interaction, password token bound/single-use/30 phút và không enumerate account.
- Kiểm ảnh qua chính sách trình duyệt khi cần; phân biệt mock SMTP và SMTP thử thật.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Luồng local/profile/password có kết quả mới và chỉ sửa gap tái hiện.
- [ ] Không tính phần mật khẩu thành FR mới của PDF.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-07-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-06-pipeline-and-logging.md) · [Tiếp theo để tham khảo](phase-08-google-session-lifecycle.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
