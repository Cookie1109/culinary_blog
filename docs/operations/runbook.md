# Culinary Blog operations runbook

Runbook này bao phủ các tình huống vận hành của Phase 7. Mọi thao tác trên staging/production phải được ghi lại cùng incident ID, operator, thời gian UTC, correlation ID liên quan và kết quả xác minh.

## 1. Điểm quan sát và quyền truy cập

| Công cụ | Local URL | Mục đích | Kiểm soát |
|---|---|---|---|
| Grafana | `http://127.0.0.1:3001` | Request, latency, error, cache, jobs, business events, dependency health | Đăng nhập; mật khẩu lấy từ secret store ngoài local |
| Prometheus | `http://127.0.0.1:9090` | Truy vấn metric và trạng thái rule | Chỉ loopback/VPN/ops network |
| Alertmanager | `http://127.0.0.1:9093` | Alert đang firing/silenced | Chỉ loopback/VPN/ops network |
| Seq | `http://127.0.0.1:5341` | Structured logs và correlation search | Chỉ loopback/VPN/ops network |
| Hangfire | `/jobs` | Retry/inspect background jobs | JWT Admin policy và ops network |

Không proxy Grafana, Prometheus, Alertmanager, Seq hoặc OTLP qua public Nginx. Các endpoint `/health`, `/health/live`, `/health/ready` chỉ trả về aggregate `status`; tên dependency, exception và cấu hình không được trả ra public.

Local Alertmanager chỉ giữ alert trong UI. Trước staging/production, thay receiver `local-operations` bằng kênh đã duyệt và gửi một alert thử để xác nhận đúng owner nhận được.

## 2. Triage chung

1. Ghi nhận alert, thời gian bắt đầu và phạm vi ảnh hưởng.
2. Mở Grafana `Culinary Blog Operations`; xác định request rate, p95, 5xx và dependency đang down.
3. Tìm correlation ID trong Seq. Nếu bắt đầu từ phản hồi API, dùng header `X-Correlation-ID`; nếu bắt đầu từ audit UI, dùng trường `correlationId`.
4. Đối chiếu log API, Hangfire job và dependency trong cùng cửa sổ thời gian.
5. Chỉ thực hiện một biện pháp giảm thiểu tại một thời điểm; ghi lại lệnh và kết quả.
6. Xác minh `/health/ready`, alert đã resolve và critical journey liên quan trước khi đóng incident.

## 3. Alert readiness hoặc dependency unavailable

- `postgresql`: kiểm tra dung lượng, connection limit, lock/query dài và log PostgreSQL. Không restart trước khi loại trừ disk-full hoặc migration đang chạy.
- `redis`: ứng dụng phải tiếp tục phục vụ bằng cache miss. Kiểm tra memory/eviction và connectivity; kỳ vọng cache hit rate giảm nhưng dữ liệu public vẫn đúng.
- `minio`: upload/media job có thể lỗi trong khi readiness vẫn phụ thuộc PostgreSQL và Redis. Tạm dừng retry hàng loạt đến khi object storage ổn định.
- `CulinaryBlogTelemetryMissing`: kiểm tra `otel-collector`, sau đó API. Mất telemetry không đồng nghĩa API down; dùng health probe và log để xác nhận riêng.

Local diagnostics:

```powershell
docker compose ps
docker compose logs --since 15m api otel-collector prometheus postgres redis minio
Invoke-RestMethod http://localhost:8080/health/ready
```

## 4. 5xx spike

1. Trong Grafana, thu hẹp khoảng thời gian và kiểm tra route/status nổi bật.
2. Lấy một correlation ID từ response hoặc Seq, rồi tìm toàn bộ request/CQRS/job chain.
3. Nếu lỗi phụ thuộc, theo mục 3. Nếu lỗi xuất hiện ngay sau deploy/migration, dừng rollout và theo mục 8.
4. Không giảm alert threshold hoặc xóa log để làm alert resolve.

## 5. Failed jobs

