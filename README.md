# Culinary Blog

Culinary Blog là nền tảng chia sẻ công thức nấu ăn được xây dựng theo đặc tả SRS v1.0.0. Repository sử dụng mô hình monorepo với hai ứng dụng độc lập: frontend Next.js và backend ASP.NET Core; tài liệu, hạ tầng, Docker Compose và CI được quản lý tập trung tại thư mục gốc.

> **Trạng thái:** Phase 0 đã chốt baseline đặc tả; Phase 1 đã hoàn thành nền tảng. Phase 2 local authentication đã được triển khai và vượt qua các gate không phụ thuộc Docker; PostgreSQL auth journey còn chờ chạy trên Docker/CI.

## Công nghệ chính

| Thành phần | Công nghệ |
|---|---|
| Frontend | Next.js 15, React 19, TypeScript, Tailwind CSS 4, TanStack Query |
| Backend | .NET 10, ASP.NET Core, Entity Framework Core, MediatR, FluentValidation |
| Dữ liệu | PostgreSQL 16, Redis 7, MinIO |
| Quan sát hệ thống | Serilog, Seq, OpenTelemetry, health checks |
| Hạ tầng phát triển | Docker Compose, Nginx, MailHog |
| Chất lượng | xUnit, Testcontainers, ESLint, TypeScript strict, GitHub Actions |

## Cấu trúc repository

```text
culinary-blog/
├── frontend/          # Ứng dụng Next.js và giao diện sản phẩm
├── backend/           # ASP.NET Core solution, source và tests
├── docs/              # SRS, kế hoạch, đặc tả và quyết định kiến trúc
├── infra/             # Cấu hình hạ tầng dùng chung
├── scripts/           # Script kiểm định và smoke test
├── .github/workflows/ # Pipeline CI
└── compose.yaml       # Môi trường phát triển đầy đủ
```

## Bắt đầu nhanh với Docker

### Yêu cầu

- Docker Desktop hỗ trợ Docker Compose v2
- Các cổng `5432`, `6379`, `8080`, `8025`, `9000`, `9001` và `5341` đang khả dụng

### Khởi động

```powershell
Copy-Item .env.example .env
docker compose up --detach --build --wait
```

