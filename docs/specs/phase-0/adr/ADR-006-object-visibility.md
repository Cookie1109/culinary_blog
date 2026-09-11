# ADR-006: Object storage visibility

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Operations
- Liên quan: GAP-022

## Bối cảnh

Bucket public-read làm ảnh Draft/Archived có thể truy cập bằng URL dù API đã bảo vệ recipe.

## Quyết định

- MinIO bucket mặc định private.
- DB lưu object key, không xem public URL bất biến là nguồn dữ liệu chính.
- Draft/Archived media chỉ trả qua short-lived presigned URL hoặc authenticated media proxy sau ownership check.
- Published media được phục vụ qua controlled public proxy/CDN namespace; unpublish/archive làm ngừng cấp public access và purge/invalidate CDN.
- Upload dùng GUID key dưới namespace recipe; không dùng filename client.
- Original có thể private lâu dài; medium/thumbnail published được tối ưu cho delivery.

## Hệ quả

- Tăng complexity của media delivery và cache invalidation.
- Bảo vệ đúng private content; dễ thay MinIO bằng S3/CDN.

## Tiêu chí xác minh

- Object key/URL Draft không truy cập anonymous.
- Unpublish/archive chặn public media theo thời hạn cache policy.
- Presigned URL ngắn hạn và không cho list bucket.
