# Phase 35 — Kiểm nhiều instance và production scaling

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 35 theo docs/specs/srs-completion-phases/phase-35-multi-instance-and-production-scaling.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-09`, `D9-10`.
- Phụ thuộc kỹ thuật: [Phase 15](phase-15-cache-isolation-and-failure.md), [Phase 16](phase-16-rate-limit-and-headers.md), [Phase 20](phase-20-welcome-and-sitemap-jobs.md), [Phase 30](phase-30-load-and-database-plans.md), [Phase 33](phase-33-observability-and-resilience.md), [Phase 34](phase-34-backup-and-restore.md)
- Loại công việc: **Multi-instance/production topology**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-JOB-003`, `NFR-PERF-002`, `NFR-SCALE-001`, `NFR-SCALE-003`, `CONS-009`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `infra/nginx/`
- `infra/production/`
- `compose.yaml`
- `infra/operations/`
- `docs/reports/`

## Task gốc phải đóng

- [ ] **D9-09 · Môi trường/kiểm chứng:** ít nhất hai API instance qua Nginx, JWT stateless/shared Redis/Hangfire PostgreSQL, multi-worker/singleton sitemap đúng lock; test rate/cache/job không nhân đôi quota hoặc rò nội dung. So throughput/latency trước và sau thêm instance theo tiêu chí D0; ghi rõ cơ chế lock so với RedLock trong PDF.
- [ ] **D9-10 · Môi trường/kiểm chứng:** production scaling theo SRS: MinIO distributed 4+ nodes, CDN cho `_next/static`, upstream load balancing, persisted services và multi-stage/non-root images. Nếu môi trường nghiệm thu không có, ghi chưa kiểm chứng hoặc quyết định phạm vi được chấp thuận, không báo đạt từ file Compose đơn node.

## Trình tự thực hiện trong phase

1. Test≥2 API qua Nginx: JWT/shared cache/quota/queue/singleton/worker không nhân đôi sai.
2. So throughput/latency trước-sau thêm instance theo decision; ghi cơ chế lock so RedLock.
3. Kiểm MinIO distributed4+ nodes/CDN/static/images multi-stage non-root trong môi trường phù hợp; nếu không có ghi điều kiện triển khai/test còn thiếu.

## Cách kiểm chứng

- Không leak private cache, quota/job đúng khi alternating instance và worker crash.
- Evidence deployment thực cho production scaling, không suy single-node Compose thành distributed.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Multi-instance có test; production requirements có evidence hoặc pending rõ.
- [ ] Không tự mua tài nguyên/cloud hoặc deploy production để đóng phase.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-35-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-34-backup-and-restore.md) · [Tiếp theo để tham khảo](phase-36-sla-alerts-and-maintenance.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
