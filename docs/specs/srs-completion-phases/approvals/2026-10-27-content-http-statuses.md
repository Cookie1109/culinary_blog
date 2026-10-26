# Ưu tiên mã lỗi của FR nghiệp vụ

Ngày27/10/2026, Asia/Bangkok. Phạm vi: mã lỗi validation, xung đột phiên bản,
thiếu điều kiện publish trong G04 và phần dùng chung của validation pipeline.
PDF gốc và đính chính A/B/C không bị sửa.

## Nguồn và ủy quyền

PDF yêu cầu validation422 tại các FR tr.26/31/32/35–37; concurrency409 trong
FR cập nhật recipe; publish chưa đủ điều kiện422 trong FR publish. Phụ lục
A/B tr.67–68 lại mô tả validation400, concurrency422 và publish thiếu400.
Đây là hai mã khác nhau cho cùng tình huống, không thể cùng thực hiện.

Agent trình phương án422/409/422, giữ malformed request và thiếu If-Match400.
Người dùng trả lời nguyên văn cho câu hỏi
`call_8ace5aa1ed744a62958fdcb632c102b3`:

> Bạn nghĩ ưu tiên nào sẽ hợp logic với sản phẩm thực tế? tại sao? Nếu bạn thấy hợp logic, hãy triển khai

Agent chọn ưu tiên FR cụ thể: dữ liệu cần sửa422, trạng thái/version cần tải
lại409. Đây là thực thi lựa chọn người dùng giao, không tự nhận cả hai phần
mâu thuẫn của PDF đều đã được đáp ứng.

## Quy tắc thực hiện

- Validation dữ liệu/giá trị query hợp lệ về cấu trúc nhưng không hợp lệ về
  nghiệp vụ:422, giữ field errors và application error code.
- Không đủ ingredient/step để publish:422, không thay trạng thái/version.
- Xung đột optimistic version:409, giữ ETag/currentETag và hướng reload.
- JSON/request sai cấu trúc, If-Match thiếu hoặc sai định dạng:400.
- Quyền401/403, không tìm thấy404, unique/delete conflict409, lỗi media theo
  hợp đồng riêng và sort/sortBy mâu thuẫn400 đã chốt trước không tự thay đổi.
- Các AuthProblemException có mã lỗi cụ thể giữ mapping hiện có; chỉ shared
  FluentValidation áp dụng422 phù hợp các FR tương ứng, không đổi401/423.

Phải kiểm HTTP thực, lỗi không ghi dữ liệu và hợp đồng docs/types. Không chỉ
đổi assert để che sai hành vi; không sửa dữ liệu/schema production.

Tham khảo ý nghĩa mã lỗi:
[RFC9110 —422](https://www.rfc-editor.org/rfc/rfc9110.html#section-15.5.21),
[RFC9110 —409](https://www.rfc-editor.org/rfc/rfc9110.html#section-15.5.10).
Mã409 cho version là hợp đồng ứng dụng/FR đã chọn, không tuyên bố mọi lỗi
If-Match của HTTP nói chung đều phải dùng409.
