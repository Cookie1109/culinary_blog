# ADR-001: Soft delete và retention

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Product Owner, Technical Lead
- Liên quan: GAP-008, GAP-009

## Bối cảnh

SRS mô tả Recipe/Category lúc hard delete, lúc soft delete; BaseEntity và NFR durability lại yêu cầu soft delete. Xóa ảnh ngay cũng làm mất khả năng restore.

## Quyết định

- Recipe và Category dùng `IsDeleted`, `DeletedAt`, `DeletedBy`; EF global query filter loại record đã xóa.
- V1 không hard-purge Recipe/Category và không suy diễn retention 30 ngày từ chính sách backup của SRS.
- Category chỉ được soft-delete khi không còn Recipe chưa bị xóa tham chiếu.
- Soft-delete Recipe giữ child metadata và object; xóa image riêng lẻ vẫn xóa metadata và enqueue object delete theo FR-RCP-008.
- Query restore/admin phải opt-in rõ ràng, không vô hiệu global filter trên public handler.
- Unique slug/name giữ unique toàn bảng; không tái sử dụng URL của record đã soft-delete.

## Hệ quả

- Tăng storage; hard purge/restore cần Change Request riêng nếu bổ sung sau v1.
- Bảo toàn durability, audit và tránh mất dữ liệu ngoài ý muốn.
- Public/read queries đơn giản hơn nhưng integration tests phải kiểm tra query filter.

## Phương án không chọn

- Hard delete tức thời: đơn giản nhưng mâu thuẫn NFR và không restore.
- Archive thay soft delete: Archive là trạng thái nghiệp vụ, không thay thế delete intent/audit.

## Tiêu chí xác minh

- DELETE trả 204 nhưng public/private normal queries không còn thấy record.
- Child metadata/object của Recipe soft-deleted không xuất hiện qua public/private query thông thường.
- Object-delete của image riêng lẻ idempotent; chạy lại không lỗi.