Sau khi các container healthy, truy cập ứng dụng tại [http://localhost:8080](http://localhost:8080).

| Dịch vụ | Địa chỉ |
|---|---|
| Ứng dụng | [http://localhost:8080](http://localhost:8080) |
| Aggregate health | [http://localhost:8080/health](http://localhost:8080/health) |
| MinIO Console | [http://localhost:9001](http://localhost:9001) |
| MailHog | [http://localhost:8025](http://localhost:8025) |
| Seq | [http://localhost:5341](http://localhost:5341) |

Dừng hệ thống bằng lệnh sau. Named volumes vẫn được giữ để tái sử dụng dữ liệu local.

```powershell
docker compose down
```

## Phát triển frontend và backend riêng biệt

### Yêu cầu

- .NET SDK `10.0.401`
- Node.js `20.19.5`
- Docker Desktop

Phiên bản công cụ được khai báo trong `backend/global.json` và `.mise.toml`. Có thể chạy `mise install` nếu máy đã cài [mise](https://mise.jdx.dev/).

### 1. Khởi động dịch vụ phụ trợ

Tại thư mục gốc của repository:

```powershell
Copy-Item .env.example .env
docker compose up --detach postgres redis minio minio-bootstrap mailhog seq
```

### 2. Chạy backend

```powershell
Set-Location backend
dotnet tool restore
dotnet restore CulinaryBlog.slnx
dotnet run --project src/CulinaryBlog.Api
```

API sử dụng địa chỉ được ASP.NET Core hiển thị trong terminal, mặc định là `http://localhost:5000` khi cấu hình local không bị ghi đè.

Khi chạy backend native lần đầu, áp migration và seed role bằng lệnh `dotnet ef database update` ở phần Database migration. Docker Compose tự áp migration khi API khởi động.

### 3. Chạy frontend

Mở terminal khác tại thư mục gốc:

```powershell
Set-Location frontend
npm ci
npm run generate:api
npm run dev
```

Frontend chạy tại [http://localhost:3000](http://localhost:3000). Khi đi qua Nginx, các request API sử dụng prefix `/api/v1`.

## Kiểm tra chất lượng

Chạy toàn bộ quality gates:

```powershell
./scripts/verify.ps1
./scripts/smoke.ps1
```

Hoặc chạy từng nhóm kiểm tra:

```powershell
Push-Location backend
dotnet build CulinaryBlog.slnx --configuration Release
dotnet test CulinaryBlog.slnx --configuration Release
Pop-Location

npm --prefix frontend run lint
npm --prefix frontend run typecheck
npm --prefix frontend run build

docker compose config --quiet
```

Pipeline tại `.github/workflows/ci.yml` tự động kiểm tra backend, frontend, cấu hình Compose, dependency, container và secrets khi có thay đổi trên repository.

## Cấu hình và bảo mật

- Sao chép `.env.example` thành `.env` để cấu hình môi trường local; không commit tệp `.env`.
- Giá trị mặc định trong `.env.example` chỉ dành cho phát triển cục bộ.
- Với backend chạy native, có thể lưu secret bằng .NET User Secrets, ví dụ:

  ```powershell
  dotnet user-secrets --project backend/src/CulinaryBlog.Api set "ObjectStorage:SecretKey" "<secret>"
  ```

- Môi trường staging và production phải lấy secret từ secret store của nền tảng triển khai.
- JWT signing key phải có ít nhất 32 ký tự ngẫu nhiên; access token sống 15 phút và refresh token sống 7 ngày.
- Admin seed mặc định tắt. Chỉ bật `ADMIN_SEED_ENABLED` khi đồng thời cấp `ADMIN_EMAIL` và `ADMIN_PASSWORD` qua secret an toàn.
- API dừng ngay khi thiếu cấu hình bắt buộc cho PostgreSQL, Redis hoặc object storage.

## Database migration

Phase 2 đã tạo migration Identity/session đầu tiên. Tạo và áp dụng migration từ thư mục `backend/`:

```powershell
dotnet ef migrations add <MigrationName> `
  --project src/CulinaryBlog.Infrastructure `
  --startup-project src/CulinaryBlog.Api `
  --output-dir Persistence/Migrations

dotnet ef database update `
  --project src/CulinaryBlog.Infrastructure `
  --startup-project src/CulinaryBlog.Api
```

Production không tự động chạy migration khi API khởi động. Migration phải là một bước có kiểm soát trong quy trình triển khai.

## Tài liệu dự án

- [SRS v1.0.0](docs/specs/SRS_Culinary_Blog_v1.0.0.pdf) — nguồn yêu cầu cốt lõi của dự án
- [Phân tích và đối chiếu SRS](docs/specs/SRS_Culinary_Blog_Analysis.md)
- [Kế hoạch triển khai](docs/specs/Culinary_Blog_Project_Plan.md)
- [Baseline Phase 0](docs/specs/phase-0/README.md)
- [Báo cáo Phase 1](docs/specs/phase-1/README.md)
- [Báo cáo Phase 2](docs/specs/phase-2/README.md)
- [OpenAPI contract v1](docs/specs/phase-0/openapi.v1.yaml)

## Xử lý sự cố thường gặp

- **Cổng đã được sử dụng:** kiểm tra các cổng được liệt kê trong phần yêu cầu Docker và dừng tiến trình xung đột.
- **Readiness không đạt:** chạy `docker compose ps` và `docker compose logs api postgres redis minio minio-bootstrap`.
- **MinIO chưa sẵn sàng ở lần chạy đầu:** đợi tác vụ `minio-bootstrap` hoàn tất; thao tác tạo bucket có thể chạy lặp an toàn.
- **Sai phiên bản .NET SDK:** kiểm tra bằng `dotnet --version`; repository yêu cầu phiên bản trong `backend/global.json`.
- **Type API không đồng bộ:** chạy lại `npm --prefix frontend run generate:api` sau khi thay đổi OpenAPI contract.

## Nguyên tắc đóng góp

Mọi thay đổi phải tuân thủ SRS, đặc tả Phase 0 và dependency direction đã chốt. Trước khi tạo pull request, hãy chạy `./scripts/verify.ps1`; với thay đổi hạ tầng hoặc luồng khởi động, chạy thêm `./scripts/smoke.ps1`.
