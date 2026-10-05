# Go/no-go và rollback plan — v1.0.0

> Trạng thái: **No-Go mặc định** cho tới khi toàn bộ gate bắt buộc có evidence và người có thẩm quyền ký. Không suy diễn pass từ ô trống.

## Meeting record

| Trường | Giá trị |
|---|---|
| Change/release ID | Chờ điền |
| Thời gian UTC / maintenance window | Chờ điền |
| Release SHA / API digest / Web digest | Chờ điền |
| Bản production đang chạy (SHA/digests) | Chờ điền |
| Release Manager / Technical Lead / Product Owner / On-call / DBA | Chờ điền |

## Go/no-go gates

| Gate | Pass khi | Evidence | Owner/decision |
|---|---|---|---|
| Scope/traceability | Feature freeze; Must-have và 5 CJ pass | Phase 8 + UAT record | PO/QA |
| Defects/security | 0 Critical/High; Medium có acceptance | defect register + staging ZAP/scans | Security/PO |
| Performance/SEO | API/cache/Lighthouse budgets và `meta-description=1` trên staging | report/run IDs | QA |
| Artifact integrity | manifest, SBOM/provenance, gate JSON và `/api/v1/release` khớp | release bundle + staging evidence | Release Manager |
| Reliability | restore/failure drill, RTO/RPO, backup storage/owner, alert delivery pass | staging evidence | DBA/On-call |
| Operations | TLS/DNS/secrets/access/runbook/on-call/maintenance message sẵn sàng | checklist | Platform |
| Rollback | previous digests/config/backup đã xác nhận; người ra lệnh rõ | mục dưới | Technical Lead |

Final decision: **NO-GO / GO** · ApprovedChangeId: ______

Signatures/references: Product Owner ______ · Technical Lead ______ · Release Manager ______ · On-call ______

## Deployment sequence

1. Công bố maintenance trước ít nhất 48 giờ nếu cần downtime; xác nhận backup cuối và RPO.
2. Chạy migration được review theo runbook trên staging rồi production; `ApplyMigrationsOnStartup=false`.
3. Chạy `deploy-release.ps1` với env ngoài repo, release manifests và `ApprovedChangeId`; không build trên host.
4. Xác minh readiness/home/security headers/release identity; chạy CJ smoke và xem dashboard/alerts/log correlation.
5. Giữ operator trực 2 giờ đầu; theo dõi tăng cường 24–48 giờ. Ghi 5xx, p95/p99, readiness, dependency, queue/job, cache và business errors tại mốc 0/15/30/60/120 phút rồi mỗi ca.

## Rollback triggers

Rollback ngay khi: readiness không ổn định quá 5 phút; smoke/Must-have lỗi; artifact identity lệch; nghi rò Draft/PII/secret; auth/authorization regression; Critical/High security issue; error/latency vượt budget liên tục 10 phút; mất dữ liệu hoặc migration sai.

## Rollback procedure

1. Release Manager tuyên bố rollback, dừng rollout/write traffic khi có nguy cơ dữ liệu và ghi UTC/correlation/incident ID.
2. Nếu schema backward-compatible, thay env bằng **previous API/Web digests + commit metadata**, chạy release gate rồi `deploy-release.ps1` với change/incident ID. Không rebuild/retag.
3. Nếu schema không backward-compatible hoặc dữ liệu hỏng, giữ write traffic dừng; Technical Lead + DBA quyết định reviewed down-migration hay restore backup. Không tự sửa trực tiếp database.
4. Chạy readiness, release identity, CJ-01/CJ-02/CJ-04 tối thiểu; xác nhận Draft/private media không lộ, cache/sitemap đúng và alert resolve.
5. Giám sát tối thiểu 60 phút sau rollback; lưu deployment evidence, logs/metrics, RTO/RPO và mở post-incident review.

Rollback complete: UTC ______ · Previous SHA/digests ______ · Data action ______ · Smoke evidence ______ · Commander ______

