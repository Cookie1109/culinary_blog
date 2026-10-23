# Phase 30 — Đo tải API và review query/index/pooling

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 30 theo docs/specs/srs-completion-phases/phase-30-load-and-database-plans.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-01`, `D9-02`.
- Phụ thuộc kỹ thuật: [Phase 15](phase-15-cache-isolation-and-failure.md), [Phase 16](phase-16-rate-limit-and-headers.md), [Phase 22](phase-22-schema-migrations-openapi.md), [Phase 26](phase-26-e2e-recipes-publish-search.md), [Phase 29](phase-29-ci-and-static-analysis.md)
- Loại công việc: **Benchmark/container**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-PERF-001`, `NFR-PERF-002`, `NFR-PERF-003`, `NFR-PERF-004`, `NFR-SCALE-002`, `CONS-006`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `performance/`
- `scripts/performance.ps1`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryPerformanceTests.cs`
- `backend/src/CulinaryBlog.Infrastructure/Persistence/`
- `docs/reports/`

## Task gốc phải đóng

- [ ] **D9-01 · Kiểm chứng:** load smoke/load/stress trên artifact có định danh; profile tối thiểu 2 vCPU/4 GB cho API, ≥100 concurrent users; ghi dataset/request mix/duration/network/TLS, cold/warm và write traffic. Đạt warm GET p50 ≤150ms, API p95 ≤500ms, p99 ≤1000ms và Redis steady-state hit rate ≥80%; nới rate limit chỉ trong profile đo, ghi rõ và không sửa cấu hình production để né D5.
- [ ] **D9-02 · Kiểm chứng/sửa theo đo:** review EXPLAIN ANALYZE truy vấn chính, N+1/projection/batch và B-tree/GIN cho điều kiện WHERE/ORDER BY; chứng minh slow log >100ms; kiểm pool Npgsql max 100 connections/instance. Không thêm read replica/partitioning chỉ vì slide có ví dụ; các phần này được SRS ghi tùy chọn/nâng cao.

## Trình tự thực hiện trong phase

1. Chọn artifact/dataset/resource/network có định danh, API tối thiểu profile SRS; smoke/load/stress ≥100 users và read/write mix.
2. Đo p50/p95/p99/cache hit warm/cold; ghi profile rate-limit đo riêng không thay production.
3. Review EXPLAIN/N+1/projection/B-tree/GIN/slow logs/pool100; chỉ tối ưu bottleneck đo được.

## Cách kiểm chứng

- Warm GET p50≤150ms, API p95≤500ms/p99≤1000ms, hit≥80%; report có tải/thời gian/tài nguyên.
- EXPLAIN và pool evidence; thiếu Docker/đúng tài nguyên phải ghi giới hạn.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có phép đo mới và ngưỡng pass/fail minh bạch trên artifact cụ thể.
- [ ] Không dùng local pass làm production pass; không thêm partition/replica tùy chọn vô cớ.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-30-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-29-ci-and-static-analysis.md) · [Tiếp theo để tham khảo](phase-31-frontend-performance-and-inp.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
