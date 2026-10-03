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
