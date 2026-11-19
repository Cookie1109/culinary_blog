# G06 — Môi trường triển khai và kế hoạch kiểm thử đồ án

**Cập nhật31/10/2026:** đã kiểm thêm hai API/Nginx quota/cache/RedLock/worker và lab MinIO4node quorum/heal. Tunnel đã sửa theo quyền mới; Render sau lần người dùng đổi endpoint/redeploy đọc health/recipe/3variant ảnh200. Release demo`e610783` khác artifact G06; các điều kiện production/DEC-11 bên dưới vẫn mở. Xem [checkpoint G06 mới](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-tiep-tuc-31102026). Nội dung30/10 là kiểm kê lịch sử, không phải quyền deploy rộng hơn.

Ngày đánh giá: **30/10/2026, Asia/Bangkok**. Đây là kiểm kê và đề xuất, chưa phải phê duyệt deploy hoặc protocol nghiệm thu. PDF SRS và đính chính A/B/C giữ nguyên; D9-01/03/04/07/09/10 vẫn mở. Không đổi checkpoint/code đã kiểm của G06.

## 1. Kết luận và phạm vi kiểm

**Có thể tận dụng local cùng Vercel/Render/Neon/Upstash đã có để hoàn thiện và demo. Chưa có bằng chứng môi trường cloud hiện tại đủ để nghiệm thu toàn bộ G06.** Cần xử lý sự tương thích source/schema trước khi redeploy; cần thêm môi trường đo có tài nguyên xác định và topology storage phù hợp khi nghiệm thu production.

- Đã đọc AGENTS, README, checkpoint G06, decision register, Compose/Nginx/collector, scripts release/performance, tài liệu và JSON triển khai gần nhất.
- HEAD local `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`; worktree có nhiều thay đổi sẵn. **697 file source/config vẫn khớp manifest G06**, không chạy lại regression còn hợp lệ. Image local đang chạy của người dùng chưa được đồng nhất với artifact đã nghiệm thu G06 chỉ từ tên tag.
- Chỉ đọc danh sách container/tài nguyên và thực hiện một GET tới mỗi endpoint release/ready/home bên dưới. Không đọc giá trị credential, đăng nhập cloud, thay environment, deploy, dispatch workflow, chạy tải/scanner, ghi/xóa dữ liệu hoặc thay hạ tầng.
- Ba GET đều timeout sau khoảng20–22giây từ máy kiểm. Không có request HEAD asset vì chưa lấy được HTML. **Không kết luận cloud đang down, không xác nhận release/health/CDN hiện tại.** Có thể cần kiểm từ Dashboard/thiết bị khác; chưa biết nguyên nhân timeout.
- [Evidence kiểm kê không-secret](g06-environment-readiness-2026-10-30.json) chứa quan sát, source hashes và giới hạn. Timestamp UTC thô có thể là29/10; ngày báo cáo dùng Asia/Bangkok.

## 2. Thành phần đã có và khoảng trống

