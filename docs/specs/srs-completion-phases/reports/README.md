# Mẫu report bàn giao phase

Chưa có report thực thi ở thời điểm chia phase. Khi làm phase NN, tạo `phase-NN-result.md` trong thư mục này và cập nhật progress; không để mẫu trở thành bằng chứng đã chạy.

```markdown
# Kết quả Phase NN — Tên phase

- Trạng thái: Đạt / Chưa đạt / Chờ môi trường / Chờ quyết định / Cần kiểm tra lại.
- Thời điểm và người/agent thực hiện:
- HEAD/worktree trước-sau; commit/image digest khi có thật:
- Runtime, config/profile/dataset; không chứa secrets:
- Report phase phụ thuộc đã đọc; phần dependency còn pending:

## Kết quả từng task được giao

| Task | Requirement/acceptance/trang nguồn | Kết quả | Evidence/điều kiện còn mở |
|---|---|---|---|
| Dn-mm | ... | ... | ... |

## Thay đổi và quyết định

- File/line và thay đổi có liên hệ task.
- API/data/migration ảnh hưởng, baseline/decision áp dụng.
- Phần hiện có đã đủ nên chỉ kiểm lại, không viết lại.

## Kiểm tra thực tế

| Cwd/runtime | Lệnh/test | Loại evidence | Expected / actual | Exit code / pass-fail-skip | Log/run |
|---|---|---|---|---|---|
| ... | ... | Unit/IT/JSDOM/HTTP/browser/provider/production | ... | ... | ... |

- Chưa chạy: ghi lý do. Tách lỗi môi trường với assertion lỗi.
- File logs lớn có thể ở tmp/artifacts được ignore; ghi summary trong report và nơi lưu bền vững khi nghiệm thu.
- Claim provider/browser/CI/image/measurement phải có kết quả tương ứng, không suy từ cấu hình.

## Cập nhật theo dõi

- Checkbox task gốc đã đóng: ...; checkbox vẫn mở và lý do: ...
- Dòng progress đã cập nhật: ...
- Gap mới/phase bị ảnh hưởng phải chạy lại: ...

## Bàn giao

- Phần code/test đã sẵn để phase sau dùng:
- Điều kiện còn chờ/quyết định cần chốt:
- Lệnh/bước tiếp theo cụ thể cho người nhận:
- Kết luận đúng phạm vi, không tự chứng nhận toàn bộ SRS.
```