1. Mở `/jobs` bằng tài khoản Admin qua ops network; đọc exception và tham số nhưng không sao chép token/secret vào ticket.
2. Sửa dependency hoặc dữ liệu gốc trước khi retry.
3. Retry một job để kiểm chứng tính idempotent; theo dõi `culinary_media_jobs_failed_total` và log correlation/job ID.
4. Chỉ retry batch sau khi job thử thành công. Nếu tiếp tục lỗi, giữ job ở trạng thái failed và chuyển owner của subsystem.

## 6. Object orphan

Job `media-reconciliation` chạy hằng ngày và metric `culinary_media_orphans_detected_total` phân biệt `missing_object` với `missing_record`.

- `missing_object`: kiểm tra version history/backup của bucket; không tạo metadata giả để che lỗi.
- `missing_record`: xác minh object không còn được tham chiếu, chuyển nó sang prefix quarantine có ngày/incident ID, quan sát ít nhất một chu kỳ reconciliation rồi mới xóa theo retention policy.
- Có thể trigger riêng `ImageReconciliationJob` từ Hangfire. Job hiện chỉ phát hiện và log; không tự xóa orphan.

## 7. Cache flush có phạm vi

Ưu tiên invalidation theo nghiệp vụ. Chỉ xóa namespace `culinary:v1:*`; không dùng `FLUSHALL` hoặc `FLUSHDB` trên Redis dùng chung.

Kiểm kê trước:

```powershell
docker compose exec redis redis-cli --scan --pattern 'culinary:v1:*'
```

Sau khi được phê duyệt cho incident, xóa đúng namespace rồi warm các trang public quan trọng:

```powershell
docker compose exec redis sh -c "redis-cli --scan --pattern 'culinary:v1:*' | xargs -r redis-cli UNLINK"
Invoke-WebRequest http://localhost:8080/recipes -UseBasicParsing
```

Xác minh dữ liệu Draft/Archived không xuất hiện và cache hit rate phục hồi.

## 8. Migration và rollback

Trước migration: xác nhận backup, image digest hiện tại, migration đích, thời lượng dự kiến và owner; đặt ứng dụng vào cửa sổ bảo trì nếu migration không backward-compatible. Production giữ `Database:ApplyMigrationsOnStartup=false`.

```powershell
Set-Location backend
dotnet ef migrations list --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.Api
dotnet ef database update <TargetMigration> --project src/CulinaryBlog.Infrastructure --startup-project src/CulinaryBlog.Api
```

Sau migration, chạy readiness và smoke journey. Rollback ứng dụng bằng image digest trước đó chỉ khi schema vẫn backward-compatible. Chỉ dùng `database update <PreviousMigration>` khi `Down` đã được review và thử trên bản sao dữ liệu; nếu migration làm mất dữ liệu hoặc không thể đảo, dừng ghi và restore backup theo runbook Phase 8 thay vì tự sửa production.

## 9. Xác minh sau xử lý

- Readiness healthy liên tục ít nhất 5 phút; alert resolve.
- 5xx và p95 trở về baseline; worker không tạo failure mới.
- Một request thử có thể truy từ response `X-Correlation-ID` sang Seq/audit.
- Ghi timeline, root cause, biện pháp, phần còn lại và owner follow-up.

## 10. Backup và restore drill

Chạy stack reliability để kích hoạt PostgreSQL dump và MinIO snapshot hằng ngày. Hai loại backup giữ 30 ngày; backup volumes trên staging/production phải nằm trên encrypted storage độc lập với data volumes và Docker host chính.

```powershell
docker compose -f compose.yaml -f infra/operations/compose.reliability.yaml up --detach --build --wait
./scripts/restore-drill.ps1
```

Restore drill chỉ ghi vào PostgreSQL/MinIO tmpfs sạch, kiểm tra checksum, migration/recipe/object count rồi ghi RTO/RPO vào `artifacts/reliability/`. Không xem backup là hợp lệ nếu chưa có restore drill pass. Production restore cần incident approval, write freeze và target mới; không restore đè trực tiếp lên database/bucket đang phục vụ.

## 11. Failure drill

