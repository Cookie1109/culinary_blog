# Báo cáo Security Phase 8

> Ngày kiểm tra: 04/10/2026
> Phạm vi: P8-10 đến P8-15
> Kết luận local: **P8-10 đến P8-14 đạt; dev toolchain đã remediation về 0 High/Critical; P8-15 vẫn No-Go cho release** cho đến khi hoàn tất ZAP trên immutable staging HTTPS.

## OWASP review — P8-10

| Nhóm rủi ro | Kiểm soát và bằng chứng | Kết quả |
|---|---|---|
| Broken access control | Policy Guest/Author/Admin, resource ownership, Admin override và private/public cache isolation đã có integration tests; dashboard Hangfire tiếp tục bị role/network gate. | Đạt local |
| Authentication/session | PBKDF2/Identity, JWT issuer/audience/signature/expiry, refresh rotation/replay/revoke, lockout và `Cache-Control: no-store`; Culinary refresh token chỉ nằm trong `sessionStorage` của tab và không đi vào Auth.js JWT cookie. | Đạt local |
| Injection/XSS | EF Core parameterization, validator/allowlist cho sort/filter, React escaping và JSON-LD serialization chống `</script>`; không có đường raw SQL nhận input người dùng. | Đạt local; CSP nonce được ghi residual risk bên dưới |
| Upload | Giới hạn 5 MB, MIME/signature/decode/pixel checks, object key GUID, bucket private và test spoofed MIME/bad magic/oversize/path traversal. | Đạt local |
| SSRF | Backend không fetch URL do người dùng cung cấp; các outbound destination (MinIO, SMTP, OTEL, revalidation) đến từ deployment configuration. | Đạt local |
| Security misconfiguration | CORS exact allowlist/fail-closed, chỉ trust một reverse-proxy hop khi bật rõ ràng, production fail-fast với secret/host/HTTPS URL không an toàn, HSTS/TLS 1.2+, secure headers và image runtime non-root. | Đạt local; TLS runtime cần staging verify |

## Hardening — P8-12

- API CORS không bật credentials, chỉ cho các origin cấu hình và các method/header cần thiết.
- Nginx production redirect HTTP sang HTTPS, chỉ cho TLS 1.2/1.3 và phát HSTS `max-age=31536000; includeSubDomains; preload`.
- Next.js phát CSP, Referrer-Policy, X-Content-Type-Options, X-Frame-Options, Permissions-Policy, COOP; `X-Powered-By` đã tắt sau vòng ZAP đầu.
- Production API dừng khởi động với exit code khác 0 nếu còn development secret, `AllowedHosts=*`, sitemap HTTP hoặc CORS origin không HTTPS.
- Reverse proxy ghi đè `X-Forwarded-For` bằng địa chỉ peer thay vì nối header do client gửi, tránh spoof partition key của rate limiter.
- Auth.js cookie chỉ chứa encrypted session/access state phục vụ SSR; Culinary refresh token không nằm trong cookie/localStorage và được rotate trong `sessionStorage`.

## Scan — P8-11

| Gate | Kết quả ngày 04/10/2026 |
|---|---|
| .NET dependency | `dotnet list ... --vulnerable --include-transitive`: không có package vulnerable trong các project. |
| npm toàn bộ dependency | `npm audit --audit-level=high`: `0 vulnerabilities`, bao gồm dev dependencies. |
| Secret history | Gitleaks 8.30.1: 38 commit, khoảng 2.36 MB, `no leaks found`. |
| API image | Trivy 0.74.0, không bỏ qua unfixed: Ubuntu 24.04 `0` High/Critical; application/runtime .NET `0`. |
| Web image | Node 22.23.3 trên distroless Debian 13.7, UID/GID 65532: `0` High/Critical cho OS và Node packages. |
| Dockerfile misconfiguration | Trivy: API `0`, Web `0` High/Critical. |
| License | Runtime Web còn libvips x64 `LGPL-3.0-or-later`, được triage thành exception đúng một SPDX ID; binary WASM/musl không dùng đã loại khỏi image. GPL-3.0/AGPL-3.0 vẫn bị dependency-review chặn. Nếu phân phối container cho bên thứ ba thay vì chỉ vận hành dịch vụ, Legal phải review lại nghĩa vụ LGPL. |

Hai finding runtime đã được remediation trong lúc review: OpenSSL High của API base được sửa bằng digest mới; Node 20 hết vòng đời và các CVE OS/npm của image Web được xử lý bằng Node 22 LTS, distroless Debian 13 và loại platform binaries không dùng. CI quét cả High/Critical có và chưa có bản vá; không còn `ignore-unfixed`.

16 High trong dev toolchain đã được remediation mà không dùng risk acceptance:

