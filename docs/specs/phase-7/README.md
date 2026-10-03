# Phase 7 — Observability và operations

Trạng thái: **Implementation complete / local accepted** (03/10/2026).

## Phạm vi hoàn thành

| Hạng mục | Bằng chứng |
|---|---|
| P7-12 Metrics dashboard | OpenTelemetry Collector, Prometheus và dashboard Grafana provisioned tại `infra/observability`; gồm request rate, p95, 5xx rate, cache hit rate, job failures, business events và dependency health. |
| P7-13 Alerts | Prometheus rules cho readiness, PostgreSQL/Redis/MinIO, telemetry scrape, 5xx spike và failed jobs; Alertmanager local được provision. |
| P7-14 Structured audit | Middleware chỉ ghi mutation thành công với event, entity, user, UTC time, correlation ID, method và path; Admin audit API đọc được các trường cấu trúc. |
| P7-15 Runbook | `docs/operations/runbook.md` bao phủ failed jobs, orphan object, cache flush, migration, rollback và alert triage. |
| P7-16 Health privacy | Ba public health endpoint chỉ trả aggregate status; integration test chặn dependency names, entries, password hoặc topology. |

## QA và bằng chứng bàn giao

| Task | Bằng chứng tự động |
|---|---|
| P7-17 Author complete journey | `RecipeCompositionApiTests.P7AuthorCanComposeRecipeAndStepReorderStaysContinuous` và `P7AuthorCanCompletePublishingArchiveAndDeleteLifecycle` chạy toàn bộ vòng đời qua HTTP thật với PostgreSQL/Redis Testcontainers. `frontend/tests/author-dashboard.test.mjs` kiểm tra dashboard nối đủ create/edit/ingredient/step/publish/unpublish/archive/unarchive/delete mà không yêu cầu gọi API thủ công. |
| P7-18 Ownership và Admin | `RecipeCoreApiTests.P7OwnershipRoleEscalationAndAdminOverrideAreEnforced` xác nhận Author khác nhận `403`, Admin đọc/cập nhật được recipe ngoài ownership; `AdminAuditLogsApiTests` xác nhận mutation được audit. |
| P7-19 Escalation/direct URL | Cùng integration test xác nhận JWT Author cũ không tự tăng quyền sau khi role trong DB thay đổi. Frontend contract test xác nhận middleware bảo vệ `/dashboard/:path*`, navigation theo role và guard riêng tại Category/Audit pages. |
| P7-20 UI states | Frontend contract tests xác nhận loading, empty, error và permission-denied/unauthenticated states cho Author dashboard, My Recipes, Category management và Audit logs. |

Các test P7 dùng hai lớp: integration test thực thi API qua HTTP với dependencies thật trong container; frontend contract test khóa wiring và trạng thái của UI theo convention test hiện có của repository. Cách này giữ suite lặp lại được mà không thêm một browser framework chỉ cho Phase 7.

### Đầu ra và exit gate

| Gate | Kết quả |
|---|---|
| Author quản lý trọn vòng đời recipe từ UI | Đạt: dashboard/wizard cung cấp đủ action; HTTP journey xác nhận create, compose, reorder, publish, unpublish, archive, unarchive và delete. |
| Admin workflow được audit và kiểm soát quyền | Đạt: Author khác bị `403`, Admin override được phép, mutation audit có user/time/correlation. |
| Direct navigation/API không bypass authorization | Đạt: middleware và page guards được kiểm tra; API ownership/Admin policies được chạy bằng integration test. |
| On-call truy vết và xử lý failed job | Đạt local: correlation ID trả về client xuất hiện trong structured API log; runbook có quy trình failed-job triage/retry. Routing receiver production vẫn là công việc Phase 8. |
| Không public operational endpoint nhạy cảm | Đạt local: public ingress trả `401` cho `/jobs`, `404` cho metrics/Grafana/Prometheus; `/health` chỉ trả aggregate status. |

## Kiểm soát truy cập

- Grafana, Prometheus, Alertmanager và Seq chỉ bind `127.0.0.1` trong local Compose.
- OTLP và Prometheus collector endpoint chỉ nằm trên Docker network, không publish qua host/Nginx.
- Hangfire tiếp tục yêu cầu Admin policy; production phải thêm network restriction ở ingress/firewall.
- Alertmanager local không gửi ra ngoài. Staging/production phải cấu hình receiver theo owner thực tế và test routing trước release; phần diễn tập/routing production thuộc Phase 8.

## Verification

```powershell
dotnet test backend/CulinaryBlog.slnx --configuration Release
npm --prefix frontend test
npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run build
docker compose config --quiet
docker compose up --detach --build --wait
```

Sau khi stack chạy, mở Grafana tại `http://127.0.0.1:3001`, tạo traffic qua ứng dụng và xác nhận dashboard nhận dữ liệu. Kiểm tra Prometheus `/rules`, gửi một request mutation kèm `X-Correlation-ID`, sau đó tìm ID đó trong Admin audit view và Seq.

Tại lần verification ngày 03/10/2026:

- Phase 7 backend integration suite qua PostgreSQL/Redis Testcontainers: **18/18 pass**.
- Frontend test suite: **54/54 pass**; lint, typecheck và production build pass.
- Backend format/build, unit/architecture tests, Compose render và Grafana JSON parse pass.
- Runtime Compose: toàn bộ service healthy, Prometheus target `up`, bốn health series được thu thập, readiness bằng `1`, dashboard Grafana được provision và năm alert rules ở trạng thái `inactive/ok`.
- Public ingress: `/jobs` trả `401`; `/metrics`, `/internal/metrics`, `/grafana` và `/prometheus` trả `404`; `/health` chỉ chứa `status`.
- Correlation smoke test: ID `8544621ce3359de6f6d9d789af84b00d` trong response khớp chính xác structured API log.