```powershell
./scripts/failure-drill.ps1
```

Script lần lượt restart API/worker, dừng Redis, MinIO, SMTP và PostgreSQL; `finally` khởi động lại toàn bộ dependency. Chỉ chạy trên local/staging hoặc trong maintenance window đã duyệt. Trước khi chạy phải xác nhận project name/URL không trỏ nhầm production. Sau drill, giữ readiness healthy tối thiểu 5 phút và đính kèm JSON evidence vào ticket.

## 12. Uptime và alert routing

Blackbox Exporter gọi `/health/ready` mỗi 10 giây. `CulinaryBlogReadinessDown` firing sau 1 phút; `CulinaryBlogWatchdog` phải luôn hiện ở receiver. Production dùng `infra/observability/alertmanager.production.yml` và URL receiver từ secret file, không commit webhook URL.

```powershell
$env:OPS_ALERT_WEBHOOK_URL_FILE = '<absolute-secret-file-path>'
docker compose -f compose.yaml -f infra/operations/compose.alert-routing.yaml up --detach alertmanager
```

Gửi alert thử, xác nhận đúng on-call owner nhận cả firing và resolved notification, rồi ghi thời gian/receiver/ticket. Nếu Watchdog biến mất ở hệ thống nhận alert nhưng còn firing trong Alertmanager, xử lý như sự cố alert routing.

## 13. Maintenance announcement

Thiết lập `MAINTENANCE_ENABLED`, `MAINTENANCE_MESSAGE`, `MAINTENANCE_ANNOUNCED_AT_UTC`, `MAINTENANCE_STARTS_AT_UTC` và `MAINTENANCE_ENDS_AT_UTC`. Production guard từ chối khởi động nếu announcement cách start dưới 48 giờ, message rỗng hoặc end không sau start. Kiểm tra `GET /api/v1/operations/maintenance` và banner trên public/dashboard pages trước khi thông báo hoàn tất.

## 14. Immutable release deployment

Lấy artifact `release-bundle-<sha>` từ workflow `release-images.yml`; kiểm tra artifact digest/SHA256SUMS và giữ nguyên `api.json`, `web.json`. Tạo env file ngoài repository theo `infra/production/release.env.example`; mọi image phải ở dạng `image@sha256`, secret lấy từ secret store và backup volume phải độc lập với data volume.

Trước deploy, chạy migration đã review theo mục 8 rồi validate mà không đổi runtime:

```powershell
./scripts/deploy-release.ps1 `
  -Target staging `
  -EnvFile <secure-staging-env-file> `
  -ManifestDirectory <downloaded-release-bundle> `
  -EvidenceDirectory artifacts/release/staging `
  -ValidateOnly
```

Sau approval, chạy lại không có `-ValidateOnly`. Production yêu cầu `-Target production -ApprovedChangeId <signed-go-no-go-id>`. Script pull với `--no-build`, chờ health, kiểm tra readiness/home/HSTS/CSP và đối chiếu `/api/v1/release`; giữ JSON evidence trong change record. Không tiếp tục nếu commit/digest khác staging UAT.

## 15. Post-deploy và rollback

Tại các mốc 0/15/30/60/120 phút rồi mỗi ca trong 24–48 giờ, ghi readiness, 5xx, p95/p99, dependency health, cache, failed jobs và business error; xác nhận Watchdog/alert receiver và thử correlation ID. Production smoke gồm CJ-01, CJ-02 và CJ-04 tối thiểu, đồng thời kiểm tra Draft/private media không xuất hiện public.

Khi gặp rollback trigger trong `docs/release/go-no-go-and-rollback.md`, dừng rollout/write traffic nếu có rủi ro dữ liệu. Nếu schema backward-compatible, điền env bằng previous commit/API/Web digest, chạy release gate và deploy lại; không rebuild/retag. Nếu schema không compatible hoặc dữ liệu hỏng, DBA/Technical Lead chọn reviewed down-migration hoặc restore sang target sạch theo mục 8/10. Sau rollback, chạy smoke, theo dõi 60 phút và lưu incident/evidence.
