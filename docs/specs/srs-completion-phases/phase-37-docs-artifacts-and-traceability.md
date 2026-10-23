# Phase 37 — Chốt docs, artifact và traceability cuối

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 37 theo docs/specs/srs-completion-phases/phase-37-docs-artifacts-and-traceability.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D10-01`, `D10-02`, `D10-03`.
- Phụ thuộc kỹ thuật: [Phase 22](phase-22-schema-migrations-openapi.md), [Phase 23](phase-23-unit-coverage-gate.md), [Phase 26](phase-26-e2e-recipes-publish-search.md), [Phase 29](phase-29-ci-and-static-analysis.md)
- Loại công việc: **Tài liệu/artifact**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-MAINT-003`, `CONS-003`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `README.md`
- `CHANGELOG.md`
- `docs/specs/phase-0/`
- `docs/specs/phase-8/README.md`
- `docs/reports/srs-acceptance-status.md`
- `docs/release/`
- `.github/workflows/release-images.yml`

## Task gốc phải đóng

- [ ] **D10-01 · Kiểm chứng/sửa:** fresh setup theo README bằng Docker Compose <5 phút trong điều kiện package/image đã được nêu; kiểm env sample/ports/migrations/seed/Scalar. Cập nhật ADR, CHANGELOG/SemVer, API docs, test commands và known deviations đúng bản cuối.
- [ ] **D10-02 · Kiểm chứng:** feature/code freeze, commit SHA và API/Web image digest xác định; build/release manifest/SBOM/provenance và `/api/v1/release` khớp khi dùng release pipeline hiện có. Chạy lại regression/gates bị ảnh hưởng trên cùng artifact, không suy kết quả worktree sang GitHub/container cũ.
- [ ] **D10-03 · Kiểm chứng:** điền evidence cho đủ bảng 74 mã bên dưới và acceptance chi tiết D0, gồm ID test/case, pass/fail/pending, run URL/file, thời điểm, runtime/dataset/môi trường. Cập nhật báo cáo trạng thái và tài liệu Phase 8 cũ để không còn câu pass/deferred/deviation mâu thuẫn với bản mới.

## Trình tự thực hiện trong phase

1. Fresh setup<5 phút với điều kiện image/package nêu rõ, docs commands/env/Scalar/ADR/changelog khớp.
2. Gắn commit/worktree/build/API-Web digests và release identity khi đã có artifact; không fake digest/CI run.
3. Tổng hợp kết quả tất cả phase, đủ74 mã và acceptance; phân biệt phase code đạt với phần external pending, sửa mô tả lịch sử gây hiểu nhầm.

## Cách kiểm chứng

- Fresh setup test, docs local links, trace requirement→code→test→artifact; reports có timestamp và scope.
- Nếu đổi source sau benchmark phải revalidate các phase bị ảnh hưởng, không kéo report cũ.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có gói traceability/documentation và identity thực hoặc pending chính xác.
- [ ] Có thể tổng hợp trước khi external xong; không đánh toàn bộ acceptance đã đạt khi còn phase chờ.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-37-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-36-sla-alerts-and-maintenance.md) · [Tiếp theo để tham khảo](phase-38-uat-and-final-handoff.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
