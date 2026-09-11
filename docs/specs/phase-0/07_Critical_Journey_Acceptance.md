# Critical Journey Acceptance Criteria v1

> Trạng thái: Accepted for implementation  
> Tất cả API error assertions dùng `04_Error_Catalog.md` và `openapi.v1.yaml`.

## CJ-01 - Register, auto-login và profile

Given một email chưa tồn tại và password đạt policy, when Guest đăng ký với `displayName`, `email`, `password`, then API trả 201 cùng cặp token và Author profile; DB chỉ lưu password hash và refresh-token hash; welcome email được ghi outbox/enqueue sau commit.

- Email trùng trả 409 `AUTH_EMAIL_EXISTS`.
- Field/password không hợp lệ trả 400 `VALIDATION_ERROR`.
- Welcome email thất bại không rollback user và retry tại 1, 5, 30 phút.
- Raw password/JWT/refresh token không xuất hiện trong log.

## CJ-02 - Login, refresh rotation và logout

Given user active, when login đúng then nhận JWT 15 phút và refresh token 512-bit TTL 7 ngày. When refresh then token cũ bị revoke và token mới được tạo trong cùng transaction.

- Credentials sai luôn trả 401 generic; sau 5 lần sai lockout 15 phút và trả 423.
- Dùng lại token đã revoke trả 401 và revoke toàn bộ family.
- Hai refresh đồng thời không tạo hai successor hợp lệ.
- Logout nhận refresh token trong body, idempotent 204 và vẫn hoạt động khi access token hết hạn.

## CJ-03 - Author tạo Draft hoàn chỉnh

Given Author đã đăng nhập, when tạo Recipe thì trạng thái luôn Draft và AuthorId là current user. Author có thể thêm ingredient, step, nutrition và ảnh qua wizard.

- Author khác không đọc/sửa child/private detail; Admin được bypass ownership.
- Ingredient cho phép quantity/unit null cho “vừa đủ”.
- StepNumber liên tục từ 1 sau add/update/delete.
- Upload chỉ JPEG/PNG/WebP/AVIF <=5MB và phải qua signature/decode validation.
- Mutation với ETag cũ trả 409; thiếu `If-Match` trả 400.

## CJ-04 - Publish và discovery

Given Draft có category hợp lệ, ít nhất 1 ingredient và 1 step, when owner publish then status thành Published, `PublishedAt` được set lần đầu, slug bị khóa và Recipe xuất hiện trong public list/detail/category/search/sitemap.

- Thiếu ingredient hoặc step trả 400 `RECIPE_PUBLISH_INCOMPLETE`.
- Ảnh không bắt buộc; route dùng default asset nếu không có ảnh.
- Search “pho” tìm được “phở”; chỉ trả Published.
- Unpublish/archive biến mất khỏi public API/cache/ISR/search/sitemap trong giới hạn invalidation policy.
- Không xóa ingredient/step cuối của Published Recipe; trả 409 `RECIPE_STATE_CONFLICT`.

## CJ-05 - Ownership và Admin

Given Author A sở hữu Recipe, when Author B gọi private read/write/delete thì không nhận dữ liệu và không mutation được; authenticated resource endpoint trả 403, public slug không Published trả 404. Admin được thao tác Recipe bất kỳ và quản lý Category.

- Direct URL và API call không bypass UI guard.
- Category còn Recipe chưa bị xóa trả 409 khi delete.
- Hangfire dashboard chỉ Admin và network policy phù hợp.
- Public/shared cache không bao giờ chứa Draft/Archived/deleted.

## Completion rule

Mỗi journey phải chạy trên staging với PostgreSQL, Redis, MinIO và Hangfire thật; không dùng in-memory database. Release bị chặn nếu một journey không pass hoặc có Critical/High security defect chưa được chấp nhận chính thức.
