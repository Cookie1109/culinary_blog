# Phase 17 — Nghiệm thu category, recipe và composition

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 17 theo docs/specs/srs-completion-phases/phase-17-recipe-category-composition.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-01`, `D6-02`, `D6-03`.
- Phụ thuộc kỹ thuật: [Phase 05](phase-05-authorization-and-transactions.md), [Phase 13](phase-13-mutation-revalidation.md), [Phase 15](phase-15-cache-isolation-and-failure.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-001`, `FR-CAT-002`, `FR-CAT-003`, `FR-CAT-004`, `FR-CAT-005`, `FR-RCP-001`, `FR-RCP-002`, `FR-RCP-003`, `FR-RCP-004`, `FR-RCP-005`, `FR-RCP-006`, `FR-RCP-007`, `FR-RCP-008`, `FR-RCP-009`, `FR-RCP-010`, `NFR-SEC-006`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs`
- `backend/src/CulinaryBlog.Application/Content/`
- `frontend/src/features/culinary/pages/dashboard/`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeCoreApiTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeCompositionApiTests.cs`

## Task gốc phải đóng

- [ ] **D6-01 · Kiểm chứng:** category CRUD/quyền Admin, tên unique/giới hạn field, Name tăng dần public/count Published, quản trị orderIndex rõ nghĩa, detail pagination, xóa category còn recipe bị conflict, soft delete theo baseline.
- [ ] **D6-02 · Kiểm chứng:** public/owner/Admin recipe list/detail và wizard Draft; owner/Admin update với ETag/If-Match, stale race 409, missing header 400 theo baseline. Publish cần ingredient + step; unpublish/archive/unarchive/delete đúng transition và không lộ private content/media sau đó.
- [ ] **D6-03 · Kiểm chứng:** ingredient CRUD/nullability/limits/order; step CRUD/title/timer/append và renumber liên tục trong transaction; ảnh primary duy nhất kể cả concurrent write. Xác minh quyền và version cho từng mutation con, rollback khi một phần thao tác lỗi.

## Trình tự thực hiện trong phase

1. Đối chiếu từng acceptance category CRUD/order/count/slug/delete conflict và role.
2. Kiểm recipe Draft/update/ETag/state transitions/soft delete/private visibility.
3. Kiểm ingredient/step CRUD/limits/nullability/order/renumber/primary/version trong transaction; chỉ sửa thiếu sót có test.

## Cách kiểm chứng

- Happy/error/role/ETag race cho endpoint liên quan, API/UI wizard regression.
- Publish ingredient+step, không tự bắt buộc ảnh; soft delete/public-private theo baseline.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Acceptance category/recipe/composition có mã/test và kết quả mới.
- [ ] Không coi “đã có API” là đủ khi UI/privacy/concurrency chưa chứng minh.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-17-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-16-rate-limit-and-headers.md) · [Tiếp theo để tham khảo](phase-18-search-filters-and-pagination.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
