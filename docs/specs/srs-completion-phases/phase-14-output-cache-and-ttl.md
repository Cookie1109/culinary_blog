# Phase 14 — Đóng cơ chế Output Cache và TTL

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 14 theo docs/specs/srs-completion-phases/phase-14-output-cache-and-ttl.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D5-01`, `D5-02`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 06](phase-06-pipeline-and-logging.md), [Phase 13](phase-13-mutation-revalidation.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-001`, `NFR-PERF-003`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Application/Abstractions/Caching/`
- `backend/src/CulinaryBlog.Infrastructure/Caching/`
- `backend/src/CulinaryBlog.Api/Program.cs`
- `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs`

## Task gốc phải đóng

- [ ] **D5-01 · Quyết định/sửa:** triển khai Redis-backed Output Cache theo GAP-013, hoặc lập thay đổi baseline cho cơ chế thay thế có đủ acceptance. Ghi rõ vai trò cache dữ liệu và cache HTTP để tránh hai tầng có TTL/invalidation mâu thuẫn; không báo cache-aside hiện có là Output Cache.
- [ ] **D5-02 · Kiểm chứng:** TTL category 30 phút, recipe detail 5 phút, list 15 phút, search 1 phút theo baseline; key bao phủ filter/sort/page/normalization. Test thời gian bằng clock/TTL query phù hợp thay vì chờ đủ 30 phút.

## Trình tự thực hiện trong phase

1. Theo quyết định phase 02: Redis-backed Output Cache hoặc thay thế có acceptance/approval rõ.
2. Phân vai data/HTTP cache để tránh chồng TTL/invalidation; không ghi cache-aside thành Output Cache.
3. Kiểm TTL category 30 phút/detail 5/list 15/search 1 và key đủ tham số/normalization.

## Cách kiểm chứng

- Unit key/clock và integration TTL/Redis store thật, không sleep 30 phút.
- Contract/no-store/authorization vẫn đúng và tag invalidation không bị cache HTTP cũ che.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Cơ chế/TTL được hiện thực theo quyết định, có phép kiểm rõ cho từng resource.
- [ ] Nếu cần chốt thay baseline: ghi Chờ quyết định, không tự đổi Accepted.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-14-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-13-mutation-revalidation.md) · [Tiếp theo để tham khảo](phase-15-cache-isolation-and-failure.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
