# ADR-005: Google OAuth/OIDC flow

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Frontend Lead
- Liên quan: GAP-005

## Bối cảnh

SRS mô tả cả Auth.js callback, backend callback, ExternalLoginInfo và ID-token exchange. Chỉ một component nên sở hữu authorization-code callback.

## Quyết định

- Auth.js/Next.js sở hữu Authorization Code Flow + PKCE, state và nonce.
- Sau callback, frontend/server-side Auth.js gửi Google ID token đến `POST /api/v1/auth/google` qua trusted flow.
- Backend xác minh signature, issuer, audience, expiry, nonce/context nếu truyền, và `email_verified` bằng thư viện Google chính thức.
- Backend tìm external login bằng `(provider, subject)`, sau đó link tài khoản theo normalized verified email hoặc tạo Author mới.
- Google access token không được dùng làm Culinary Blog access token và không persist nếu không cần gọi Google API.
- Auto-link chỉ áp dụng verified email; account conflict trả lỗi rõ và không merge mù.

## Hệ quả

- Frontend/Auth.js và backend đều có trách nhiệm bảo mật rõ.
- Cần test credentials/callback cho local, staging, production.
- Google login là Should-have; local auth không phụ thuộc provider.

## Tiêu chí xác minh

- Token sai audience/issuer/expired/unverified email bị từ chối.
- Login lặp lại trả cùng Culinary user; không tạo duplicate.
