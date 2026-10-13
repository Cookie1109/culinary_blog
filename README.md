# Culinary Blog

Ứng dụng web chia sẻ và khám phá công thức nấu ăn, xây dựng theo [SRS v1.0.0](docs/specs/SRS_Culinary_Blog_v1.0.0.pdf). Dự án gồm frontend Next.js và backend ASP.NET Core theo Clean Architecture, CQRS.

**Trạng thái:** Đã triển khai và kiểm thử các phần chính ở môi trường local. Chưa được phê duyệt triển khai production; xem [báo cáo Phase 8](docs/specs/phase-8/README.md).

## Chức năng chính

- Đăng ký, đăng nhập, quản lý phiên và hồ sơ cá nhân.
- Tạo, chỉnh sửa, xuất bản và quản lý công thức với nguyên liệu, các bước, ảnh và dinh dưỡng.
- Tìm kiếm tiếng Việt, lọc, sắp xếp và phân trang công thức.
- Quản trị danh mục, phân quyền Author/Admin và kiểm soát quyền sở hữu công thức.
- Xử lý ảnh nền, sinh sitemap, tối ưu SEO và giám sát hệ thống.

## Công nghệ

| Thành phần             | Công nghệ                                                               |
| ---------------------- | ----------------------------------------------------------------------- |
| Frontend               | Next.js 15, React 19, TypeScript, Tailwind CSS, TanStack Query, Auth.js |
| Backend                | .NET 10, EF Core, MediatR, FluentValidation                             |
| Dữ liệu                | PostgreSQL 16, Redis 7, MinIO                                           |
| Tác vụ nền và giám sát | Hangfire, Serilog, Seq, OpenTelemetry                                   |
| Hạ tầng và kiểm thử    | Docker Compose, Nginx, xUnit, Testcontainers, GitHub Actions            |

## Chạy với Docker

Yêu cầu Docker Desktop đang chạy và các cổng trong [compose.yaml](compose.yaml) còn trống. Thực hiện tại thư mục gốc:

```powershell
# Tạo cấu hình local nếu chưa có
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
docker compose up --detach --build --wait
```

| Dịch vụ           | Địa chỉ                      |
| ----------------- | ---------------------------- |
| Ứng dụng          | http://localhost:8080        |
| Tài liệu API      | http://localhost:8080/scalar |
| Kiểm tra hệ thống | http://localhost:8080/health |
| MinIO Console     | http://localhost:9001        |
| Email thử nghiệm  | http://localhost:8025        |
| Log hệ thống      | http://localhost:5341        |

Dừng hệ thống bằng `docker compose down`; dữ liệu trong named volumes được giữ lại.

### Phát triển giao diện

Sau khi khởi tạo stack, dùng cấu hình sau để Next.js tự cập nhật khi sửa code:

```powershell
docker compose -f compose.yaml -f compose.dev.yaml up --detach --no-build --wait
```

Truy cập tại `http://localhost:8080`. Dừng môi trường này bằng `docker compose -f compose.yaml -f compose.dev.yaml down`.

## Cấu hình

- Cấu hình local nằm trong `.env`, tạo từ [.env.example](.env.example). Không commit secrets.
- Đăng nhập Google cần `AUTH_GOOGLE_ID` và `AUTH_GOOGLE_SECRET` tương ứng với cấu hình Google OAuth.
- `SITE_URL` là địa chỉ công khai; `REVALIDATION_SECRET` bảo vệ callback cập nhật nội dung giữa backend và frontend.
- Admin seed mặc định tắt. Khi cần, cấu hình `ADMIN_SEED_ENABLED`, `ADMIN_EMAIL` và `ADMIN_PASSWORD`.
- Docker local tự áp migration khi API khởi động. Production áp migration theo [quy trình phát hành](docs/release/v1.0.0-release-notes.md).

## Tài liệu API

API sử dụng tiền tố `/api/v1`. [Scalar](http://localhost:8080/scalar) và [OpenAPI JSON](http://localhost:8080/openapi/v1.json) được sinh từ code, chỉ mở trong môi trường Development/Testing.

Để thử API cần xác thực, gọi `/api/v1/auth/login` rồi nhập `data.accessToken` vào mục Bearer của Scalar. Khi sửa công thức hoặc các thành phần của công thức, gửi header `If-Match` với ETag hiện tại, ví dụ `"1"`. Upload ảnh sử dụng multipart form với trường `file`.

[OpenAPI contract](docs/specs/phase-0/openapi.v1.yaml) dùng để sinh TypeScript types bằng `npm --prefix frontend run generate:api`.

## Kiểm tra chất lượng

Để chạy kiểm tra trên máy, cần .NET SDK `10.0.401`, Node.js `22.23.3` và Docker Desktop:

```powershell
./scripts/verify.ps1
./scripts/smoke.ps1
```

`verify.ps1` kiểm tra backend, frontend và cấu hình Compose. `smoke.ps1` build, khởi động stack và kiểm tra health, trang chủ cùng CSP.

## Cấu trúc dự án

```text
frontend/          # Next.js, giao diện và API client
backend/           # ASP.NET Core, nghiệp vụ và kiểm thử
infra/             # Nginx, giám sát và cấu hình triển khai
scripts/           # Kiểm tra, vận hành và triển khai
docs/              # Đặc tả, kế hoạch và báo cáo
.github/workflows/ # CI và phát hành image
```

## Tài liệu liên quan

- [Baseline và quyết định kiến trúc](docs/specs/phase-0/README.md)
- [Kế hoạch triển khai](docs/specs/Culinary_Blog_Project_Plan.md)
- [Kết quả kiểm thử và trạng thái phát hành](docs/specs/phase-8/README.md)
- [Hướng dẫn vận hành](docs/operations/runbook.md)
- [Cấu hình môi trường và secrets](docs/release/environment-and-secrets-checklist.md)
- [Lịch sử thay đổi](CHANGELOG.md)

Nếu stack chưa sẵn sàng, kiểm tra `docker compose ps` và `docker compose logs --tail 100 api web nginx`. Khi cập nhật backend, build lại image bằng `docker compose up --detach --build --wait api`; khi sửa Nginx, kiểm tra và nạp lại bằng `docker compose exec nginx nginx -t` và `docker compose exec nginx nginx -s reload`.
