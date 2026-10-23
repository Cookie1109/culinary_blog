# Phase 01 — Lập inventory acceptance và contract

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

Kết quả ngày23/10/2026: **Đạt trong phạm vi tài liệu D0-01/D0-05**. Xem [inventory](acceptance-inventory.md) và [report kiểm chứng](reports/phase-01-result.md); acceptance runtime và quyết định của các phase sau vẫn mở.

## Giao việc bằng prompt này

```text
Thực hiện Phase 01 theo docs/specs/srs-completion-phases/phase-01-acceptance-inventory.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D0-01`, `D0-05`.
- Phụ thuộc kỹ thuật: Không có; bắt đầu từ đây.
- Loại công việc: **Tài liệu**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: Phần bổ sung/hợp đồng liên quan trong checklist gốc.. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `docs/specs/SRS_Culinary_Blog_v1.0.0.pdf`
- `docs/specs/phase-0/06_Traceability_Matrix.md`
- `docs/reports/srs-acceptance-status.md`

## Task gốc phải đóng

- [x] **D0-01 · Kiểm chứng:** tách từng acceptance của 34 FR/30 NFR/10 CONS thành điều kiện kiểm tra được; ghi trang nguồn, mức Must/Should, API/job/screen, cách kiểm tra, hiện trạng và bằng chứng. Rà cả luồng thay thế/lỗi, không chỉ tiêu đề FR.
- [x] **D0-05 · Kiểm chứng:** lập danh sách contract UI/API/data Chương 5–8/phụ lục: routes/redirects, status/header, request/response/nullable/enum, giới hạn field, relationships/index/concurrency/audit/soft delete. Mỗi sai khác mới phát hiện được thêm vào đợt tương ứng, không coi báo cáo hiện tại là audit hết mọi chi tiết nhỏ.

## Trình tự thực hiện trong phase

1. Đọc các phần FR/NFR/CONS và UI/data/API của SRS, tái dùng ma trận hiện có.
2. Tách acceptance thành từng dòng có mã, trang, mức ưu tiên, API/job/screen, expected và loại kiểm tra; đánh dấu chưa đối chiếu thay vì suy pass.
3. Tạo acceptance-inventory.md cạnh file phase; ghi các sai khác mới phát hiện để phase phụ trách tiếp nhận.

## Cách kiểm chứng

- So số mã duy nhất với 34 FR, 30 NFR, 10 CONS; kiểm acceptance không chỉ là bản chép tiêu đề.
- Review đủ UI/architecture/data/API/phụ lục; các dòng chưa rõ có decision reference.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [x] Có inventory có thể dùng để viết test, không thiếu mã hoặc chương hợp đồng.
- [x] Chưa thay code/runtime hoặc tự phê duyệt sai khác.
- [x] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [x] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-01-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](README.md) · [Tiếp theo để tham khảo](phase-02-decisions-and-conflicts.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
