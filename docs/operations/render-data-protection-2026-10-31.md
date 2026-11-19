# Render: Data Protection sau cập nhật MinIO endpoint

Cập nhật05/11/2026: **bản sửa đã chạy thật**, không còn ở bước chờ triển khai. Main/Render`41f619bd1d91079dbd37ee73c10c44fbbc5a6298`, deploy`dep-dblhtudckfvc73fircbg` live04/11. `PersistKeysToDatabase=true`, certificate từ private secret hiện có;1key mã hóa trong Neon giữ fingerprint qua restart. Chỉ đổi2biến Data Protection và MinIO endpoint, mọi biến khác bảo toàn. Không chạy lại migration keyring đã áp hoặc tạo certificate mới. Source G06/schema14 vẫn là candidate local, Neon/main hotfix schema5. Endpoint hiện tại`https://ron-christmas-browsers-midwest.trycloudflare.com` phụ thuộc PC/cloudflared; xem [checkpoint hiện tại](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-05112026). Nội dung31/10 dưới đây là lịch sử, không phải danh sách việc chưa làm.

Cập nhật sau ủy quyền31/10/2026: đã áp riêng bảng/ledger keyring lên Neon, giữ nguyên5migration nghiệp vụ và28tables/2042rows cũ. Hotfix dựa live đã kiểm/push; chưa đổi Data Protection Environment hoặc deploy source lên Render. Xem [checkpoint mới](../specs/srs-completion-phases/reports/package-G06-result.md#authorized-rollout-31102026) và [trình tự áp hotfix](g06-authorized-rollout-2026-10-31.md). Các phần chuẩn bị trước đó dưới đây là lịch sử; không chạy lại migration đã áp.

## Kết quả thực tế

Người dùng cung cấp ảnh log Save and deploy: service Live; Redis/PostgreSQL có timeout ở đầu startup, sau đó `/health/ready` trả200 nhiều lần. Kiểm HTTP sau đó: release/ready/health/list/detail và3media đều200; release vẫn trả commit `e6107839283767594c598f84e70dbf139a626655`,3SHA256 media khớp đường đọc tunnel đã kiểm. Chưa đọc trực tiếp Render Environment; kết quả xác nhận hành vi đọc hiện tại, không chứng nhận schema G06 đã deploy hay mọi luồng ghi/reset password.

`HEAD /` hoặc `GET /` trả404 không phải bằng chứng service chết: endpoint API nghiệp vụ dùng `/api/v1/...`, health dùng `/health/ready`.

## Lỗi còn lại

Log thực có `UnauthorizedAccessException: Access to the path '/var/data' is denied` khi Data Protection đọc/tạo keyring tại `/var/data/data-protection`. Ứng dụng vẫn khởi động; không lấy trạng thái Live/healthy để kết luận keyring hoạt động. Token provider của Identity dùng Data Protection, nên cần xử lý trước khi nghiệm thu các luồng phụ thuộc keyring.

Dockerfile tại đúng commit live đã có:

```dockerfile
RUN mkdir -p /app/logs /app/data-protection && chown "$APP_UID" /app/logs /app/data-protection
USER $APP_UID
```

Source nhận `DataProtection__KeyPath` và gọi `PersistKeysToFileSystem`. Probe read-only trên image G06 local cùng cách chuẩn bị thư mục xác nhận non-root ghi được `/app/data-protection`, không ghi được `/var`; đây không phải kiểm quyền hoặc mount trên Render thật.

## Chọn cách sửa theo disk thực tế

- **Đã có disk:** xác nhận mount path và quyền UID/GID đang chạy trước khi đổi cấu hình. Nếu mount `/var/data`, kiểm quyền thư mục con và keyring cũ, giữ nguyên keys cần dùng. Không chown/xóa/move keyring hoặc chuyển khỏi disk chỉ để hết log. Lệnh đọc trong Shell nếu sẵn có: `id; ls -ld /var/data /var/data/data-protection /app/data-protection`. Không gửi nội dung XML keys/secret.
- **Chưa có disk / dùng Free:** `/app/data-protection` là đường dẫn image đã chuẩn bị để giải quyết quyền ghi, nhưng filesystem ephemeral không đủ yêu cầu giữ keys qua deploy/restart. Chỉ đổi path không đóng D9-07; không coi đây là phương án persistence hoàn chỉnh. Bản sửa database bền vững đã được chuẩn bị và kiểm local ở phần dưới; chưa áp thật.
- **Phương án tận dụng Neon:** ASP.NET Core hỗ trợ EF Core lưu keyring vào database. Nếu chọn, cần package/model/migration, kiểm bảo vệ keys/quyền đọc và restart/multi-instance; chuẩn bị và kiểm trên clone trước. Provider đã được bổ sung và kiểm local theo phần dưới, chưa được phép migrate Neon thật. Không suy PostgreSQL200 thành keyring đã được lưu trong DB.

Với thông tin Free/no disk đã xác nhận, phương án đã chuẩn bị là dùng Neon làm storage keyring, không mua disk. Render chỉ giữ dữ liệu trong mount path của persistent disk, và disk yêu cầu service trả phí; không tự bật gói trả phí. Tham khảo [Render Disks](https://render.com/docs/disks) và [Microsoft Data Protection configuration](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0).

Checkpoint/evidence và trạng thái các task: [report G06](../specs/srs-completion-phases/reports/package-G06-result.md#data-protection-free-31102026).

## Bản sửa dành cho Free đã chuẩn bị

- `DataProtectionKeyDbContext` dùng cùng connection string Database đang có; migration/history riêng `__DataProtectionMigrations`. Migration `20261031134534_PersistDataProtectionKeys` tạo bảng `DataProtectionKeys`. Không thay model, snapshot hoặc ledger migration của `AppDbContext`.
- Chế độ database bật bằng `DataProtection__PersistKeysToDatabase=true`, mặc địnhfalse để giữ deployment filesystem hiện tại. Khi bật, KeyPath cũ được bỏ qua và certificate có private RSA key là bắt buộc; không fallback lưu plaintext vào database.
- Keys được mã hóa bằng certificate qua provider X.509 chuẩn của ASP.NET Core; certificate load với EphemeralKeySet, không cần ghi keystore vào filesystem container. Khi bật chế độ DB, startup kiểm table, protect/unprotect và kiểm đã persist key; thiếu migration sẽ fail startup trước khi báo ready.
- Certificate private đi qua `DataProtection__CertificateBase64` trong Render Environment/secret configuration. Không gửi giá trị qua chat, không commit/copy vào DB backup. Một certificate phải giữ ổn định trên mọi replica/redeploy cần đọc cùng keyring.

### Secret được tạo local

Chạy trên Windows với PowerShell7:

```powershell
pwsh -NoProfile -File scripts/new-data-protection-certificate.ps1
```

Script tạo RSA3072/PFX base64 trong `tmp/data-protection/certificate.base64`, chỉ cấp ACL cho user hiện tại, yêu cầu Git-ignore, không in secret và từ chối ghi đè. Metadata/fingerprint ở `certificate-metadata.json`. Certificate này dùng mã hóa keyring, không phải certificate HTTPS. Đã tạo một bản candidate local; chạy lại không tạo certificate khác âm thầm. Giữ private file/backup trong secret management ngoài PostgreSQL và kiểm thời hạn trong metadata. Không tự thay certificate khi hệ thống đã có keys; mất/thay private key có thể khiến dữ liệu được bảo vệ trước đó không giải mã được.

### Trình tự áp dụng sau khi có quyền

1. Review diff/source fingerprint/image/SQL và target. Bản `e610783` hiện tại chưa có provider mới; chỉ Save and deploy lại build cũ hoặc thêm flag sẽ không cài được bản sửa.
2. Có backup DB và backup certificate độc lập, xác nhận role/mount/quyền cần thiết. Nếu dùng role migration khác role app, cấp đúng quyền SELECT/INSERT và sequence cho storage keys theo phạm vi được duyệt; không cấp quyền rộng tùy ý.
3. Inject ConnectionStrings:Database qua environment/secret của migration runner, không đưa URL/password vào chat hoặc command arguments. Chạy riêng keyring context; tắt App startup migration/background jobs trong runner để không khởi động nghiệp vụ.

```powershell
# cwd: backend; chỉ chạy sau khi được phép trên target đã xác nhận
$env:Database__ApplyMigrationsOnStartup='false'
$env:BackgroundJobs__Enabled='false'
$env:DataProtection__PersistKeysToDatabase='false'
dotnet tool run dotnet-ef database update --context DataProtectionKeyDbContext --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.Api --configuration Release --no-build
```

Chỉ đặt các biến này trong migration runner; không tự sửa Environment của service đang chạy. SQL idempotent đã được xuất để review trong evidence G06. Không bỏ `--context` vì source nay có hai DbContext. Rollback key migration sẽ từ chối drop khi bảng đã có keys; không xóa keys để vượt guard.

4. Deploy binary mới đúng fingerprint với flagDBtrue và certificate secret đã chuẩn bị. Mọi instance dùng cùng app name `CulinaryBlog` và certificate. **Candidate G06 đầy đủ vẫn có13migration nghiệp vụ so với5 trong snapshot Neon cũ; migration keyring riêng không thay thế bước review/áp các migration nghiệp vụ của release plan.** Không dùng một bảng keyring để bỏ qua dependency này.
5. Kiểm release/ready, logs, reset trên tài khoản thử/sink được phép và khả năng dùng token sau recreate/redeploy. Không gửi test mail tới người khác khi chưa có quyền.

Default filesystem vẫn dùng `DataProtection__KeyPath` như cũ. Không chuyển từ DB về filesystem hoặc đổi certificate trong rollback một cách tự động; phải giữ keyring/certificate đang bảo vệ dữ liệu hiện hữu. Chưa áp migration/config/deploy lên Render/Neon thật nên D9-07 vẫn mở.

## Gates đã đo trên candidate

Strict build0warnings/0errors, format verify0;129unit+3architecture+242integration tests,0fail/0skip. API image `sha256:378b9ff6130892e6a0e63fb2a72178a9b2a0fbefcc68f19995de48497715f397` build/scan0High/Critical. Lab Linux non-root xác nhận fail startup khi thiếu migration, SQL áp lại an toàn, recreate giữ encrypted keyring; lab2replica dùng cùng keyring. Backup/restore/reset token là integration test trên PostgreSQL synthetic, chưa phải Neon/Render nghiệm thu. Bằng chứng và checkpoint mới ở [report](../specs/srs-completion-phases/reports/package-G06-result.md#data-protection-free-31102026).
