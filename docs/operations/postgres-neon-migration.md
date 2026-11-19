# PostgreSQL local → Neon: đã restore và xác minh

Rà soát kết nối/migration mới nhất: [Render Neon/Upstash review](render-neon-upstash-review.md). Npgsql đã kết nối VerifyFull;6 migration pending và lỗi model được xác minh chỉ đọc. Giá trị database đã được điền vào draft private và file import chung; chưa đổi Render. Checkpoint restore bên dưới được giữ theo thời điểm thực hiện.

Cập nhật 26/10/2026, Asia/Bangkok. **Đã restore Neon sau phê duyệt rõ của người dùng và xác minh đủ 28 bảng/2.042 bản ghi. Phase5 đã chuẩn bị giá trị kết nối riêng và hướng dẫn; chưa đổi connection string, chạy ứng dụng hoặc deploy Render.** Người dùng đã trả lời “Phê duyệt restore bản backup này” cho đúng snapshot/endpoint. Mọi checkpoint “chờ duyệt” bên dưới là lịch sử trước phê duyệt, không phải trạng thái hiện tại.

## Nguồn, backup và đích đã xác minh

| Mục | Kết quả thực tế |
|---|---|
| PostgreSQL local |16.10, container `culinary-blog-postgres-1`, database `culinary_blog`, role `culinary`. |
| Network/volume |Host Windows5433 → container5432; volume `culinary-blog_postgres-data` tại `/var/lib/postgresql/data`. Không restart/reset/drop local. |
| Schema |`public`, `hangfire`;28 bảng,2.042 bản ghi trong cùng snapshot read-only với pg_dump. |
| Dữ liệu chính |11 tài khoản,20 category,100 recipe,100 ảnh metadata,1.000 ingredient,500 step. Không tự seed50 hoặc bỏ dữ liệu khác. |
| Extensions |`plpgsql`1.0, `unaccent`1.1, `pg_trgm`1.6. |
| Ràng buộc |61 logical constraints,14 FK,78 index hợp lệ,11 sequence. Không phát hiện constraint/index invalid. |
| Backup |Custom archive200.029 byte; pg_dump16.10, `--quote-all-identifiers`, snapshot đồng bộ; đã kiểm `pg_restore --list` và trích schema để review. |
| Đường dẫn ngoài Git |`D:\Culinary_Backups\PostgreSQL-Neon\20261026-161615-278332f6\culinary-local.dump`. |
| SHA-256 |`ea8aea26f0edbf0fac9e5b9d85b2de461b23904ff6af9b98cbd52b4c37127277`. |
| Neon |Direct Connection, database `neondb`, server18.6; branch `production` theo lựa chọn/thông tin người dùng, tên branch chưa xác minh độc lập qua Neon API. |
| Hiện trạng Neon |Trước restore: trống. Sau restore: đủ28 bảng/2.042 row, schemas public/hangfire,11 sequence,1 user function/1 trigger khớp snapshot. |
| Quyền đích |Role không-superuser, có CREATE trong database/public và quyền owner của public; restore đã cài unaccent1.1/pg_trgm1.6 như archive. Không tạo role local hoặc chuyển quyền superuser. |
| Bảo vệ kết nối |Client `verify-full`, channel binding=require, TLS được xác nhận bằng psql conninfo; toàn bộ truy vấn preflight trong transaction READ ONLY rồi ROLLBACK. |

Backup là bản logical database đầy đủ, gồm account/password hash ứng dụng, token, audit/outbox/Hangfire nếu có; không phải pg_dumpall và không sao chép cluster superuser/role password. File backup/danh mục/audit chi tiết đều ở thư mục ACL riêng ngoài repository. Không commit, gửi chat, upload GitHub hoặc dùng thư mục này làm build context.

## Tương thích PostgreSQL16 →18 đã kiểm

Đã restore archive vào **container PostgreSQL18 tạm độc lập**, không publish port, `--network none`, data trên tmpfs. Restore bằng role không-superuser với `--no-owner --no-acl --single-transaction --exit-on-error`; container tạm đã dọn. Đây không phải restore Neon.