- Bỏ `@lhci/cli`; release performance gate dùng `lighthouse` trực tiếp, chạy ba lần cho mỗi URL, lấy median, giữ các ngưỡng LCP/CLS/TBT/script size/meta description và lưu JSON/HTML cùng assertion summary.
- Bỏ `eslint-config-next`/`@next/eslint-plugin-next`; ESLint flat config dùng trực tiếp TypeScript, React, React Hooks và JSX accessibility plugins.
- `npm ls braces extract-zip basic-ftp tmp @lhci/cli @next/eslint-plugin-next` trả về cây rỗng.
- CI chạy full `npm audit --audit-level=high`; kết quả local ngày 04/10/2026 là `0 vulnerabilities`.
- Lighthouse runner thay thế đã chạy thực tế trên 4 route, 3 lần/route: performance `0.99–1.00`, LCP median `770–913 ms`, CLS `0`, TBT `0 ms`, script transfer lớn nhất `200.978 byte`; toàn bộ release assertion pass.
- Security headers trùng giữa Next và Nginx đã được loại ở reverse proxy; edge vẫn phát đủ CSP, COOP, Referrer-Policy, nosniff, frame và permissions policy đúng một lần.

## Rate limit — P8-13/P8-13a

| Partition | Cấu hình | Bằng chứng |
|---|---:|---|
| Auth | 10 request/phút/IP | Integration test request thứ 11 nhận 429. |
| API chung | 100 request/phút/IP | Cấu hình mặc định được giữ; test với quota thu nhỏ chứng minh partition theo forwarded client IP qua đúng một trusted hop. |
| Upload | 5 request/phút/IP | Integration test upload request thứ 6 nhận 429. |

Response 429 có `Retry-After`; limiter dùng client IP đã được chuẩn hóa sau trusted proxy processing.

## Dynamic scan — P8-14

ZAP baseline local đã spider **78 URL**: `0 FAIL`, `7` nhóm WARN, `60` rule PASS. Report mới nhất nằm trong `artifacts/security-remediation/` (gitignored). Vòng đầu phát hiện `X-Powered-By`; sau remediation, các vòng sau chuyển rule 10037 sang PASS.

Manual triage:

- Medium `script-src/style-src 'unsafe-inline'`: Next.js 15 cần inline bootstrap/style trong cấu hình hiện tại. CSP vẫn khóa `default-src`, `base-uri`, `frame-ancestors`, `form-action`, `object-src`, script/connect về same-origin. Chuyển sang per-request nonce là hardening tiếp theo vì có tác động render/cache và cần regression riêng.
- Medium thiếu SRI: chỉ áp dụng cho chunk Next.js cùng origin, tên file content-hashed và bị CSP `script-src 'self'` giới hạn; không có third-party script trong finding.
- Low COEP/CORP: chưa bật vì có thể làm hỏng media/resource hợp lệ; COOP đã bật. Cần test cross-origin isolation trước khi thay đổi.
- Informational content-type trên redirect, cache/session detection và “Modern Web Application” là hành vi framework, không phải exploit.

Workflow `security-dynamic.yml` yêu cầu target HTTPS và ba định danh bất biến: full commit SHA, API image digest, Web image digest. Trước khi scan, workflow gọi `/api/v1/release` và từ chối nếu deployment không khớp; production API cũng fail-fast nếu thiếu metadata này. Workflow dùng ZAP image theo digest, fail khi có FAIL/ERROR, sinh `evidence.json` kèm report checksum/run URL và upload JSON/HTML/Markdown với artifact digest. Local HTTP scan không thay thế staging evidence.

Workflow `release-images.yml` build và push riêng API/Web lên GHCR với tag duy nhất là full commit SHA, xuất digest thật, SBOM và build-provenance attestation. Staging phải deploy bằng `image@sha256:...`, không deploy bằng tag.

## Release gate — P8-15

Trạng thái hiện tại: **No-Go chỉ vì chưa có external staging evidence**. Toolchain gate đã đạt. Chuyển sang Go sau khi deploy đúng commit/API digest/Web digest lên HTTPS staging, chạy workflow ZAP, manual-triage mọi warning và lưu run ID, artifact digest cùng report.

Không có Critical/High mở trong hai runtime images hoặc toàn bộ npm dependencies. Các Medium của ZAP không thuộc ngưỡng P8-15 nhưng vẫn được ghi nhận ở trên để không mất dấu.

Biến bắt buộc trên staging:

- `Release__CommitSha=<40-character Git SHA>`
- `Release__ApiImageDigest=sha256:<64 hex>`
- `Release__WebImageDigest=sha256:<64 hex>`

Ví dụ kích hoạt evidence workflow sau khi deploy:

```powershell
gh workflow run release-images.yml --ref <branch-or-tag-containing-commit>

gh workflow run security-dynamic.yml `
  --ref <branch-or-tag-containing-commit> `
  -f target_url=https://<immutable-staging-host> `
  -f commit_sha=<commit-sha> `
  -f api_digest=sha256:<api-digest> `
  -f web_digest=sha256:<web-digest>
```
