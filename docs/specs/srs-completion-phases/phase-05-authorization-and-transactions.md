# Phase 05 — Kiểm quyền và tính nguyên tử của command

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 05 theo docs/specs/srs-completion-phases/phase-05-authorization-and-transactions.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D2-03`, `D2-04`.
- Phụ thuộc kỹ thuật: [Phase 04](phase-04-cqrs-and-validation.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `FR-CAT-003`, `FR-CAT-004`, `FR-RCP-003`, `FR-RCP-007`, `NFR-SEC-006`, `NFR-MAINT-004`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Application/Content/`
- `backend/src/CulinaryBlog.Application/Features/Recipes/`
- `backend/src/CulinaryBlog.Infrastructure/Content/ContentService.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeCoreApiTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/RecipeCompositionApiTests.cs`

## Task gốc phải đóng

- [ ] **D2-03 · Sửa/kiểm chứng:** kiểm tra policy/quyền sở hữu tại Application, không chỉ route. Bao phủ Guest, Author-owner, Author khác, Admin và tài khoản inactive; sensitive mutation phải kiểm tra trước commit, không nhận userId/isAdmin do client tự khai.
- [ ] **D2-04 · Kiểm chứng:** rà Repository/UoW/transaction cho command cần thiết; create recipe đã có UoW phải được giữ. Chứng minh atomicity của composition, renumber, primary image, version và outbox; chỉ bổ sung abstraction/transaction ở nơi thiếu.

## Trình tự thực hiện trong phase

1. Rà policy/ownership trong Application cho Guest/owner/other Author/Admin/inactive; không tin userId/isAdmin từ client.
2. Kiểm UoW/transaction/version/outbox cho create, composition, renumber và primary image; giữ UoW create hiện có.
3. Chỉ sửa lỗ hổng quyền/atomicity được tái hiện bằng test; không tạo repository cho đủ tên pattern.

## Cách kiểm chứng

- Role matrix và fault giữa transaction; dữ liệu/version/outbox không được cập nhật một phần.
- ETag race và owner/Admin regression trong integration.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Quyền kiểm trước commit, không bypass route để bỏ qua Application.
- [ ] Mutation nhiều bước atomic và không hồi quy create/UoW có sẵn.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-05-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-04-cqrs-and-validation.md) · [Tiếp theo để tham khảo](phase-06-pipeline-and-logging.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
