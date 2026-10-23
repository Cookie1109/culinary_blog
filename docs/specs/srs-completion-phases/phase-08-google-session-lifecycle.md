# Phase 08 — Sửa vòng đời phiên Google và refresh

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 08 theo docs/specs/srs-completion-phases/phase-08-google-session-lifecycle.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D3-02`, `D3-03`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 04](phase-04-cqrs-and-validation.md), [Phase 07](phase-07-local-auth-profile-password.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-003`, `FR-AUTH-004`, `NFR-SEC-002`, `CONS-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/auth.ts`
- `frontend/src/features/culinary/contexts/AuthContext.tsx`
- `frontend/src/lib/api/session-token-store.ts`
- `backend/src/CulinaryBlog.Application/Auth/`
- `backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs`

## Task gốc phải đóng

- [ ] **D3-02 · Sửa:** bỏ việc revoke refresh token ngay sau Google login thành công; hoàn thiện lưu/refresh/revoke theo hợp đồng đã chốt, không lộ refresh token qua session công khai/log và không tạo thêm cơ chế cookie/BFF ngoài phạm vi một cách ngầm định.
- [ ] **D3-03 · Sửa/kiểm chứng:** dùng được phiên sau access token hết hạn, refresh rotation một lần, chống cạnh tranh refresh và reuse revoke family; logout thu hồi đúng phiên Google/local, xử lý lỗi/expired session rõ ràng, không lặp refresh vô hạn.

## Trình tự thực hiện trong phase

1. Tái hiện callback đang revoke refresh ngay khi exchange thành công; chốt thiết kế token theo baseline.
2. Sửa refresh/rotation/concurrency/expiry/logout của phiên Google, không lộ refresh trong session public hoặc log.
3. Thêm test lifecycle qua client/server boundaries và failure/reuse; update contract/types nếu thực sự thay đổi.

## Cách kiểm chứng

- Chứng minh access expiry vẫn refresh được; refresh race không cấp nhiều phiên trái policy, reuse revoke family.
- Dùng HTTP simulation/provider test có kiểm soát cho phần cô lập; báo rõ chưa phải Google thật.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Không còn revoke nhầm token ngay sau login; token lifecycle có test success/failure.
- [ ] Không tự đổi sang BFF/cookie architecture ngoài các quyết định được chốt.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-08-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-07-local-auth-profile-password.md) · [Tiếp theo để tham khảo](phase-09-logout-token-owner.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
