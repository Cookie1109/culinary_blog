# Phase 13 — Kiểm invalidation từ mutation tới Next

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 13 theo docs/specs/srs-completion-phases/phase-13-mutation-revalidation.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D4-05`, `D4-06`.
- Phụ thuộc kỹ thuật: [Phase 06](phase-06-pipeline-and-logging.md), [Phase 12](phase-12-isr-ttl-production.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-RCP-005`, `FR-RCP-006`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Jobs/PublicContentRefreshScheduler.cs`
- `backend/src/CulinaryBlog.Application/Behaviors/CacheInvalidationBehavior.cs`
- `frontend/src/app/api/revalidate/route.ts`
- `backend/tests/`
- `frontend/scripts/`

## Task gốc phải đóng

- [ ] **D4-05 · Sửa/kiểm chứng:** chạy mutation create/update/delete/publish/unpublish/archive/category trên backend thật → pipeline → scheduler/job → callback Next → nội dung home/category/list/detail/search/media cập nhật; Draft/Archived biến mất khỏi public/cache.
- [ ] **D4-06 · Kiểm chứng:** callback đúng secret mới invalidation; thiếu/sai secret bị từ chối, không lộ secret. Kiểm tra retry khi Next tạm unavailable và tránh làm rollback nghiệp vụ đã commit; build không phụ thuộc API đang online nếu phương án ISR đã chọn yêu cầu vậy.

## Trình tự thực hiện trong phase

1. Dựng stack test riêng và chạy mutation thật tới scheduler/job/callback.
2. Kiểm mọi thay đổi public visibility/content: create/update/delete/category/image/composition/publish/unpublish/archive.
3. Kiểm secret callback và retry khi Next unavailable, không rollback write đã commit.

## Cách kiểm chứng

- Backend thật → scheduler → HTTP callback → page mới; thử sai/missing secret.
- Đo bài failure HTTP dependency bằng simulation khi cần, kiểm outbox/retry và log, không gọi provider production.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Public cache/pages cập nhật sau write và Draft/Archived biến mất đúng quyền.
- [ ] Có kết quả stack thật ngoài bài simulation của phase 12.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-13-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-12-isr-ttl-production.md) · [Tiếp theo để tham khảo](phase-14-output-cache-and-ttl.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