- Cả28 bảng khớp row count và fingerprint toàn bộ dữ liệu từ snapshot backup, tổng2.042 row.
- Định nghĩa cột/type/default/nullability khớp.61 logical constraints khớp;14 FK không có orphan;78 index valid;11 owned sequence an toàn so với max ID.
- `unaccent`, `pg_trgm`, truy vấn FTS và trigger cập nhật recipe chạy được. Cả100 recipe không có mismatch khi tính lại normalization/vector. Test UPDATE trigger chỉ chạy trong scratch rồi ROLLBACK.
- Local dùng libc `en_US.utf8`; Neon dùng builtin `C.UTF-8`. Rehearsal cuối đã dùng đúng builtin C.UTF-8 của Neon. Đây là khác biệt thực; thứ tự sắp xếp chuỗi có thể thay đổi, không tự đổi collation hoặc nghiệp vụ.
- PG18 có193 dòng pg_constraint thay vì61: thêm132 NOT NULL catalog constraints tương ứng132 cột NOT NULL. Khi loại phần biểu diễn mới này, logical constraints vẫn khớp. Đây là thay đổi catalog của PG18, không phải mất/thêm ràng buộc nghiệp vụ ngoài ý muốn.

**Giới hạn quan trọng:** database local chỉ có5 EF migration đến `20261003120000_RecipeDiscoveryIndexes`, trong khi working tree hiện có11 migration. Identity ID trong backup vẫn `uuid`; các thay đổi schema G04 mới chưa được áp dụng vào database người dùng. Bản chuyển sẽ giữ nguyên5 migration đó. Không tự chạy6 migration còn lại hoặc coi restore PG18 là nghiệm thu ứng dụng mới nhất. Phải dùng phiên bản app khớp schema, hoặc lập kế hoạch nâng schema riêng sau khi được duyệt.

## Credential và TLS

Thông tin Direct Connection đã được lưu qua stdin được che vào `neon-private/pg_service.conf` và `neon-private/pgpass` trong thư mục backup ngoài Git. Password không đặt trong CLI arguments, source hoặc log. Người dùng đã cung cấp credential để agent kiểm; hiện không cần nhập lại. Có thể dùng `scripts/new-neon-connection.ps1` cho credential mới/đã xoay, với thư mục riêng mới để tránh overwrite.

Image PostgreSQL18 dùng cho client không có system CA bundle, nên lần đầu verify-full thất bại. Đã tải **CA công khai ISRG Root X1** từ Let's Encrypt, kiểm DER SHA256 `96bcec06264976f37460779acf28c5a7cfe8a3c0aae11a8ffcee05c0bddf08c6`, cấp riêng cho client qua SSL_CERT_FILE. Không tắt certificate/hostname verification và không đổi trust store hệ thống. `pg_stat_ssl` phía compute trả ssl=false trong lượt kiểm; kết nối phía client được xác nhận bằng conninfo và cấu hình verify-full, không dùng giá trị compute đó để suy diễn đường kết nối bên ngoài.

Credential đã xuất hiện trong chat nên nên xoay trước khi dùng lâu dài. Không sao chép connection string thật vào tài liệu hoặc ảnh chụp.

## Phương án đã được phê duyệt và thực hiện

Phương án dưới đây được trình trước restore; đã nhận phê duyệt rõ và thực hiện đúng cờ restore. Kết quả cụ thể tại checkpoint cuối:

1. Xác nhận đúng endpoint Direct đã cấp, database neondb/branch production theo Neon Console; đối chiếu SHA backup và fingerprint endpoint trong hồ sơ riêng. Giữ ứng dụng/client không ghi Neon trong lúc restore/verify; chưa chuyển Render sang Neon.
2. Chạy lại preflight chỉ đọc ngay trước restore. Nếu xuất hiện bất kỳ bảng/view/sequence/function hoặc dữ liệu ứng dụng mới, **dừng**, lập phương án giữ nguyên đích hoặc target/branch riêng rồi xin duyệt lại. Không merge/ON CONFLICT/overwrite tùy đoán.
3. Dùng PostgreSQL18 client và archive đã duyệt, giữ public schema hiện có, tạo hangfire và các object trong dump. Các cờ dự kiến:

```text
pg_restore --no-owner --no-acl --single-transaction --exit-on-error
           --dbname=<direct-service-loaded-from-private-files>
           <approved-custom-archive>
```

