# Phase 20 — Nghiệm thu welcome email và sitemap job

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 20 theo docs/specs/srs-completion-phases/phase-20-welcome-and-sitemap-jobs.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-08`, `D6-09`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 06](phase-06-pipeline-and-logging.md), [Phase 17](phase-17-recipe-category-composition.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-AUTH-001`, `FR-JOB-001`, `FR-JOB-003`, `NFR-REL-002`, `NFR-SEO-003`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Email/`
- `backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapGenerationJob.cs`
- `backend/src/CulinaryBlog.Infrastructure/Jobs/SitemapXml.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/WelcomeEmailJobTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/SitemapGenerationJobTests.cs`

## Task gốc phải đóng

- [ ] **D6-08 · Kiểm chứng:** welcome HTML có encode tên/link tin cậy, Hangfire persistent queue/outbox, retry 1/5/30 phút/Failed state/log, concurrency/restart không tạo job trùng thông thường; SMTP fail giữ đăng ký. Dashboard chỉ Admin; ghi giới hạn duplicate email nếu crash sau SMTP accept, không hứa exactly-once.
- [ ] **D6-09 · Sửa/kiểm chứng:** sitemap hàng ngày 02:00 UTC/retry 2/singleton/public URLs + static/category, `lastmod`, `changefreq`, `priority` theo hợp đồng; lỗi upload giữ artifact cũ; robots khai báo URL sitemap. Rà quyết định ping Google ở D0, không thêm request lỗi hoặc tự bỏ yêu cầu mà không ghi sai khác.

## Trình tự thực hiện trong phase

1. Kiểm welcome HTML/link/encoding/Hangfire/outbox/retry/Failed/concurrency/SMTP failure.
2. Kiểm sitemap 02:00 UTC/retry2/public URLs/XML fields/robots và giữ artifact cũ khi upload lỗi.
3. Theo decision về Google ping và singleton lock; không yêu cầu kích hoạt email ngoài scope, không hứa SMTP exactly-once.

## Cách kiểm chứng

- MailHog/SMTP thử và Hangfire DB queue thật trong môi trường riêng.
- XML/Published URLs/failed upload/retry metadata/dashboard permissions.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Hai job đủ contract và failure evidence, lịch/múi giờ chính xác.
- [ ] Sai khác ping/lock có quyết định nguồn, không tự miễn yêu cầu.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-20-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-19-media-storage-and-jobs.md) · [Tiếp theo để tham khảo](phase-21-seo-metadata-and-slugs.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
