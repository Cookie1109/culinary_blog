# Phase 31 — Đo bundle, Core Web Vitals và INP

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 31 theo docs/specs/srs-completion-phases/phase-31-frontend-performance-and-inp.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-03`.
- Phụ thuộc kỹ thuật: [Phase 12](phase-12-isr-ttl-production.md), [Phase 21](phase-21-seo-metadata-and-slugs.md), [Phase 27](phase-27-ux-responsive-accessibility.md), [Phase 29](phase-29-ci-and-static-analysis.md)
- Loại công việc: **Browser/benchmark/RUM**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-PERF-005`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/scripts/`
- `frontend/package.json`
- `frontend/src/`
- `docs/reports/`

## Task gốc phải đóng

- [ ] **D9-03 · Kiểm chứng:** production bundle tất cả route ≤200 KB gzip, Lighthouse/CWV LCP ≤2,5s, CLS ≤0,1; lấy INP ≤200ms từ phép đo tương tác/RUM phù hợp và ghi cửa sổ mẫu. TBT là lab proxy, không thay thế chứng nhận INP; lưu audit meta/ảnh/code splitting và môi trường.

## Trình tự thực hiện trong phase

1. Build đúng artifact và chạy JS budget/Lighthouse cho routes public/auth/dashboard cần thiết.
2. Kiểm LCP/CLS/bundle/metadata/ảnh/code splitting và đo INP bằng tương tác hoặc RUM đúng tiêu chí phase02.
3. Lưu lab/field evidence riêng; nếu cần cửa sổ RUM chưa đủ thì chuẩn bị thu thập và handoff pending.

## Cách kiểm chứng

- LCP≤2.5s, CLS≤0.1, INP≤200ms, First Load JS≤200KB gzip theo SRS/phạm vi đo.
- TBT không thay INP; không fabricate samples hoặc suy từ bundle sang CWV.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có report mới, metric/môi trường/sample window chính xác.
- [ ] Chưa đủ RUM không được đánh INP đạt.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-31-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-30-load-and-database-plans.md) · [Tiếp theo để tham khảo](phase-32-security-and-https.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