| Thành phần | Bằng chứng hiện có | Phần chưa xác nhận/còn thiếu |
|---|---|---|
| Local Docker |13 container: API, web, Nginx, PostgreSQL16.10, Redis7.4.5, MinIO, MailHog, Seq, collector, Prometheus, Grafana, Alertmanager, Blackbox. Container ứng dụng/dependency có health đang healthy; không restart. | Đây là stack người dùng. Không dùng trực tiếp làm đối tượng stress, outage hoặc restore. Stack thử G06 trước đã dọn. |
| Máy local |12 logical CPUs, RAM host≈15,8GiB; Docker được cấp8.243.060.736bytes≈7,68GiB. | Phải tránh tranh chấp với stack đang dùng. Benchmark, browser lab và fault drill nên chạy tuần tự trên stack riêng khi được phép; giới hạn container không chứng minh máy chủ không bị tranh CPU/RAM. |
| Frontend cloud |URL có trong repo: `https://culinary-blog.vercel.app/`. Evidence ngày27/10 ghi home200. | Chưa xác nhận deployment/build hiện tại, G06 đã được đưa lên hay chưa, cache headers của `/_next/static`, RUM/INP và Cloudflare CDN. GET hiện tại timeout. |
| API cloud |`https://culinary-blog-api.onrender.com`. [Evidence gần nhất](frontend-audit-fix-result.json) ghi release/ready200 và API commit `d512494dd9c7876d305f6df8b946cf84a1ec9853`, CI success; không migration/seed. | Chưa xác nhận live commit, CPU/RAM/plan, worker, exporter hoặc artifact G06. GET release/ready hiện tại timeout. Đây là deployment demo có bằng chứng, chưa xác nhận staging riêng cho G06. |
| Neon |Evidence đã kiểm snapshot28bảng/2.042row và kết nối Npgsql TLS; applied migrations ghi5. | Source hiện có13migration IDs: **8 migration mới so với evidence**. Không truy vấn DB live trong lượt này nên không khẳng định DB hiện vẫn chỉ có5. Cần đọc history/schema mới và rehearsal trước thay đổi. |
| Upstash |Evidence TLS/PING và16 compatibility checks cho cache/Lua/RedLock đã có. | Chưa xác nhận plan/quota/region hiện tại, shared rate state và workers của release G06 trên cloud. Không dùng Redis của demo cho benchmark ghi/xóa khi chưa được phép. |
| MinIO |Local single node, persistent volume/private/versioning; đã có gateway và Quick Tunnel để demo API cloud truy cập ảnh. Cloudinary đã bị người dùng rút. | Tunnel phụ thuộc PC/Docker/process/URL phiên; repo từng ghi phải cập nhật endpoint Render nhưng chưa xác nhận hoàn tất. Không có deployment MinIO distributed4node. |
| Quan sát hệ thống |G06 đã kiểm HTTP/EF/outbox/job→collector→Seq, metrics/Grafana và failure drill trên stack riêng. Source collector đã có exporter. | Chưa chứng minh collector/dashboard/Jaeger hoặc Tempo được triển khai và nối với API cloud hiện tại. Production template yêu cầu `OTEL_TRACE_BACKEND_ENDPOINT`; hostname `tempo` trong env example chỉ là placeholder. |
| Backup |Scheduler03:00UTC, retention30ngày, restore19public tables/schema và154object checksum đã kiểm local. | Chưa có storage backup vận hành độc lập với Docker host/data volumes, lịch chạy thực cloud và restore từ bản backup đó. `MINIO_MC_IMAGE` mặc định từng pull thất bại; image thử có `mc/find/sha256sum` chưa thành artifact vận hành được chấp thuận. |
| Production templates |Có immutable-image workflow, release gate, Compose, TLS Nginx, persistent/backup volumes. | Template không phải deployment. Compose hiện khai báo một service API và một MinIO `server /data`, Nginx upstream một API; chưa có4node storage/CDN deployment. Ví dụ digest/domain không được dùng như giá trị thật. |

Nguồn: [README](../../README.md), [G06](../specs/srs-completion-phases/reports/package-G06-result.md), [review cloud](render-neon-upstash-review.md), [MinIO tunnel](../../DEMO_MINIO_TUNNEL.md), [Cloudinary đã rút](cloudinary-storage.md), [production Compose](../../infra/production/compose.production.yaml), [Nginx](../../infra/nginx/nginx.production.conf).

## 3. Phân loại công việc

“Có thể” dưới đây là khả năng và đầu vào cần chuẩn bị, không phải quyền chạy ngay. Lượt này không tạo stack mới hoặc thay cloud.

