# G06 — Kế hoạch phát hành để review

Cập nhật05/11/2026: quyền triển khai các bản sửa đã kiểm thử đã có; Render đã chạy hotfix`41f619b`/Neon schema5 và encrypted key giữ qua restart. Full G06 local có14migration sau cache fix, chưa được đưa lên dịch vụ; không đồng nhất artifact này với hotfix. Bộ local strict157unit+3architecture+290integration và affectedUI3đã đạt; load100VU10min và stress local đã đạt các ngưỡng, còn formalproduction/DEC-11 theo [checkpoint](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-05112026). MinIO security/production4node, backup độc lập, RUM/HTTPS và DEC-11 còn mở; không mua tài nguyên hoặc tự ký ngoại lệ. Các đề xuất31/10 bên dưới phải đọc cùng cập nhật này.

Cập nhật quyền31/10/2026: người dùng đã cho phép triển khai bản sửa đã kiểm, cập nhật Neon và thử tải/hai backend trong môi trường riêng xác định trước. [Kết quả và scope hiện tại](g06-authorized-rollout-2026-10-31.md): keyring migration đã áp, hotfix đã push; còn truy cập Render để deploy. Không mua/nâng gói, đổi topology/storage hoặc chạy load/DAST công khai khi chưa được chỉ định. Kế hoạch full G06 bên dưới giữ cho release đầy đủ, không thay hotfix chỉ có1migration keyring.

## Artifact và phạm vi

Render đã kiểm trả commit `e6107839283767594c598f84e70dbf139a626655`. G06 là worktree nhiều thay đổi trên HEAD`7ed7a58`, không phải commit/image đã được đưa lên Render/Vercel. Phải dùng source fingerprint và kết quả mới nhất trong [report gói](../specs/srs-completion-phases/reports/package-G06-result.md), không deploy nhãn mutable hoặc mặc định latest để thay artifact đã kiểm. Giữ các thay đổi ngoài phạm vi, không broad-stage/reset/clean.

## Trình tự khi được chỉ định môi trường và cấp quyền

1. Tạo candidate từ đúng source đã kiểm, review diff và secrets/build context; hoàn tất gates bị ảnh hưởng. Commit/push chỉ khi có quyền, mô tả tiếng Việt không dấu, không nói hoàn thành phase. Không tự sửa branch production để lấy URL test.
2. Chỉ định staging riêng, có DB/storage/dataset synthetic hoặc clone được phép. Xác nhận plan/CPU/RAM/quota/provider region và quyền log/export; không tự nâng gói hay mua tài nguyên. Staging không thay production acceptance theo PDF.
3. Kiểm migration history trước deploy. Source G06 có13migration; Neon đã kiểm có5. Rehearsal5→13→5→13 đã giữ dữ liệu trên clone, không cho phép áp thật tự động. Target thật cần backup mới, write-freeze/maintenance được duyệt, migration role phù hợp và kế hoạch phục hồi theo dữ liệu phát sinh; không reset/seed đè hoặc mặc định rollback sẽ an toàn sau writes mới.
4. Apply migrations và deploy candidate đúng identity chỉ trong phạm vi được duyệt. Giữ startup migration/seed/worker flags có kiểm soát; không bật worker trước khi schema/config tương thích. Smoke API/auth/recipe/media và E2E trên tài khoản thử, kiểm live release/config identity.
5. Frontend: bật CWV theo [hướng dẫn](web-vitals.md) trong build/runtime được phép, gắn deployment ID; xác nhận beacon204/log/window, rồi thu sample phù hợp. Chưa có mẫu production thì giữ D9-03 mở dù lab xanh.
6. Security: staging HTTPS đúng candidate, quyền dynamic scan/TLS theo window; triage findings và giữ ngưỡng H/C. Không scan demo cũ rồi gắn kết quả cho G06 mới.
7. Backup: [candidate tool](minio-backup-tool.md) đã có build/scan/roundtrip local; review identity và cấp quyền publish/override riêng nếu chọn dùng. Storage backup độc lập cần quyền truy cập qua secret management, lịch03:00UTC/retention30ngày; restore vào target sạch và đo RTO/RPO thực. Named volume khác trên cùng PC chưa chứng minh host independence.
8. Load/scaling: duyệt [protocol DEC-11 đề xuất](../specs/srs-completion-phases/dec-11-measurement-proposal.md), target/resources/window/dataset trước chạy. Không suy smoke hoặc lab nhiều container thành production100users/scaling tuyến tính.
9. Topology: chỉ triển khai MinIO4+node/Nginx/CDN sau khi có host/domain/storage được phép. Lab4node/quorum/heal đã có; không chuyển volume MinIO hiện hữu tại chỗ hoặc miễn điều khoản Cloudflare/CDN trong PDF.

