# Phase 2 — Identity và quản lý phiên

**Trạng thái:** **Core Implementation Complete / Docker Acceptance Pending**
**Baseline:** [Phase 0](../phase-0/README.md)
**Ngày triển khai:** 13/09/2026

## Phạm vi đã triển khai

| ID | Kết quả | Bằng chứng chính |
|---|---|---|
| P2-01–04 | ASP.NET Core Identity, `ApplicationUser`, Author/Admin, PBKDF2-HMACSHA512 100.000 iterations, refresh-token schema/index và migration | `Infrastructure/Identity`, `Persistence/Migrations` |
| P2-05–11 | Register/auto-login, login/lockout, JWT 15 phút, refresh 512-bit TTL 7 ngày, rotation/reuse-family revoke, idempotent logout, GET/PATCH profile | `AuthService`, `AuthEndpoints` |
| P2-12–13 | `ActiveUser`, `AuthorPolicy`, `AdminPolicy`; auth rate limit 10 request/phút/IP với `Retry-After` | `ActiveUserAuthorization`, presentation DI |
| P2-15 | Welcome email được ghi cùng transaction đăng ký; worker SMTP retry sau 1, 5 và 30 phút; MailHog bật trong Compose | `WelcomeEmailOutbox`, `WelcomeEmailWorker` |
| P2-16–22 | Form local register/login, auth provider thật, protected dashboard, return URL, single-flight refresh, logout/cache clear và profile edit/error states | `frontend/src/lib/api/auth-client.ts`, `AuthContext.tsx`, auth/profile pages |
| P2-23–28 | Unit test entropy/hash/validator; integration test lifecycle, raw-secret persistence, replay, concurrent refresh, expiry/tamper, lockout, policy và rate limit | `AuthSecurityTests`, `AuthLifecycleTests`, `AuthRateLimitTests` |

## Quy tắc bảo mật đã áp dụng

- Access token chỉ ở memory của browser; refresh token chỉ ở `sessionStorage`, không dùng `localStorage` hoặc cookie.
- Database chỉ lưu SHA-256 của refresh token. Rotation dùng conditional update trong transaction; reuse revoke toàn bộ token family.
- Password do ASP.NET Core Identity hash bằng PBKDF2-HMACSHA512 với 100.000 iterations; raw password/token không được ghi log.
- Login trả lỗi credentials chung; 5 lần sai khóa tài khoản 15 phút.
- Admin seed mặc định tắt. Khi bật, email/password chỉ nhận qua configuration/secret; quá trình seed role và Admin có tính idempotent.
- Endpoint profile kiểm tra trạng thái `IsActive` từ database ở mỗi authorization decision.

## Cấu hình local

Docker Compose tự áp migration, bật SMTP worker và kết nối MailHog. Để seed Admin local, đặt trong `.env`:

```dotenv
ADMIN_SEED_ENABLED=true
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=<strong-local-password>
JWT_SIGNING_KEY=<at-least-32-random-characters>
```

Không bật Admin seed bằng giá trị mặc định ở staging/production. Production phải chạy migration như bước deploy có kiểm soát và lấy JWT/Admin secret từ secret store.

## Kết quả kiểm định

| Gate | Kết quả ngày 13/09/2026 | Trạng thái |
|---|---|---|
| NuGet locked restore | Restore toàn solution và sinh lockfile bằng .NET SDK 10.0.401 | Đạt |
| Backend build | Debug build toàn solution: `0 warning / 0 error` | Đạt |
| Unit/architecture | Unit `7/7`; architecture `3/3` | Đạt |
| Integration không cần container | Liveness + auth rate limit `2/2` | Đạt |
| Migration consistency | `dotnet ef migrations has-pending-model-changes`: không có model change chưa migrate | Đạt |
| Frontend static gates | TypeScript strict, ESLint và Prettier đều pass | Đạt |
| Frontend production build | Next.js build thành công, sinh đủ 13 route | Đạt |
| PostgreSQL auth journeys | Testcontainers test đã build; không chạy được vì Docker engine trên máy kiểm định không hoạt động | Chờ môi trường |

## Hạng mục chưa đóng

- P2-14/P2-27 Google OAuth là Should-have. Repo chưa có Auth.js/provider credentials nên UI Google giả lập đã bị gỡ; không tạo endpoint xác minh token giả hoặc auto-link không an toàn.
- P2-15 yêu cầu đích là Hangfire. Hiện welcome email chạy bằng transactional outbox + hosted worker với đúng retry schedule và không rollback user. Cần chuyển consumer sang Hangfire khi Hangfire foundation/storage được đưa vào platform.
- Chạy `AuthLifecycleTests` trên CI/Docker để có bằng chứng PostgreSQL thật cho register/profile/rotation/replay/concurrency/inactive-policy.
- Exit gate chỉ chuyển `Accepted / Closed` khi các test container pass và security review không còn issue Critical/High.
