# Mẫu report gói và phase

Mặc định giao theo [7 gói](../work-packages.md): tạo/cập nhật **một** `package-GNN-result.md` trong thư mục này. Report chung có mục riêng cho mỗi phase đã xử lý và mapping task→evidence; các dòng phase trong progress dẫn tới report/mục đó. Giữ report cũ để truy vết, không sao chép log hoặc tạo report phase trùng nội dung. Chỉ tạo report khi thực sự bắt đầu gói; mẫu dưới không phải bằng chứng đã chạy.

## Mẫu report gói

```markdown
# Kết quả gói GNN — Tên gói

- Trạng thái tổng: Đang làm / Đạt / Chưa đạt / Chờ môi trường / Chờ quyết định / Cần kiểm tra lại.
- Phạm vi phase/task đã xử lý; phần chưa bắt đầu:
- Thời điểm/agent; HEAD + worktree fingerprint trước/sau; bảo toàn thay đổi có sẵn:
- Runtime/config/dataset/artifact; dependency đã đủ và còn pending:
- Evidence cũ tái dùng và lý do còn hợp lệ; phần đã kiểm lại do source đổi:

## Kết quả theo phase/task

Tạo mục `## Phase NN` cho mỗi phase đã xử lý; progress có thể trỏ vào mục đó.
Phase chưa xử lý giữ nguyên trạng thái, không đánh Chưa đạt chỉ vì chưa đến lượt.

| Phase | Task | Acceptance/trang PDF | Kết quả | Evidence ID / phần còn mở |
|---|---|---|---|---|
| NN | Dn-mm | ... | Pass/Fail/Pending | E01 / ... |

## Thay đổi

- File và thay đổi gắn với task; phần hiện có chỉ cần kiểm, không viết lại:
- Contract/schema/migration ảnh hưởng và điểm cần quyết định; không tự thay PDF:

## Evidence dùng chung

| ID | Cwd/runtime/artifact/config | Lệnh/test thực tế | Loại evidence | Expected/actual/count/exit | Task được chứng minh / log |
|---|---|---|---|---|---|
| E01 | ... | ... | Unit/IT/HTTP/browser/provider/production | ... | ... |

Một run chỉ dùng cho nhiều task khi cùng artifact/config và đủ assertions tương ứng.
Ghi chưa chạy/skip, phân biệt lỗi môi trường với assertion fail. Source đổi sau run
thì đánh dấu evidence bị ảnh hưởng và kiểm lại, không tái dùng để đóng task.

## Điểm chặn và cập nhật tiến độ

- Phase/task chờ; điều kiện cụ thể; ảnh hưởng dependency; phần độc lập đã tiếp tục:
- Task gốc đóng/mở và lý do; dòng phase/gói trong progress đã cập nhật:
- Phase/gói khác bị ảnh hưởng cần kiểm lại; không tự thực thi ngoài phạm vi:

## Checkpoint tiếp tục

- Đã xong và evidence còn phù hợp:
- Đang làm; file/diff liên quan; lỗi mới nhất và trạng thái tài nguyên thử:
- Bước tiếp theo cụ thể, cwd/lệnh cần chạy (chưa chạy thì ghi rõ):
- Điều kiện cần người dùng/môi trường; không có thì phần nào vẫn tiếp tục được:
- Kết luận đúng phạm vi; còn pending bắt buộc thì chưa ghi toàn gói/SRS Đạt.
```

## Khi chỉ giao riêng một phase

Dùng `phase-NN-result.md` và mẫu bên dưới. Report phase01/02 đã có giữ làm lịch sử; khi làm gói chỉ dẫn chiếu phần cần dùng, không lập lại báo cáo cũ.

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
