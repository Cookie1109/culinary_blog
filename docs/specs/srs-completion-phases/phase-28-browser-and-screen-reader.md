# Phase 28 — Kiểm browser matrix và screen reader thật

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 28 theo docs/specs/srs-completion-phases/phase-28-browser-and-screen-reader.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D8-10`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 27](phase-27-ux-responsive-accessibility.md)
- Loại công việc: **Thiết bị/browser/screen reader thật**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-USE-001`, `NFR-USE-002`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `docs/reports/`
- `frontend/e2e/`
- `docs/specs/phase-0/00_SRS_Conformance_and_Resolution.md`

## Task gốc phải đóng

- [ ] **D8-10 · Môi trường/kiểm chứng:** chạy browser matrix minimum Chrome/Firefox/Edge/Safari theo GAP-024, iOS/Android theo quyết định đã chốt; ghi phiên bản và giới hạn kiểm tra. Kiểm NVDA trên Windows và VoiceOver trên macOS/iOS, lưu người thực hiện/kịch bản/kết quả; không coi JSDOM là screen reader thật.

## Trình tự thực hiện trong phase

1. Lập browser/device/version matrix theo GAP-024 và acceptance inventory.
2. Chạy kịch bản keyboard/NVDA Windows/VoiceOver macOS-iOS trên thiết bị hoặc môi trường thực có.
3. Ghi phần không có thiết bị/credential là Chờ môi trường với hướng dẫn tái chạy, không tạo bảng PASS giả.

## Cách kiểm chứng

- Ghi người thực hiện, version, route, action, expected/actual và evidence cho từng dòng.
- Recheck lỗi UI phát hiện rồi chỉ đóng dòng tương ứng sau kết quả đạt.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Browser/screen reader thật có kết quả hoặc pending rõ từng môi trường.
- [ ] Thiếu thiết bị không chặn phase29 nếu code/harness đã sẵn.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-28-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-27-ux-responsive-accessibility.md) · [Tiếp theo để tham khảo](phase-29-ci-and-static-analysis.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
