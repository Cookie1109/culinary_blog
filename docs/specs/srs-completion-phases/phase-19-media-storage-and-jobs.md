# Phase 19 — Nghiệm thu upload, variants và xóa media

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 19 theo docs/specs/srs-completion-phases/phase-19-media-storage-and-jobs.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-05`, `D6-06`, `D6-07`.
- Phụ thuộc kỹ thuật: [Phase 05](phase-05-authorization-and-transactions.md), [Phase 15](phase-15-cache-isolation-and-failure.md), [Phase 17](phase-17-recipe-category-composition.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-RCP-008`, `FR-FILE-001`, `FR-FILE-002`, `FR-JOB-002`, `NFR-SEC-004`, `NFR-REL-002`, `CONS-007`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Đường dẫn Storage/ là phạm vi gợi ý; tìm adapter IFileStorageService thực tế trước sửa, không tạo folder chỉ để khớp tài liệu.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Application/Media/`
- `backend/src/CulinaryBlog.Infrastructure/Jobs/ImageProcessingJob.cs`
- `backend/src/CulinaryBlog.Infrastructure/Jobs/ObjectDeletionJob.cs`
- `backend/src/CulinaryBlog.Infrastructure/Storage/`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeCompositionApiTests.cs`

## Task gốc phải đóng

- [ ] **D6-05 · Kiểm chứng/sửa:** upload JPEG/PNG/WebP/AVIF ≤5 MB, magic bytes/decode/pixel/path traversal, kiểm tra size trước buffering theo SRS; filename GUID, bucket private, quyền tải/xem/xóa. Có ít nhất test adapter MinIO thật bên cạnh test storage giả, không suy kết quả fake thành storage thật.
- [ ] **D6-06 · Kiểm chứng:** resize decode thực medium 800×600, thumbnail 300×300, lưu bản gốc + variants/URL; retry 3, xử lý lỗi/restart/idempotency và fallback ảnh gốc khi job thất bại. Kiểm tra canvas/giữ tỷ lệ theo lựa chọn đã ghi, không đánh đủ fallback chỉ từ test kích thước.
- [ ] **D6-07 · Kiểm chứng:** delete object idempotent/retry 3/outbox, object không còn và metadata không trôi khi storage lỗi; không xóa ảnh recipe khác hoặc làm mất bản gốc khi resize thất bại.

## Trình tự thực hiện trong phase

1. Kiểm file allowlist/5MB/magic/decode/pixel/prebuffer size/GUID/private access và adapter storage thật.
2. Decode output 800×600/300×300, ba versions/DB URLs; test resize failure/retry/fallback gốc/restart.
3. Kiểm deletion idempotent/outbox/retry và storage error không mất dữ liệu ngoài phạm vi.

## Cách kiểm chứng

- MinIO thật riêng + fake storage khi cần fault injection; báo nhãn từng bài.
- Permissions/ETag/race/media visibility và ảnh gốc còn khi variant fail.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có evidence upload/resize/delete cả success/failure và đúng kích thước.
- [ ] Không thay test fallback bằng assertion source/kích thước.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-19-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-18-search-filters-and-pagination.md) · [Tiếp theo để tham khảo](phase-20-welcome-and-sitemap-jobs.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
