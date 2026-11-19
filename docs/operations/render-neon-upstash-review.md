# Rà soát Neon/Upstash và cấu hình Render

## Checkpoint ngày27/10/2026 — main đã deploy, MinIO cần đổi URL

Theo phê duyệt cập nhật main và auto-deploy, đã fast-forward đúng commit `2ed7f4258af1dd651edd12bebe4d62d4b2a2c047` lên main; hook vẫn chạy. Render trả đúng SHA/provider/service/instance ở `/api/v1/release`; `/health/live`, `/health/ready`, categories và recipes đều HTTP200. Recipes có100 bản ghi. Neon kiểm lại sau startup lúc23:57 ngày26/10 vẫn khớp archive:28 bảng/2.042 row, schema/fingerprint/FK/index/sequence đều đạt. Không chạy migration/seed hoặc sửa cloud Environment. Nguồn deploy giữ5 migration; chênh lệch5/11 dưới đây chỉ thuộc working tree chưa commit.

CI backend và compose/container scans đạt; frontend format/lint/typecheck/build/tests đạt nhưng npm audit thất bại (1 moderate,4 high). Filesystem Trivy thất bại với zod4.6.2, scanner ghi bản sửa4.6.6. Không sửa dependencies hoặc bỏ gate trong commit metadata. Chưa thể coi toàn bộ CI hay production SRS đạt.

Ảnh log mới là lỗi DNS của Quick Tunnel MinIO cũ. Đã khởi động lại đúng gateway có sẵn và cloudflared đã cài; không đổi container MinIO/IAM/bucket/credentials/volume. Tunnel mới đọc ảnh có cùng SHA256/bytes với local; anonymous/Admin/Console trả403. Người dùng cần sửa **duy nhất `ObjectStorage__Endpoint`** trong Render Environment thành URL ở `render-metadata-deployment-result.json` rồi áp dụng deployment. Chưa kiểm Render→tunnel mới vì chưa đổi cloud Environment. File riêng `tmp/minio-tunnel/render-minio.env` đã có URL mới. Máy Windows, Docker và tunnel phải tiếp tục chạy.

Evidence hiện tại: [render-metadata-deployment-result.json](render-metadata-deployment-result.json). Các mục bên dưới ghi lại chẩn đoán và checkpoint trước deployment, không thay thế checkpoint này.

## Cập nhật sau phê duyệt metadata Render

Đã đối chiếu PDF và ghi [ADR-011](../specs/phase-0/adr/ADR-011-release-metadata-providers.md). Code hỗ trợ opt-in `Release__Provider=Render`, lấy SHA/service/instance từ Render; đường Docker mặc định và immutable release gates giữ nguyên. Metadata bổ sung không chứng nhận môi trường hybrid đạt §6.5 production Compose của SRS. Đã tách commit `2ed7f4258af1dd651edd12bebe4d62d4b2a2c047` từ source GitHub7ed7a58, kiểm27 regression và hai format gate rồi push nhánh `fix/render-release-metadata`; nhánh này giữ5 migration/dependencies của source GitHub. Chưa cập nhật main, chưa đổi Render/deploy. Không chỉ thêm biến trên Render rồi cho rằng code cũ tự hỗ trợ. Xem `release-metadata-push-result.json`.

## Cập nhật log Render lúc22:54 ngày26/10

Ảnh người dùng cung cấp xác nhận API thoát với exit1 tại ProductionConfigurationGuard/Program startup. Lỗi hiện tại chỉ liệt kê3 trường release không hợp lệ: `Release:CommitSha`, `Release:ApiImageDigest`, `Release:WebImageDigest`. Log này chưa chứng minh kết nối database/Redis từ Render thành công, vì guard chạy trước AddInfrastructure và host startup.

Đã đối chiếu `.github/workflows/release-images.yml`: workflow chỉ chạy qua workflow_dispatch, tạo api.json/web.json/release-bundle.json chứa commitSha và image digest thật. Kiểm GitHub Actions bằng kết nối GitHub chỉ đọc trả0 workflow_dispatch run trong repository tại lượt kiểm; chưa có bundle từ workflow này để điền3 biến. `artifacts/release-template` là template với aaaa/bbbb/cccc, không dùng cho deploy thật. Không tạo hash giả, không chạy/publish workflow hoặc thay cloud trong lượt chẩn đoán.

Cần chốt cách nhận dạng release phù hợp mô hình Render build từ repo + frontend Vercel, hoặc cung cấp/publish và triển khai đúng immutable images của quy trình release hiện có. Thay đổi mô hình metadata cần được review để giữ các gates đã có; chưa tự bỏ guard hoặc đổi sang Development. Chênh lệch schema5/11 vẫn là vấn đề riêng, chưa áp migration.

