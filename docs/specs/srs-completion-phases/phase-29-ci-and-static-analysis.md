# Phase 29 — Hoàn thiện CI, analyzers và các quality gate

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 29 theo docs/specs/srs-completion-phases/phase-29-ci-and-static-analysis.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D7-04`, `D7-05`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 23](phase-23-unit-coverage-gate.md), [Phase 24](phase-24-integration-matrix-and-playwright.md), [Phase 25](phase-25-e2e-registration-and-login.md), [Phase 26](phase-26-e2e-recipes-publish-search.md), [Phase 27](phase-27-ux-responsive-accessibility.md)
- Loại công việc: **Mã/test local**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-MAINT-001`, `NFR-MAINT-002`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `.github/workflows/ci.yml`
- `backend/Directory.Build.props`
- `backend/Directory.Packages.props`
- `frontend/package.json`
- `frontend/eslint.config.mjs`
- `scripts/`

## Task gốc phải đóng

- [ ] **D7-04 · Sửa/kiểm chứng:** CI chạy locked restore, backend format/build/test/architecture/coverage, frontend API generation/format/lint/typecheck/build/tests, integration và browser E2E theo thứ tự có artifact/log khi lỗi. Không bỏ test để làm pipeline xanh.
- [ ] **D7-05 · Quyết định/sửa:** đóng D0 về SonarAnalyzer/StyleCop/Airbnb/Jest/Testing Library; bổ sung công cụ/ruleset đúng yêu cầu hoặc ghi phương án thay thế được chấp thuận và scope kiểm tra tương đương. Rà compiler/analyzer warnings và cảnh báo Next ESLint, không tự tắt cảnh báo nghiệp vụ để đạt số 0.

## Trình tự thực hiện trong phase

1. Theo decisions về SonarAnalyzer/StyleCop/Airbnb/Jest/Testing Library và Next ESLint, không tự đổi toolchain lớn.
2. Nối restore/format/build/tests/architecture/coverage/API generation/E2E, reports khi fail.
3. Chạy tương đương CI local và pipeline thật nếu môi trường/ủy quyền có; phân biệt đã chạy local với GitHub Actions.

## Cách kiểm chứng

- Gate fail đúng với report/test lỗi kiểm soát, cấu hình production/runtime không bị nới.
- Không bỏ test, tắt analyzer nghiệp vụ hoặc tăng timeout vô căn cứ để xanh.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Workflow dùng tiêu chí đúng và có lệnh local tái lập/negative gate test.
- [ ] Không ghi CI remote xanh nếu chưa có run thực.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-29-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-28-browser-and-screen-reader.md) · [Tiếp theo để tham khảo](phase-30-load-and-database-plans.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
