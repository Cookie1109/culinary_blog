# Phase 16 — Sửa rate limiter, headers và quota chia sẻ

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 16 theo docs/specs/srs-completion-phases/phase-16-rate-limit-and-headers.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D5-05`, `D5-06`, `D5-07`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 03](phase-03-environment-and-baseline.md), [Phase 15](phase-15-cache-isolation-and-failure.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-002`, `NFR-SEC-003`, `NFR-SCALE-001`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Api/Presentation/DependencyInjection.cs`
- `backend/src/CulinaryBlog.Infrastructure/Caching/`
- `backend/tests/CulinaryBlog.IntegrationTests/AuthRateLimitTests.cs`
- `docs/specs/phase-0/openapi.v1.yaml`
- `infra/nginx/`

## Task gốc phải đóng

- [ ] **D5-05 · Sửa:** auth dùng sliding window 10 request/phút/IP; global fixed window 100 và upload 5 theo SRS. Chốt rõ ảnh hưởng khi nhiều limiter cùng áp dụng và phương án shared counter cho nhiều API instance, tránh mỗi instance cho thêm một quota độc lập.
- [ ] **D5-06 · Sửa:** phát `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` và `Retry-After` khi 429, thống nhất đơn vị/thời điểm và expose qua CORS khi UI cần. Cập nhật OpenAPI và test header thực.
- [ ] **D5-07 · Kiểm chứng:** test auth tại ranh giới cửa sổ trượt, request limit+1, IP độc lập, trusted proxy/header spoof, health không tiêu quota, upload quota và HTTP 429 Problem Details. Test luân phiên qua ít nhất hai API instance.

## Trình tự thực hiện trong phase

1. Auth sliding 10/phút/IP, global fixed 100, upload 5; chốt rõ shared counter/window và limiter kết hợp.
2. Phát Limit/Remaining/Reset/Retry-After đúng đơn vị/status và CORS exposure phù hợp.
3. Kiểm boundary, proxy spoof, hai instance và health exemption bằng test, giữ production quotas.

## Cách kiểm chứng

- Request limit+1, window boundary, độc lập IP, 429 Problem Details/header thực.
- Quota không nhân đôi khi chuyển instance và trusted proxy không tin header client tự đặt.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Đúng cơ chế/quota/headers trên một và hai instance.
- [ ] OpenAPI/tests phản ánh contract mới, không chỉ sửa cấu hình tên limiter.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-16-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-15-cache-isolation-and-failure.md) · [Tiếp theo để tham khảo](phase-17-recipe-category-composition.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