Ngày26/10/2026, Asia/Bangkok. **Kết nối Neon/Upstash từ máy kiểm đã đạt; dữ liệu Neon khớp toàn bộ snapshot đã chuyển. Source hiện tại chưa khớp schema Neon và chưa sẵn sàng redeploy chỉ bằng việc nhập env.** Chưa thay Environment hoặc deploy Render, chưa chạy EF migration/seed, không sửa dữ liệu PostgreSQL/Redis hoặc dịch vụ MinIO.

## Kết quả có bằng chứng

| Nội dung | Kết quả |
|---|---|
| Neon so backup đã duyệt |28 bảng/2.042 row, fingerprint từng bảng đều khớp;28 inventory bảng và định nghĩa cột/type/default/nullability khớp. |
| Dữ liệu chính |11 tài khoản,20 category,100 recipe,100 ảnh **metadata**,1.000 ingredient,500 step cùng tokens/outbox/Hangfire. Không chuyển file ảnh MinIO trong nhiệm vụ này. |
| Ràng buộc |14 FK không orphan,78 index valid/định nghĩa khớp,11 sequence an toàn/khớp archive rehearsal;61 logical constraints.132 NOT NULL catalog constraints mới củaPG18 đã đối chiếu với132 cột NOT NULL. |
| Neon client thực |Npgsql kết nối neondb PostgreSQL18.6 bằng `SSL Mode=VerifyFull`, `Channel Binding=Require`; transaction/session READ ONLY, rollback sau kiểm. `Database.CanConnectAsync()` trả true. |
| Upstash client thực |AUTH/PING mới nhất đạt qua StackExchange.Redis, TLS bật, không bỏ certificate validation, không ghi/xóa key. |
| Upstash compatibility đã có |16/16 kiểm thực tế đạt: TTL, distributed cache, Lua hash/sorted-set, output cache/tag eviction, RedLock acquire/exclusion/release. Dùng lại evidence còn hợp lệ; không chạy lại phép ghi/xóa demo trong lượt rà soát. |
| Thư viện |StackExchange.Redis3.2.0; RedisCache/OutputCache10.0.13; RedLock.net2.3.2; Npgsql EF provider10.0.3; Hangfire.PostgreSql1.21.1. AssemblyVersion Redis3.0.0.0 trong log không phải NuGet package version. |
| Kiểm HTTP Render |GET `/api/v1/release`, `/health/live`, `/health/ready` tại URL người dùng cấp đều timeout từ máy kiểm trong35s. Không kết luận backend ngừng hoạt động; chưa chứng minh release hay kết nối thực tế từ Render. |

Backup là snapshot lúc **16:23:29 ngày26/10**, không bao gồm ghi mới local sau thời điểm đó. Docker hiện chưa chạy trong checkpoint trước; lượt này không tự khởi động stack để so nguồn live. “Đủ dữ liệu” ở đây là đủ snapshot đã duyệt, không phải bảo đảm đồng bộ mọi ghi phát sinh sau snapshot. Local và volume vẫn nguyên.

Khác biệt đã ghi nhận từ lần rehearsal: local PostgreSQL16.10 dùng libc `en_US.utf8`, Neon18.6 dùng builtin `C.UTF-8`; thứ tự sắp xếp chuỗi có thể khác. Không tự đổi collation hoặc dữ liệu để xóa khác biệt này.

## Chênh lệch schema phải xử lý riêng

EF kiểm bằng đúng model hiện tại phát hiện **5 migration đã áp dụng/11 migration trong assembly**. Sáu migration pending:

```text
20261023061828_WelcomeEmailHangfire
20261025050000_PublicCacheGeneration
20261025053000_SharedRateLimitFallback
20261025063821_ContentSlugsAndImageUrls
20261025064611_SrsIdentityAndDataTypes
20261025065808_SrsDefaultsAndConcurrency
```

Đã kiểm bằng truy vấn đọc có savepoint/rollback:

- Identity ID trong Neon là `uuid`; model mới dùng string. Projection `Users.Id` và `Recipes.AuthorId` ném `InvalidCastException`.
- `RecipeSlugs` chưa có; query model tương ứng trả SQLSTATE42P01. Projection Category.Id vẫn đọc được.
- `PublicCacheState`, `SharedRateLimitState` và hàm `ReserveRateLimit(text,text,integer,integer)` chưa có. Cache generation và rate limiter của source mới phụ thuộc các object này.
- Health database chỉ kiểm khả năng kết nối, **có thể Healthy dù model không khớp schema**. Không dùng `/health/ready` để thay cho kiểm migration/API.

