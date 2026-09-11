# ADR-004: Refresh token rotation và browser storage

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Frontend Lead
- Liên quan: GAP-006, GAP-007

## Bối cảnh

SRS yêu cầu rotation/reuse detection nhưng chưa có family identifier và chưa nhất quán cách lưu/gửi refresh token.

## Quyết định

- Access token JWT TTL 15 phút, giữ trong memory của browser client.
- Refresh token là random 512-bit, TTL 7 ngày, gửi trong request body đúng contract Chương 5/8 của SRS; browser giữ trong `sessionStorage`, không dùng `localStorage` để tránh tồn tại lâu ngoài phiên tab.
- DB chỉ lưu SHA-256 `TokenHash`, `FamilyId`, `ExpiresAt`, `RevokedAt`, `ReplacedByTokenHash`, IP/user-agent audit tối thiểu.
- Refresh rotation revoke token cũ và tạo token mới trong một transaction.
- Reuse token đã revoked làm revoke toàn bộ family và ghi security alert.
- Frontend dùng single-flight refresh để tránh replay giả do request đồng thời.
- Logout idempotent, nhận refresh token trong body và vẫn revoke được khi access token hết hạn.
- CSP, output encoding, dependency review và không log token là kiểm soát bắt buộc vì token body/sessionStorage tăng bề mặt XSS.

## Hệ quả

- Giữ đúng contract no-cookie của PDF và giảm CSRF; đổi lại phải kiểm soát XSS nghiêm ngặt.
- API refresh/logout dùng request body thống nhất với OpenAPI.

## Tiêu chí xác minh

- Raw refresh token không xuất hiện trong DB/log/localStorage; sessionStorage bị xóa khi logout/tab session kết thúc.
- Reuse và concurrent refresh có integration tests.
- Logout khi access token hết hạn vẫn revoke được phiên.
