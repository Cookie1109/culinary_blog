# Culinary Blog

Ứng dụng web chia sẻ và khám phá công thức nấu ăn, xây dựng theo [SRS v1.0.0](docs/specs/SRS_Culinary_Blog_v1.0.0.pdf). Dự án gồm frontend Next.js và backend ASP.NET Core theo Clean Architecture, CQRS.

**Trạng thái:** Đã triển khai và kiểm thử các phần chính ở môi trường local. Chưa được phê duyệt triển khai production; xem [báo cáo Phase 8](docs/specs/phase-8/README.md).

## Chức năng chính

- Đăng ký, đăng nhập, quản lý phiên và hồ sơ cá nhân.
- Đổi mật khẩu trong hồ sơ và khôi phục mật khẩu qua email; liên kết đặt lại dùng một lần, hết hạn sau 30 phút.
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

Yêu cầu Git, Docker Desktop (Linux containers) hoặc Docker Engine kèm Compose, kết nối Internet để tải image/package và các cổng trong [compose.yaml](compose.yaml) còn trống. Khi chạy hoàn toàn bằng Docker, máy không cần cài sẵn Node.js hoặc .NET SDK.

Lấy mã nguồn trên máy mới:

```powershell
git clone https://github.com/Cookie1109/culinary_blog.git
Set-Location culinary_blog
```

Thực hiện tại thư mục gốc:

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

Lần đầu, ứng dụng tự tạo schema, role và dữ liệu mẫu kèm ảnh. Admin không có tài khoản/mật khẩu mặc định: nếu cần quản trị, cấu hình `ADMIN_SEED_ENABLED=true`, `ADMIN_EMAIL` và `ADMIN_PASSWORD` trong `.env` rồi chạy lại lệnh trên. Đăng nhập Google cần cấu hình OAuth riêng; có thể sử dụng đăng nhập email khi chưa bật Google.

### Cập nhật mã nguồn và Docker local

Sao lưu PostgreSQL và MinIO trước khi cập nhật dữ liệu quan trọng; xem [hướng dẫn vận hành](docs/operations/runbook.md#10-backup-và-restore-drill). Sau đó, tại checkout đang sử dụng:

```powershell
git pull --ff-only
docker compose up --detach --build --wait
docker compose ps
```

Giữ nguyên `.env` và tên Compose project. Không dùng `docker compose down --volumes` hoặc xóa named volumes khi cập nhật. Lệnh `up --build` build lại mã mới và thay container khi cần, đồng thời dùng lại các volume PostgreSQL, MinIO và nhật ký. Không cần gỡ Docker Desktop hay xóa database để cập nhật ứng dụng.

Initializer chỉ bổ sung dữ liệu mẫu còn thiếu, không xóa công thức/danh mục do người dùng tạo, không ghi đè nội dung mẫu đã sửa và không khôi phục bản ghi đã xóa mềm. Migration local vẫn tự chạy khi API khởi động; quy trình production riêng ở mục Cấu hình.

Git clone không sao chép `.env`, database, ảnh người dùng tải lên hoặc lịch sử đang lưu trên máy cũ. Nếu cần chuyển cả dữ liệu sang máy khác, phải sao lưu/khôi phục PostgreSQL và MinIO; khôi phục thêm volume nhật ký khi cần giữ lịch sử thao tác.

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
- Quên mật khẩu: chọn **Quên mật khẩu?** trên trang đăng nhập. Docker local gửi email vào [MailHog](http://localhost:8025); mở email và dùng liên kết để đặt mật khẩu mới. Khi chạy API trực tiếp, bật `Email:Enabled` và cấu hình SMTP. Production hỗ trợ `SMTP_ENABLE_SSL`, `SMTP_USERNAME`, `SMTP_PASSWORD` trong cấu hình release.
- Đổi hoặc đặt lại mật khẩu thu hồi mọi phiên đăng nhập. Bản cập nhật này cũng yêu cầu các phiên JWT cũ đăng nhập lại một lần. Volume `api-data-protection` lưu khóa bảo vệ token đặt lại mật khẩu; giữ volume này khi khởi động lại hoặc cập nhật container.
- Docker local tự áp migration khi API khởi động. Production áp migration theo [quy trình phát hành](docs/release/v1.0.0-release-notes.md).

## Tài liệu API

API sử dụng tiền tố `/api/v1`. [Scalar](http://localhost:8080/scalar) và [OpenAPI JSON](http://localhost:8080/openapi/v1.json) được sinh từ code, chỉ mở trong môi trường Development/Testing.

Để thử API cần xác thực, gọi `/api/v1/auth/login` rồi nhập `data.accessToken` vào mục Bearer của Scalar. Khi sửa công thức hoặc các thành phần của công thức, gửi header `If-Match` với ETag hiện tại, ví dụ `"1"`. Upload ảnh sử dụng multipart form với trường `file`.

[OpenAPI contract](docs/specs/phase-0/openapi.v1.yaml) dùng để sinh TypeScript types bằng `npm --prefix frontend run generate:api`.

## Kiểm tra chất lượng

Để chạy kiểm tra trên máy, cần .NET SDK `10.0.402`, Node.js `22.23.3` và Docker Desktop:

```powershell
./scripts/verify.ps1
./scripts/smoke.ps1
```

`verify.ps1` kiểm tra backend, frontend và cấu hình Compose. `smoke.ps1` build, khởi động stack và kiểm tra health, trang chủ cùng CSP.

`verify.ps1` dừng ngay khi một lệnh thất bại. Sau khi clone và cài dependencies, bật kiểm tra định dạng trước mỗi lần push:

```powershell
git config --local core.hooksPath .githooks
```

Trên macOS/Linux, chạy thêm `chmod +x .githooks/pre-push` để Git có thể thực thi hook.

Nếu kiểm tra định dạng báo lỗi, chạy hai lệnh sau tại thư mục gốc rồi xem lại diff và commit phần sửa:

```powershell
dotnet format backend/CulinaryBlog.slnx --no-restore
npm --prefix frontend run format:write
```

Hook kiểm tra code trong thư mục làm việc. Chạy `./scripts/verify.ps1` để kiểm tra đầy đủ trước khi push.

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