Không sửa model về kiểu cũ hoặc tự áp sáu migration trong nhiệm vụ cấu hình. Trước redeploy source mới cần kế hoạch nâng schema được duyệt riêng, hoặc xác minh chính xác phiên bản app phù hợp schema hiện có. Repository HEAD có5 migration; chưa lấy được commit thực tế của Render vì GET release timeout. Không suy ra bản deploy từ HEAD hoặc từ ảnh deploy cũ.

## Migration, seed và Hangfire

`Program.cs` chỉ gọi `DatabaseInitializer.InitializeAsync` khi `Database:ApplyMigrationsOnStartup=true`. Initializer gọi cả `MigrateAsync`, tạo role, seed authors/categories/recipes/media, xóa cache liên quan rồi seed admin. **`AdminSeed__Enabled=false` chỉ tắt phần admin; không tự tắt các seed còn lại nếu initializer chạy.** Vì vậy trên Render cần cả `Database__ApplyMigrationsOnStartup=false` và `AdminSeed__Enabled=false`.

`BackgroundJobs__Enabled=false` tắt worker, dashboard và các lệnh đăng ký/enqueue recurring jobs lúc startup. Trước sửa, Hangfire storage vẫn được đăng ký với `PrepareSchemaIfNecessary=true` mặc định, có thể tự chuẩn bị schema khi storage được resolve.

Sửa duy nhất trong code ứng dụng: truyền `PostgreSqlStorageOptions { PrepareSchemaIfNecessary = backgroundJobsEnabled }` vào registration hiện có. Khi jobs tắt, storage không tự chuẩn bị schema. Development mặc định jobs bật nên giữ hành vi trước đây. Không thêm flag hoặc ngoại lệ production, không đổi nghiệp vụ job/MinIO.

Regression `StartupStorageConfigurationTests.DisabledJobsResolveStorageWithoutConnectingToPostgresForSchemaPreparation` đạt: resolve JobStorage/IBackgroundJobClient với địa chỉ PostgreSQL loopback không hoạt động trong giới hạn5s; không sử dụng database thật. Build probe và test không có warning/error. Đây là kiểm riêng cấu hình tắt jobs; chưa nghiệm thu chạy workers trên Neon, không bật workers/seed.

## File import riêng đã chuẩn bị

```text
D:\Culinary_Blog\tmp\upstash\render-neon-upstash.env
```

File nằm trong thư mục ACL riêng, được Git ignore và Docker exclude. Hiện có đúng9 biến dưới đây; credentials thật đã điền từ file riêng đã kiểm, không ghi trong tài liệu/source/log. Không import nguyên `runtime.env` của Upstash hoặc toàn bộ draft để thay các cấu hình khác. Giá trị provider mới cần source mới tương ứng.

| Key trên Render | Value/cách lấy |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080`, phù hợp Dockerfile hiện tại. |
| `AllowedHosts` | `culinary-blog-api.onrender.com`, chỉ hostname, không scheme hoặc dấu `/`. |
| `ConnectionStrings__Database` | Giá trị thật trong file import; dạng Npgsql key=value ở dưới. |
| `ConnectionStrings__Redis` | Giá trị thật trong file import; dạng StackExchange.Redis ở dưới. |
| `Database__ApplyMigrationsOnStartup` | `false` |
| `AdminSeed__Enabled` | `false` |
| `BackgroundJobs__Enabled` | `false` trong giai đoạn kiểm kết nối. |
| `Release__Provider` | `Render`, chỉ có hiệu lực sau khi triển khai bản sửa metadata đã kiểm. |

Dạng giá trị, **không copy nguyên placeholder**:

```text
ConnectionStrings__Database=Host=<direct-neon-host>;Port=5432;Database=neondb;Username=<role>;Password=<secret>;SSL Mode=VerifyFull;Channel Binding=Require;Pooling=true;Include Error Detail=false
ConnectionStrings__Redis=<upstash-host>:6379,user=default,password=<secret>,ssl=True,sslHost=<upstash-host>,abortConnect=False,connectTimeout=5000,syncTimeout=5000,asyncTimeout=5000,connectRetry=1
```

Neon đang dùng Direct endpoint đã kiểm, không tự chuyển sang host `-pooler` hoặc thay username. Không dùng nguyên URI `postgresql://...` làm giá trị Npgsql. Redis dùng TCP/TLS, không dùng hai biến REST JavaScript để thay connection string. Giữ đúng hai dấu gạch dưới trong Key; copy toàn bộ connection string vào một Value, không chỉ password.

