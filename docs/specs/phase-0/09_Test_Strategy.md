# Test Strategy v1

> Trạng thái: Accepted for implementation  
> Scope: toàn bộ 34 FR và NFR của SRS, theo `06_Traceability_Matrix.md`.

## 1. Test levels

| Level | Scope | Tool/reference | Gate |
|---|---|---|---|
| Unit/domain | Invariant, transition, validator, token, slug, ordering | xUnit; frontend Jest/Testing Library | Application/domain line coverage >=80% |
| Architecture | Dependency direction và CQRS boundaries | ArchUnit.NET/custom tests | Không có violation |
| Integration | API với PostgreSQL/Redis/MinIO/Hangfire thật | WebApplicationFactory + Testcontainers | Mỗi endpoint có happy và error case |
| Contract | OpenAPI request/response/status/problem codes | OpenAPI validator/client generation | Không drift contract |
| E2E | CJ-01 đến CJ-05 | Playwright | 5 critical journeys pass |
| Security | OWASP, access control, token, upload, CORS/CSP/TLS | Automated scan + manual verification | Không Critical/High mở |
| Performance | API/cache/database/frontend | k6, EXPLAIN ANALYZE, Lighthouse CI | Đạt toàn bộ NFR-PERF |
| Accessibility | WCAG 2.1 AA | axe + keyboard + NVDA/VoiceOver | Không Critical/Serious |
| Reliability | Restart/outage/outbox/backup restore | Failure drill/runbook | Dữ liệu bền và recovery pass |

## 2. Required environments and data

- Local/CI dùng ephemeral PostgreSQL 16, Redis 7 và MinIO; không thay PostgreSQL bằng provider in-memory.
- Staging dùng cùng immutable image/digest sẽ promote production.
- Dataset chuẩn: 50 Recipe, 5 Author và Category; có tiếng Việt có dấu/không dấu, Draft/Published/Archived và media states.
- Performance profile ghi hardware 2 vCPU/4GB, dataset, network, read/write ratio, payload và warm/cold cache.

## 3. Functional suites

- Auth: password policy/hash, register response, generic login, lockout, Google validation, refresh rotation/reuse/race, logout expired access, profile.
- Authorization: Guest/owner/other Author/Admin trên Recipe và child resources; Admin Category/Hangfire.
- Data: migration, global unique slug/name, global soft-delete filter, ETag atomicity và cascade semantics.
- Recipe: Draft create/update, ingredient nullable quantity/unit, step renumber, image primary, publish/unpublish/archive/unarchive.
- Discovery: Published-only, accent-insensitive FTS, rank, filters AND, sort allowlist, pagination and empty result.
- Jobs/storage: upload signature/size/path, resize dimensions, outbox crash window, retries, idempotent delete and sitemap.
- Contract: 400 validation/precondition, 409 duplicate/state/concurrency, 401/403/404 boundaries, RFC 7807 fields and correlation IDs.

## 4. NFR gates

- API warm-cache p50 <=150ms, all endpoint p95 <=500ms, p99 <=1000ms.
- At least 100 concurrent users on 2 vCPU/4GB without defined degradation.
- Redis steady-state hit rate >=80%; category/detail/list/search TTL = 30/5/15/1 minutes.
- Slow-query warning >100ms; critical WHERE/ORDER BY indexes reviewed with EXPLAIN ANALYZE.
- LCP <=2.5s, CLS <=0.1, INP <=200ms, first-load JS <=200KB gzip.
- Browser minimum: Chrome 112, Firefox 113, Edge 112, Safari 16; mobile Chrome 90+ and iOS Safari 14+.
- Readiness probe every 10s; uptime alert after >1 minute; backup daily at 03:00 UTC, retention 30 days, restore drill pass.

## 5. CI/release policy

PR runs build, lint, analyzers, unit, architecture, contract and integration tests. Main/release adds E2E, security scan, Lighthouse and deploy smoke. Release candidate adds k6, accessibility manual smoke, backup restore, dependency outage drills and Product Owner UAT.

A flaky or disabled critical test is a release blocker. Test exceptions require owner, expiry date and written risk acceptance.