| Hạng mục | Làm được ở local | Tận dụng demo/staging hiện có | Cần production hoặc tài nguyên/quyền bổ sung |
|---|---|---|---|
| Hoàn thiện mã và functional regression |Unit/integration/browser, schema rehearsal trên bản sao riêng; tái dùng G06 còn đúng hash. |Smoke/E2E trên deployment đúng release sau khi có dataset/tài khoản thử và quyền ghi thử. |Không cần mua cloud để sửa mã; migration/redeploy dịch vụ đang phục vụ cần kế hoạch/phê duyệt riêng. |
| D9-01/DEC-11 |Chuẩn bị workload có read/write,100VU, cold/warm, metrics và kiểm generator; benchmark riêng có giới hạn2CPU/4GiB. |Đọc release/health/config/plan khi truy cập được. Load chỉ khi xác nhận tài nguyên/quota và có quyền rõ. |SRS§4.1 yêu cầu production/tải thực; cần target đúng artifact, CPU/RAM/network/TLS có định danh và protocol được chốt. Demo chưa biết plan không đủ căn cứ. |
| D9-03/CWV/INP |Build23route/budget, Lighthouse, Event Timing/luồng tương tác. Có thể chuẩn bị thu Web Vitals bằng `useReportWebVitals` có sẵn trong Next15. |Đo lab trên bản staging đúng release; thu tương tác thật có ghi thiết bị/window sau khi được phép cài instrumentation. |Nghiệm thu CWV/INP production cần dữ liệu ở deployment đó, không lấy mẫu local16ms hoặc TBT thay. |
| D9-04/HTTPS |Dependency/image/config/secret review và test CORS/cookie/guard, giữ gates hiện có. |HTTPS/header/cookie/redirect/TLS và dynamic scan trên staging đúng artifact khi có quyền; không suy từ proxy config. |Quyền scan, môi trường xác định, release identity và tài khoản thử. Không quét demo công khai chỉ vì URL truy cập được. |
| D9-05/06 |Task đã có evidence local/container; tiếp tục kiểm targeted nếu code liên quan đổi. |Kiểm exporter/worker/retry trên staging riêng sau khi schema tương thích và được bật có kiểm soát. |Không dừng DB/Redis/MinIO/worker của deployment dùng chung khi chưa có cửa sổ thử được phép. |
| D9-07 |Scheduler, retention, checksum, restore target riêng và tool image reproducible. |Kiểm backup snapshot của DB/storage staging sau khi có quyền đọc/ghi backup và target restore riêng. |Storage backup độc lập, lịch chạy thực, retention và RTO/RPO ở môi trường vận hành. Named volume khác trên cùng PC chưa chứng minh độc lập host. |
| D9-09 |2API/Nginx/shared state functional; đã có JWT/cache/welcome dedup, còn hoàn thiện ma trận quota/job/lock theo phạm vi. |Có thể dùng staging nhiều instance nếu plan, routing, schema và shared dependencies đáp ứng. |Đo scaling trước/sau trên phần cứng xác định, protocol DEC-11; không mặc định Render demo đã có2instance. |
| D9-10/MinIO/CDN |Có thể chuẩn bị lab4container/4volume mới độc lập với bucket hiện hữu, kiểm quorum/restart/heal. Chưa dựng. |Vercel có CDN nền tảng để tận dụng; kiểm asset cache và chứng minh provider/topology thật. Demo MinIO tunnel hiện tại vẫn single node. |Production4node storage persistent, kết nối ổn định, load balancing và CDN theo PDF. Cần xác nhận nguồn lực/domain/quyền; không tự chuyển dữ liệu hoặc miễn yêu cầu topology. |

## 4. Đề xuất DEC-11 và kiểm tải

Các mục sau là **bản đề xuất để review**, chưa sửa decision register hoặc coi là protocol đã duyệt. Không tái dùng80/20, error1%, efficiency90% hay hệ số1,8× đã bị rút.

1. **Chuẩn hóa target:** một artifact được review, API2vCPU/4GiB; ghi tài nguyên DB/Redis/storage, versions/region/TLS, network path, worker count, pool100. Dùng dataset synthetic riêng, tài khoản/recipe riêng cho mỗi VU; writes không tác động recipe đang demo.
2. **Tách phép đo:** bộ GET warm để kiểm p50/cache và bộ nghiệp vụ có đăng nhập/tạo/cập nhật/publish/search để kiểm cả write latency. Ghi request mix thực; lựa chọn trọng số gộp phải review theo kịch bản đồ án. Không đổi mix sau khi thấy kết quả để làm hit rate đẹp hơn.
3. **Khung chạy đề xuất:** smoke5VU/30s để kiểm wiring; load100VU với warm-up3phút rồi steady10phút; stress tăng bậc50→100→150→200,2phút/bậc. Có thể dừng sớm khi thấy lỗi, ảnh hưởng dịch vụ, hàng đợi tăng không kiểm soát hoặc chạm quota; lượt dừng ghi thất bại/chưa đủ bằng chứng. Cần chấp thuận duration/VU/tải ghi trước khi chạy. Không chạy khung này lên URL công khai trong lượt kiểm kê.
4. **Ngưỡng giữ nguyên:** warm GETp50≤150ms, APIp95≤500ms/p99≤1000ms, Redis steady-state hit≥80%,≥100concurrent users. Ghi mọi lỗi HTTP/nghiệp vụ, không tự cho phép1%error.100VU không đồng nghĩa100RPS; lưu achieved throughput và think time/pacing.
5. **Scaling:** so1API và2API cùng2CPU/4GiB mỗi instance, giữ dataset/mix/version và tài nguyên shared services. Đo cả cùng tải100VU để so latency, lẫn tải tăng theo số instance để tìm capacity đạt ngưỡng; tránh kết luận từ generator bị giới hạn. Báo `RPS2/RPS1` và `RPS2/(2×RPS1)` cùng các lần lặp; không tự đặt cutoff để gọi “tuyến tính”. Cách đánh giá sai số/no-degradation cần chốt DEC-11 trước nghiệm thu.
6. **Giảm chi phí/nhiễu:** ưu tiên máy lab đang có hoặc mượn thêm máy chạy k6; chạy tuần tự, không trùng Lighthouse/fault/backup drill. Chỉ dùng rate-limit override trong profile benchmark tách biệt, ghi lại; test quota production riêng với policy gốc. Đọc giới hạn/quota nhà cung cấp trước khi dự toán và chấp thuận bất kỳ tải cloud nào.

