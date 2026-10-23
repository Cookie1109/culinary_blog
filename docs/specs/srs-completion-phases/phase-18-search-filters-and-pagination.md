# Phase 18 — Nghiệm thu tìm kiếm và khám phá công thức

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 18 theo docs/specs/srs-completion-phases/phase-18-search-filters-and-pagination.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-04`.
- Phụ thuộc kỹ thuật: [Phase 15](phase-15-cache-isolation-and-failure.md), [Phase 17](phase-17-recipe-category-composition.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-RCP-001`, `FR-SRCH-001`, `FR-SRCH-002`, `FR-SRCH-003`, `FR-SRCH-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Application/Features/Recipes/Queries/`
- `backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs`
- `frontend/src/features/culinary/pages/RecipesList.tsx`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeDiscoveryApiTests.cs`

## Task gốc phải đóng

- [ ] **D6-04 · Kiểm chứng:** FTS có/không dấu, weighted rank/relevanceScore, q ≥2 sau trim, special characters/injection, empty result; AND filters category/difficulty/maxCookTime/minServings/prep bounds/maxTime, sort hai chiều/tie-break, pageSize 1–50 và metadata. UI/URL/default relevance đồng nhất API, `maxCookTime=0` không bị mất.

## Trình tự thực hiện trong phase

1. Đối chiếu q/rank/score/accent/privacy với FR-SRCH, giữ FTS config baseline.
2. Kiểm AND filters, sort/default/tie-break, paging/meta/cache key và maxCookTime=0.
3. Kiểm UI/URL/back-forward/form/error đồng nhất API, ghi gap mới và sửa trong phạm vi tìm kiếm.

## Cách kiểm chứng

- FTS DB thật có/không dấu/special/empty/short q; all filter/sort/page boundaries.
- React interaction và URL state; endpoint browse không yêu cầu q, search yêu cầu q hợp lệ.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Acceptance search/filter/sort/paging xanh; score và default đúng.
- [ ] Không làm yếu assertion chỉ để phù hợp cách query đang viết.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-18-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-17-recipe-category-composition.md) · [Tiếp theo để tham khảo](phase-19-media-storage-and-jobs.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
