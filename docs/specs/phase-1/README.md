# Phase 1 — Platform foundation

**Trạng thái:** **Implementation Complete / Local Accepted** — còn 2 gate nghiệm thu ngoài máy hiện tại  
**Baseline:** [Phase 0](../phase-0/README.md)  
**Ngày triển khai:** 12/09/2026

## Quyết định tích hợp giao diện

Gói giao diện được bàn giao sử dụng React/Vite và phụ thuộc một tệp metadata không có trong source, trong khi Phase 0 đã chốt Next.js 15 App Router. Vì vậy:

- Component, context và page cần thiết được tích hợp vào `frontend/src/features/culinary/`.
- Entrypoint, package và cấu hình preview Vite không thuộc runtime sản phẩm được loại bỏ.
- Next app tại `frontend/` sử dụng trực tiếp giao diện qua adapter React Router → Next Navigation.
- Design token được đưa vào `frontend/src/app/globals.css`.
- Không tiếp tục Vite như runtime sản phẩm vì sẽ trái GAP-025 và P1-11.

## Cấu trúc repository

Từ ngày 12/09/2026, repository được chia thành hai application boundary:

- `frontend/`: Next.js runtime và toàn bộ source giao diện sản phẩm.
- `backend/`: .NET solution, source, tests, package management và tool manifest.
- `docs/`, `infra/`, `scripts/`, `compose.yaml` và `.github/` tiếp tục ở root vì là tài sản dùng chung cho cả hai phần.

Quyết định này chỉ thay đổi tổ chức vật lý và đường dẫn build; không thay đổi requirement, API contract hoặc kiến trúc đã chốt trong PDF/Phase 0.

## Theo dõi công việc

| ID | Kết quả | Bằng chứng |
|---|---|---|
| P1-01–04 | Solution Clean Architecture, dependency direction, central packages và composition roots | `backend/CulinaryBlog.slnx`, `backend/Directory.*.props`, `AddApplication`, `AddInfrastructure`, `AddPresentation` |
| P1-05 | MediatR pipeline logging, FluentValidation, Redis cache và cảnh báo >500 ms | `Application/Behaviors` |
| P1-06–10a | Problem Details, correlation, JSON logs/Seq, OpenTelemetry, health và 100 req/min/IP | `CulinaryBlog.Api` |
| P1-11–16 | Next App Router strict, Tailwind, route groups, design primitives, OpenAPI types, Query provider/error handling | `frontend/` |
| P1-17–22 | PostgreSQL, Redis, MinIO private bootstrap, MailHog, Seq, healthcheck, Dockerfiles và Nginx | `compose.yaml`, `infra/nginx` |
| P1-23–24 | Build/test/lint plus dependency, container/config và secret scanning | `.github/workflows/ci.yml` |
| P1-25 | Testcontainers foundation cho PostgreSQL/Redis/MinIO | `backend/tests/CulinaryBlog.IntegrationTests/Fixtures` |
| P1-26 | Stack smoke test qua aggregate `/health` | `scripts/smoke.ps1`, `scripts/smoke.sh` |
| P1-27 | Setup, run, test, migration, secret và troubleshooting | root `README.md` |

## Exit evidence

| Gate | Kết quả kiểm định ngày 12/09/2026 | Trạng thái |
|---|---|---|
| Backend restore/build | Restore bằng lockfile thành công; Release build `0 warning / 0 error` | Đạt |
| Backend test/format/security | `5/5` test pass; `dotnet format` không có thay đổi; NuGet không phát hiện package dễ tổn thương | Đạt |
| Frontend | Prettier, ESLint, TypeScript strict và production build đều pass; build sinh đủ 13 route | Đạt |
| Repository separation | Backend build/test và frontend build được chạy lại sau khi chuyển vào `backend/` và `frontend/`; không còn tham chiếu tới layout `apps/web` cũ | Đạt |
| Frontend dependency security | `npm audit --audit-level=high --omit=dev`: `0 vulnerabilities` | Đạt |
| Container build/config | API và web multi-stage build thành công; `docker compose config --quiet` pass | Đạt |
| Local stack | PostgreSQL, Redis, MinIO, MailHog, Seq, API, web và Nginx healthy; MinIO bootstrap exit code `0` | Đạt |
| Smoke/correlation | `scripts/smoke.ps1` pass; `/health/live`, `/health/ready`, `/health` và `/` trả HTTP 200; response API có `X-Correlation-ID` | Đạt |
| Clean CI runner | Workflow đã tạo nhưng repository hiện chưa có source-control remote/CI runner để chạy thực tế | Chờ bên ngoài |
| Independent onboarding | Cần một developer khác đo quy trình README dưới 5 phút sau khi đã có SDK/Docker và cache image/package | Chờ bên ngoài |

Phase 1 đã hoàn tất toàn bộ phần có thể triển khai và kiểm định cục bộ. Chỉ chuyển trạng thái cuối sang `Accepted / Closed` sau khi hai gate bên ngoài ở trên có bằng chứng; việc này không làm phát sinh thêm mã nghiệp vụ Phase 1.
