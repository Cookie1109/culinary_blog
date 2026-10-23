# Phase 15 — Kiểm cache privacy, invalidation và Redis failure

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 15 theo docs/specs/srs-completion-phases/phase-15-cache-isolation-and-failure.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D5-03`, `D5-04`.
- Phụ thuộc kỹ thuật: [Phase 14](phase-14-output-cache-and-ttl.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-001`, `NFR-PERF-003`, `NFR-REL-002`, `NFR-SCALE-001`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Caching/`
- `backend/src/CulinaryBlog.Application/Behaviors/`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryApiTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/RedisApplicationCacheTests.cs`

## Task gốc phải đóng

- [ ] **D5-03 · Kiểm chứng:** invalidation sau mọi mutation liên quan cả hai nhóm tags, kể cả primary image và composition; cache không lộ Draft/Archived hoặc token/user state giữa Guest/Author/Admin; hai instance cùng thấy dữ liệu mới.
- [ ] **D5-04 · Kiểm chứng:** Redis down/timeout vẫn đọc DB và không làm hỏng write đã commit; xác minh phục hồi/cache metrics, giới hạn và xử lý cạnh tranh fill cache nếu phát hiện vấn đề qua đo lường.

## Trình tự thực hiện trong phase

1. Rà tất cả mutation/tags, cả primary/composition/category và hai API instance test.
2. Kiểm Guest/Author/Admin không nhận Draft/private state từ shared cache.
3. Tắt/timeout Redis của môi trường thử, chứng minh DB fallback/write/recovery/metrics.

## Cách kiểm chứng

- Alternating instances đọc sau write phải cùng thấy dữ liệu mới.
- Cache outage và phục hồi không làm crash public read hoặc hỏng commit; chỉ thêm stampede protection nếu đo chỉ ra cần.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Không leak private data, invalidation xuyên instance có evidence.
- [ ] Redis fail gracefully và không che lỗi DB nghiệp vụ.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-15-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-14-output-cache-and-ttl.md) · [Tiếp theo để tham khảo](phase-16-rate-limit-and-headers.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
