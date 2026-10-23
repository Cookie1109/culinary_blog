# Phase 12 — Hoàn thiện ISR và bài HTTP production

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 12 theo docs/specs/srs-completion-phases/phase-12-isr-ttl-production.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D4-02`, `D4-07`.
- Phụ thuộc kỹ thuật: [Phase 11](phase-11-http-status-and-routes.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-001`, `FR-CAT-002`, `FR-RCP-002`, `NFR-PERF-005`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/app/(public)/`
- `frontend/src/lib/api/public-content-server.ts`
- `frontend/scripts/check-isr.mjs`
- `postman/simulations/public-isr.sim.yaml`
- `postman/mocks/culinary-public/`

## Task gốc phải đóng

- [ ] **D4-02 · Sửa/kiểm chứng:** `/` và `/categories` ISR 3600 giây; `/recipes/[slug]` ISR 300; `/categories/[slug]` ISR 600. Kiểm tra Full Route Cache/headers/MISS-HIT và thời điểm hết hạn hoặc tái tạo, không chỉ có dòng `revalidate` hay fetch Data Cache.
- [ ] **D4-07 · Kiểm chứng:** hoàn thiện bài HTTP production đang có, chạy hết assertion kể cả các bước đứng sau lỗi hiện tại; giữ Postman simulation từ OpenAPI cho test cô lập, bổ sung test stack thật cho D4-05. Hai loại kết quả có nhãn riêng.

## Trình tự thực hiện trong phase

1. Kiểm route rendering thật và TTL 3600/3600/300/600 cho home/categories/recipe/category detail.
2. Sửa phần dynamic/cache không đúng acceptance, giữ pagination và 404 đã đóng ở phase 11.
3. Chạy hết test production với Postman simulation từ contract, kiểm logs chứng minh upstream được gọi.

## Cách kiểm chứng

- Fresh production build → MISS/HIT/header/TTL/revalidation/404 boundaries, cả các assertion trước đây chưa tới.
- Không dùng --skip-build sau khi sửa source; mock trả đúng contract, không giả test pass bằng response của mock.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Bài HTTP chạy hết xanh và đủ loại rendering/TTL.
- [ ] Chỉ claim đã chạy Next thật với mock; backend mutation xuyên suốt để phase 13.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-12-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-11-http-status-and-routes.md) · [Tiếp theo để tham khảo](phase-13-mutation-revalidation.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
