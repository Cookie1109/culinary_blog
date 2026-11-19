# Neon/MinIO — Kiểm kết nối và dữ liệu chỉ đọc

**Cập nhật31/10/2026:** tunnel đã được sửa theo quyền mới, Render sau khi người dùng đổi endpoint/redeploy đọc recipe và3variant ảnh200; SHA khớp MinIO,300key snapshot Neon còn đủ. Xem [checkpoint G06](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-tiep-tuc-31102026). Evidence30/10 bên dưới giữ nguyên thời điểm/phạm vi chỉ đọc; không phải kết quả migrate Neon thật.

Ngày30/10/2026, Asia/Bangkok. Yêu cầu: xác minh kết nối và dữ liệu hiện có, không deploy hoặc thay hạ tầng. [Evidence không-secret](neon-minio-readonly-check-2026-10-30.json). Raw UTC bắt đầu29/10lúc18:50, tương ứng30/10lúc01:50Asia/Bangkok.

**Kết luận:** Neon kết nối được và dữ liệu khớp backup; MinIO local đọc được và có đủ media mà Neon tham chiếu. Đường Quick Tunnel được lưu trong cấu hình hiện không hoạt động trên máy kiểm. Chưa xác nhận được đường Render→Neon/MinIO của deployment đang chạy.

## Kết quả thực kiểm

| Nội dung | Kết quả | Giới hạn |
|---|---|---|
| Neon TCP/TLS/authentication |Đạt; PostgreSQL18.6, `neondb`, certificate `verify-full`, channel binding `require`. Endpoint trùng nguồn cấu hình đã dùng cho backup/restore trước đây. |Client kết nối từ máy kiểm, không phải request phát ra từ process Render. |
| Neon data |**28bảng/2.042dòng**, count và fingerprint từng bảng đều khớp snapshot26/10 đã được duyệt. |So với snapshot đó; không tự khẳng định dữ liệu ngoài snapshot hoặc ghi ở một deployment khác đã được đồng bộ. |
| Ràng buộc |14foreign keys kiểm thực tế, **0orphan**,0invalid index/0unvalidated constraint. |Không có sửa chữa hoặc chạy DDL. |
| Schema ứng dụng |DB có **5migration**, source hiện có13;8pending. Identity ID trên DB vẫn `uuid`. |Kết nối thành công không đồng nghĩa schema đã tương thích source G06 mới, vốn dùng Identity string. Không redeploy/migrate tự động. |
| MinIO local |Health200, authenticated ListBucket200; **301object** hiện hành; named volume `culinary-blog_minio-data` mount `/data`. |Không restart để thử durability ở lượt này, không đọc/xóa version cũ. |
| Media Neon→MinIO |100active image rows có **300unique object keys**; **300/300 có trong bucket**,0missing. |Kiểm đầy đủ presence qua listing, không tải toàn bộ300file để chứng nhận checksum từng file. |
| Bytes ảnh mẫu |4existing object GET200, kích thước khớp listing. Mẫu có baseline lịch sử vẫn khớp SHA256 `dc92ba65d5b2a09e25148e81d5d7c486698ada5a799a50a8cbc486a7fdba2963`. |Ba mẫu còn lại lưu hash hiện tại để truy vết, không có baseline cũ cho chúng. Không xuất nội dung ảnh hoặc object keys vào report công khai. |
| Quyền ảnh |Anonymous GET một object hiện hữu trả403; signed GET của user demo trả200. |Đây là kiểm quyền đọc mẫu, không phải security scan toàn bộ hệ thống. |
| Quick Tunnel đã lưu |Hostname `ranked-congratulations-supports-hwy.trycloudflare.com` **không phân giải DNS từ máy kiểm**. Gateway `culinary-minio-tunnel-07c9fcb00712` không còn; không thấy Windows process `cloudflared.exe`. |Không phát hiện/xác nhận tunnel khác đang dùng trên máy khác hoặc endpoint khác đã được nhập lên Render. Không tự tạo gateway/tunnel hay sửa Environment. |
| API Render |Một request release bằng curlIPv4 timeout20giây/0bytes; truy cập health/release qua công cụ web thay thế cũng không được. |**Trạng thái runtime chưa xác minh**, không kết luận API down. Không có credential/token Render được đọc hoặc thay đổi. |

Dữ liệu chính trong Neon:11user,20category,100recipe,100image metadata,1.000ingredient,500step,19refresh token rows. Chỉ công bố counts/hashes; không xuất email/tên/token hoặc dữ liệu hàng.

## Phương pháp và phạm vi an toàn

- Áp dụng hướng dẫn `postman:api-testing`; tái dùng cấu hình và logic read-only/MinIO SigV4 hiện có trong repository. Không gọi `MinioTunnelProbe hold/verify-delete` vì các mode đó có thao tác ghi/xóa.
- Neon: client PostgreSQL18cached chạy tạm, bind private config/CA/pgpass **read-only**, password chỉ ở private file/temp client tmpfs; không có trong CLI arguments hoặc output. Session `default_transaction_read_only=on`, transaction `REPEATABLE READ READ ONLY`, statement timeout15s/lock timeout3s, kết thúc`ROLLBACK`. Không khởi động API/jobs, không gọi initializer/migration/seed/restore. Client tạm đã tự dọn; không đổi service/volume đang dùng.
- Queries đọc inventory, tất cả table count/fingerprint, migration history, index/constraint status, orphan FK và technical media keys. Không SELECT giá trị user/password/token ra báo cáo. Existing metadata keys chỉ nằm trong ignored tmp để đối chiếu object.
- MinIO: chỉ HTTP GET health/ListBucket/GetObject bằng user demo đã lưu; list giới hạn/pagination và sample download≤10MiB mỗi file. Không Put/Delete/rename, không tạo bucket/user/policy, không restart service. Chỉ đọc Docker inspect/state, không xuất environment/credentials.
- Private `backend/.env` có Neon host/database và MinIO endpoint trùng đích đã kiểm; **không đọc Environment đang active trên Render**. Sự trùng cấu hình file không đủ chứng minh cloud process dùng các giá trị đó.
- Helpers và raw logs: `tmp/package-G06/connection-check-20261030/neon_readonly.py`, `minio_readonly.py`; SHA256 trong evidence. Hai lệnh Python exit0, Neon report`completed=true`, MinIO`localCompleted=true`; lỗi tunnel được giữ thành kết quả chẩn đoán, không gọi toàn bộ connection check là pass.

## Việc cần làm tiếp

1. **Neon:** giữ dữ liệu/kết nối hiện tại; chuẩn bị rehearsal8pending migration trên bản sao và review kế hoạch trước cập nhật source/schema. Không đổi connection string chỉ vì migration pending. Không bật startup migration/seed để thử kết nối.
2. **MinIO tunnel:** cần xác nhận endpoint thực tế đang đặt trong Render. Nếu vẫn là hostname đã lưu ở trên, phải khôi phục gateway/tunnel rồi cập nhật endpoint theo phiên mới sau khi được phép. Nếu có tunnel khác, kiểm lại endpoint đó; không cần gửi AccessKey/SecretKey qua chat.
3. Sau khi đường cloud truy cập được, kiểm `/api/v1/release`, `/health/ready`, `/health` và một ảnh có sẵn qua API để chứng minh đúng deployment và đường Render→storage. Chưa chạy mutation hoặc yêu cầu người dùng đổi hạ tầng trong lượt kiểm này.

Không thay đổi checkbox G06: dữ liệu đang tồn tại không tự chứng nhận backup vận hành, MinIO4node, benchmark production hoặc schema G06 đã được triển khai.
