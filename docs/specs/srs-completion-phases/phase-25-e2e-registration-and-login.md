# Phase 25 — E2E đăng ký và vòng đời đăng nhập

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 25 theo docs/specs/srs-completion-phases/phase-25-e2e-registration-and-login.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D8-03`, `D8-04`.
- Phụ thuộc kỹ thuật: [Phase 07](phase-07-local-auth-profile-password.md), [Phase 08](phase-08-google-session-lifecycle.md), [Phase 09](phase-09-logout-token-owner.md), [Phase 24](phase-24-integration-matrix-and-playwright.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-001`, `FR-AUTH-002`, `FR-AUTH-004`, `FR-AUTH-005`, `FR-AUTH-007`, `NFR-SEC-002`, `NFR-MAINT-002`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/e2e/`
- `frontend/tests/`
- `frontend/package.json`
- `backend/tests/`
- `scripts/`

## Task gốc phải đóng

- [ ] **D8-03 · Sửa/kiểm chứng E2E-01:** đăng ký từ UI → Author/auto-login → profile → welcome email capture; kiểm lỗi field/duplicate và hồ sơ sửa được. SMTP failure được kiểm riêng không rollback user.
- [ ] **D8-04 · Sửa/kiểm chứng E2E-02:** local login → route riêng → hết access token/refresh → logout; token/session cũ không dùng được. Google smoke/provider test D3 được bổ sung riêng, không thay luồng local hoặc đánh provider thật từ mock.

## Trình tự thực hiện trong phase

1. Dùng harness phase24, viết register/auto-login/profile/welcome capture từ UI.
2. Viết login/private route/access expiry/refresh/logout/session rejection từ UI.
3. Kiểm lỗi field/duplicate/invalid login; không dùng direct API thay các thao tác chính cần browser.

## Cách kiểm chứng

- Hai luồng chạy browser thật với backend test thật; trace/screenshot lỗi không chứa secrets.
- SMTP capture/failure test có phạm vi rõ và Google provider smoke không giả là local E2E.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] E2E-01 và E2E-02 chạy lại một lệnh và có assertion cuối.
- [ ] Không skip expiry/refresh vì test chậm; dùng clock/config test phù hợp không đổi production.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-25-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-24-integration-matrix-and-playwright.md) · [Tiếp theo để tham khảo](phase-26-e2e-recipes-publish-search.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
