# Phase 22 — Đồng bộ schema, migration và OpenAPI/types

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 22 theo docs/specs/srs-completion-phases/phase-22-schema-migrations-openapi.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D6-13`, `D6-14`, `D6-15`.
- Phụ thuộc kỹ thuật: [Phase 16](phase-16-rate-limit-and-headers.md), [Phase 18](phase-18-search-filters-and-pagination.md), [Phase 19](phase-19-media-storage-and-jobs.md), [Phase 20](phase-20-welcome-and-sitemap-jobs.md), [Phase 21](phase-21-seo-metadata-and-slugs.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-REL-003`, `NFR-MAINT-003`, `CONS-005`, `CONS-006`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `backend/src/CulinaryBlog.Infrastructure/Persistence/`
- `backend/src/CulinaryBlog.Api/Presentation/`
- `docs/specs/phase-0/openapi.v1.yaml`
- `frontend/src/lib/api/schema.d.ts`
- `backend/tests/CulinaryBlog.IntegrationTests/MigrationUpgradeTests.cs`
- `backend/tests/CulinaryBlog.IntegrationTests/ApiDocumentationTests.cs`

## Task gốc phải đóng

- [ ] **D6-13 · Kiểm chứng/sửa:** đối chiếu tất cả schema/data type/length/nullability/default/constraint/index/relationship/audit/soft-delete/Version và API method/path/status/error/envelope/header với inventory D0. Bao gồm refresh-token family/hash, media/outbox và tables mới; không chỉ kiểm OpenAPI có endpoint.
- [ ] **D6-14 · Kiểm chứng:** migration upgrade từ bản trước giữ user/recipe/composition/token/outbox/media, migration mới nullable/backfill đúng; restart/seed idempotent không ghi đè dữ liệu người dùng; kiểm tra dữ liệu seed theo SRS và ghi rõ sai khác dùng seed thủ công thay Bogus nếu còn. Test trên DB/container riêng, chưa tự áp dụng vào DB người dùng.
- [ ] **D6-15 · Sửa/kiểm chứng:** cập nhật OpenAPI/XML comments/Scalar và generate frontend types; kiểm diff để phát hiện contract drift. Bảo đảm docs auth/multipart/ETag/headers/error response khớp hành vi thật và môi trường được mở docs đúng quy định.

## Trình tự thực hiện trong phase

1. So từng field/length/null/default/enum/FK/index/Version/audit/outbox/token với inventory, không chỉ endpoint names.
2. Test upgrade từ release trước giữ user/recipe/token/media/outbox và seed/restart không ghi đè user edits.
3. Update XML/Scalar/OpenAPI rồi generate client types; review diff và contract tests.

## Cách kiểm chứng

- Migration upgrade trên DB thử riêng, nullable/backfill/retained data assertions.
- Generate API/types, TypeScript/build, API documentation status/security/multipart/ETag/header tests.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Không còn contract drift được phát hiện chưa giải thích; schema/data/API có evidence.
- [ ] Không apply migration vào database người dùng để hoàn thành phase.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-22-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-21-seo-metadata-and-slugs.md) · [Tiếp theo để tham khảo](phase-23-unit-coverage-gate.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
