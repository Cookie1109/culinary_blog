# Hợp đồng validation nội dung —27/10/2026

Ưu tiên các FR nghiệp vụ cụ thể khi mã lỗi mâu thuẫn với phụ lục A/B,
theo [ủy quyền của người dùng](../specs/srs-completion-phases/approvals/2026-10-27-content-http-statuses.md).
PDF và đính chính A/B/C được giữ nguyên để truy vết.

| Trường hợp | HTTP | Cách xử lý của giao diện |
|---|---|---|
| JSON sai cú pháp/cấu trúc; thiếu hoặc sai định dạng If-Match |400| Sửa request |
| Dữ liệu/query sai quy tắc nghiệp vụ; CategoryId tham chiếu không tồn tại |422| Hiển thị field errors, sửa dữ liệu |
| Publish thiếu nguyên liệu hoặc bước |422| Bổ sung nội dung; trạng thái/version không đổi |
| Version cũ, cập nhật đồng thời |409| Dùng currentETag, tải lại dữ liệu rồi thao tác lại |

Giữ401/403/404, unique/delete conflict409 và các lỗi media theo hợp đồng riêng.
Xung đột sort/sortBy vẫn400 theo quyết định đã có. Các AuthProblemException có
mã riêng giữ mapping; shared FluentValidation trả422 theo FR tương ứng.

Tên danh mục nhập2–50 ký tự, không HTML; cột vẫn varchar(100).
Tên nguyên liệu nhập1–100 ký tự theo FR-RCP-009, trang34; cột vẫn varchar(200).
OpenAPI phân biệt giới hạn input và sức chứa response cũ. Không cắt hoặc sửa
dữ liệu cũ, không có migration mới. Khi sửa tên cũ quá dài phải nhập tên hợp lệ.

Kiểm chứng bằng API thật trên PostgreSQL Testcontainers riêng: POST/PUT biên
độ dài, không ghi dở dang hoặc tăng version khi từ chối, lỗi FK, JSON hỏng,
publish, quyền và stale version; không thử ghi vào Neon hoặc Render production.
