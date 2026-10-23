# Phase 38 — UAT, tổng kết và bàn giao nghiệm thu

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 38 theo docs/specs/srs-completion-phases/phase-38-uat-and-final-handoff.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D10-04`, `D10-05`, `D10-06`, `D10-07`.
- Phụ thuộc kỹ thuật: Review kết quả tất cả [phase 01–37](progress.md); nếu còn pending, chỉ bàn giao điều kiện mở, không nghiệm thu toàn bộ.
- Loại công việc: **UAT/người nghiệm thu**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-SEC-007`, `NFR-MAINT-001`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `docs/release/uat-signoff.md`
- `docs/release/go-no-go-and-rollback.md`
- `docs/release/environment-and-secrets-checklist.md`
- `docs/reports/srs-acceptance-status.md`
- `docs/specs/srs-completion-checklist.md`
- `docs/specs/srs-completion-phases/progress.md`

## Task gốc phải đóng

- [ ] **D10-04 · Môi trường/nghiệm thu:** UAT đủ CJ-01–05/UX trên staging đúng artifact, triage defects, có người nghiệm thu/giảng viên/QA thực hiện đúng vai trò; không tự ký thay, không đổi Pending thành Accept chỉ vì test tự động xanh.
- [ ] **D10-05 · Kiểm chứng:** nếu nằm trong phạm vi triển khai, hoàn tất checklist secrets/TLS/DNS/backup/alert/maintenance, go/no-go và rollback/recovery theo tài liệu hiện có. Kế hoạch này không tự cho phép gửi thông báo, publish workspace, deploy production hoặc thực hiện restore dữ liệu thật.
- [ ] **D10-06 · Kiểm chứng:** review diff theo từng nhóm, ghi commit mô tả rõ chức năng bằng tiếng Việt không dấu khi thực hiện commit theo yêu cầu người dùng. Không gộp các PDF người dùng đang chuyển thư mục hoặc thay đổi ngoài phạm vi; phân biệt việc lập kế hoạch hiện tại với lần commit/push triển khai sau.
- [ ] **D10-07 · Nghiệm thu:** chỉ kết luận “hoàn thành toàn bộ SRS” khi các acceptance bắt buộc đều đạt hoặc có điều chỉnh rõ ràng được chấp thuận, không còn pending không giải thích. Nếu mới đạt local/chức năng cốt lõi, kết luận đúng mức đó và liệt kê điều kiện còn mở; không đưa phần trăm tự suy đoán.

## Trình tự thực hiện trong phase

1. Tập hợp CJ01–05/UX trên đúng artifact, defects/deviations và điều kiện môi trường còn mở.
2. Chuẩn bị/hỗ trợ UAT và release/rollback review; người có thẩm quyền xác nhận, không tự ký thay.
3. Review diff và final gates; ghi kết luận đúng mức. Chỉ commit/push/deploy/gửi thông báo khi có yêu cầu/ủy quyền áp dụng cho hành động đó.

## Cách kiểm chứng

- Đối chiếu tất cả phase/mọi acceptance, không chỉ tổng test hoặc số checkbox.
- Bằng chứng thật cho chữ ký/CI/image/provider/môi trường; mục pending giữ mở và nêu bước tiếp.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Handoff đủ nguồn, known gaps và kết luận phạm vi đạt chính xác.
- [ ] Chỉ kết luận toàn bộ SRS khi mọi điều kiện bắt buộc đạt/được điều chỉnh hợp lệ; không tự suy chữ ký.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-38-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-37-docs-artifacts-and-traceability.md) · [Tiếp theo để tham khảo](README.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
