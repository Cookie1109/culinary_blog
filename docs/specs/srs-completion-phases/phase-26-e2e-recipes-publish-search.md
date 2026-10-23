# Phase 26 — E2E tạo, xuất bản và tìm kiếm công thức

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 26 theo docs/specs/srs-completion-phases/phase-26-e2e-recipes-publish-search.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D8-05`, `D8-06`, `D8-07`.
- Phụ thuộc kỹ thuật: [Phase 17](phase-17-recipe-category-composition.md), [Phase 18](phase-18-search-filters-and-pagination.md), [Phase 19](phase-19-media-storage-and-jobs.md), [Phase 21](phase-21-seo-metadata-and-slugs.md), [Phase 24](phase-24-integration-matrix-and-playwright.md), [Phase 25](phase-25-e2e-registration-and-login.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-RCP-003`, `FR-RCP-005`, `FR-RCP-006`, `FR-RCP-009`, `FR-RCP-010`, `FR-SRCH-001`, `FR-SRCH-002`, `FR-SRCH-003`, `FR-SRCH-004`, `NFR-MAINT-002`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/e2e/`
- `frontend/tests/`
- `frontend/src/features/culinary/pages/dashboard/`
- `scripts/`

## Task gốc phải đóng

- [ ] **D8-05 · Sửa/kiểm chứng E2E-03:** tạo Draft bằng wizard với thông tin, ingredient, step, image; kiểm preview/private visibility, validation và save/error/conflict recovery theo SRS.
- [ ] **D8-06 · Sửa/kiểm chứng E2E-04:** publish từ UI → home/list/detail/category/search thấy nội dung → unpublish/archive mất khỏi public/cache; Author khác không được sửa, Admin đúng quyền; kiểm cả nội dung vừa mutation và invalidation.
- [ ] **D8-07 · Sửa/kiểm chứng E2E-05:** tìm “pho/phở” → relevance/filter/sort/page/URL/back-forward → mở đúng detail; có empty/error/loading và query không hợp lệ rõ ràng.

## Trình tự thực hiện trong phase

1. Tạo Draft bằng wizard gồm ingredient/step/image và preview/private/error handling.
2. Publish → public/home/category/search → unpublish/archive mất nội dung; kiểm quyền cần thiết.
3. Search pho/phở, relevance/filter/sort/page/URL/back-forward/detail/empty/error.

## Cách kiểm chứng

- Ba luồng browser thật với backend thật và dữ liệu synthetic của test.
- Chạy cả năm E2E cùng nhau để bắt phụ thuộc dữ liệu/cache; cleanup theo owner resources.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Đủ năm critical browser journeys thực chạy, có assertion nội dung/state/visibility.
- [ ] HTTP simulation không thay backend thật của các bài nghiệm thu này.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-26-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-25-e2e-registration-and-login.md) · [Tiếp theo để tham khảo](phase-27-ux-responsive-accessibility.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
