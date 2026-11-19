# G06 — Triển khai được cho phép và tài nguyên đang có

Cập nhật05/11/2026: đã dùng CLI Render chính thức sau user Authorize CLI/xác nhận My Workspace, fast-forward main`8b9a7e7→41f619b`, hotfix live và keyring/media kiểm qua restart04/11. Không cần xin lại quyền triển khai đã được cấp. Không apply lại migration keyring, không rotate certificate. Full G06 local nay14migration khác hotfix thực5migration; cần đối chiếu/rehearsal candidate đầy đủ trước thao tác tiếp theo, không deploy cả worktree đang chứa thay đổi ngoài phạm vi. Vercel plugin chưa cung cấp công cụ điều khiển sử dụng được trong phiên. [Checkpoint mới](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-05112026); trình tự31/10 dưới đây giữ lịch sử.

Ngày31/10/2026, Asia/Bangkok. Checkpoint chi tiết tại [report G06](../specs/srs-completion-phases/reports/package-G06-result.md#authorized-rollout-31102026).

## Quyền mới từ người dùng

Người dùng xác nhận tài khoản/bài viết/ảnh hiện có là dữ liệu thử; cho phép đưa bản sửa đã kiểm lên hệ thống, thử tải trong một môi trường thử được xác định trước và thử hai backend. Chưa có nơi lưu backup độc lập; chưa rõ máy chủ có thể tận dụng. Chưa cấp ngân sách hoặc chỉ định domain mới. Không suy dữ liệu thử thành quyền xóa/reset toàn bộ; vẫn backup và kiểm dữ liệu cũ.

Quyền cập nhật source/Neon/Render/Vercel trong kế hoạch trước đã có. Không hỏi lại quyền đó ở mỗi bước. Quyền này chưa bao gồm mua dịch vụ, nâng gói, chuyển/xóa MinIO hiện tại, load/DAST vào dịch vụ công khai hoặc tự duyệt tolerance scaling/ngoại lệ SRS. Giữ PDF và A/B/C.

## Những việc đã làm với quyền mới

- Kiểm máy:12logicalCPU, RAM khoảng15,85GiB; Docker engine12CPU/~7,68GiB. Khôi phục Docker Desktop đã dừng;13container người dùng giữ nguyên IDs. Có thể tận dụng để thử riêng trước, không chứng nhận production từ cấu hình này.
- Neon read-only/TLS verify-full thành công: PostgreSQL18.0.6,28tables/2042rows,5migration nghiệp vụ. Tạo pg_dump mới ngoài repository trong thư mục ACL user hiện tại; cùng exported snapshot với row fingerprints. Đây vẫn là backup cùng PC, không phải storage độc lập.
- Restore backup mới vào PostgreSQL18 riêng có localeC.UTF-8 đúng nguồn:28tables/2042rows khớp toàn bộ count/hash; keyring SQL chạy2lần giữ dữ liệu cũ. Lab đã dọn.
- Áp thật **chỉ** `20261031134534_PersistDataProtectionKeys` lên Neon theo quyền mới; ledger riêng1migration, key table hiện0rows trước khi bật provider. Main ledger vẫn5;28tables/2042rows cũ khớp trước/sau. Không áp8migration nghiệp vụ của G06 đầy đủ, không reset/seed/xóa dữ liệu thật.
- Tách hotfix từ đúng live`e610783`, chỉ14files của encrypted DB keyring/certificate/test/locks; giữ nguyên fixes nghiệp vụ mới trên main. Nhánh `fix/render-free-keyring-20261031`, commit`4cfe24348b29e82c0e622d73941ceacbfd7dfa2e`, đã push, main chưa đổi. [Source review](https://github.com/Cookie1109/culinary_blog/compare/main...fix/render-free-keyring-20261031).
- Hotfix: compiler0warnings/errors, format0,79unit+3architecture+124IT0fail/skip; API image`sha256:dacef4fe135dc6b129ed6fd848dc6f65ae24d91e1ab8546df28acfe1a808262a`scan0H/C;14source files Gitleaks0findings. Git pre-push backend/frontend format đều đạt, không bỏ hook. Windows checkout LF đã chuẩn hóa trong worktree riêng, không đổi text frontend hoặc thư mục gốc.

## Phân biệt hotfix với candidate G06 đầy đủ

Hotfix chỉ sửa lưu khóa cho binary đang chạy; không mang toàn bộ G06 vào live. API main dùng IdentityGuid/5migration, trong khi candidate G06 đầy đủ đã kiểm trước đó dùng schema/model khác và13migration. Thử hợp nhất full candidate với10commit mới trên main có30conflicts: đã abort trong worktree riêng, giữ snapshot commit local`0568c2fc6b639f07d601ee67de2091d78978b0b3`, chưa push/promote snapshot đó. Không ghi đè các sửa nghiệp vụ mới để lấy deployment nhanh.

Analyzer bổ sung của G06 trên thin live baseline báo295style diagnostics tại Domain không đổi; compiler/format theo CI của live đã đạt. Không gọi analyzer đó pass hoặc lấy strict0 của candidate G06 cũ gắn cho hotfix. Full G06 release vẫn cần hợp nhất dependency và chạy đủ gates; không có waiver mới.

## Môi trường tải được xác định trước

Project riêng `culinary-g06-f54dfdfa36c0`; API imageG06`378b9ff...`,2CPU/4GiB mỗi API; PostgreSQL/Redis/MinIO/collector/Prometheus/Nginx synthetic riêng, ports chỉloopback. Dataset50recipes/20categories, App13/keyring1migration. Không dùng Neon/Upstash/MinIO demo hoặc URL cloud để bắn tải. Image này khác hotfix, không dùng số đo của nó chứng nhận tốc độ hotfix.

Chạy public-read100VU/5phút trên1API rồi2API, cùng dataset và script90%warm/10%cold hiện có, thu counters theo window. Có kiểm chứng2upstreams. Chỉ appending summary allowlist trong bản copy runner private; không đổi workload/gates. Hai lần đầu dừng trước tải do wrapper parser và collector command trỏ sai file; giữ log/exit, sửa harness và kiểm counters trước chạy lại. Script điều phối đã sửa để trả lỗi nếu trial lỗi, không dựa riêng wrapper exit cũ0 để báo pass.

Đây là diagnostic local đã được phép, chưa phải formal DEC-11/production/scaling tuyến tính. Giữ ngưỡng PDF; protocol/sample/tolerance cần chốt theo quyết định hiện hành. Kết quả cuối và cleanup cập nhật tại report/manifest sau phép đo.

## Bước áp backend lên Render

Quyền đã có; phần còn thiếu là truy cập dashboard/API của dịch vụ, không phải hỏi lại quyền deploy. Browser host/native pipe hiện unavailable; không cóRenderAPIcredential sẵn và kết nối Render/Vercel/Neon trong phiên chưa được xác nhận. Đã đề xuất kết nối các dịch vụ; chỉ dùng sau khi được kết nối. Neon đã thao tác được qua cấu hình private/TLS sẵn có, không bắt buộc thêm kết nối Neon cho bước SQL vừa làm.

1. Kết nối Render, đọc service/env/branch và release hiện tại; không dump secrets.
2. Chọn đúng source hotfix đã kiểm. Nếu cần đưa lên main, chỉ fast-forward từ base đã kiểm; gặp main mới phải đối chiếu lại, không force push.
3. Secret candidate đã có local tại `tmp/data-protection/certificate.base64`; chuyển riêng vào Render secret `DataProtection__CertificateBase64` và bật `DataProtection__PersistKeysToDatabase=true` trên binary mới. Không gửi value qua chat, không commit PFX/base64; giữ certificate ổn định và backup riêng khỏi DB.
4. Deploy binary mới, kiểm release identity/ready/log và key table có encrypted row, reset token trên tài khoản/sink được phép, token qua restart. Không lấy key table0rows hiện tại hoặc redeploy binary cũ làm bằng chứng keyring đã sửa thật.

## Những điều vẫn cần tài nguyên hoặc quan sát

| Mục | Có thể làm ngay | Điều kiện chưa có |
|---|---|---|
| Lỗi keyring | SQL đã áp; hotfix đã kiểm/push | Kết nối Render để cấu hình/deploy và nghiệm thu runtime |
| Tải/hai backend | Lab riêng trên PC đã chọn | Formal DEC-11 và artifact/resources/network production phù hợp |
| CWV/INP | Collector G06 đã có | Source frontend phù hợp main, deploy được phép, log/window và mẫu thực tế; không lấy controlled visits thay RUM |
| Backup | Backup/restore mới cùng PC đạt | Nơi lưu ngoài PC, lịch/retention/restore deployment; user chưa có nơi đó |
| MinIO4node | Lab4node/quorum/heal đã có | Storage security và topology triển khai thực/persistence được phép; không mặc định cần mua4máy |
| Security | Hotfix API scan0H/C | Full G06 gates, HTTPS staging/DAST/TLS đúng artifact và xử lý MinIO server H/C |

Tạm dùng ngân sách phát sinh0 vì chưa có mức được phép; không mở dịch vụ trả phí hoặc đề nghị người dùng gửi APIkey/password qua chat. Không tự đóng task từ bản kế hoạch này.