`backend/.env` và `tmp/render/backend-render.env.draft` cũng đã cập nhật đúng9 dòng này; mọi byte ngoài các dòng patch giữ nguyên, bao gồm MinIO/JWT/CORS. Đây là draft cloud; .NET không tự nạp file `.env` khi chạy native. `compose.yaml`, `compose.dev.yaml`, appsettings.json và appsettings.Development.json giữ nguyên SHA đầu lượt kiểm. Docker Development vẫn dùng host `postgres:5432`, `redis:6379`, MinIO local; Windows truy cập PostgreSQL Docker qua5433.

### Các điều kiện production khác cần giữ

`ProductionConfigurationGuard` của dự án vẫn yêu cầu các giá trị thật sẵn có: Jwt__SigningKey, ObjectStorage__AccessKey/SecretKey, Sitemap__PublicBaseUrl/CORS HTTPS. Với **Docker mặc định**, ba biến release vẫn bắt buộc:

| Key | Định dạng bắt buộc |
|---|---|
| `Release__CommitSha` |40 ký tự hex thường của **đúng source được deploy**. |
| `Release__ApiImageDigest` |`sha256:` +64 hex của **API artifact thật**. |
| `Release__WebImageDigest` |`sha256:` +64 hex của **frontend artifact thật**. |

Với **Render opt-in**, không cần nhập ba biến Docker trên. Render tự cấp `RENDER=true`, `RENDER_GIT_COMMIT`, `RENDER_SERVICE_ID`, `RENDER_INSTANCE_ID`; không nhập giá trị mô phỏng vào Environment cloud. Guard kiểm các field này; nếu `Release__CommitSha` trống/không khai báo thì dùng SHA nền tảng. Nếu bạn đã nhập SHA cũ thì xóa biến đó hoặc bảo đảm nó trùng revision mới. Hai image digest được trả null ở chế độ Render, không dùng legacy hash làm bằng chứng.

Nếu muốn ghi nhận frontend, nhập tùy chọn `Release__WebDeploymentId=dpl_...` từ deployment Vercel thật; bỏ trống thì trả null, không lấy domain `culinary-blog.vercel.app` làm deployment ID. Backend không cần Vercel token hoặc gọi Vercel lúc startup. Guard chỉ kiểm format, chưa xác thực ID đó qua Vercel.

File9 biến là patch kết nối/metadata, không phải full production config. Metadata nền tảng chưa được kiểm trên runtime Render sau sửa. Không đổi Production sang Development hoặc sửa các biến MinIO/SMTP/JWT/CORS/revalidation. Khả năng startup không được dùng để đóng các gate Docker Compose/SLA/security/UAT còn mở.

## Nhập trên Render, chưa triển khai trong phiên này

1. Vào **Render Dashboard → Web Service `culinary-blog-api` → Environment → Edit**. Đối chiếu các Key đang có, giữ giá trị của dịch vụ khác.
2. Nhanh nhất: **Add from .env**, chọn `D:\Culinary_Blog\tmp\upstash\render-neon-upstash.env`; nếu giao diện yêu cầu nội dung thì mở file riêng và dán tại Render. Đừng dán file vào chat hoặc terminal để hiển thị.
3. Nếu nhập từng biến, tạo/cập nhật9 Key đúng bảng. Với hai connection string, lấy phần sau dấu `=` đầu tiên của đúng dòng làm Value. Những Key đã tồn tại cần cập nhật, tránh tạo bản trùng/để giá trị cũ thắng.
4. Với source mới hỗ trợ Render, đặt Release__Provider=Render; để trống/xóa Release__CommitSha nếu đang khai báo SHA cũ. Không tự nhập RENDER_* hoặc image digest giả. Với code Docker cũ chưa có bản sửa, vẫn cần ba metadata Docker thật; biến provider chưa được hỗ trợ. **Không lưu/deploy source hiện tại để thử may rủi khi schema vẫn5/11.** Chốt phiên bản app/schema trước.
5. Khi bạn chủ động cho phép áp dụng sau các kiểm tra: chọn tùy chọn Save/deploy phù hợp trong Dashboard. Nếu có **Save only**, lưu cấu hình để chuẩn bị và chỉ redeploy khi đã đủ đầu vào. Agent chưa bấm Save hoặc redeploy. Thay env/redeploy không thay được code Hangfire mới nếu bản sửa chưa nằm trong source triển khai.

