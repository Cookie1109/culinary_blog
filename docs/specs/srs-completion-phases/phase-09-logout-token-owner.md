# Phase 09 — Sửa kiểm tra owner khi logout

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 09 theo docs/specs/srs-completion-phases/phase-09-logout-token-owner.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D3-04`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 04](phase-04-cqrs-and-validation.md), [Phase 08](phase-08-google-session-lifecycle.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-003`, `FR-AUTH-005`, `NFR-SEC-002`, `CONS-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Api/Presentation/AuthEndpoints.cs`
- `backend/src/CulinaryBlog.Application/Auth/AuthRequests.cs`
- `backend/src/CulinaryBlog.Infrastructure/Identity/AuthService.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/AuthLifecycleTests.cs`

## Task gốc phải đóng

- [ ] **D3-04 · Sửa:** hoàn thiện logout bearer optional theo GAP-007: bearer còn hạn phải cùng owner với refresh token, bearer hết hạn vẫn cho revoke bằng refresh hợp lệ, request lặp idempotent; quy định status khi bearer/token khác chủ trong contract rồi kiểm thử.

## Trình tự thực hiện trong phase

1. Theo GAP-007, xác định principal hợp lệ và owner refresh token tại Application.
2. Cho logout với bearer optional/expired access khi refresh hợp lệ; chặn bearer còn hạn khác owner theo status đã chốt.
3. Giữ idempotent và không tiết lộ token/user qua response.

## Cách kiểm chứng

- Same owner, different owner, missing bearer, expired access, repeated logout và old refresh rejection.
- So status/error/header với OpenAPI sau sửa.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Không revoke token người khác bằng bearer không cùng owner.
- [ ] Logout lặp và access hết hạn vẫn đúng contract.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-09-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-08-google-session-lifecycle.md) · [Tiếp theo để tham khảo](phase-10-google-provider-verification.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
