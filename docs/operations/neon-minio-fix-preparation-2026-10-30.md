# Neon/MinIO — Bản sửa đã chuẩn bị để duyệt

**Cập nhật31/10/2026:** người dùng đã cho phép sửa tunnel/endpoint Render. Gateway và tunnel đã khôi phục; người dùng save/redeploy endpoint, agent kiểm live health/recipe/3media200 và SHA khớp MinIO. Chưa áp migration Neon thật. Xem [checkpoint G06 mới](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-tiep-tuc-31102026). Nội dung30/10 bên dưới là bản chuẩn bị lịch sử, không còn là yêu cầu xin lại quyền sửa tunnel.

Ngày30/10/2026, Asia/Bangkok. Tiếp nối [kiểm tra chỉ đọc](neon-minio-readonly-check-2026-10-30.md). [Evidence](neon-minio-fix-preparation-2026-10-30.json). Chưa thay dữ liệu Neon thật, Environment Render, IAM/storage hoặc hạ tầng demo đang chạy.

## 1. MinIO: sửa đúng đường kết nối

MinIO local và300object được Neon tham chiếu đang có đủ; không cần di chuyển hay tạo lại dữ liệu. Phần hỏng được xác nhận trong cấu hình đã lưu là gateway không còn, Windows cloudflared không chạy và hostname Quick Tunnel cũ không phân giải DNS từ máy kiểm. Chưa đọc được Environment hiện tại của Render.

Script start tunnel hiện có đồng thời tạo/attach IAM policy. Không chạy script đó trong lượt chuẩn bị. Script resume cũ chỉ nhận gateway còn tồn tại nên chưa xử lý trường hợp gateway bị mất.

Đã chuẩn bị `tmp/package-G06/fix-preparation-20261030/repair-tunnel.ps1`, mặc định chỉ validate, thực thi phải có`-Apply`:

- Xác minh đúng saved session/container/bucket, volume `/data` và port của MinIO hiện có, binary cloudflared đã có, không có tunnel process khác; không tải binary/image mới.
- So toàn bộ gateway config với template repo và demo access key đã lưu; không tạo user, đổi key, attach policy, đổi private bucket hoặc mount data volume sang service khác.
- Chạy Nginx config test bằng cached image đúng digest, container kiểm tạm không publish port. **Validate exit0, syntax pass**; gateway thật chưa được tạo.
- Khi được phép apply: tạo lại đúng gateway/label/loopback port nếu thiếu, rồi dùng code resume đã có để khởi động cloudflared ẩn và cập nhật duy nhất dòng endpoint trong private env. Xác minh các dòng credential/bucket không đổi.
- Sau apply phải GET signed list/read và so checksum ảnh hiện có qua hostname mới, kiểm anonymous/Admin/Console theo phạm vi được phép, rồi mới chuẩn bị cập nhật Render. Không tạo/xóa ảnh để chứng minh đường đọc.

Lệnh đã chạy:

```powershell
pwsh -NoProfile -File tmp/package-G06/fix-preparation-20261030/repair-tunnel.ps1
```

Lệnh **chưa chạy**, cần cho phép thay hạ tầng:

```powershell
pwsh -NoProfile -File tmp/package-G06/fix-preparation-20261030/repair-tunnel.ps1 -Apply
```

Render: xác nhận endpoint/release đang active trước sửa. Nếu đang dùng endpoint cũ, chỉ đổi `ObjectStorage__Endpoint` sang URL mới đã kiểm; không thay key, database/Redis, migration/seed/worker flags hoặc source. Áp dụng Environment cần restart/redeploy backend để nhận config; không tự thực hiện khi chưa có quyền. Nếu thao tác của nền tảng sẽ lấy source khác release đã xác nhận, phải dừng để review thay vì mặc định deploy latest. Endpoint mới chỉ xuất hiện khi Quick Tunnel được tạo; không bịa hostname trước apply. Không cần gửi AccessKey/SecretKey/password qua chat.

## 2. Neon: schema phải đi cùng release

