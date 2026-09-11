# Traceability Matrix v1

> Trạng thái: Accepted for implementation  
> Quy ước test: `UT` unit/domain, `IT` integration, `E2E` end-to-end, `NFT` non-functional.

## 1. Functional requirements

| Requirement | Baseline behavior | Endpoint/job | Screen | Verification |
|---|---|---|---|---|
| FR-AUTH-001 | Register Author, auto-login, welcome job | `POST /auth/register` | `/auth/register` | UT validator; IT 201/400/409; E2E-01 |
| FR-AUTH-002 | Local login, generic failure, lockout | `POST /auth/login` | `/auth/login` | IT 200/401/423/429; E2E-02 |
| FR-AUTH-003 | Google code+PKCE, backend ID-token verification | `POST /auth/google` | `/auth/login` | IT issuer/audience/expiry/link; E2E optional |
| FR-AUTH-004 | Rotate 512-bit refresh token, revoke family on reuse | `POST /auth/refresh` | Auth provider | UT token; IT race/reuse; E2E-02 |
| FR-AUTH-005 | Body token logout, idempotent with expired access | `POST /auth/logout` | Account menu | IT 204 and old-token rejection; E2E-02 |
| FR-AUTH-006 | View safe profile | `GET /auth/me` | `/profile` | IT 200/401/404 |
| FR-AUTH-007 | Update displayName/avatarUrl/bio | `PATCH /auth/me` | `/profile` | IT 200/400/401 |
| FR-CAT-001 | Public category list and Published count | `GET /categories` | `/`, `/categories` | IT empty/data/cache |
| FR-CAT-002 | Category detail with Published recipes only | `GET /categories/{slug}` | `/categories/[slug]` | IT 200/404/privacy |
| FR-CAT-003 | Admin create category and stable slug | `POST /categories` | `/dashboard/categories` | UT slug; IT 201/400/403/409 |
| FR-CAT-004 | Admin update without changing slug | `PUT /categories/{id}` | `/dashboard/categories` | IT 200/403/404/409 |
| FR-CAT-005 | Admin soft-delete empty category | `DELETE /categories/{id}` | `/dashboard/categories` | IT 204/403/404/409 |
| FR-RCP-001 | Public Published listing; owner/Admin private listing | `GET /recipes`; `GET /me/recipes`; `GET /admin/recipes` | `/recipes`, dashboard | IT filters/visibility/cache |
| FR-RCP-002 | Published detail; private owner/Admin detail | `GET /recipes/{slug}`; `GET /me/recipes/{id}`; `GET /admin/recipes/{id}` | detail/edit | IT nested projection/privacy |
| FR-RCP-003 | Create Draft with optional composition | `POST /recipes` | new wizard | UT invariant; IT 201/400/409; E2E-03 |
| FR-RCP-004 | Owner/Admin update with ETag | `PUT /recipes/{id}` | edit wizard | IT owner/stale race; E2E-05 |
| FR-RCP-005 | Publish needs ingredient+step; unpublish to Draft | `PATCH .../publish`, `.../unpublish` | preview/edit | UT transitions; IT 200/400/409; E2E-04 |
| FR-RCP-006 | Archive; supporting unarchive to Draft | `PATCH .../archive`, `.../unarchive` | dashboard | UT/IT transitions |
| FR-RCP-007 | Recipe soft delete | `DELETE /recipes/{id}` | dashboard | IT 204/global filter/media privacy |
| FR-RCP-008 | Upload/update/delete image and atomic primary | image endpoints | wizard/gallery | IT MIME/size/auth/storage/jobs |
| FR-RCP-009 | Ingredient CRUD with nullable quantity/unit | ingredient endpoints | wizard | UT validation; IT CRUD/order/owner |
| FR-RCP-010 | Step CRUD and continuous numbering | step endpoints | wizard | UT renumber; IT transaction/owner |
| FR-SRCH-001 | Vietnamese FTS and rank Published only | `GET /recipes/search` | `/search` | IT accent/no-accent/injection/index; E2E-04 |
| FR-SRCH-002 | AND filters: category, difficulty, cook/serving/prep bounds | recipe/search GET | recipes/search | IT combinations |
| FR-SRCH-003 | Allowlisted sorting, default newest | recipe/search GET | recipes/search | IT asc/desc/invalid |
| FR-SRCH-004 | Offset pagination 1..50 | recipe/search/category GET | list pages | IT boundaries/empty/meta |
| FR-FILE-001 | MinIO upload <=5MB, allowlist and signature | `POST .../images` | wizard | IT spoof/oversize/path/storage |
| FR-FILE-002 | Idempotent object delete, retry 3 | object-delete job | N/A | IT retry/missing object |
| FR-JOB-001 | Welcome email retry 1/5/30 min | Hangfire/outbox | N/A | IT commit/outbox/idempotency |
| FR-JOB-002 | 800x600 and 300x300 variants | resize job | Gallery | IT output/retry/fallback original |
| FR-JOB-003 | Sitemap 02:00 UTC, retry 2 | recurring job | `/sitemap.xml` | IT Published URLs only/latest retained |
| FR-OBS-001 | Aggregate/live/ready probes | health endpoints | Ops | IT dependency matrix |
| FR-OBS-002 | Structured correlated request/CQRS logs | middleware/behavior | Admin audit/Ops | IT fields/redaction/slow warning |
| FR-OBS-003 | HTTP/EF/custom traces and metrics | OTEL | Ops | IT export/correlation/metric |

## 2. NFR trace

| Group | Implementation evidence | Release gate |
|---|---|---|
| NFR-PERF-001-005 | Redis cache, indexes/projection, k6, Lighthouse CI | p50/p95/p99, 100 concurrent users, hit rate >=80%, CWV budgets |
| NFR-SEC-001-007 | Identity PBKDF2, JWT/rotation, rate limits, upload checks, TLS/CORS/CSP, policies, secret scan | Security tests and no open Critical/High |
| NFR-USE-001-004 | Responsive routes, WCAG AA, Problem Details, skeleton/toast/progress | Axe plus keyboard/NVDA/VoiceOver and visual tests |
| NFR-REL-001-003 | probes/monitor, graceful Redis fallback, Hangfire/outbox, backup/restore | 99.5% SLO evidence and restore drill |
| NFR-MAINT-001-004 | analyzers, 80% Application coverage, docs, architecture tests | CI quality gates |
| NFR-SCALE-001-003 | stateless API, Redis, PostgreSQL queue, Nginx and persistent services | multi-instance/cache isolation/failure tests |
| NFR-SEO-001-004 | JSON-LD, metadata, sitemap/robots and immutable Published slug | schema/metadata/crawler tests |

No requirement is considered complete without an implementation link and passing test evidence in the delivery tracker.