## Quyết định cần từ chủ môi trường

- Môi trường nào được dùng làm staging và production nghiệm thu; resources/quota đang có, host/VM/domain/storage nào được tận dụng.
- Phạm vi cho phép deploy/migration, HTTP security tests, load/stress và window thử; dataset nào được phép ghi/xóa. Đây là các quyền riêng, không suy từ câu hỏi về khả năng triển khai.
- Nơi backup độc lập, retention/log export và protocol đánh giá scaling/sample adequacy. Agent chuẩn bị kỹ thuật và bằng chứng để review; không yêu cầu gửi password/API key qua chat. Cấu hình qua provider Environment/Secrets hoặc secret file được cấp quyền.
- Phương án xử lý MinIO Community server có advisory High/Critical chưa có bản vá Community: review phiên bản/storage được bảo trì và compatibility trước triển khai. Candidate CLI0H/C và guard unsigned-trailer trong template không vá server hoặc miễn gate; xem [bằng chứng và giới hạn](minio-backup-tool.md#gap-bảo-mật-minio-server). Gateway demo đã được khôi phục với guard theo quyền sửa tunnel hiện có; việc đó không thay kế hoạch vá server. Không tự mua license hoặc chuyển dữ liệu.

Không có ngân sách mặc định và không tạo CR/waiver mới. Task chỉ được đóng khi đủ evidence đúng môi trường, không vì đã chuẩn bị kế hoạch này.

## Bổ sung Data Protection cho Render Free/no disk

Người dùng đã xác nhận Free/không disk. Candidate đã thêm encrypted PostgreSQL keyring theo [runbook](render-data-protection-2026-10-31.md), không yêu cầu mua disk. API image mới `sha256:378b9ff6130892e6a0e63fb2a72178a9b2a0fbefcc68f19995de48497715f397` strict/tests/scan local đạt; xem [manifest và diff cụ thể](../specs/srs-completion-phases/reports/package-G06-result.md#data-protection-free-31102026).

Main AppDbContext vẫn13migration so với5 trong snapshot Neon; keyring thêm1migration ở context/history riêng, không phải migration nghiệp vụ thứ14. Khi tạo migration scripts/runner phải ghi `--context AppDbContext` hoặc `--context DataProtectionKeyDbContext` rõ ràng. SQL keyring idempotent và rollback guard đã kiểm local; role app cần quyền đọc/ghi/sequence phù hợp. Chưa áp bất kỳ migration này lên Neon thật.

Candidate certificate private đã tạo local, Gitignored/ACL hạn chế; không đưa giá trị vào report/commit/chat. Sau khi cho phép áp release, đưa secret qua Render Environment/secret management (`DataProtection__CertificateBase64`) và bật `DataProtection__PersistKeysToDatabase=true` trên binary mới đúng identity; backup certificate ngoài DB, giữ ổn định trên mọi replica/redeploy. Binary live`e610783` chưa chứa provider này, nên Save and deploy build cũ không cài được bản sửa. Không thay KeyPath để coi ephemeral filesystem là persistence.

Quyền cần cho bước áp thật gồm target/release cụ thể, backup/write-freeze và migration nghiệp vụ5→13 cộng keyring riêng, source deploy và smoke account/sink được phép. Quyền tunnel/endpoint cũ không bao gồm bước này. Có thể chuẩn bị candidate/SQL/gates mà không thay Neon/Render; toàn bộ bước áp thật vẫn chờ ủy quyền, không tự bật migration/seed/worker hoặc gửi email thật.
