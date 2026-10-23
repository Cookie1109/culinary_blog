# Phase 36 — Theo dõi SLA, alert và maintenance

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 36 theo docs/specs/srs-completion-phases/phase-36-sla-alerts-and-maintenance.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-08`.
- Phụ thuộc kỹ thuật: [Phase 02](phase-02-decisions-and-conflicts.md), [Phase 33](phase-33-observability-and-resilience.md), [Phase 34](phase-34-backup-and-restore.md)
- Loại công việc: **Thời gian quan sát/receiver được phép**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-REL-001`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `infra/observability/`
- `infra/operations/compose.alert-routing.yaml`
- `docs/reports/`
- `docs/release/`

## Task gốc phải đóng

- [ ] **D9-08 · Môi trường/kiểm chứng:** probe readiness mỗi 10s, alert down >1 phút tới receiver thử được chỉ định và maintenance công bố ≥48h/banner đúng; đo uptime ≥99,5% theo cửa sổ D0. Thời gian chờ quan sát không được tự coi là đã đạt SLA.

## Trình tự thực hiện trong phase

1. Probe ready10s, alert down>1 phút, maintenance banner công bố≥48h theo quyết định.
2. Dùng receiver thử đã được chỉ định/ủy quyền; không gửi thông báo tới người khác chỉ để test.
3. Thiết lập cửa sổ đo uptime99.5%, lưu evidence quan sát và handoff nếu chưa đủ thời gian; không chờ SLA dài trong một turn.

## Cách kiểm chứng

- Probe cadence/alert transition/firing-resolved trong hệ thử riêng.
- Tính SLA theo interval đã chốt và dữ liệu thật, không đánh đạt vì đã có rule.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có vận hành quan sát/alert/banners thực, giới hạn thời gian/môi trường rõ.
- [ ] Nếu interval chưa đủ: Chờ quan sát, không kết luận SLA pass.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-36-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-35-multi-instance-and-production-scaling.md) · [Tiếp theo để tham khảo](phase-37-docs-artifacts-and-traceability.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