Không có password/URI trong command arguments; service/passfile mount read-only, password file được copy vào tmpfs chmod600 của client. Không `--clean`, `DROP`, `--create`, truncate, reset, copy global role hoặc đổi password/role đích. Object mới thuộc role Neon hiện tại; không tạo role local-superuser `culinary` trên Neon. Restore có lỗi thì rollback transaction, ghi lỗi đã che secret và dừng, không bỏ qua lỗi tùy tiện.

4. Sau restore, vẫn giữ app chưa cutover: so28 bảng/2.042 row/fingerprints với **snapshot đã dump**, cột/type/default/nullability, logical constraints/FK/index,11 sequence và dữ liệu chính; kiểm FTS/read-only queries trên Neon. Không dùng phép so với local live ở thời điểm khác để che chênh lệch.
5. Giữ nguyên source local, volume và backup cho rollback. Source vẫn hoạt động nên ghi mới sau snapshot chưa nằm trong archive. Trước cutover thực sự phải thống nhất write freeze/final dump hoặc chuyển phần delta có kiểm soát; không tuyên bố zero-loss cutover chỉ từ bản sao snapshot này.

Mặc định đề xuất giữ đủ cả public/hangfire/outbox/session thay vì tự bỏ bảng/token/job. Khi bật worker sau này phải rà jobs/outbox cũ để tránh chạy lại side effect; chưa cho phép bật worker chỉ vì restore thành công.

## Chuẩn bị Render — chưa áp dụng

Backend đọc `GetConnectionString("Database")`, nên Environment Variable chính xác là **`ConnectionStrings__Database`**. Npgsql dùng định dạng key=value, không dán nguyên URI postgresql:// vào biến này:

```text
Host=<direct-neon-host>;Port=5432;Database=neondb;Username=<neon-role>;Password=<private-secret>;SSL Mode=VerifyFull;Channel Binding=Require;Pooling=true;Include Error Detail=false
```

Giá trị chứa ký tự đặc biệt cần được escape/quote đúng bằng NpgsqlConnectionStringBuilder; không dựng chuỗi thật trong source/log. Khi được duyệt chuyển kết nối, vào Render Dashboard → culinary-blog-api → Environment; thay đúng biến database, giữ Redis/MinIO và các biến khác.

Trước lần kết nối/khởi động app đầu tiên, phải review:

- `Database__ApplyMigrationsOnStartup=false`: DatabaseInitializer hiện gọi cả MigrateAsync và seed khi cờ bật. Không chạy lại trên dữ liệu đã restore.
- `AdminSeed__Enabled=false`; giữ worker/Hangfire chưa kích hoạt trong lúc xác minh ban đầu (`BackgroundJobs__Enabled=false` theo kế hoạch được người dùng duyệt).
- Hangfire PostgreSQL storage còn có cơ chế chuẩn bị schema của thư viện; phải review version/schema/PrepareSchemaIfNecessary trước khi bật app/worker. Không nhận cờ EF là đã chặn mọi DDL của Hangfire.
- Phải đối chiếu app source với5 migration trong backup. Không deploy source11 migration mà bỏ qua chênh lệch schema.

Đã chuẩn bị giá trị để review tại thư mục riêng ngoài Git; chưa thay backend/.env hoặc Environment Render, chưa redeploy, chưa chạy API/seed/migration với Neon. Redis, MinIO và tunnel của nhiệm vụ trước không bị thay đổi.

Giá trị thật đã được escape bằng .NET DbConnectionStringBuilder và kiểm đọc lại từng trường, lưu tại:

```text
D:\Culinary_Backups\PostgreSQL-Neon\20261026-161615-278332f6\render-neon-connection.txt
```

File này chỉ chứa **Value** cho biến `ConnectionStrings__Database`, không chứa tên biến. Không gửi nội dung vào chat, Git hoặc log. Ba cờ tắt migration/seed/worker nằm cạnh đó trong `render-neon-safety.env`. Đây là file chuẩn bị, không phải cấu hình đã áp dụng. Kết nối thực tế của ứng dụng Npgsql trên Render chưa được chạy thử.

Sau khi đã review schema/phiên bản app/Hangfire và được duyệt riêng bước đổi Render, thao tác thủ công:

1. Mở **Render Dashboard → culinary-blog-api → Environment → Edit**.
2. Đặt Key `ConnectionStrings__Database`; copy toàn bộ một dòng trong file `render-neon-connection.txt` vào **Value**, không dán nguyên URI PostgreSQL.
3. Đặt `Database__ApplyMigrationsOnStartup=false`, `AdminSeed__Enabled=false`, `BackgroundJobs__Enabled=false` theo kế hoạch khởi động đã duyệt. Giữ Redis/MinIO và các biến khác.
4. Việc lưu/triển khai trên Render là bước đổi môi trường đang chờ duyệt; **chưa bấm Save/redeploy trong nhiệm vụ restore này**. Cần kiểm tra commit đang chạy và chênh lệch5/11 migration trước.
5. Sau lần khởi động được duyệt, kiểm log không có Migrate/seed hay nâng schema ngoài dự kiến; kiểm health, API đọc danh mục/công thức và các lỗi kết nối. Các kiểm tra này hiện chưa thực hiện, không ghi PASS.

## File đã tạo và checkpoint tiếp tục

| File | Chức năng |
|---|---|
| `tools/postgres-neon/backup_local.py` | pg_dump custom + audit snapshot read-only; refuse overwrite backup; verify-existing chỉ kiểm archive. |
| `tools/postgres-neon/save_connection.py` | Nhận URI qua getpass, lưu service/passfile ngoài Git, không mở kết nối. |
| `scripts/new-neon-connection.ps1` | Wrapper ACL/thông tin branch và masked input cho người dùng. |
| `tools/postgres-neon/prepare_client_ca.py` | Tải/kiểm CA công khai cho client riêng, không đổi hệ thống. |
| `tools/postgres-neon/inspect_neon.py` | Chỉ READ ONLY: TLS, target tables/data/extensions/role/schema; hỗ trợ Docker client hoặc `--psql` trỏ client có sẵn. |
| `tools/postgres-neon/rehearse_local_pg18.py` | Rehearsal isolated tmpfs, so schema/data/FK/sequences/FTS, dọn đúng container có label riêng. |
| `tools/postgres-neon/restore_neon.py` | Kiểm phê duyệt/SHA/endpoint/đích trống, journal chống tự retry, restore một transaction không clean/drop; không đổi Render. |
| `tools/postgres-neon/verify_neon.py` | Kiểm Neon sau restore trong một transaction REPEATABLE READ READ ONLY; schema, row fingerprints, FK/index/sequence/FTS. |
| `docs/operations/postgres-neon-migration-evidence.json` | Bằng chứng tóm tắt không có password, hostname/role Neon hoặc dữ liệu row. |

Hồ sơ chi tiết ngoài Git: `backup-manifest.json`, `archive-list.txt`, `schema-for-review.sql`, `local-snapshot.json`, `local-database-settings.json`, `neon-preflight.json`, `pg18-rehearsal.json`. Thư mục riêng có ACL cho người dùng hiện tại/SYSTEM/Administrators; backup chưa mã hóa nên phải giữ trong khu vực lưu trữ được bảo vệ.

**Checkpoint hiện hành: restore đã commit và kiểm chứng, Render chưa chuyển.** Chi tiết tại phần cuối. Không chạy lại script restore: journal tồn tại để chặn retry vào đích đã có dữ liệu. Các checkpoint tiếp theo được giữ làm lịch sử thao tác.