## Xác minh sau lần redeploy do bạn thực hiện

Các bước dưới đây là quy trình chờ thực hiện, chưa ghi PASS từ Render:

1. **Render → Deploys → Logs**: kiểm process listen cổng8080; không có lỗi ProductionConfigurationGuard, Npgsql authentication/TLS, Redis WRONGPASS/timeout. Không dump toàn bộ environment hoặc bật `Include Error Detail=true` để chẩn đoán.
2. Gọi **`https://culinary-blog-api.onrender.com/api/v1/release`** để đối chiếu source/artifact đang chạy. Nếu timeout, xem trạng thái deploy/log từ Dashboard; timeout từ máy kiểm không đủ xác định nguyên nhân.
3. Gọi **`/health/live`**: HTTP200 + `{"status":"Healthy"}` chỉ chứng minh tiến trình hoạt động. Render Health Check Path nên là `/health/ready` nếu mục tiêu là chờ PostgreSQL và Redis; ứng dụng không có route `/healthz`.
4. Gọi **`/health/ready`**: HTTP200/Healthy chứng minh hai health check PostgreSQL/Redis đều thành công ở runtime đó. HTTP503/Unhealthy thì xem Logs phân biệt hai dịch vụ. Endpoint chỉ trả aggregate status để tránh lộ thông tin; không cung cấp credentials trong URL. Redis health là PING, PostgreSQL health là CanConnect.
5. Sau khi schema/app đã tương thích, gọi **`GET /api/v1/categories`**, **`GET /api/v1/recipes`** và một công thức thật, xác nhận không có42P01/InvalidCast và dữ liệu đúng. GET ở API có thể tiêu thụ rate-limit ledger/cache theo nghiệp vụ; đây không phải kiểm bất biến database.
6. Gọi lại một URL để kiểm cache; chứng minh hit/miss bằng telemetry/log/metrics của môi trường đó, không chỉ suy từ latency. Bài demo mutation/tag invalidation và rate-limit cần kế hoạch thử được phép riêng; không tạo/xóa recipe production để thử kết nối.
7. Kiểm migration history vẫn giữ số phiên bản **đã được duyệt cho release đó**, seed/worker không chạy lại. Giữ3 cờ false trong giai đoạn này. Không tự bật jobs để thử Redis.

Với metadata Render mới, `/api/v1/release` phải trả `provider: "Render"`, `commitSha` trùng Source trong Deploys, `apiServiceId` và `apiInstanceId` từ nền tảng. `apiImageDigest`/`webImageDigest` là null; `webDeploymentId` chỉ có khi người vận hành đã khai báo ID thật. Instance ID giúp truy vết lần chạy và có thể đổi khi restart/scale, không phải deploy ID/image digest. Không nhập manual các biến RENDER_* để làm guard xanh.

`/health` kiểm cả MinIO; không dùng kết quả endpoint này để kết luận riêng Neon/Upstash. Nhiệm vụ không thay hoặc kiểm lại MinIO. Nếu hết timeout nhưng API vẫn lỗi model, xử lý kế hoạch schema/app; không đổi connection string hoặc reset database để che lỗi.

## Files và bằng chứng

- `backend/src/CulinaryBlog.Infrastructure/DependencyInjection.cs`: sửa hẹp option Hangfire theo cờ jobs hiện có.
- `backend/tests/CulinaryBlog.IntegrationTests/StartupStorageConfigurationTests.cs`: regression offline tương ứng.
- `tools/RenderConnectionsProbe`: Npgsql/EF review chỉ đọc, không host/seed/migrate; report ghi riêng ngoài Git.
- `tools/UpstashProbe`: bổ sung `--ping-only`, giữ nguyên probe16 điều kiện và evidence cũ.
- `tools/postgres-neon/prepare_render_connections.py`: tạo import và patch draft riêng; kiểm byte ngoài patch bất biến.
- `render-neon-upstash-review-evidence.json`: bằng chứng không-secret. Backup và credential riêng vẫn giữ nguyên.

Nguồn chính chủ: [Npgsql connection parameters](https://www.npgsql.org/doc/connection-string-parameters.html), [Neon connection](https://neon.com/docs/connect/connect-from-any-app), [Upstash compatibility](https://upstash.com/docs/redis/overall/compatibility), [Hangfire.PostgreSql1.21.1 storage](https://github.com/hangfire-postgres/Hangfire.PostgreSql/blob/1.21.1/src/Hangfire.PostgreSql/PostgreSqlStorage.cs), [Render environment variables](https://render.com/docs/configure-environment-variables).
