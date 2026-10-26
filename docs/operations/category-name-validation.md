# Giới hạn tên danh mục

Cập nhật27/10/2026 theo yêu cầu sửa validation cho đúng PDF SRS.

- FR-CAT-001, trang25: tên danh mục từ2–50 ký tự, không chứa HTML.
- Schema Category, trang58: cột Name là varchar(100), NOT NULL, UNIQUE.

Hai quy định áp dụng cho hai lớp khác nhau. Tạo mới và cập nhật phải kiểm
giới hạn2–50 ở giao diện, Application và Domain. Cột database vẫn giữ100,
không cần migration. Việc dùng sức chứa100 của cột để cho phép nhập100 ký tự
trước đây là diễn giải sai quy tắc nghiệp vụ.

Giao diện giới hạn ô nhập50, kiểm tên đã trim trước khi gửi và báo lỗi HTML.
API kiểm chiều dài và HTML; Domain cũng bảo vệ invariant khi gọi trực tiếp.
OpenAPI CategoryWrite.name giới hạn50; Category.name trong response vẫn100
để mô tả được dữ liệu cũ. Không tự cắt, đổi tên hoặc xóa danh mục hiện có.
Khi chỉnh sửa một tên cũ dài hơn50, người dùng cần đổi về tên hợp lệ.

Quy tắc này thay cách hiểu category input trong GAP-019; phần cookTime và
các quyết định khác của GAP-019 không thay đổi. PDF và đính chính A/B/C giữ
nguyên. Mã lỗi validation là422 theo [quy tắc HTTP đã chốt](content-validation-contract.md);
JSON hỏng và thiếu/sai If-Match vẫn400.

Kiểm chứng gồm mốc1/2/50/51/100, HTML, cả POST/PUT, rollback khi từ chối và
metadata model xác nhận cột vẫn100. Các phép ghi thử chỉ chạy trên fixture
Testcontainers riêng; không dùng database Neon để thử tạo hoặc sửa danh mục.
