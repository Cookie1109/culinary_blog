# Backup PostgreSQL trên máy Windows

Cập nhật10/11/2026. Phạm vi G06/D9-07: backup database Neon đang dùng và certificate phục hồi keyring. Không sao lưu ảnh hoặc dùng Google Drive.

## Cấu hình đang vận hành

- Task `Culinary-G06-PostgreSQLBackup` chạy mỗi ngày **03:00UTC /10:00giờ Bangkok**. Task chạy với tài khoản Windows hiện tại, quyền Limited, không lưu mật khẩu trong tham số.
- Cần máy bật, tài khoản đăng nhập, mạng và Docker Desktop hoạt động. Task chạy bù khi khả dụng, thử lại tối đa3lần, cách5phút. Không có cam kết luôn bật hoặc RPO24giờ khi máy tắt. Đây là điều kiện vận hành phải giữ, không phải miễn trừ yêu cầu backup hằng ngày.
- Bản sao ở `D:\Culinary_Backups\G06\postgres-scheduled\runs`. Mỗi lượt tạo archive7z AES-256, mã hóa cả tên file. Khóa phục hồi tách khỏi archive, dùng khóa hiện có đã được người dùng xác nhận lưu ở nơi khác; không đưa khóa lên Git/chat.
- Giữ30ngày theo timestamp UTC trong tên lượt chạy. Chỉ các lượt thành công của đúng job, đúng mẫu tên và đúng danh sách file được xét xóa. Lượt thất bại, symlink hoặc thư mục có file lạ được giữ để kiểm tra.

Config và TLS/credential nằm trong thư mục `private` có ACL chỉ cho tài khoản chạy, SYSTEM và Administrators. Config chỉ ghim đường dẫn, imageID và SHA256 script; giá trị credential ở secret file riêng, không trong argv. Không dùng rclone hoặc cấu hình MinIO.

Công cụ PostgreSQL18.6 dùng image riêng từ `infra/operations/postgres-tools.Dockerfile`, ghim base Alpine theo digest, chạy với user không phải root. Binary `gosu` không được dùng ở chế độ này đã được loại khỏi image sau khi scan phát hiện lỗ hổng trong binary đó; không thêm ignore. Image công cụ mới đã được kiểm lại cả scan và restore. Khi đổi image phải lặp hai gate này rồi cập nhật imageID trong config private; không kéo tag mới tự động khi task chạy.

Bản sao PostgreSQL trên máy Windows độc lập với dịch vụ Neon/Render, nhưng vẫn phụ thuộc ổ đĩa và khả năng hoạt động của máy này. Không phải bản sao ảnh độc lập ngoài máy và không chứng minh khả năng khôi phục nếu đồng thời mất máy cùng bản sao. Giữ khóa phục hồi ở nơi riêng đã xác nhận; không để khóa trong archive.

## Mỗi lượt thực hiện

1. Kết nối Neon bằng TLS `verify-full`, session read-only và repeatable-read exported snapshot.
2. Lấy số dòng/hash của tất cả bảng và pg_dump từ **cùng snapshot**, không ghi hoặc migrate Neon.
3. Restore vào PostgreSQL mới, network `none`, tmpfs và không publish port. So đủ schema/migration, số dòng/hash, kiểm keyring vẫn mã hóa. Dọn đúng container thử có label của lượt chạy.
4. Đóng gói dump, manifest và certificate vào archive mã hóa; giải mã thử và so SHA256. Xóa đúng scratch do lượt chạy tạo sau khi đã kiểm đạt.
5. Ghi `result.json`/`last-run.json`, áp retention đúng phạm vi.

Neon dùng WAL proposer và quorum safekeepers để bảo đảm độ bền, không dựa vào fsync trên ổ compute. Đã đọc runtime `wal_level=replica`, `synchronous_commit=on`, `synchronous_standby_names=walproposer`; `fsync=off` của compute được ghi nguyên trạng, không đổi cấu hình database. Cơ chế được mô tả trong [protocol chính thức của Neon](https://github.com/neondatabase/neon/blob/main/docs/safekeeper-protocol.md) và [kiến trúc lưu trữ Neon](https://neon.com/blog/pitr-deep-dive).

## Kiểm tra trước demo

Mở Docker Desktop rồi kiểm task/bản sao gần nhất bằng PowerShell:

```powershell
Get-ScheduledTaskInfo -TaskName Culinary-G06-PostgreSQLBackup |
  Select-Object LastRunTime, LastTaskResult, NextRunTime
Get-Content D:\Culinary_Backups\G06\postgres-scheduled\last-run.json
```

`LastTaskResult=0` và `status=success` cho biết lượt vừa chạy đạt. Kiểm thời điểm snapshot thực tế; bản sao cũ hoặc lượt lỗi phải được xử lý, không chỉ nhìn thấy task đã đăng ký. Có thể chạy lại đúng pipeline:

```powershell
Start-ScheduledTask -TaskName Culinary-G06-PostgreSQLBackup
```

Không chạy đồng thời bản manual Python với task. Không restore đè Neon để thử. Script đã tự restore vào target mới trong mỗi lượt; khi cần khôi phục thật phải chọn target và kế hoạch riêng.

## Thay đổi script

Source ở `infra/operations/backup-neon.py`, đăng ký lịch bằng `register-neon-backup.ps1`. Khi source thay đổi, phải kiểm lại phần ảnh hưởng rồi cập nhật SHA256 đã review trong config private; script từ chối chạy nếu hash khác. Kiểm retention:

```powershell
$env:PYTHONDONTWRITEBYTECODE='1'
C:\Python313\python.exe -m unittest discover -s infra/operations/tests -p test_backup_neon.py -v
```

Không tự sinh khóa mới, đổi credential hoặc chép giá trị secret vào lệnh. Chi tiết các lượt chạy, RTO đo được và phạm vi nghiệm thu ở [report G06](../specs/srs-completion-phases/reports/package-G06-result.md).
