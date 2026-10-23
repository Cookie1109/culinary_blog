# Phase 27 — Rà UX, responsive và accessibility tự động

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 27 theo docs/specs/srs-completion-phases/phase-27-ux-responsive-accessibility.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D8-08`, `D8-09`.
- Phụ thuộc kỹ thuật: [Phase 17](phase-17-recipe-category-composition.md), [Phase 18](phase-18-search-filters-and-pagination.md), [Phase 19](phase-19-media-storage-and-jobs.md), [Phase 21](phase-21-seo-metadata-and-slugs.md), [Phase 26](phase-26-e2e-recipes-publish-search.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-USE-001`, `NFR-USE-002`, `NFR-USE-003`, `NFR-USE-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `frontend/src/components/`
- `frontend/src/features/culinary/`
- `frontend/src/app/`
- `frontend/tests/`
- `frontend/e2e/`

## Task gốc phải đóng

- [ ] **D8-08 · Kiểm chứng/sửa:** rà mọi form/async/write flow dùng RHF/Zod hoặc cách đã chốt, inline/server error actionable, error code i18n-ready, không lộ stack; skeleton/loading/empty, toast thành công/thất bại, optimistic rollback phù hợp và upload progress % thật.
- [ ] **D8-09 · Kiểm chứng:** responsive 320–767/768–1199/≥1200 trên public + auth + profile + dashboard; không tràn ngang, thao tác touch/keyboard, focus/escape/modal/menu, contrast text ≥4,5:1 và UI ≥3:1 theo WCAG AA. Lưu screenshot/axe/browser results, không chỉ regex class CSS.

## Trình tự thực hiện trong phase

1. Inventory form/async/write và chỉ sửa gap RHF/Zod/inline/server/i18n codes/loading/toast/rollback/progress.
2. Kiểm public/auth/profile/dashboard ở 320–767/768–1199/≥1200, touch/keyboard/focus/escape/modal/menu.
3. Đo axe/contrast/screenshots thật và giữ error/loading/empty flows.

## Cách kiểm chứng

- Interaction + browser checks, contrast text≥4.5:1/UI≥3:1; screenshot không tràn ngang.
- Không dùng source regex/JSDOM làm chứng nhận screen reader hoặc toàn bộ WCAG.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Gap UX/a11y tự động trong phạm vi đã sửa và có evidence mới.
- [ ] Manual browser/screen-reader matrix vẫn do phase28 đóng.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-27-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-26-e2e-recipes-publish-search.md) · [Tiếp theo để tham khảo](phase-28-browser-and-screen-reader.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