Chưa biết plan Render hiện tại. **Nếu** dùng Free, nền tảng có spin-down sau15phút và các giới hạn khác; không thể dùng cấu hình đó làm bằng chứng profile2vCPU/4GB. Không giữ service thức bằng ping chỉ để che cold start. [Render Free docs](https://render.com/docs/free).

## 5. Đề xuất CWV/INP và CDN

- Giữ gate23route và Lighthouse hiện có. Chạy trên build production đúng release, ghi desktop/mobile, cache/network/CPU,3navigation mỗi route; lab có tác dụng tìm lỗi trước khi lên cloud.
- Sau khi được phép cập nhật staging, dùng `useReportWebVitals` của Next15 để gửi LCP/CLS/INP về nơi thu do dự án kiểm soát. Có thể dùng endpoint cùng hệ thống để tránh mua dịch vụ mới. Chỉ gửi metric, route pattern/build/device group; không gửi token, nội dung form, URL có query nhạy cảm hoặc dữ liệu tài khoản. Đây là code/instrumentation còn phải triển khai, chưa có từ việc đọc tài liệu.
- Đề xuất mở đợt thu trong buổi chạy thử/đánh giá đồ án, ghi chính xác start/end và số page visits/interactions theo mobile/desktop; nếu có thêm thời gian thì tiếp tục thu để tăng số mẫu. Không tự đặt7/28/30ngày thành yêu cầu SRS hoặc coi một số lượng mẫu nhỏ là đủ đại diện. Báo p75 field theo từng nhóm và số mẫu thực; thiếu mẫu ghi chưa đủ bằng chứng.
- Ngưỡng SRS vẫn LCP≤2,5s/CLS≤0,1/INP≤200ms và First Load JS≤200KBgzip. Navigation Lighthouse/TBT và3visit local16ms không thay tương tác production. Google hướng dẫn đánh giá INP field ở percentile75, tách mobile/desktop. [INP](https://web.dev/articles/inp), [Web Vitals](https://web.dev/articles/vitals).
- Vercel có CDN cache cho static assets: ưu tiên xác minh `/_next/static` đúng build, cache-control và cache response ở deployment đang có. Hiện chưa lấy được header do timeout. Chưa có bằng chứng Cloudflare CDN trong repo; không tự coi CDN Vercel thay điều khoản Cloudflare của PDF. Nếu cần cấu hình Cloudflare đúng SRS, phải có domain/quyền DNS và thiết kế caching/static riêng được phép; không cache nhầm API/auth hoặc phá ISR. [Vercel CDN](https://vercel.com/docs/caching/cdn-cache), [Next.js CDN caching](https://nextjs.org/docs/app/guides/cdn-caching).

## 6. Đề xuất MinIO4node và backup

- **Bước ít chi phí:** chuẩn bị một Compose lab4node trên máy sẵn có với4volume mới và gateway riêng, sau khi được phép tạo lab. Dùng dữ liệu synthetic; kiểm client upload/read/delete ở prefix thử được phép, SHA256, quorum khi dừng một node, khôi phục/healing, versioning và backup/restore. Không mount volume/bucket MinIO hiện tại vào cluster mới và không đổi `server /data` của container đang có để “chuyển tại chỗ”.
- Lab4container trên cùng PC chứng minh distributed-mode chức năng nhưng cùng chung lỗi host/đĩa/nguồn điện. Nghiệm thu production cần topology4node persistent triển khai thật, tài nguyên/network ổn định và bằng chứng hoạt động; không tự gọi4process trên PC là production HA. Ưu tiên mượn tài nguyên phòng lab/VM đã có, xác định failure domains; chỉ tính thuê thêm sau khi kiểm kê được tài nguyên sẵn. [MinIO deployment architecture](https://min.io/docs/minio/kubernetes/eks/operations/concepts/architecture.html).
- Chốt image server/client có nguồn tải/build tái lập, digest và scan; tool backup phải có `mc`, `find`, `sha256sum`. G06 đã có image thử riêng giải quyết missing-find, chưa publish hoặc thay defaults vận hành. Auto-review ở lượt trước từ chối thay defaults dùng chung; không dùng quyền môi trường mới để tự áp thay đổi đó trong lượt chỉ kiểm kê.
- Giữ MinIO theo quyết định hiện hành, không chuyển Cloudinary. Quick Tunnel phục vụ demo tạm; Cloudflare giới hạn200request in-flight và không cam kết uptime cho Quick Tunnel. Nó không thay storage distributed4node hoặc môi trường benchmark production. Named tunnel/domain ổn định có thể cân nhắc cho demo sau khi có quyền, vẫn phụ thuộc máy nguồn. [Quick Tunnels](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/).
- Backup vận hành cần nơi lưu độc lập với host/data volumes, có quyền truy cập qua secret management và khả năng restore vào target khác. Giữ lịch03:00UTC/retention30ngày theo baseline hiện hành; ghi RTO/RPO mới từ môi trường vận hành, không gắn số local3,502s/2,502s cho cloud. [Runbook](runbook.md#10-backup-và-restore-drill).

## 7. Environment variables và quyền truy cập

**Không yêu cầu gửi password/API key qua chat.** Dùng Environment/Secrets của đúng project Render/Vercel hoặc secret store/file ngoài repository và ngoài Docker build context; cấp phiên truy cập có phạm vi khi cần agent thao tác. Không dump environment hoặc đưa secret vào tham số URL/log.

| Nơi cấu hình | Tên/thông tin cần có | Cách giữ an toàn/phạm vi |
|---|---|---|
| Render API |`ConnectionStrings__Database`, `ConnectionStrings__Redis`, `Jwt__SigningKey`, `ObjectStorage__Endpoint/AccessKey/SecretKey/BucketName`, CORS/hostname/public URL, SMTP nếu kiểm email thật. |Nhập qua provider Environment/secret import; dùng Npgsql/Redis connection format và TLS đã kiểm, IAM MinIO tối thiểu. Không dùng root credential local cho endpoint công khai. |
| Render trước migration review |`Database__ApplyMigrationsOnStartup=false`, `AdminSeed__Enabled=false`, `BackgroundJobs__Enabled=false` theo checkpoint cloud. |Giữ trong giai đoạn đọc/đối chiếu. Các cờ này không giải quyết schema không tương thích; không redeploy G06 rồi mặc định request sẽ chạy. Chỉ bật worker sau rehearsal/release review và cho phép. |
| Vercel frontend |`INTERNAL_API_BASE_URL=https://culinary-blog-api.onrender.com/api/v1`, `AUTH_SECRET`, `AUTH_URL`/trust-host theo cấu hình Auth.js của deployment, Google OAuth nếu cần. |Server secrets ở project env. Browser chỉ cần public base path/URL; không đưa JWT signing key/storage secret/DB string vào `NEXT_PUBLIC_*`. Chưa áp những giá trị này lên cloud. |
| Docker release template |`API_IMAGE`, `WEB_IMAGE` theo digest thật; `RELEASE_COMMIT_SHA`, image digests, `OTEL_TRACE_BACKEND_ENDPOINT`, TLS file paths, backup volume/storage, SMTP/auth secrets. |Tham khảo [release.env.example](../../infra/production/release.env.example), thay toàn bộ placeholder. Bản Docker dùng digest; Render provider dùng metadata nền tảng theo ADR hiện có, không bịa image digest. |
| Registry/CI |Phiên đăng nhập được cấp quyền và GitHub Environment Secrets; workflow hiện dùng `GITHUB_TOKEN`. |Chưa dispatch/publish. Không cần tạo PAT gửi vào chat chỉ để đọc repo; quyền push/deploy phải được xác định riêng. |

Đã chỉ kiểm **tên file**: root `.env` và `backend/.env` tồn tại, không đọc giá trị; `frontend` không có file`.env*` hiện tại. Git ignore bảo vệ `.env*`, backend Docker context có rule tương ứng; root `.dockerignore` chưa có rule`.env*`. Vì frontend Dockerfile có `COPY frontend frontend`, tiếp tục ưu tiên secret ngoài build context, rà ignore trước khi thêm env frontend. Git ignore không tự thay Docker ignore. Không phát hiện/khẳng định có secret đã lọt image chỉ từ việc kiểm cấu hình này.

## 8. Thứ tự thực hiện phù hợp đồ án

1. **Giữ nguyên hệ thống đang dùng.** Tận dụng evidence G06 còn đúng hash, không mua/nâng gói hoặc chạy tải ngay. Kiểm trạng thái Render/Vercel và plan qua Dashboard bằng phiên đăng nhập có sẵn khi được cấp; xác nhận deployment nào được dùng làm staging.
2. **Chuẩn bị release tương thích:** đọc migration history/schema hiện tại theo chế độ read-only; rehearsal8migration chênh lệch trên bản sao được phép; kiểm dữ liệu, job payload, rollback/backup. Tạo tập thay đổi/release artifact được review từ worktree nhiều thay đổi; chưa commit/push/deploy trong lượt này.
3. **Chuẩn bị local trước:** protocol tải dự thảo có read/write, RUM instrumentation và lab4node/2API riêng. Chỉ dựng/chạy khi phạm vi local mới được cho phép; ưu tiên tuần tự để tiết kiệm RAM. Không cần chi phí cloud cho việc chuẩn bị mã/cấu hình.
4. **Khi được phép cập nhật staging:** dùng đúng artifact/schema, cấu hình env qua secret management, smoke/E2E với dataset riêng; xác minh TLS/CDN/telemetry. Dynamic scan và fault/restore cần target/cửa sổ/phạm vi riêng, không tác động data người dùng.
5. **Chỉ bổ sung tài nguyên cho phần không thể chứng minh bằng demo:** benchmark production đúng2vCPU/4GB, scaling,4node storage, independent backups, CWV/INP field. Có thể tận dụng máy/VM phòng lab; nếu thiếu thì trình chi phí và thời gian thuê giới hạn trước. Không tự hạ chuẩn thành local-pass.

Thông tin cần xác nhận tiếp theo **không phải secret**: project/deployment nào được coi là staging; plan/CPU/RAM/quota hiện có; máy/VM/domain/storage có thể dùng; phạm vi deploy/migration/tải/scan được phép; trần ngân sách và lịch chạy thử. Phần protocol có thể do agent soạn thành bản review; người dùng không phải tự cung cấp các tham số kỹ thuật từ đầu.

**Không có task G06 nào được đóng thêm từ bản kiểm kê này.** Các mục còn thiếu môi trường, quyền hoặc protocol tiếp tục giữ mở.

## 9. Cập nhật sau kiểm trực tiếp Neon/MinIO

Theo yêu cầu tiếp theo của người dùng, đã kiểm bằng cấu hình riêng ở chế độ read-only: Neon kết nối TLS được,28bảng/2.042dòng khớp backup,5migration/8pending; MinIO local có đủ300object keys của100ảnh Neon và mẫu lịch sử khớp checksum. Quick Tunnel lưu trong cấu hình không resolve DNS, gateway và Windows cloudflared không chạy. Chưa xác nhận Environment/đường truy cập thực từ Render. Xem [báo cáo kiểm trực tiếp](neon-minio-readonly-check-2026-10-30.md). Các mục “chưa đọc DB” phía trên mô tả lượt inventory trước đó; giữ nguyên evidence snapshot cũ để truy vết. Không sửa dữ liệu, migration, cloud env hoặc hạ tầng.
