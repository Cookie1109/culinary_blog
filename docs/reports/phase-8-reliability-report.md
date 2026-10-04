# Báo cáo Reliability và Operations Phase 8

> Ngày chạy: 05/10/2026 (Asia/Bangkok)  
> Phạm vi: P8-16 đến P8-21a  
> Kết quả: **Local Accepted**

## Backup và restore

| Gate | Cơ chế | Kết quả local |
|---|---|---|
| P8-16 | `pg_dump` custom format mỗi 86.400 giây; SHA-256; `pg_restore --list`; giữ 30 ngày | Pass |
| P8-17 | MinIO bucket versioning + snapshot hằng ngày; retention 30 ngày | Pass |
| P8-18 PostgreSQL | Restore vào PostgreSQL 16 sạch trên tmpfs; so 5 migration và 100 recipe | Pass |
| P8-18 MinIO | Restore vào bucket sạch có versioning; so 1 object | Pass |

Restore drill bắt đầu `2026-10-04T17:52:53Z` (00:52:53 ngày 05/10/2026 tại Bangkok), đạt **RTO 10,45 giây** và **RPO 11 giây**. JSON máy đọc được lưu dưới `artifacts/reliability/` (gitignored) ở mỗi lần chạy.

```powershell
docker compose -f compose.yaml -f infra/operations/compose.reliability.yaml up --detach --build --wait
./scripts/restore-drill.ps1
```

## Failure drill

| Scenario | Kỳ vọng | Recovery local |
|---|---|---:|
| Restart API + worker in-process | Liveness/readiness hồi phục | 4,95 giây |
| Redis unavailable | Readiness 503; alert sau 1 phút; hồi phục khi Redis trở lại | 2,17 giây |
| MinIO unavailable | Full health 503 nhưng readiness vẫn 200 | 4,25 giây |
| SMTP unavailable | Request-serving readiness vẫn 200; outbox không rollback nghiệp vụ | 0,47 giây |
| PostgreSQL unavailable | Readiness 503; hồi phục sau khi DB healthy | 4,64 giây |

`CulinaryBlogReadinessDown` đã xuất hiện trong Alertmanager sau khi Redis down hơn 1 phút. Hangfire và email outbox worker hiện chạy trong API process, vì vậy restart API cũng là worker restart drill; đây không phải một worker container độc lập.

```powershell
./scripts/failure-drill.ps1
```

## Runtime và monitoring

- Docker Compose render pass; API container được inspect với `restart=unless-stopped`, 2 CPU, 4 GiB, logging `json-file`, `max-size=10m`, `max-file=5`.
- Prometheus `promtool check config` pass, đọc 6 alert rules.
- Alertmanager production config `amtool check-config` pass; receiver URL lấy từ `/run/secrets/operations_webhook_url` và gửi cả resolved notification.
- Blackbox Exporter probe readiness mỗi 10 giây; alert threshold là 1 phút.
- `CulinaryBlogWatchdog` luôn firing để bên ngoài phát hiện pipeline alert bị đứt.
- Trên staging/production, Prometheus/Blackbox phải chạy ở failure domain độc lập với application host để phát hiện cả trường hợp host chính mất hoàn toàn.
- Maintenance banner lấy cấu hình runtime, poll mỗi 60 giây; production guard chặn window thiếu message, end time không hợp lệ hoặc công bố dưới 48 giờ.

## Phần phải xác nhận trên staging

Local Accepted không thay thế production readiness. Trước release phải:

1. Đặt `postgres-backups` và `minio-backups` trên encrypted storage độc lập với data volumes/host chính và kiểm tra lifecycle 30 ngày.
2. Chạy lại restore/failure drill trên staging artifact bất biến; lưu JSON evidence vào kho release.
3. Mount secret receiver thật qua `infra/operations/compose.alert-routing.yaml`, gửi alert thử và xác nhận đúng on-call owner nhận firing/resolved notification.
4. Cấu hình maintenance window với timestamp công bố thực tế trước tối thiểu 48 giờ và kiểm tra banner trên staging.
