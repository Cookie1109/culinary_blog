# SRS-2026-10-23-ABC-CORRECTION — Đính chính A/B/C theo PDF

- Ngày ghi nhận: **23/10/2026, Asia/Bangkok**; không tự gán timestamp hoặc chữ ký.
- Người chỉ dẫn: người dùng giao việc trong phiên chat hiện tại.
- Nguồn nguyên văn theo thứ tự: “File [SRS_Culinary_Blog_v1.0.0.pdf](../../SRS_Culinary_Blog_v1.0.0.pdf) là tiêu chuẩn rồi. sao lại có phương án khác nữa nhỉ?”; tiếp đó “Thế chỉnh lại các gói ABC đi”.
- Diễn giải phạm vi thực hiện: trả tiêu chuẩn nghiệm thu về PDF; thay hiệu lực11 ngoại lệ/clarification đóng gói trước đây; chỉ sửa hồ sơ A/B/C, không đổi runtime. Đây là diễn giải công việc của agent theo chỉ dẫn mới, không phải người dùng đã duyệt một protocol thay thế mới.
- Mốc trước sửa: HEAD `866394388c07371638364e5145c235bfcb0a2aca`; approval cũ được ghi tại commit `7cfd0754c6ff8537fef8ba8d891fbfd472d06313`.

| Gói | IDs được thay hiệu lực | Kết quả hiện tại |
|---|---|---|
| A | DEC-13, DEC-15, DEC-18, DEC-22, DEC-25 | Theo optional/query/message/URL/routes trong PDF; không giữ thêm null/merge/conflict400 hoặc alias307/308 như tiêu chuẩn đã duyệt; phần PDF chưa rõ để mở |
| B | DEC-02, DEC-07, DEC-20, DEC-21 | RedLock, Identity string, Instructions NOT NULL, enums smallint; ping giữ yêu cầu nhưng ghi provider đã ngừng, không thay bằng waiver |
| C | DEC-09, DEC-11 | Rút protocol30 ngày/80–20/5+15phút/error1%/efficiency90%; giữ ngưỡng PDF; mâu thuẫn uptime và protocol đo còn mở |

## Hiệu lực và giới hạn

- [Approval cũ](2026-10-23-abc.md) được giữ nguyên lịch sử và gắn nhãn hết hiệu lực; không xóa phản hồi “Duyệt gói A/B/C”. [Register](../decision-register.md) và [inventory](../acceptance-inventory.md) chứa expected hiện tại.
- Không có miễn trừ PDF còn hiệu lực từ A/B/C. Phần tương thích PDF được giữ bằng căn cứ PDF, không bằng việc duyệt gói cũ. Chưa rõ hoặc bất khả thi do provider phải ghi vướng mắc trước nghiệm thu, không tự chọn phương án dễ hơn.
- D0-02/D0-03 giữ đóng ở phạm vi rà/ghi nhận; **D0-04 mở lại**, tiến độ02=`Chờ quyết định`. Không suy approval thành runtime pass.
- Backup03:00UTC là quy ước baseline Test Strategy§4, không phải timezone được PDF chỉ rõ. Hồ sơ ngoài A/B/C giữ lịch sử, không được coi là chứng nhận mọi baseline khớp PDF.
- Chưa sửa code/API/schema/migration/deploy/provider; không cho phép tự chạy các phase thực thi. Report/tiến độ hiện tại dẫn về [report02](../reports/phase-02-result.md).
