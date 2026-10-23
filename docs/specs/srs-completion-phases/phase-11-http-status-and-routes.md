# Phase 11 — Sửa HTTP 404 và bảo vệ routes

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 11 theo docs/specs/srs-completion-phases/phase-11-http-status-and-routes.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D4-01`, `D4-03`, `D4-04`.
- Phụ thuộc kỹ thuật: [Phase 03](phase-03-environment-and-baseline.md), [Phase 08](phase-08-google-session-lifecycle.md), [Phase 09](phase-09-logout-token-owner.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-002`, `FR-RCP-001`, `FR-RCP-002`, `NFR-PERF-005`, `NFR-SEO-002`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/app/(public)/categories/[slug]/page.tsx`
- `frontend/src/app/(public)/recipes/[slug]/page.tsx`
- `frontend/src/middleware.ts`
- `frontend/next.config.mjs`
- `frontend/src/lib/api/public-content-server.ts`

## Task gốc phải đóng

- [ ] **D4-01 · Sửa:** thêm kiểm tra tái hiện rồi sửa category không tồn tại trả HTTP 200. Kiểm tra cả recipe không tồn tại, unknown URL, URL thừa segment và đường nội bộ; phân biệt trang nhìn như 404 với HTTP 404 thực sự, không dùng assertion source thay phép đo HTTP.
- [ ] **D4-03 · Kiểm chứng:** list/search SSR theo query, các private page/media no-store và có auth. Public rewrite không gây redirect login, không làm lộ đường nội bộ hoặc cache nội dung người dùng; direct private URL vẫn bị chặn.
- [ ] **D4-04 · Kiểm chứng:** mapping `/search`, `/auth/login`, `/auth/register`, canonical và status redirect đúng quyết định D0; đăng nhập có redirect khi phù hợp, dashboard/new/edit/profile đúng quyền và loại rendering. Giữ home featured recipes + categories đã có.

## Trình tự thực hiện trong phase

1. Tái hiện missing category HTTP 200 bằng production HTTP test trước khi sửa.
2. Sửa soft 404, unknown/extra/internal routes và middleware public rewrite/private auth.
3. Kiểm SSR list/search, no-store private/media và alias redirects/canonical/status theo D0.

## Cách kiểm chứng

- HTTP actual status/redirect manual cho missing resources/internal routes và anonymous private routes.
- Build/lint/typecheck và regression auth/public route; không chỉ nhìn nội dung trang 404.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Missing resource đúng 404 và direct private URL không bypass auth.
- [ ] Redirect/rewrite không làm public URL nhảy login hoặc lộ nội dung private.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-11-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-10-google-provider-verification.md) · [Tiếp theo để tham khảo](phase-12-isr-ttl-production.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