Neon kết nối và dữ liệu đã kiểm tốt. **Không cần đổi connection string hoặc reset DB để sửa kết nối.** Chỉ cần8pending migration nếu đưa source G06 mới lên backend; backend cũ đang phục vụ không được tự ý đổi schema trước.

Đã tạo SQL Up/Down từ chính EF model/migrations hiện tại và chạy trên PostgreSQL18 clone local mới, tmpfs, locale builtin`C.UTF-8` giống Neon. Dump backup được mount read-only và kiểm SHA256; restore single transaction/no-owner/no-acl; apply bằng DB owner không phải superuser. Không khởi động API/jobs/initializer/seed.

| Bước trên clone local | Kết quả |
|---|---|
| Restore backup |5migration; counts các bảng gốc khớp snapshot. |
| Nâng8migration |13migration; tất cả original fields của27original tables ngoài migration history được giữ nguyên. |
| Đọc bằng EF model mới |11users,100recipes,100slugs;0pending migration. |
| Rollback thử |Về5migration; original data giữ nguyên. |
| Nâng lại |13migration; đọc EF tiếp tục đạt, original data giữ nguyên. |
| Index/constraint |0invalid index/0unvalidated constraint ở các checkpoint. |
| Dọn clone |Đã kiểm label sở hữu và dọn đúng clone/tmpfs, không đụng stack người dùng. |

So sánh giá trị tất cả cột gốc có chuẩn hóa numeric scale để không coi đổi biểu diễn integer/smallint/numeric là mất dữ liệu; migration history được kiểm riêng theo5/13/5/13. Các trường mới như slug/context là thay đổi có chủ đích. Rollback pass trên bản sao **chưa có ghi mới từ ứng dụng** không bảo đảm mọi dữ liệu phát sinh sau deploy thật đều rollback được; migration đã có guards cho dữ liệu không tương thích.

Lượt harness đầu bắt nhầm readiness của temporary init server PostgreSQL rồi mất kết nối khi server đó shutdown. Không có kết quả migration ở lượt đó; clone đã dọn. Đã sửa kiểm readiness qua TCP trước restore và chạy lại toàn chuỗi thành công. Raw attempt/diagnostic ở ignored tmp; không đổi lỗi harness thành lỗi Neon hoặc bỏ gate.

Lệnh thực chạy:

```powershell
dotnet build tmp/package-G06/fix-preparation-20261030/Rehearsal/Rehearsal.csproj -c Release
python tmp/package-G06/fix-preparation-20261030/rehearse.py
```

Build0warning/0error; final rehearsal`passed=true`. SQL và helper SHA256 trong evidence; SQL ở `tmp/package-G06/fix-preparation-20261030/migrations-up.sql` và`migrations-down.sql`. Không có credential cloud trong SQL hoặc CLI arguments; connection của EF check được guard chỉ nhận loopback DB`g06_rehearsal`.

Trước áp thật cần: xác nhận reviewed backend release/source, backup Neon mới tại thời điểm triển khai, maintenance/write-freeze phù hợp, kiểm8migration trên đúng target/role, chạy post-migration smoke/ORM và kế hoạch phục hồi theo tình trạng dữ liệu. Chưa chạy các bước này trên cloud, chưa commit/push/deploy source G06. Private source archive vẫn nguyên.

## 3. Phạm vi đang chờ cho phép

1. Khôi phục gateway/tunnel local từ cấu hình đã kiểm, giữ MinIO data/IAM.
2. Sau khi tunnel mới đọc đúng ảnh hiện có, cập nhật duy nhất endpoint trên Render và restart/redeploy đúng backend release đã xác minh.
3. Neon live migration/release G06 là thay đổi riêng, chưa áp từ câu hỏi về khả năng sửa. Giữ các cờ startup migration/seed hiện tại cho tới khi có kế hoạch triển khai được chấp thuận.

Yêu cầu phê duyệt xuất phát từ chỉ dẫn của người dùng: **“Không tự ý deploy lên production, chạy kiểm thử tải trên dịch vụ công khai, xóa dữ liệu hoặc thay đổi hạ tầng khi chưa được cho phép.”** Bản chuẩn bị này không đóng thêm task G06 hoặc thay A/B/C.
