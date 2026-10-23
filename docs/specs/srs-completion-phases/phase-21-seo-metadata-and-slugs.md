# Phase 21 — Hoàn thiện metadata, JSON-LD và lịch sử slug

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 21 theo docs/specs/srs-completion-phases/phase-21-seo-metadata-and-slugs.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-10`, `D6-11`, `D6-12`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 11](phase-11-http-status-and-routes.md), [Phase 17](phase-17-recipe-category-composition.md), [Phase 20](phase-20-welcome-and-sitemap-jobs.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-RCP-002`, `FR-RCP-004`, `NFR-SEO-001`, `NFR-SEO-002`, `NFR-SEO-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/lib/seo.ts`
- `frontend/src/app/(public)/`
- `backend/src/CulinaryBlog.Domain/Recipes/`
- `backend/src/CulinaryBlog.Infrastructure/Persistence/`
- `backend/tests/`
- `frontend/tests/`

## Task gốc phải đóng

- [ ] **D6-10 · Sửa:** metadata title hoàn chỉnh, gồm hậu tố `| Culinary Blog` khi áp dụng, ≤60 ký tự; description 150–160 ký tự có ý nghĩa, OG 1200×630/Twitter/canonical/index-noindex phù hợp; kiểm tra long/short title/description và trang phân trang. Không chèn văn bản vô nghĩa chỉ để đủ độ dài.
- [ ] **D6-11 · Sửa:** lưu lịch sử draft slug khi đổi title/slug, 301 từ slug cũ sang slug hiện hành, xử lý collision và tránh loop/chain không cần thiết. Slug sau publish giữ ổn định; URL cũ không được dẫn tới Draft riêng tư hoặc phá public/private split.
- [ ] **D6-12 · Kiểm chứng:** Recipe JSON-LD đầy đủ field có dữ liệu thật, duration/yield/ingredients/steps/nutrition đúng; escape chống XSS, ảnh mặc định hợp lệ; chạy Rich Results Test trên bản triển khai phù hợp và lưu kết quả, không tạo rating/nutrition giả.

## Trình tự thực hiện trong phase

1. Sửa title đầy đủ ≤60/description 150–160 có nghĩa và OG/Twitter/canonical/robots.
2. Thêm history/301 draft slug cũ theo contract, migration tối thiểu, không lộ Draft hoặc thay published slug.
3. Kiểm Recipe JSON-LD dữ liệu thật/escape/nutrition/duration; chuẩn bị/chạy Rich Results theo môi trường có được.

## Cách kiểm chứng

- Long/short metadata, pagination, HTML head, collision/redirect loop/old slug/private access.
- JSON-LD snapshot/behavior và Google Rich Results thật ghi riêng khi có URL triển khai.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] SEO contract có test, migration history giữ dữ liệu và không lộ private content.
- [ ] Không fake rating/nutrition/SEO validation bên ngoài khi chưa chạy.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-21-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-20-welcome-and-sitemap-jobs.md) · [Tiếp theo để tham khảo](phase-22-schema-migrations-openapi.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