Nguồn: [Neon pg_dump/pg_restore](https://neon.com/docs/import/migrate-from-postgres), [Neon extensions](https://neon.com/docs/extensions/pg-extensions), [PostgreSQL pg_restore](https://www.postgresql.org/docs/18/app-pgrestore.html), [PG18 NOT NULL catalog change](https://www.postgresql.org/docs/18/release-18.html), [Npgsql connection parameters](https://www.npgsql.org/doc/connection-string-parameters.html), [Let's Encrypt trust chains](https://letsencrypt.org/certificates/).


## Checkpoint tạm dừng theo người dùng — 26/10/2026

Người dùng yêu cầu: “Ghi nhớ việc đang làm, tý tôi về rồi làm tiếp nhé”. Dừng tại đây, không thực hiện thêm restore hoặc thao tác cloud trong lúc chờ.

- Phase1–3 đã có bằng chứng: backup custom ngoài Git đã verify/list; snapshot28 bảng/2.042 row,100 recipe/11 account; Neon Direct18.6 kết nối TLS verify-full và READ ONLY, chưa có bảng/dữ liệu ứng dụng.
- Rehearsal chỉ ở container PG18 tmpfs riêng, cùng builtin C.UTF-8 như Neon: schema/cột/type/default/nullability, dữ liệu cả28 bảng,14 FK,78 index,11 sequence, FTS/trigger đạt; container scratch đã dọn. Không gọi đó là restore Neon.
- Backup đã duyệt kỹ thuật nhưng **người dùng chưa phê duyệt restore**: `D:\Culinary_Backups\PostgreSQL-Neon\20261026-161615-278332f6\culinary-local.dump`, SHA256 `ea8aea26f0edbf0fac9e5b9d85b2de461b23904ff6af9b98cbd52b4c37127277`, archive tạo09:23:29 UTC =16:23:29 Asia/Bangkok ngày26/10. Không dump/overwrite lại bản này chỉ vì đổi phiên.
- Thông tin Neon đã lưu riêng ngoài Git trong thư mục `neon-private` cạnh backup; không cần xin/gửi lại password trong chat. Có CA công khai đã kiểm fingerprint cho client Docker thiếu CA bundle; không tắt TLS.
- Hồ sơ cụ thể ngoài Git: `restore-plan-awaiting-approval.json`, `backup-manifest.json`, `local-snapshot.json`, `neon-preflight.json`, `pg18-rehearsal.json`. Bản tóm tắt không-secret: `docs/operations/postgres-neon-migration-evidence.json`. Pointer không-secret: `tmp/neon-migration/session.json`.
- Local chưa bị sửa/reset/restart, Redis/MinIO không đổi, Render/backend/.env chưa đổi kết nối. Dữ liệu Neon vẫn chưa restore. Source local có5 migration, working tree11: không tự Migrate/seed để xóa chênh lệch.

Khi tiếp tục: đọc checkpoint và hồ sơ trước; đối chiếu git diff để giữ thay đổi người dùng. Kiểm SHA backup, kiểm target vẫn đúng Direct neondb và chưa phát sinh dữ liệu bằng READ ONLY, đối chiếu branch production theo thông tin người dùng/Console. Nếu snapshot đã cũ so với các ghi mới thì báo rõ và thống nhất snapshot/final-freeze/delta, không giả đồng bộ. Hoàn tất kiểm syntax/tài liệu helper còn cần trước hành động tiếp theo rồi **xin duyệt rõ Phase4** bằng archive/target/phạm vi cụ thể. Không coi lời “làm tiếp”, credential hoặc yêu cầu ghi nhớ này là tự động cho phép restore. Sau approval mới restore trong một transaction không DROP/--clean, verify; chỉ đổi Render khi được duyệt riêng.

## Tiếp tục ngày 26/10/2026 — chờ duyệt restore

Người dùng đã yêu cầu tiếp tục; trạng thái tạm dừng đã gỡ. Đã xác minh SHA backup và toàn bộ 9 artifact riêng còn nguyên trước khi kiểm lại. Năm helper Python và wrapper PowerShell qua kiểm cú pháp; kiểm whitespace diff trong phạm vi không báo lỗi.

Docker Desktop hiện chưa chạy. Lần thử client Docker không kết nối được daemon và chưa mở kết nối database. Không tự bật Docker/toàn bộ stack. Đã bổ sung lựa chọn client native vào helper chỉ đọc và dùng PostgreSQL psql 18.3 có sẵn tại `C:\Program Files\PostgreSQL\18\bin\psql.exe` để kiểm thực tế.

Lượt kiểm lại thành công: cùng fingerprint Direct endpoint; neondb PostgreSQL18.6, client TLS verify-full/channel binding require, transaction READ ONLY rồi ROLLBACK; 0 bảng/row/view/sequence/user function, quyền CREATE và public owner vẫn đủ, extensions cần thiết vẫn có sẵn. Chi tiết thời gian UTC trong `resumeCheck` của evidence JSON và hồ sơ riêng đã cập nhật. Chưa xác minh tên branch qua Console/API; production vẫn theo thông tin người dùng. Chưa kiểm drift local vì Docker tắt; giữ nguyên giới hạn snapshot 16:23:29 và yêu cầu final-freeze/delta trước cutover.

**Bước kế tiếp cần người dùng duyệt rõ:** restore đúng archive `culinary-local.dump` nêu trên (28 bảng/2.042 row) vào Direct endpoint đã cung cấp cho neondb/production, trong một transaction, không DROP/--clean, kiểm lại đích trống ngay trước ghi. Sau đó so schema/data/fingerprint/FK/sequences. Phạm vi phê duyệt này không bao gồm đổi Render, deploy, chạy EF migration/seed hoặc bật worker. Chưa thực hiện restore trong lượt tiếp tục này.

## Checkpoint hiện hành — restore đã được duyệt và kiểm chứng ngày26/10/2026

Người dùng trả lời **“Phê duyệt restore bản backup này”** tại câu hỏi `call_12fe15e87f724b6ba71c269afefa1442`. Hồ sơ `restore-approval.json` riêng ghi đúng archiveSHA/endpoint fingerprint/phạm vi; không suy diễn thành duyệt đổi Render.

- Recheck ngay trước restore bằng psql18.3 có sẵn: cùng Direct endpoint, neondb trống, chỉ public/plpgsql; quyền CREATE/public owner đủ, extensions cần thiết có sẵn; TLS verify-full/channel binding require. Tên branch production vẫn theo thông tin người dùng, chưa xác minh Console/API.
- `pg_restore18.3 --no-owner --no-acl --single-transaction --exit-on-error` trả exit0, commit thành công. Không DROP/--clean/--create, không reset source hoặc đổi database local. Không có lỗi restore bị bỏ qua; log restore đã che secret được lưu ngoài Git.
- **So toàn bộ28 bảng/2.042 row với cùng snapshot trong backup: row count và fingerprint đều khớp**, gồm11 tài khoản,20 category,100 recipe,100 image metadata,1.000 ingredient,500 step cùng các bảng token/outbox/Hangfire. Không tự bỏ session/job hoặc thêm dữ liệu seed.
- Inventory bảng, cột/type/default/nullability,61 logical constraints,78 định nghĩa index, function/trigger,3 extensions và5 dòng EF migration history đều khớp.132 NOT NULL constraints bổ sung trong catalogPG18 tương ứng132 cột NOT NULL của source; không phải khác biệt nghiệp vụ.
-14 FK không có orphan;78 index valid;11 sequence có giá trị an toàn và khớp kết quả restore archive trong rehearsal. `unaccent`/trigram/FTS đạt, truy vấn “cơm” trả10 recipe;100 recipe không có mismatch normalization/vector. Xác minh Neon chỉ đọc rồi ROLLBACK; không chạy UPDATE trigger trên Neon.
- SHA backup giữ nguyên; đã quét credential Neon/endpoint trong các đầu vào Git và credential trong log chẩn đoán. Credential, backup, report chi tiết nằm ngoài Git với ACL riêng.
- Đã chuẩn bị `render-neon-connection.txt` và `render-neon-safety.env` riêng để thao tác thủ công sau review/phê duyệt. Chưa sửa backend/.env, chưa lưu Env Render hoặc deploy; chưa xác minh runtime API với Neon. Không tự chạy seed/EF migration, không bật worker, không sửa Redis/MinIO.

Chi tiết thời gian UTC/row fingerprints/định nghĩa schema/sequences: `neon-restore-execution.json`, `neon-restore-verification.json` trong thư mục backup riêng. Báo cáo không-secret: `postgres-neon-migration-evidence.json`. Tại checkpoint này Neon chứa đúng bản snapshot16:23:29 ngày26/10; ghi mới ở local sau đó chưa được đồng bộ. Docker đang tắt, nên chưa kiểm drift nguồn live. Không gọi đây là cutover ứng dụng hoặc cam kết zero-loss dữ liệu phát sinh sau snapshot.

**Còn mở trước khi chuyển Render:** xác nhận commit ứng dụng đang deploy khớp5 migration đã restore (working tree11 migration/Identity ID string khác source uuid); review Hangfire schema preparation và jobs/outbox cũ; thống nhất source write freeze/delta nếu có ghi mới; nhận phê duyệt riêng cho đổi connection/deploy. Giữ local, volume và backup làm nguồn đối chiếu. Không tự restore lần hai vào Neon đã có dữ liệu.
