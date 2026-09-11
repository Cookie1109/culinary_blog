# Threat Model v1

> Trạng thái: Accepted for implementation  
> Phương pháp: STRIDE, tập trung vào các trust boundary trong SRS.

## 1. Trust boundaries và tài sản

| Boundary | Tài sản cần bảo vệ |
|---|---|
| Browser ↔ Nginx/API | Password, JWT, refresh token, user data |
| Next.js/Auth.js ↔ Google | Authorization code, PKCE verifier, ID token, state/nonce |
| API ↔ PostgreSQL | Identity, Recipe, refresh-token hash, outbox |
| API/Worker ↔ MinIO | Original/derived images và object key |
| API ↔ Redis | Public cached response, rate-limit counters |
| API/Worker ↔ Hangfire/SMTP/OTLP | Job payload, email, logs/traces/metrics |

## 2. STRIDE register

| ID | Threat | S/T/R/I/D/E | Control bắt buộc | Verification |
|---|---|---|---|---|
| TM-01 | Credential stuffing/user enumeration | S/I/D | Generic 401, lockout 5/15m, auth rate 10/min/IP | Auth integration and load tests |
| TM-02 | JWT forgery/replay | S/T | HS256 key management, issuer/audience/expiry/jti validation, TLS | Tampered/expired/wrong-audience tests |
| TM-03 | Refresh-token theft/reuse | S/T | 512-bit token, SHA-256 DB hash, sessionStorage, rotation transaction, family revoke, CSP | Replay/race/XSS review |
| TM-04 | CSRF on auth operations | S/T | Refresh/logout token in JSON body, strict CORS, no credential cookie | Cross-origin tests |
| TM-05 | Google account substitution | S/E | PKCE, state, nonce, issuer/audience/expiry/email_verified, `(provider,sub)` mapping | OAuth negative tests |
| TM-06 | Broken object-level authorization | E/I | Owner/Admin policy in endpoint and Application handler | Full actor/resource matrix |
| TM-07 | Draft leak through public cache/ISR | I | Published-only public endpoints, Redis key isolation, private no-store routes | Cache warmed by Admin then Guest test |
| TM-08 | Draft image leak | I | Private bucket, authorized presigned URL, controlled Published namespace, CDN invalidation | Anonymous object access tests |
| TM-09 | Malicious/oversized upload | T/D | 5MB streaming limit, MIME allowlist, magic/decode, GUID key, no client filename | Spoof/chunk/path tests |
| TM-10 | SQL/tsquery injection | T/I | EF parameterization, sort allowlist, normalized token builder | Special-character and fuzz tests |
| TM-11 | Lost update | T/R | Version bigint ETag/If-Match and atomic increment | Concurrent write test |
| TM-12 | Lost/duplicated background work | T/R | Transactional outbox, persistent Hangfire, idempotency key and retry | Crash/replay test |
| TM-13 | Secret/token leakage in logs | I | Redaction, structured allowlist, secret scanning, production no stack trace | Log inspection and scanner |
| TM-14 | Health/telemetry topology exposure | I | Public status only; detail/internal backend protected; raw telemetry ops-authenticated | Anonymous endpoint test |
| TM-15 | Rate-limit bypass/DoS | D | Auth 10, general 100, upload 5 requests/min/IP; Nginx and API limits | Distributed/load test |
| TM-16 | Soft-deleted data disclosure | I | Global query filter, explicit admin opt-in, cache/search/sitemap exclusion | DB/API/search regression |

## 3. Security release criteria

- TLS 1.2+, HSTS one year and CORS allowlist are active in production.
- Password uses ASP.NET Core Identity PBKDF2-HMACSHA512 with at least 100.000 iterations.
- Secrets come from user-secrets/env/secret store and are never committed.
- No Critical/High finding remains without written risk acceptance.
- Backup/restore and Redis/MinIO/SMTP/worker failure drills pass.
