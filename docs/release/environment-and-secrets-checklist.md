# Environment và secrets checklist

Checklist áp dụng riêng cho staging và production. Giá trị thật chỉ nằm trong secret store/deployment system, không nằm trong Git, ticket, log, artifact hoặc file bàn giao. Release Manager đính kèm bằng chứng kiểm tra nhưng che toàn bộ giá trị.

## Ownership và rotation

| Nhóm | Biến/tài sản | Owner | Chu kỳ/trigger rotation | Kiểm chứng không lộ giá trị |
|---|---|---|---|---|
| Database | `POSTGRES_PASSWORD`, `DATABASE_CONNECTION_STRING` | DBA/Platform | 90 ngày; ngay khi nghi lộ/người có quyền rời nhóm | API readiness + backup/restore |
| Object storage | `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` | Platform | 90 ngày; ngay khi nghi lộ | upload/private-read + snapshot |
| Application signing | `JWT_SIGNING_KEY` | Security Lead | 90 ngày hoặc incident; có kế hoạch vô hiệu phiên | login/refresh/logout |
| Web session | `AUTH_SECRET` | Security Lead | 90 ngày hoặc incident; chấp nhận logout phiên hiện tại | Auth.js login/session |
| Revalidation | `REVALIDATION_SECRET` | Backend Lead | 90 ngày hoặc incident | publish → cache/sitemap refresh |
| OAuth | `AUTH_GOOGLE_ID/SECRET` | Product/Security | Theo provider hoặc incident | Chỉ bắt buộc nếu scope OAuth được duyệt |
| SMTP | credential tại SMTP provider | Operations | 90 ngày hoặc provider/incident | welcome-email synthetic account |
| Alerting | `OPS_ALERT_WEBHOOK_URL_FILE` | On-call Lead | Khi đổi receiver/owner hoặc incident | Gửi alert test và resolved |
| Grafana | `GRAFANA_ADMIN_PASSWORD` | On-call Lead | 90 ngày; ngay khi đổi operator | Login, anonymous access bị chặn |
| TLS | cert/key qua `TLS_CERT_FILE`, `TLS_KEY_FILE` | Platform/Security | Tự động trước hết hạn ≥30 ngày; incident | chain/hostname/TLS 1.2+ |

## Pre-deploy theo môi trường

- [ ] Domain/DNS, `PUBLIC_URL`, `ALLOWED_HOSTS` và CORS cùng trỏ đúng môi trường; staging không dùng production data.
- [ ] `RELEASE_COMMIT_SHA`, `API_IMAGE(_DIGEST)` và `WEB_IMAGE(_DIGEST)` khớp release bundle; toàn bộ image phụ trợ là `image@sha256` đã được scan.
- [ ] Secret được inject từ secret store với quyền least privilege; file env thật nằm ngoài repo, permission chỉ dành cho deploy identity.
- [ ] `AdminSeed__Enabled=false`; Google credential trống nếu OAuth không thuộc RC; không có `local-development`, `example-` hoặc placeholder.
- [ ] TLS certificate/key và alert webhook file tồn tại trên host; Nginx chỉ publish 80/443; dashboard/datastore không public.
- [ ] Data volumes nằm trên encrypted persistent storage; hai backup volumes độc lập với data volumes và host chính; retention 30 ngày.
- [ ] SMTP sender/domain được duyệt; không dùng MailHog trong staging/production.
- [ ] On-call, backup owner, DBA và Security Lead có primary/secondary contact; quyền emergency access đã test và audit.
- [ ] `./scripts/release-gate.ps1` pass với manifest thật; evidence được đính kèm change record.

## Rotation procedure

1. Tạo credential mới trong secret store, ghi change/owner/time nhưng không ghi giá trị.
2. Cập nhật consumer theo thứ tự hỗ trợ dual-key nếu có; deploy và chạy smoke.
3. Thu hồi credential cũ sau khi xác nhận metric/log không còn dùng; với signing/session key, thực hiện communication và revoke-session plan.
4. Chạy secret scan, kiểm tra audit log và cập nhật `rotatedAt/nextDueAt` trong hệ thống quản lý secrets.
5. Nếu rotation do lộ lọt, mở incident, thu hồi ngay, đánh giá log/access và không chờ chu kỳ thông thường.

Sign-off: Environment ______ · Release SHA ______ · Platform ______ · Security ______ · UTC time ______

