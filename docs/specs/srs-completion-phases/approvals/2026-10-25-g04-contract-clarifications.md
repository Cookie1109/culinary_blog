# Làm rõ contract G04 theo ủy quyền của người dùng

- Ngày: 25/10/2026, Asia/Bangkok. Người thực hiện lựa chọn: Codex theo ủy quyền, không phải chữ ký nghiệm thu của người dùng.
- Nguồn ủy quyền nguyên văn: “Bạn hãy chốt giúp tôi theo nguyên tắt tối ưu và hiệu quả nhất. Và đảm bảo phù hợp với PDF”. Phản hồi này trả lời câu hỏi về PUT omit/null/nutrition, sort đồng thời và backfill Instructions.
- PDF vẫn là tiêu chuẩn. Giữ nguyên đính chính A/B/C ngày23/10; không phục hồi ngoại lệ cũ, không miễn Google ping/Rich Results/gates khác.

| Điểm | Quy tắc triển khai | Căn cứ và giới hạn |
|---|---|---|
| DEC-13, PUT | Field không xuất hiện giữ nguyên. Null bị từ chối ở title/name/description/categoryId/time/servings/difficulty/orderIndex/stepNumber bắt buộc; nullable quantity/unit/notes/timerMinutes/imageUrl được xóa bằng null. Empty object là no-op, vẫn kiểm quyền/If-Match. Nutrition cập nhật từng field; null cả object xóa khối; null từng chỉ số xóa chỉ số đó. | PDF tr.63/65 đánh các field PUT là optional; schema tr.55–58 xác định nullability. Giữ dữ liệu chưa được yêu cầu sửa, validation trước write và ETag hiện tại. Không suy optional thành phải gửi lại payload tạo mới. |
| DEC-15, sort | Giữ cả sort và sortBy/sortOrder. Chuẩn hóa newest/oldest/az/za thành -createdAt/createdAt/title/-title. Hai dạng cùng nghĩa được nhận; xung đột trả400 Problem Details, không chọn thứ tự query làm precedence. sortBy đứng riêng mặc định asc; relevance mặc định desc. Chỉ sortOrder thì field createdAt. Không truyền tham số: browse -createdAt, search relevance; pageSize12/max50. | PDF tr.37/62–63 liệt kê hai dạng nhưng không nói precedence. Quy tắc này bảo toàn cả hai dạng và ngăn kết quả ngầm khác ý định. Category detail giữ thứ tự cũ khi không yêu cầu sort; sortBy hiện có hiệu lực tại API. |
| DEC-21, Instructions | Lưu text NOT NULL. POST không có Instructions hoặc null và PUT Instructions:null biểu diễn “chưa có nội dung legacy” bằng chuỗi rỗng. Migration chuyển NULL cũ thành chuỗi rỗng, giữ nguyên nội dung đã có; không sinh text từ steps. Không giữ giới hạn10000 không có trong PDF. | PDF tr.29 cho request optional, tr.55 bắt buộc cột NOT NULL. Empty string biểu diễn thiếu nội dung mà không tạo dữ liệu thực phẩm giả hoặc ép wizard cung cấp legacy text. Down migration chặn nội dung mới vượt giới hạn của schema cũ. |

Các lựa chọn này không đổi ownership task. Evidence triển khai/kiểm tra ở [report G04](../reports/package-G04-result.md); D0-04 vẫn giữ mở cho các vấn đề khác ngoài ba quy tắc này.
