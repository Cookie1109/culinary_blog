# Kết quả gói G06 — Hiệu năng, bảo mật và vận hành

## Đóng G06 cho báo cáo môn học miễn phí — 11/11/2026

<a id="free-demo-closeout-20261111"></a>

**Đã đóng cả 9 task trong phạm vi báo cáo môn học.** D9-02/03/05/06/07 giữ kết quả nghiệm thu đã ghi. D9-01/04/09/10 đã đóng cho báo cáo với các sai khác bên dưới. Các ô nghiệm thu SRS của bốn task vẫn để trống để truy vết. Phạm vi đóng theo yêu cầu triển khai miễn phí của người dùng. D9-08 thuộc phase 36, theo dõi riêng.

| Task | Bằng chứng miễn phí | Điều kiện SRS còn thiếu trên bản demo |
|---|---|---|
| **D9-01 — Tải API** | Lab loopback với dữ liệu thử đã chạy k6 smoke, load và stress. Lượt đọc/ghi trên hai API giới hạn 2 CPU/4 GiB mỗi instance giữ 100 VU trong 10 phút, 0 lỗi nghiệp vụ, cached GET p50 7,11 ms, API p95 53,69 ms và p99 89,34 ms. Counter Redis Output Cache đạt 100% trong cửa sổ đo. Lượt bổ sung một API ngày 10/11 chỉ hoàn tất smoke. [Kết quả k6](evidence/G06-20261105-cache-retry-summary.json), [counter](evidence/G06-20261105-retry-cache-measurement.json), [lượt một API](evidence/G06-20261110-acceptance-open-conditions.json). | §4.1, tr. 40, NFR-PERF-001/002/003 yêu cầu đo với tải thực tế trên production, một instance 2 vCPU/4 GB phục vụ ≥100 người dùng đồng thời, warm GET p50 ≤150 ms, API p95 ≤500 ms, p99 ≤1000 ms và Redis hit ≥80% ở steady state. Render đang chạy Free, 0,1 CPU/512 MB. Các phân vị và cache trên demo chưa có phép đo production tương ứng. [Render Compute Plans](https://render.com/docs/compute-plans). |
| **D9-04 — Bảo mật và HTTPS** | Staging HTTPS cô lập với API image `49163d6` và web image `069aca9` đạt TLS 1.2/1.3, từ chối TLS 1.0/1.1, redirect 308, HSTS 31536000, CORS và kiểm cookie. ZAP 2.17.0 có 0 FAIL, 1 WARN, 59 PASS. Trên demo, Render/Vercel chấp nhận TLS 1.2 và redirect HTTP sang HTTPS. API Render cấp CORS cho origin Vercel đã cấu hình, bỏ header cấp quyền với origin kiểm tra ngoài danh sách. [Staging](evidence/G06-20261105-closure-https-security-checks.json), [ZAP](evidence/G06-20261105-closure-https-dast.json), [HTTP demo](evidence/G06-20261111-free-demo-checks.json). | §4.2, tr. 41–42 yêu cầu review bảo mật trước khi release và NFR-SEC-005 ghi Nginx với HSTS 31536000. Các phản hồi Render đã kiểm thiếu HSTS. Vercel trả 63072000 giây. MinIO Community `RELEASE.2025-09-07T16-13-09Z` còn advisory High. Bản vá chính thức có trong AIStor và license Free hỗ trợ một node. Việc đăng ký license, thử tương thích, thay server và quét lại hệ thống demo chưa thực hiện. Đây là phần triển khai và xác minh còn thiếu của D9-04. [Advisory High](https://github.com/minio/minio/security/advisories/GHSA-9c4q-hq6p-c237), [AIStor Free](https://docs.min.io/aistor/installation/container/install/). |
| **D9-09 — Hai API và scaling** | Hai API qua Nginx trong lab đã kiểm JWT, quota Redis dùng chung, cách ly cache, Hangfire/PostgreSQL, hai worker và RedLock. Cùng tải đọc cố định 100 VU, warm p95 giảm từ 11,43 xuống 9,06 ms. [Kiểm hai instance](evidence/G06-20261031-multi-instance.json), [phép đo một và hai API](evidence/G06-20261105-next-paired-public-summary.json). | NFR-PERF-002 tr. 40 yêu cầu năng lực tăng tuyến tính khi thêm instance. NFR-SCALE-001/003 tr. 44 yêu cầu shared state và Nginx upstream cho nhiều API. Render Free chỉ hỗ trợ một instance trên dịch vụ hiện tại. Phép đo cố định 100 VU cho thấy độ trễ ở một mức tải, còn thiếu phép đo năng lực tối đa một và hai instance trên production cùng cách đánh giá tuyến tính theo DEC-11. [Giới hạn Render Free](https://render.com/docs/free#other-limitations), [DEC-11](../dec-11-measurement-proposal.md). |
| **D9-10 — Hạ tầng production** | Lab MinIO bốn container/bốn volume trên một máy giữ đọc/ghi khi dừng một node và qua heal/checksum. Demo dùng MinIO trên named volume, có bằng chứng build image multi-stage/non-root ở lab. Vercel phục vụ `_next/static` qua CDN. Kiểm mới một tệp JS trả 200, cache HIT, immutable 31536000 giây và SHA-256 khớp bằng chứng bản `df0dbb9`. [Lab MinIO](evidence/G06-20261031-minio4.json), [bundle CDN](evidence/G06-20261110-layout-provider-primary-bundle-cdn.json), [HTTP demo](evidence/G06-20261111-free-demo-checks.json). | NFR-SCALE-003 tr. 44 ghi MinIO distributed ≥4 node trong production, Nginx upstream cho nhiều API, các dịch vụ chạy riêng và Cloudflare CDN cho `_next/static`. Demo có MinIO một node qua Quick Tunnel và dùng CDN Vercel. Distributed AIStor thuộc Enterprise Lite/Enterprise. Quick Tunnel có URL tạm thời và không có bảo đảm uptime. Việc dùng CDN khác Cloudflare là sai khác cấu hình được ghi nhận. [License AIStor](https://docs.min.io/aistor/operations/licenses/#minio-aistor-free-community-use), [Cloudflare Quick Tunnels](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/). |

PDF §2.4.1 tr. 13 ghi tối thiểu 2 vCPU, 4 GB RAM và 20 GB SSD. §5.4 tr. 48 ghi 50 GB SSD, mạng ≥1 Gbps và IP tĩnh. Hai mục ghi dung lượng khác nhau. Chưa có bằng chứng hạ tầng miễn phí hiện tại đáp ứng các chỉ tiêu SSD/mạng/IP của profile production đầy đủ. Render Free dùng filesystem tạm thời và không hỗ trợ persistent disk gắn vào web service. Bằng chứng độ bền D9-07 thuộc Neon và volume MinIO hiện có. [PDF SRS](../../SRS_Culinary_Blog_v1.0.0.pdf#page=13), [Render Free](https://render.com/docs/free).

**Khôi phục ảnh demo miễn phí.** Kiểm trực tiếp phát hiện `/health` và ảnh mẫu trả 503 trong khi `/health/ready` trả 200. MinIO local, gateway và Quick Tunnel hiện hành đều trả 200. Gateway chặn truy cập anonymous vào root/admin/bucket và từ chối marker unsigned trailer. Ba mẫu đọc SigV4 qua tunnel khớp byte local. Cấu hình Render chỉ đổi `ObjectStorage__Endpoint`; 25 giá trị khác giữ nguyên. Lượt deploy cùng commit đầu tiên vẫn ghi lỗi DNS với hostname cũ. Lượt `deploy_only` `dep-dbpppj0n74is738lsrjg` áp cấu hình lên bản build `41f619b`. Sau đó cả ba endpoint health và ba biến thể ảnh đều trả 200, SHA-256 của ảnh qua Render và Vercel khớp. HSTS trên Render vẫn vắng mặt khi kiểm lại. [Kết quả trước và sau khôi phục](evidence/G06-20261111-free-demo-checks.json).

## Rà soát nghiệm thu 10/11/2026 — Đóng task theo yêu cầu PDF

<a id="acceptance-review-10112026"></a>

**Đã đóng thêm D9-03 và D9-07: hiện5task đóng/4task mở.** D9-02/03/05/06/07[x]; D9-01/04/09/10[ ]. Review này cập nhật trạng thái nghiệm thu, không đổi PDF/A/B/C hoặc viết lại kết quả đo cũ. Checkpoint và manifest10/11 bên dưới giữ nguyên như lịch sử tại thời điểm phát hànhdf0. Không yêu cầu thêmGoogleDrive, chờ28ngày,100người thật hoặc phát hành mọi thay đổi dirty trong repository để nghiệm thu bản đã định danh.

### D9-03 — Đạt hiệu năng frontend trên bản production đã chọn

Đã bổ sung **Lighthouse trực tiếp trên primaryproduction**`https://culinary-blog.vercel.app`,7routes×3runs theo nguyên runner/config/gates hiện có: **42/42assertions đạt,0WARN/0FAIL,exit0**. Inspect trước và sau xác nhận cùng deployment`dpl_CzWEgVuacUVkQFs9oKSweUPb1YHM`/source`df0dbb927e0430a485d47570e8849a11340065cb`; không lấy nativeLighthouse làm phép đo production. Một lượt GET tuần tự mỗi route làm warmup, thời gian cold của lượt này giữ riêng; không load/stress/VU hoặcDAST công khai. [Lighthouse production](evidence/G06-20261110-acceptance-production-lighthouse.json).

Budget đúng providergraph gồm20page routes/58JSassets đã kiểm trước đó và chưa đổi; gzipupperbound189577bytes, toàn bộ assets có compiledSHA khớp. Cùng source/deployment đã có18acceptedmetric từ6lượt tương tác trên browser thật, window10/11`16:17:40.672–16:18:25.247UTC`, observedmaxLCP1728ms/CLS0,000602/INP32ms. INP được đo riêng quaweb-vitals/EventTiming, không dùngTBT. Mẫu có điều khiển được ghi đúng tên, không giả số người dùng thực hay chỉ sốpopulationp75. [Review từng điều kiện](evidence/G06-20261110-acceptance-frontend.json).

Task gốc cho phép phép đo **tương tác hoặcRUM phù hợp**. Đã làm rõ dòng gatephase31 trước đây chỉ ghiRUM để khớp task/PDF, giữ nguyên các ngưỡng số và bằng chứng bắt buộc. Không đóngD9-03 bằng cách bỏgate; phép đo còn thiếu trênproduction đã thực hiện và đạt. Bản được nghiệm thu làdf0; việc phát hành mọi thay đổi chưa chọn ở root không phải điều kiện mới củaPDF. Artifact khác hoặc source liên quan đổi phải kiểm lại; việc đóngD9-03 không chứng nhận khả năng chịu tải/scaling củaD9-01/09 hay topologyD9-10.

### D9-07 — Đạt lịch sao lưu, độ bền và khôi phục dữ liệu hiện tại

PDFtr43 yêu cầu pg_dump tự động03:00/giữ30ngày, WAL, MinIOpersistent, tokenDB survive restart và softdelete. Lịch03UTC theo DEC-10 đã bật, DaysInterval1/StartWhenAvailable; action thực chạy qua TaskScheduler kết thúc0. Đây là kiểm action bằng Start-ScheduledTask, không phải tuyên bố đã quan sát lượt timer03UTC ngày11/11 hoặc có lịch sửbackup30ngày. Cấu hình giờ/retention được kiểm trực tiếp và bằng testboundary, không cần chờ30ngày để chứng minh thuật toán. [Review từng điều kiện](evidence/G06-20261110-acceptance-backup.json).

SourceNeon read-only,30tables2044rows/5migration/keyring encrypted được restore vào PostgreSQL mới và so đủ rowhash/schema; WAL walproposer/synchronouscommit đã kiểm. Dữ liệu/token/keyring trênNeon giữ nguyên qua Renderrecreation. MinIO301object trên namedvolume giữ nguyên qua restartDocker; restore301object/301SHA05/11 đã được bind lại với nội dung hiện tại10/11. Không backup ảnh mới hoặc tiếp tụcDrive. Bản PostgreSQL lưu tạiWindows tách khỏi nguồnNeon, đáp ứng phần storageđộc lập cho backupDB; ảnh được phục hồi vào bucket riêng trong lab kiểm thử.

RTO đã đo: DBrestore/verify4,384s; mediarestore/verify3,135s ở lượt05/11; đây là hai phép đo có phạm vi riêng, không phải thời gian phục hồi toàn deployment. Chu kỳbackup dự kiến24h khi máy hoạt động; độ mới thực tế tính từ timestamp snapshot, không có cam kếtRPO/RTO riêng được PDF đặt ra. Máy/user/Docker/mạng phải hoạt động là điều kiện vận hành được ghi trong [runbook](../../../operations/neon-backup-windows.md). **Việc mất cảPC/ổ đĩa và mọi bản sao không phải kịch bản mà PDFtr43 yêu cầu; không dùng điều kiện bổ sung này để giữD9-07 mở.** Bảo mậtMinIO và topologyproduction4node vẫn mở tạiD9-04/D9-10, không được suy đạt từD9-07.

### Bốn task còn thiếu điều kiện thực tế

Providerinventory mới xác nhận backendRender đangplan`free`,1instance,regionSingapore; không có thay đổi cấu hình/phí. RenderFree không hỗ trợ scale nhiềuinstance theo [tài liệu chính thức](https://render.com/docs/free). [Bằng chứng các điều kiện còn mở và cleanup](evidence/G06-20261110-acceptance-open-conditions.json).

| Task | Điều kiện chưa có để đóng |
|---|---|
|D9-01|Productiontarget được phép đo tải, profile2vCPU/4GB, đúng source/data/config, smoke/load/stress≥100user và cachewindow/latency đạt. RenderFree hiện tại chưa là bằng chứng cho profile này.|
|D9-04|MinIOserver đã xử lý High/Critical trên phiên bản/artifact được phép triển khai; gatewayguard và công cụ đã vá không thay bảnváserver.|
|D9-09|Ít nhất hai backendproduction qua loadbalancer, đo capacity/tuyến tính đúngDEC-11; cặp localfixed100VU trước chỉ so latency và không chứng minh capacity.|
|D9-10|MinIOproductiondistributed4+node được vá và license phù hợp, persistent/topology/upstream/CDN đúngPDF; lab4container không phải deploymentproduction.|

- D9-03/D9-07 đã đóng theo các review phía trên; giữ phạm vi artifact/window/điều kiện vận hành đã công bố, không tự suy toànG06đạt.
- D9-01/D9-09: **PDFtr40§4.1 có yêu cầu production với tải thực tế**, không thể bỏ câu này để đóng từ local. Lượt bổ sung singleAPI đã setup/smoke đạt, sau đó dừng chủ động khi rà lại yêu cầuproduction; load bị ngắt giữexit1, không đưa số liệu chưa hoàn tất thànhpass. Projectriêng đãcleanup0; không chạy tải trênRender/Vercel công khai. Không tự nhận lệnh “đóng ngay” là phê duyệt môi trường/chi phí hoặc thay ngưỡng tuyến tính còn mở trongDEC-11.
- D9-04/D9-10: bảnMinIOserver hiện có vẫn thuộc phạm vi ảnh hưởng của advisoryHigh. Nhà cung cấp công bố bản váAIStor; tài liệu hiện hành yêu cầu license cho production vàFree chỉsingle-node. Không tự mua/đăng kýlicense, chuyểnstorage hoặc thayserverđangdùng. [Advisory chính thức](https://github.com/minio/minio/security/advisories/GHSA-9c4q-hq6p-c237), [điều kiện triển khai/license](https://docs.min.io/aistor/installation/linux/install/deploy-aistor-on-ubuntu-server/). Lab4node vàCDNVercel hiện có không đủ để chứng nhận toàn bộtopologyproduction theoPDF.

Không mởgói khác/phase10/28/36; không giảm gate hoặc tự tạo ngoại lệ. Các task thiếu môi trường production/bảnvá/license chưa được đánh hoàn thành. Checkpoint private mới`tmp/package-G06/acceptance-closure-20261110/STATE.md`. [Manifest review](evidence/G06-20261110-acceptance-manifest.json), [validation review](evidence/G06-20261110-acceptance-validation.json); các manifest/proof trước vẫn giữ nguyên, hashtracking cũ phản ánh đúng snapshot lịch sử của chúng.

## Checkpoint 10/11/2026 — Khôi phục runtime, sao lưu PostgreSQL và sửa CLS

<a id="checkpoint-10112026"></a>

**G06 chưa đạt toàn bộ; D9-02/05/06 giữ[x], D9-01/03/04/07/09/10 giữ[ ].** Phạm vi chỉ phase30–35. Giữ PDF và đính chính A/B/C, ưu tiên demo PC nhưng không bỏ responsive được PDF trang42 yêu cầu. Không chạy G07 hoặc phase10/28/36. Không tiếp tục ảnh/Drive, mua tài nguyên, migrate/reset Neon hoặc chạy tải/DAST trên dịch vụ công khai.

### Runtime và dữ liệu — phase32/33/34

Docker Desktop đã hoạt động lại; giữ nguyên13containerID và volume của người dùng. Khôi phục đúng gateway/tunnel hiện có; health200, đường anon/admin403,301object và ba biến thể ảnh đọc được. Chỉ đổi `ObjectStorage__Endpoint` trên Render;25giá trị môi trường khác giữ nguyên. Lần restart đầu chưa áp endpoint mới và media503 được giữ như evidence thất bại; deploy lại **cùng commit41f619bd1d91079dbd37ee73c10c44fbbc5a6298** mới có hiệu lực. Render deployment`dep-dbpj3c6tlv9c73fnjg0g`; không phát hành backend source/schema mới. Sau sửa11HTTP/media checks200, byte/SHA ảnh qua Render và Vercel khớp. [Endpoint](evidence/G06-20261110-render-endpoint-change.json), [HTTP/media](evidence/G06-20261110-live-read-checks.json).

Neon30tables/2044rows/5main migrations và encrypted keyring giữ nguyên sau Render recreation; mọi row hash khớp05/11. MinIO301keys/size/ETag khớp bản restore đã kiểm, một LastModified đổi nên chỉ đọc lại đúng object đó và SHA khớp. Không backup/upload ảnh mới. [Neon continuity](evidence/G06-20261110-neon-continuity.json), [binding media](evidence/G06-20261110-media-content-evidence-binding.json). Những kiểm tra đọc này không thay thế bản vá MinIOserver hoặc chứng nhận topology production.

### Sao lưu PostgreSQL tự động — phase34

Thêm `infra/operations/backup-neon.py`, đăng ký lịch và hai test retention có kiểm boundary30ngày/tên lạ/job khác/lượt thất bại. Source read-only snapshot/pg_dump cùng exported snapshot, TLSverify-full; restore vào PostgreSQL sạch, networknone/tmpfs/không port, so đủ30bảng/2044dòng/5migrations và1keyring mã hóa. Đóng góiAES256/header-encrypted bằng khóa hiện có đã xác nhận lưu riêng; giải mã thử và so SHA của ba file. Không ghi/migrate source hoặc restore đè Neon. [Kết quả lịch/restore](evidence/G06-20261110-postgres-backup.json), [runbook](../../../operations/neon-backup-windows.md).

Phát hiện công cụPG18bookworm cũ123High/Critical; Alpine mới còn26finding trong binarygosu không dùng khi chạy non-root. Image riêng ghim officialbase, loại đúng binary đó và chạy non-root: Trivy0.74.0 **0High/0Critical,exit0**, không ignore. Image`941fc536d7db…` đã chạy lại fullbackup/restore và task thực tế thành công; không dùng riêng scan để kết luận restore đạt. [Scan/remediation](evidence/G06-20261110-postgres-tool-security.json).

Task`Culinary-G06-PostgreSQLBackup` đã bật, Limited/Interactive,03:00UTC=10:00Bangkok, retention30ngày theo timestamp UTC, chạy bù/retry; lượt thực chạy10/11`16:07:16–16:07:27UTC`, LastTaskResult0, restore/so dữ liệu4,384s. NextRun11/11`03:00UTC`. Kho PostgreSQL trên Windows tách khỏi Neon/Render. Máy phải bật, user đăng nhập, Docker/mạng khả dụng; không chứng nhận luôn bật, RPO24giờ khi máy tắt hoặc lịch sử30ngày từ một lượt chạy. Chưa chứng minh nơi khôi phục độc lập khi mất toàn bộ PC/MinIO; D9-07 vẫn mở ở phần điều kiện vận hành/khôi phục đầy đủ, không bắt buộc người dùng quay lại Google Drive.

### Bản sửa frontend — phase31/32

Phép đo mới trên primaryd50 phát hiện lỗi thật: CLSrecipesdesktop0,83677/mobile0,10379; coldLCPlogin8,6s. Driverquan sát beacon bị thiếuCLS/INP khi teardown(exit1) được giữ nguyên; exportlogVercel độc lập nhận đủ18metric/6visit, không sửa exit thành pass. Không gộp lượt điều khiển thành RUM. [Các lỗi giữ lại](evidence/G06-20261110-prior-production-failures.json).

Sửa tối thiểu RecipesList: giữ kích thước toolbar/search/count row và chiều cao vùng kết quả để footer không nhảy vào viewport. Không sửa RecipeCard; giữ bộ lọc nâng cao đang có ở root. Candidate mang thêm hai sửa accessibility đã có tại root: nhãn login và tương phản chip. Regression tái hiện đỏ trước sửa; cuối cùng2browsercase mỗi artifact xanh, axe0violation/không tràn ngang. CLS cuối candidate desktop0,000059/mobile0; root desktop0,000239/mobile0. Root153Node/43Jest, candidate135Node/22Jest; lint/types/format/build đều0. Backend không đổi nên giữ157unit/3architecture/290integration và coverage89,84% đúng artifact cũ, không chạy lại toàn bộ. [Gates local](evidence/G06-20261110-frontend-local-gates.json).

Lighthouse mới trên **native candidate`s5rZOTW5vqQkMvgAx7_Cm`**,7routes×3runs:41/42assertions đạt, **0requiredfailure**; WARNperformance trang chủ0,89<0,90 giữ nguyên. Đây là standalone loopback và Render public-read dependency; không gắn kết quả sang imageWeb069 cũ hoặc artifactVercel build riêng. TBT không thay INP. [LH](evidence/G06-20261110-final-native-lighthouse.json).

Commit`df0dbb927e0430a485d47570e8849a11340065cb` trên nhánh riêng`fix/cwv-metrics-20261105`, push thường qua hooks; chỉ8file thuộc sửa UI/backup, rootindex không đổi. Source/newcommit Gitleaks8.30.1exit0. npm audit cùng lock hiện tại0High/0Critical,22Moderate/3Low; không gọi toàn bộ dependency clean. Chỉ đổi WEB_VITALS_BUILD_ID trên Vercel,7envrecords khác giữ nguyên. [Env preservation](evidence/G06-20261110-layout-env-preservation.json), [upload inventory](evidence/G06-20261110-layout-upload-safety.json).

Đã build`--prod --skip-domain`, kiểm protectedstage rồi promote **cùng deployment**`dpl_CzWEgVuacUVkQFs9oKSweUPb1YHM`; inspect primary độc lập xác nhận alias`https://culinary-blog.vercel.app` trỏ đúng bảnREADY/source`df0dbb9`. Rollback trước`dpl_6UHCvBm31hSkn3AWF79uvVDeqV4R`. Không deploy fullroot hoặc backend mới. [Deployment](evidence/G06-20261110-layout-deployment.json).

Stage và primary đều đạt sáu lượt browser desktop/mobile cho login/recipes/search: tìm kiếm, clear, footer/CLS, axe và không overflow/uncaught errors; APIGET200JSON, CSPconnect-srcself, receiver schema/origin400/403. Các lượt không tạo tài khoản hoặc phiên đăng nhập hợp lệ. [Stage](evidence/G06-20261110-layout-staged-checks.json), [primary](evidence/G06-20261110-layout-primary-checks.json). Sau promote,11read/media checks vẫn200 và SHA ba variant qua Render/Vercel khớp. [Final live checks](evidence/G06-20261110-final-live-read-checks.json).

Provider build machineproof bao đủ **20page routes/58JSassets**; tải kiểm từng asset stage và primary, compiled SHA khớp. Webpack suffix toolbar cookieopt-in của Vercel được tách/so prefixSHA; cộng toàn bộ267byte gzip tăng thêm vào **mọi route**, upperbound lớn nhất189577bytes<204800bytes. CDNimmutable/max-age31536000 đã kiểm; không nới budget hoặc suy native sang provider. [Stage assets](evidence/G06-20261110-layout-provider-staged-bundle-cdn.json), [primary assets](evidence/G06-20261110-layout-provider-primary-bundle-cdn.json).

Vercel runtime export exit0,18acceptedmetrics của sáu lượt tương tác có điều khiển trên đúng build, window`16:17:40.672–16:18:25.247UTC` ngày10/11. Cả12cohort/metricgroups đạt ngưỡng; observedmaxLCP1728ms,CLS0,000602,INP32ms. Browser observer cộng layoutshift cả lượt có thể khác CLSsessionwindow của web-vitals; báo cáo hai phép đo tách biệt, không chọn lại số để thay lỗi d50 cũ. Không dùng TBT thayINP hoặc gộp controlledvisits thành RUM; export không có metric ngoài window này. Mẫu còn nhỏ và candidate là bản sửa mỏng, không chứng nhận fullroot hoặc mọi điều kiện mạng/khởi động lạnh. D9-03 vẫn mở ở phạm vi đó. [Provider summary](evidence/G06-20261110-layout-controlled-provider-summary.json), [gate](evidence/G06-20261110-layout-controlled-gate.json), [window/export](evidence/G06-20261110-layout-metrics-export.json).

### Phần còn mở và điểm tiếp tục

| Phase | Trạng thái theo bằng chứng hiện có | Còn mở |
|---|---|---|
|30|D9-02[x]; giữ load/stress/query/cache và cặp public1→2 đã kiểm, sourcebackend không đổi|D9-01[ ]: DEC-11 và target/artifact/tài nguyên nghiệm thu; fixed100VU không chứng minh tăng capacity tuyến tính|
|31|D9-03[ ]; sửaCLS phát hànhdf0;7×3LH0requiredFAIL,58assets/20pages và18acceptedCWV trong ngưỡng|Mức đại diện/window và fullrootartifact; controlled18metrics không thay nghiệm thu mọi route/điều kiện|
|32|D9-04[ ]; backup-toolscan mới0H/C, source/newcommitsecret0; HTTPS/DAST491/069 cũ giữ đúng phạm vi|MinIOserverH/C, không dùng toolmc/gateway hay PostgreSQLtool làm bản váserver; security của artifact đổi phải phân biệt|
|33|D9-05/06[x]; runtime/media và data continuity đã hồi phục; backend/telemetry giữ nguyên|Không mở phase36; không suy vài request200 thành SLA|
|34|D9-07[ ]; lịchDB thực chạy thành công, retention/restore/checksum/durability có evidence mới|Khả dụng lịch khi PC tắt và khôi phục độc lập khi mấtPC/MinIO chưa chứng minh; ảnh/Drive vẫn dừng|
|35|D9-09/10[ ]; giữ multiworker/quota/cache/lock, lab4node và cặp1→2 cũ|Formalcapacity/production4+MinIO được vá, persistence/upstream/đường truy cập ổn định; local4container không phải productionHA|

Checkpoint private`tmp/package-G06/resume-20261110/STATE.md`; trước đổi1384file có hash. Chỉ kiểm lại source/artifact bị ảnh hưởng, giữ mọi thay đổi người dùng và evidence05/11. Các mục thiếu tài nguyên/quyết định không tự trở thành ngoại lệ hoặc task hoàn thành. [Manifest mới](evidence/G06-20261110-manifest.json), [kiểm bảo toàn](evidence/G06-20261110-preservation.json), [validation](evidence/G06-20261110-validation.json). Khi tiếp tục, dùng bảnVerceldf0 hiện tại; không chạy lại frontend/backup/load đã đạt nếu source/runtime/artifact liên quan chưa đổi. Không tự bật lại Drive/ảnh hoặc nới gate để đóng các ô còn mở.

**Cập nhật kế hoạch demo05/11 sau checkpoint:** theo yêu cầu người dùng, bỏ các bước bổ sung tại [phạm vi G06/G07](../demo-scope-2026-11-05.md). Không tiếp tục ảnh/Drive, tìm100người thật, chờ28ngày, tạoSBOM/provenance hoặc hồ sơ/chữ ký riêng chỉ cho demo. Giữ kiểm thử/yêu cầu bắt buộc PDF;3task đóng/6mở không đổi. Đây là sửa kế hoạch, không phải runtime evidence mới; manifest trước giữ nguyên lịch sử.

**Checkpoint lịch sử 05/11/2026 — tiếp tục sau khi dừng sao lưu ảnh: 3 task đóng / 6 task mở.** Đã so tải local một/hai API, kiểm budget trên artifact Vercel và sửa lỗi API cùng origin404 trên website chính. Người dùng yêu cầu bỏ phần sao lưu ảnh/Drive; không đánh phần này đạt. [Checkpoint của lượt05/11](#checkpoint-05112026-after-backup-skip); trạng thái hiện tại ở đầu report.

## Checkpoint 05/11/2026 — API Vercel, phép đo một/hai API và dừng sao lưu ảnh

<a id="checkpoint-05112026-after-backup-skip"></a>

Phạm vi vẫn chỉ G06/phase30–35. PDF/A/B/C giữ nguyên; không push main, deploy backend, migrate/reset Neon, mua tài nguyên hoặc tải/DAST công khai. Đọc kết quả trước và chỉ kiểm lại phần ảnh hưởng. Người dùng hỏi thêm phạm vi demo G06/G07: đối chiếu tài liệu G07 để giải thích, không thực hiện phase37/38 hoặc10/28/36.

### Dừng phần sao lưu ảnh — phase34

Người dùng yêu cầu bỏ phần sao lưu ảnh/Google Drive. Đã dừng công việc này, không tải thêm, tải xuống/restore từ cloud, bật lịch hoặc retention. Một lệnh upload bản mã hóa đã kết thúc exit0 **trước** khi người dùng yêu cầu dừng; chưa kiểm download/restore từ Drive. Không xóa dữ liệu/khóa đã tạo. Công cụ lịch mới chỉ là bản nháp đã chuyển vào tmp, không cài scheduled task. D9-07 giữ[ ]; bản restoreNeon và301ảnh/301SHA local trước vẫn là bằng chứng hợp lệ ở phạm vi đã đo. [Ghi nhận dừng](evidence/G06-20261105-next-backup-skip.json).

**Đối chiếu PDF trang43:** NFR-REL-003 bắt buộc pg_dump tự động mỗi ngày03:00, giữ30ngày, PostgreSQL WAL, MinIO persistent volume và refresh token trong DB. PDF không chỉ định Google Drive hoặc bắt buộc tải toàn bộ ảnh lên Drive. Phương án Drive là lựa chọn triển khai bổ sung, có thể bỏ mà không sửa yêu cầu PDF. Việc bỏ Drive không đồng nghĩa bỏ backup DB/lưu trữ ảnh bền vững; D9-07 còn mở vì chưa có lịch DB vận hành và bằng chứng durability/khôi phục đầy đủ trên deployment. Không tự tạo ngoại lệ mới.

### Phép đo local một API so hai API — phase30/35

Project riêng`culinary-g06-02902a45e485`, synthetic50recipes/20categories,14main migration+1keyring; APIimage49163d… giữ nguyên, mỗi API cap2CPU/4GiB, pool100, sharedPostgreSQLmax250, workers/SMTP tắt. Dùng nguyên profile public-read hiện có100VU5phút,90%warm10%cold, think1s và ngưỡng cũ. Guard bác target cloud trước HTTP(exit107). Đây là phép đo chẩn đoán độc lập; không nghiệm thu scaling khi dependency34 hoặc DEC-11 còn thiếu. [Identity](evidence/G06-20261105-next-paired-identity.json).

| Phép đo | Một API | Hai API |
|---|---:|---:|
| Business GET |29.800|29.813|
| Warm p50/p95/p99(ms)|4,766 /11,428 /37,450|4,683 /9,062 /13,918|
| Cold p95/p99(ms)|20,529 /54,151|17,077 /26,476|
| Unexpected API failures|0|0|
| Cache hit window|89,41%|89,85%|

Hai lượt exit0; k6 iteration-rate87,306→87,361/rate-ratio1,00063. Rate k6 có window setup/teardown, không tự lấy count/300 làm rate. Với100VU cố định và think1s, mức cầu đã bị chặn; tỷ lệ gần1 không đo capacity hay chứng minh tăng tuyến tính. Chỉ một cặp lượt trên sharedDockerVM, không phải ba lần lặp/tài nguyên production dành riêng; counters có độ trễ export. D9-01/09 vẫn mở theo formalDEC-11/target/tài nguyên production. Lab đã dọn đúng project, không đụng stack người dùng. [Kết quả](evidence/G06-20261105-next-paired-public-summary.json), [exit](evidence/G06-20261105-next-paired-pipeline-exits.json).

### Sửa API Vercel và gate của artifact phát hành — phase31/32/35

Đã tái hiện lỗi thật: publicJS dùng`NEXT_PUBLIC_API_BASE_URL=/api/v1` nhưng primaryVercel trả404HTML cho categories/recipes, trong khi Render trả200JSON. SSR dùng INTERNAL_API_BASE_URL nên smoke trang trước chưa phát hiện lỗi browserAPI. [Đỏ](evidence/G06-20261105-next-proxy-red-binding.json). Chỉ thêm rewrite khi`VERCEL=1`, bắt buộc INTERNAL_API_BASE_URL HTTPS hợp lệ; Dockerproduction vẫn dùng Nginx như cũ, không mở thêm CSPorigin. Root giữ nguyên các rewriteISR sẵn có. Thêm4behavioral regression tests cho Vercel/Docker/dev/config sai. [Docker config equivalence](evidence/G06-20261105-next-proxy-docker-config-equivalence.json).

Worktree frontend riêng trên base đang deploy41f619b, branch`fix/cwv-metrics-20261105`: commit9240478 thêm gate budget mạnh vào build và machineproof SHA/gzip/routegraph; commitd50f9dd sửa APIproxy. Cả hai push thường qua hooks, mô tả tiếng Việt không dấu. Gate không giảm200KiB, không bỏ ancestors/global-error. Root153Node/candidate135Node, lint/format/build0; sourceUI/package-lock không đổi nên giữ22Jest/3suites trước. Nativebuild`tWVKFb5qF1kb1-HXqamyT` được kiểm lại APIGET200/invalidsyntheticlogin401, receiver204/403/413, browser desktop/mobile6visits15records. Native Lighthouse **7routes×3runs,41/42 tổng hợp đạt**,0requiredfail; giữ1WARNperformance0,89<0,90 và Lantern diagnostics, không bỏ assertion. Backend157unit/3architecture/290integration và failure drills trước giữ nguyên do backend/Docker runtime config không đổi. [Gates](evidence/G06-20261105-next-frontend-gates.json), [API native](evidence/G06-20261105-next-proxy-native-api-roundtrip.json), [CWV có điều khiển](evidence/G06-20261105-next-proxy-native-cwv.json), [LH mới](evidence/G06-20261105-next-proxy-native-lighthouse.json).

Deploy`--prod --skip-domain`, kiểm protectedstage, sau đó promote **cùng deployment**`dpl_6UHCvBm31hSkn3AWF79uvVDeqV4R` lên`https://culinary-blog.vercel.app`; rollback trước`dpl_HCgPP4wkGH9ybFHAi6xWgF2e1b96`. Source`d50f9dd439000a1ae865b0b84fc6d7465a4926c6`; chỉ cập nhật WEB_VITALS_BUILD_ID,7envrecords khác giữ nguyên, không tiết lộ secret. Primary alias được inspect độc lập vì v13detail trả aliasarray trống dù promote thành công; không suy alias từ array này. [Deployment](evidence/G06-20261105-next-proxy-deployment.json), [env](evidence/G06-20261105-next-proxy-env-preservation.json).

Protectedstage và primary đều đạt: GETcategories20items, GETrecipe1item trả200JSON, credentials synthetic không tồn tại trả401ProblemDetails, receiver schema/origin400/403; browser fetch cùng origin200, maintenance200,0uncaught errors và CSPconnect-src self. Không đăng nhập tài khoản thật/tạo phiên người dùng, không kiểm providerGoogle. [Stage](evidence/G06-20261105-next-proxy-staged-checks.json), [primary](evidence/G06-20261105-next-proxy-primary-checks.json).

Provider build gate đúng graph hiện tại **20page routes/58JSassets** đều≤204800bytes gzip. Kiểm từng asset stage trước promote và primary sau promote: compiledpayloadSHA khớp;57file toàn bộ khớp, webpack có suffix toolbar do Vercel chèn theo cookieopt-in, prefixcompiled khớp. Tính mọi phần gzip tăng thêm vào **mọi route** làm upperbound, không bỏ overhead/giảm gate; staticCDN immutable/max-age31536000 đã kiểm trực tiếp. Số route/asset lấy manifest thật, không ép theo nativeartifact24/59 cũ. Đây là ordinaryGET từng artifact, không loadtest/DAST công khai. [Provider gate](evidence/G06-20261105-next-proxy-provider-bundle.json), [CDN thực](evidence/G06-20261105-next-proxy-provider-primary-bundle-cdn.json). Không dùng CDN của frontend để đóng toàn bộ topology D9-10.

Các lượt browser mới có điều khiển được tách khỏi RUM. Mẫu b4 trước(3metric/1visit) giữ lịch sử, không gộp sang d50. D9-03 vẫn mở về sample/window phù hợp và fullG06artifact. Không tự đặt28ngày hoặc100người thật; không dùng TBT thay INP. Frontend này là bản sửa mỏng, chưa triển khai toàn source root; Render41f619b/Neon5main migration+1keyring không đổi, rootlocal14main migration.

### Trạng thái từng phase và điểm tiếp tục

| Phase | Trạng thái/task | Phần còn mở |
|---|---|---|
|30|D9-02[x]; bổ sung cặp public-read1→2, giữ mixedjourney100VU10min/cache trước|D9-01[ ]: formalDEC-11, productiontarget/artifact/resource theo PDF|
|31|D9-03[ ]; đã sửaAPIproxy404, phát hành gate/budget/CDN, nativeCWV/LH mới|Sample/window phù hợp, fullrootartifact và nghiệm thu hiệu năng|
|32|D9-04[ ]; giữ HTTPS/TLS/DAST stage491/069; proxyCSP/security kiểm đúng phạm vi|MinIO **server** H/C còn mở; không lấy patchedmc/gateway làm bản váserver|
|33|D9-05/06[x], không đổi backend/telemetry|Giữ regression hợp lệ, không tạo lại failure thật|
|34|D9-07[ ]; dừng ảnh/Drive theo người dùng|BackupDB tự động/retention/durability/restore deployment vẫn cần; không bắt buộc chọnDrive|
|35|D9-09/10[ ]; cặp1→2 và CDNfrontend đã bổ sung|Formalcapacity/scaling; production4+MinIOnode được vá/persistence/upstream; lab4node không thayproduction|

**Gate môi trường cuối lượt chưa đạt:** Docker API báo named pipe dockerDesktopLinuxEngine không tồn tại; không quan sát process Docker Desktop/cloudflared. Giữ receipt cleanup trước06:28UTC (13ID bảo toàn, project thử đã dọn); không báo đã kiểm lại runtime hiện tại, không tự khởi động lại hạ tầng trong bước đối chiếu phạm vi demo. Cần xác nhận Docker/MinIO/tunnel hoạt động và kiểm ảnh trước demo; lỗi này không biến thành source regression hoặc pass. [Runtime gate](evidence/G06-20261105-next-runtime-unavailable.json).

Checkpoint private mới`tmp/package-G06/continuation-20261105/STATE.md`; evidence mới prefix`G06-20261105-next-`, giữ nguyên85+20proof cũ. [Manifest](evidence/G06-20261105-next-manifest.json), [bảo toàn](evidence/G06-20261105-next-preservation.json), [kiểm cuối](evidence/G06-20261105-next-validation.json). Không chạy lại fullregression/load/HTTPS lab nếu source/artifact tương ứng không đổi. Không tự tiếp tục ảnh/Drive khi chưa có chỉ dẫn mới. Không thực hiện G07 chỉ từ câu hỏi phạm vi demo.

## Checkpoint tiếp tục 05/11/2026 — HTTPS và Vercel

<a id="checkpoint-05112026-closure"></a>

Chỉ G06/phase30–35; giữ PDF/A/B/C và mọi thay đổi ngoài phạm vi. Tiếp tục quyền triển khai bản sửa đã kiểm thử trên dịch vụ hiện có. Không mua tài nguyên, tải/DAST trên dịch vụ công khai, đổi storage hoặc reset dữ liệu. Chỉ có một sửa source mới trong root: exact location `/api/web-vitals` chuyển tới Next trong `infra/nginx/nginx.production.conf`; tái hiện404 trước sửa và204 sau sửa. [Đỏ](evidence/G06-20261105-closure-cwv-proxy-red.json), [xanh](evidence/G06-20261105-closure-cwv-proxy-green.json).

### HTTPS staging và bảo mật — phase32

Đã dựng rồi dọn project riêng `culinary-g06-https-7aa6dc3b8d84`: synthetic50recipes/20categories,14migration nghiệp vụ, APIimage`49163d607861…`/Webimage`069aca9fa0d4…` giữ nguyên từ gates05/11; không trỏ Neon/Upstash/MinIO thật. API/Web/storage/scanner trong mạng internal; proxy chỉ publish127.0.0.1. CA chỉ trust theo client, không sửa truststore Windows. [Identity](evidence/G06-20261105-closure-https-identity.json).

TLS1.2/1.3 xác thực hostname/certificate đạt; TLS1.0/1.1 bị server từ chối; HTTP308→HTTPS, HSTS31536000, CSP/nosniff/frame/permissions, CORS exact origin và cookie HttpOnly/Secure/SameSite đạt. [Checks](evidence/G06-20261105-closure-https-security-checks.json). Đây là staging HTTPS riêng đúng artifact, không phải triển khai production hoặc scan endpoint demo.

ZAP2.17.0, image digest pin theo workflow hiện có, rules không đổi: **0FAIL/1WARN/59PASS, exit0**, không High/Critical trong dynamic report. Lượt đầu spider nhanh chạm APIquota100/phút, downstream429 làm SSR500; giữ log lỗi. Lượt sau giới hạn **scanner**1req/s và1spiderthread, không nới quota ứng dụng; không còn finding500. Cảnh báo CSP unsafe-inline/wildcard mức Medium và các Informational đã ghi triage, không giả là đã vá. Raw report/cookies/query synthetic giữ private, evidence công khai chỉ có summary/hash. [DAST/triage](evidence/G06-20261105-closure-https-dast.json).

Staging đã dọn đúng project/volumes thử,13container người dùng giữ nguyên ID; không global prune, không stop MinIO/gateway/tunnel. [Cleanup](evidence/G06-20261105-closure-https-cleanup.json). D9-04 vẫn mở vì MinIO **server** còn finding H/C; image công cụ `mc` hoặc gateway guard không thay bản vá server.

### Frontend và phát hành — phase31

Đã dùng Vercel CLI65.11.0 sau người dùng xác nhận đăng nhập; project/team/Git rootDirectory đã đối chiếu. Tạo worktree riêng từ release frontend đang chạy41f619b, branch`fix/cwv-metrics-20261105`; commits `29ff736` và `b4f3376`, mô tả tiếng Việt không dấu. Chỉ thêm collector/receiver/summarizer/tests, routeNginx và tối ưu tải schema lỗi/env. Không đẩy root dirty worktree, không push main hoặc tự deploy backend. Push qua đầy đủ backend/frontend hooks; lỗi checkout CRLF được chuẩn LF trong candidate rồi kiểm lại, không bypass hook.

Candidate đầu bị checker G06 phát hiện5dashboard routes vượt200KiB dù checker cũ Main đạt. Đã giữ cách kiểm mạnh gồm toàn bộ ancestors/global-error, tải `zod/mini` chỉ khi cần parse lỗi và bỏ Zod khỏi env validation nhẹ; không giảm budget hoặc sao chép toàn UI root. Candidate cuối: **129Node/22Jest (3suites)**, lint/typecheck/format/build0; toàn route≤204800bytes gzip, npm production audit0, secret range2commits0leak. Native build`tFot7ni_tHUD-2Tm_0J8M`. Lighthouse **7routes×3runs,41/42assertions tổng hợp đạt** (1performance warning0,89<0,90 theo gate WARN có sẵn; mọi assertion bắt buộc đạt), cùng build; giữ warning Lantern diagnostic trong raw log, không bỏ assertion. [Gates/artifact](evidence/G06-20261105-closure-frontend-gates.json).

Native receiver controls204/403/413 và browser desktop/mobile6visits đạt, log14records đủ LCP/CLS/INP; browser observer chỉ thấy2request/204, không sửa count thành18. Đây là kiểm pipeline có điều khiển, không phải RUM. [Native CWV](evidence/G06-20261105-closure-native-cwv.json).

Thêm đúng3biến production `WEB_VITALS_ENABLED=true`, `WEB_VITALS_BUILD_ID=b4f3376e8001c283faf1773c87cf0e4ccc3b06e1`, `SITE_URL=https://culinary-blog.vercel.app`. Metadata5biến cũ giữ nguyên; không đọc lại/ghi đè giá trị sensitive. Dry upload không có.env riêng/.vercel/node_modules/private artifacts. [Env preservation](evidence/G06-20261105-closure-vercel-env-preservation.json), [upload safety](evidence/G06-20261105-closure-vercel-upload-safety.json).

Triển khai production với`--skip-domain`, kiểm bằng quyền CLI rồi promote **cùng deployment** `dpl_HCgPP4wkGH9ybFHAi6xWgF2e1b96` lên `https://culinary-blog.vercel.app`; giữ rollback`dpl_846HSi2cNSAiV1FSXuv9yjGcxHC1`. URL staging có Vercel SSO: các200 do follow redirect ban đầu bị loại; bản smoke sau dùng automation token do CLI chính thức tạo, không tắt protection, không công khai token. Browser3routes/receiver schema-origin controls đạt. Primary public5routes/CSRF200, receiver400/403 đúng; chưa dùng negative controls làm metric. [Staged check](evidence/G06-20261105-closure-vercel-staged-checks.json), [deployment](evidence/G06-20261105-closure-vercel-deployment.json), [public read](evidence/G06-20261105-closure-vercel-production-read.json).

Đã xuất log đúng deployment, buildID khớp. Hai lệnh browser production observer exit1 vì chỉ capture LCP trước khi pagehide beacon được ghi; giữ thất bại, không sửa exit. Provider log xác nhận CLS/INP/LCP cho mobile/desktop của **lượt có điều khiển**; tách toàn cửa sổ hai lượt và ID routing control. Người dùng xác nhận Chrome/tab mới; đã nhận thêm3metric ngoài control trên `/recipes`/desktop lúc05:33:22–05:33:32UTC: **LCP972ms, CLS0, INP0ms**. Đây là một mẫu đầu tiên, chưa đủ để suy toàn website hoặc mẫu đại diện. [First field summary](evidence/G06-20261105-closure-production-field-first.json). Không nhận13log tự động là RUM thực, không lấy TBT thay INP hoặc tự đặt28ngày/sốmẫu tối thiểu. [Log summary](evidence/G06-20261105-closure-production-cwv-log.json). D9-03 còn window/mẫu phù hợp và đối chiếu artifact fullG06; bản phát hành này là bản CWV tối thiểu, không phải toàn snapshot root.

### Bổ sung sao lưu ảnh thật — phase34

Đã sao lưu **301current objects/94.473.101bytes** qua signedGET tới MinIO local hiện có; không ghi/xóa object nguồn, không dùng root credential để đọc. Listing trước/sau giống nhau. Restore vào MinIO mới chỉ có internal network/tmpfs, bucket private/versioning enabled; reread **301SHA256** đều khớp. Restore+verify **3,135s**, tổng backup+restore8,916s, là đo local chứ không phải RTO production. Script restore hiện có chạy nguyên bản; target/tool/network thử đã dọn,13container người dùng giữ nguyên. [Backup/restore ảnh](evidence/G06-20261105-closure-actual-media-backup-restore.json).

Backup chỉ chứa các version hiện tại, không tuyên bố backup toàn lịch sử object hoặc snapshot nguyên tử đồng thời với dumpNeon00:23UTC. DB/keyring backup trước vẫn hợp lệ; rawảnh/dump/manifest filenames giữ private. Có thể dự trù30bản đầy đủ khoảng2,84GB trước overhead, cần kiểm quota kho thực tế. Đã đóng gói DB+301ảnh+certificate/manifest thành **7z AES-256, header mã hóa,94.639.410bytes**; giải mã rồi kiểm304file hashes khớp, key sai bị từ chối exit2. Key ngẫu nhiên256bit nhập qua stdin, lưu file ACL riêng ngoài archive, không lộ trong argv/chat/Git. [Encrypted roundtrip](evidence/G06-20261105-closure-encrypted-backup.json). Đã xin chọn/cho phép kho Drive cụ thể và lịch theo phương án, chưa upload cloud hoặc đăng ký lịch khi chưa có câu trả lời.

### Mapping từng phase và điều kiện còn lại

| Phase / task | Kết quả hiện tại | Phần còn mở |
|---|---|---|
|30 / D9-01|Giữ load100VU10min/stress/cache đã đạt05/11 trên image491; không chạy lại|FormalDEC-11 và môi trường/artifact/tài nguyên production phù hợp PDF; load2replica cố định chưa chứng minh capacity/scaling|
|30 / D9-02|Giữ[x], query/index/pool/slowlog không đổi|Không có thay đổi ảnh hưởng cần kiểm lại|
|31 / D9-03|Frontend gates và deploy CWV trên Vercel đã có như trên|RUM/window/mức đại diện, fullartifact identity; giữ[ ]|
|32 / D9-04|Staging HTTPS/TLS/DAST đúng API/Web artifact đã kiểm, finding được triage|MinIOserver H/C; giữ[ ]|
|33 / D9-05/06|Giữ[x], source backend/telemetry không đổi; không chạy lại failure drill đã hợp lệ|Không restart backend hoặc làm gián đoạn dependency thật để tạo lại evidence|
|34 / D9-07|BackupNeon30tables2044rows+encryptedkey và ảnh301objects/301SHA restore đã khớp|Kho ngoài PC, lịch03:00UTC/retention30ngày vận hành, fullmedia/certificate và RTO/RPO deployment; giữ[ ]|
|35 / D9-09|Giữ lab2API/sharedstate/quota/privacy/worker/load|So1→2 theo protocol chốt và tài nguyên được chỉ định; giữ[ ]|
|35 / D9-10|Giữ lab4node/quorum/heal, Docker multistage/nonroot|MinIOproduction4+node có bản vá, persisted topology/CDN/upstream đúng deployment; giữ[ ]|

[Phương án và đầu vào còn thiếu](../../../operations/g06-remaining-inputs-2026-11-05.md) phân biệt việc agent có thể sửa và quyết định/tài nguyên chưa được cung cấp. Không tự tạo cloud backup/schedule hoặc mua license. Backend Render vẫn41f619b, Neon vẫn5migration nghiệp vụ+keyring; root local14migration không được tự xem là đã deploy.

### Checkpoint và bảo toàn

Workspace/helper/private logs ở`tmp/package-G06/closure-20261105`; candidate worktree còn giữ cho rollback/tiếp tục, branch đã push. Branch CWV chưa merge main; lần phát hành main sau phải mang theo hai commit này để tránh mất collector. Không tự push main vì sẽ kích hoạt Render ngoài thay đổi frontend cần thiết. TokenVercel/OIDC/env/PFX/raw backup giữ private/ignored, không đưa vào Git/chat. Secret scan công khai sau thêm metadata mã hóa có rawexit1/1falsepositive mô tả cách giữ key (không phải giá trị key), triage0unresolved; không đổi rules/allowlists. [Triage](evidence/G06-20261105-closure-secret-triage.json). Exact private-value scan0matches; evidence05/11 cũ giữ nguyên. Postman CLI vẫn không hỗ trợ `status` (exit1), không nhận đó là API test pass. RootHEADce4ae4e/index trống, giữ mọi diff sẵn có; chỉ rootNginx và hồ sơ G06 được sửa trong lượt này. [Manifest mới](evidence/G06-20261105-closure-manifest.json) ghi delta so1333files đầu lượt và hashes evidence; manifest05/11 trước còn nguyên. Không chạy phase10/28/36 hoặc gói khác.

Lượt tiếp tục đọc manifest này trước: không dựng lại HTTPS lab hoặc chạy lại load/full regression nếu source/artifact liên quan không đổi. Xuất log Vercel đúng deployment/window, loại control trước tổng hợp; nếu đã có user metric thì đánh giá mức đại diện, không suy chỉ từ xác nhận chat. Khi có kho backup/topology/protocol được chốt mới thực hiện phần phụ thuộc. G06 vẫn3đóng/6mở, không đánh hoàn thành từ deployment CWV.

## Checkpoint tiếp tục 05/11/2026

<a id="checkpoint-05112026"></a>

Phạm vi/quyền: chỉ G06/phase30–35; thử synthetic/loopback và hai backend, triển khai bản sửa đã kiểm thử lên dịch vụ có sẵn. Người dùng bổ sung quyền dọn phần Docker không còn cần của dự án, bảo toàn công việc. Không mua tài nguyên, tải/DAST công khai, reset dữ liệu hoặc tự đặt ngoại lệ. PDF/A/B/C giữ nguyên. RootHEAD`ce4ae4e`, main/Render hotfix`41f619b`; rootindex trống, không commit/push toàn bộ worktree có thay đổi ngoài phạm vi.

### Phần sửa và gates đã xử lý

- Migration mới`20261104121500_PublishedContentCacheGeneration` (migration14) chỉ tăng generation khi recipe/composition ảnh hưởng nội dung published. Pipeline so generation trước/sau commit để giữ public cache khi chỉ ghi nháp; giữ schedule nền, metadata lỗi thì evict bảo thủ mà không làm write đã commit thành lỗi. Regression tái hiện đỏ trước sửa; không sửa identity migration cũ, không apply schema14 lên Neon thực/schema5.
- Strict build **0warning/0error**, format0; **157unit +3architecture +290integration**,0skip. Integration/architecture giữ từ full run đã đạt; sau đó chỉ thêm mock `IPublicCacheVersion` vào hai unit fixture và chạy lại toàn bộ157unit. Không biến exit1 của full run cũ thành pass; [gate tổng hợp](evidence/G06-20261105-cache-combined-gate.json), [TRX counters/hashes](evidence/G06-20261105-test-counters.json). Application unit coverage **920/1024=89,84%**, vượt gate hiện có80%;3control tests đạt. [Coverage](evidence/G06-20261105-final-coverage-gate.log).
- Fixture Hangfire bind đúng storage riêng, tránh process-global trỏ nhầm DB giữa test hosts; không thay worker production. Redis recovery test giữ deadline15s và chờ cache HTTP thật hồi phục. Ba UI bị ảnh hưởng đạt lại: tạo nháp, publish/unpublish, search. Cùng frontend build`DuFuwIA9Nwlu7KlpSfAmx`; giữ Node149/Jest43, bundle24/24, CWV6visits/18records (INPmax24ms), Lighthouse7×3 đã đạt04/11 vì frontend không đổi. [Browser](evidence/G06-20261105-browser-cache-run.json).
- APIimage`49163d607861…`, Webimage`069aca9fa0d4…`: build/Trivy0.74.0 exit0, **0High/0Critical**, DB04/11. Docker inspect sau cleanup, tar config/layers và Trivy identity khớp; [binding](evidence/G06-20261105-image-identity-final.json). Webimage này có identity riêng với native UI build; không gắn Lighthouse/CWV của build khác sang nó. NuGet advisory check05/11 không finding; npm audit exit1 với3Low+22Moderate,0High/0Critical. Không tự đổi dependency ngoài phạm vi.

### Kết quả tải local trên image đã sửa

CWD`D:/Culinary_Blog`, project`culinary-g06-d89e049a7662`, dataset đầu50recipes/20categories, appmigration14/keymigration1; hai upstream Nginx đã kiểm. MỗiAPI giới hạn2CPU/4GiB/pool100, labPGmax_connections250. Docker Desktop12CPU/~7,68GiB RAM dùng chung với stack người dùng, không phải hardware production dành riêng. Windows k6/HTTPloopback, không TLS/WAN; sáu HTTP nghiệp vụ/iteration (3GET/3write nháp), thinktime1s; không upload/publish/email trong traffic này. Workers/SMTP tắt và rate override chỉ lab (global1.000.000/auth1.000), không thay production. [Summary/identity](evidence/G06-20261105-cache-retry-summary.json).

| Lượt | Iterations | HTTP gồm control | CachedGET p50 ms | APIp95 ms | APIp99 ms | Failure rate | Exit |
|---|---:|---:|---:|---:|---:|---:|---:|
| Smoke5VU/30s | 140 | 856 | 6.033 | 45.894 | 91.632 | 0 | 0 |
| Load100VU/10min | 52776 | 316862 | 7.106 | 53.686 | 89.344 | 0 | 0 |
| Stress ramp0→50→100→150→0/5min | 23090 | 138846 | 8.890 | 76.487 | 123.636 | 0 | 0 |

Stress được cấu hình đỉnh150 nhưng không giữ đỉnh; gauge ghi max149, allocation150. Không tuyên bố đã duy trì150users hoặc tìm được capacity cực đại. Load100VU giữ đủ10min. Các ngưỡng p50≤150/p95≤500/p99≤1000ms và failures0 đều đạt trong phạm vi đo.

OutputCache delta quan sát hai replica: load **99719hit/0miss=100%**; stress **41380hit/8miss≈99,9807%**, đều≥80%. Snapshot lấy trước setup k6 và sau20s settle, không chọn cửa sổ theo giá trị. Counter OTLP có độ trễ xuất, có thể vắt qua ranh giới lượt; không dùng số counter làm tổng số HTTP hoặc bằng chứng capacity. [Load counter](evidence/G06-20261105-retry-cache-measurement.json), [stress counter](evidence/G06-20261105-retry-cache-measurement-stress.json). Không gán lại89% của read-only31/10 lên artifact mới.

Lượt đầu cùng image bị ổC hết chỗ/Docker500:100HTTP0/timeouts, exit1, stress chưa chạy; giữ nguyên [lỗi host](evidence/G06-20261105-benchmark-host-failure.json) và artifact`interrupted-*`, không tính là đạt. Lượt retry sau xử lý host có smoke/load/stress/cleanup đều0; tài nguyên thử đã dọn và13container người dùng giữ nguyên ID. Runner dừng riêng k6 khi monitor lỗi hoặc Cfree<5GiB, kiểm10GiB trước chạy; đây là bảo vệ host, không phải sửa ngưỡng SRS.

### Dọn Docker và dữ liệu thật

- Đã dọn8container lab hỏng,8tagimage G06 superseded,58cache backend và26cache frontend theo exactID/lineage của Dockerfile dự án. `buildx prune --all` luôn có filterID đã duyệt; không globalprune, không xóa ảnh/DB/volume ứng dụng. Giữ imageAPI/Web hiện tại, hotfix rollback, MinIO tools, mọi image của21container còn lại và41volume. Các app khác giữ nguyên. Không chuyển cache NuGet.
- Docker báo23,65GB+6,181GB cache được dọn; offline TRIM/compact giảm VHD **72,82→38,20GiB**, ổC **0→34,75GiB trống** tại thời điểm cleanup. Sau bài tải còn khoảng32GiB trống. [Kết quả cleanup](evidence/G06-20261105-docker-cleanup-final.json). Không cộng các số cache thành dung lượng Windows thu hồi vì có layers dùng chung.
- Sau khởi động lại: localready/recipe200, MinIO **301objects/300Neon references đủ**,3media SHA giữ nguyên/anonymous403. [Media](evidence/G06-20261105-tunnel-media-after-cleanup-20261105.json). Renderrelease/ready/health/recipe/detail/3media200, vẫn hotfix`41f619b`; không sửa env/deploy/restart Render trong lượt05/11. [Render](evidence/G06-20261105-render-after-docker-cleanup-20261105.json). QuickTunnel`ron-christmas-browsers-midwest.trycloudflare.com` vẫn phụ thuộc PC/cloudflared.
- Backup Neon read-only05/11 sau hotfix và restore vào PostgreSQL18 networknone/tmpfs mới: **30tables/2044rows/5migration khớp count và toàn bộ sorted-row hashes**, phục hồi **1key mã hóa, không plaintext**; restore+verify3,092s là phép đo local, không phải RTO production. Target thử đã dọn. [Backup](evidence/G06-20261105-neon-backup-20261105.json), [restore](evidence/G06-20261105-restore-20261105.json). Archive/PFX private giữ ACL, không đưa vào Git, không rotate cert.

### Từng task và điều kiện còn mở

| Phase / task | Trạng thái | Bằng chứng hiện tại / phần chưa đủ |
|---|---|---|
|30 / D9-01|Mở|Load/stress local đạt như trên; còn DEC-11 về workload/đánh giá capacity, artifact/môi trường nghiệm thu phù hợp. Worker-off/nháp/loopback không thay production100users.|
|30 / D9-02|Giữ đóng|Query10k/EXPLAIN/N+1/pool100/slowlog thuộc integration hiện tại đã đạt; không tự thêm partition/replica.|
|31 / D9-03|Mở|UI/CWV/INP/budget/Lighthouse local đạt; chưa có RUM production phù hợp và full candidate deploy có identity. Không tự đặt28ngày/sốmẫu mới.|
|32 / D9-04|Mở|API/Web0H/C; MinIOserver cũ vẫn có gap security, CLI/gateway sạch không vá server. Chưa có HTTPS dynamic/TLS nghiệm thu fullartifact được xác định/cho phép.|
|33 / D9-05|Giữ đóng|Telemetry/traces/log/dashboard evidence còn khớp source; integration hiện tại và health/media sau cleanup đã kiểm.|
|33 / D9-06|Giữ đóng|Dependency/Redis recovery/privacy/job/media tests đạt; actual keyring giữ qua restart04/11, phục hồi archive mới có encryptedkey.|
|34 / D9-07|Mở|Backup/restore mới đạt; chưa triển khai lịch03:00UTC/retention30ngày và nơi backup media/certificate độc lập phù hợp. D trên cùngPC không độc lập với MinIO trênPC.|
|35 / D9-09|Mở|HaiAPI/upstream/sharedstate functional+load đạt; fixed offered-load không chứng minh scaling tuyến tính/capacity hoặc productionmultiworker.|
|35 / D9-10|Mở|Đã có4node lab lịch sử, Docker multistage/nonroot; thiếu productionMinIO4+node/CDN theo PDF và giải pháp MinIO có bản vá/quyền sử dụng phù hợp.|

Chi phí mặc định0: chưa có máy chủ bổ sung hay nơi backup độc lập được cung cấp. Theo [AIStor Free Agreement](https://www.min.io/legal/aistor-free-agreement), bản Free chỉ standalone/single-node, không cấp distributed. Tài liệu [distributed Compose](https://docs.min.io/aistor/installation/container/distributed/) nêu cùng một host dành cho dev/test/staging, production dùng các host tách biệt. Đã đối chiếu05/11; không tự mua license, đổi fork/storage hoặc tạo ngoại lệ PDF. Cần tài nguyên/phạm vi hợp lệ do chủ môi trường cung cấp/quyết định để xử lý phần còn lại.

### Secret và bảo toàn cuối

Gitleaks8.30.1 source delta134files: rawexit1 với128detections (digest/fingerprint/DockerID119, RenderID6, imageTag3); đã triage **0unresolved**, không đổi rule/allowlist. Ba tag đối chiếu đúng image hotfix còn giữ và Trivy identity. Kiểm exact private values trên toàn bộ fileGiteligible **0matches**, không log value; historyrange`7ed7a58..41f619b`23commits **0leak/exit0**. [Review](evidence/G06-20261105-secret-review.json), [triage](evidence/G06-20261105-secret-triage.json). Postman CLI hiện có báo`unknown command status`; bộ xUnit/k6/Playwright có sẵn là bằng chứng thực thi, không ghi CLIsetup là pass.

### Tiếp tục từ checkpoint này

1. Work`tmp/package-G06/resume-20261104`; baseline1206files trước lượt04/11, mọi thay đổi ngoài scope được giữ. Lượt host lỗi ở`benchmark-cache-interrupted-20261105`, lượt xanh ở`benchmark-cache`, exits`cache-retry-*`. Không chạy lại native/UI/load/scan nếu source/artifact không đổi.
2. Giữ certificate`tmp/data-protection/certificate.base64` và backup private ngoài Git. Neon5migration nghiệp vụ +1keyringmigration khác candidateG06/schema14; không apply/seed/reset tự động. Quyền deploy đã có, nhưng không đưa cả dirtyworktree có prerequisite ngoài phạm vi lên main. Vercel tools vẫn chưa sử dụng được trong phiên; không hỏi lại quyền đã có.
3. G06 còn D9-01/03/04/07/09/10[ ]; D9-02/05/06[x]. Không chạy gói khác hoặc phase10/28/36. Secret review đã chốt; [manifest cuối](evidence/G06-20261105-manifest.json) và [validation](evidence/G06-20261105-validation.json) dẫn tới gates/source hashes/cleanup. Thiếu resources/acceptance thì tiếp tục từ điều kiện cụ thể ở bảng, không báo hoàn thành.

## Checkpoint tiếp tục 04/11/2026

<a id="checkpoint-04112026"></a>

Quyền hiện tại: triển khai bản sửa đã kiểm thử lên dịch vụ có sẵn, thử riêng với dữ liệu synthetic và hai backend; không mua tài nguyên, xóa/reset dữ liệu hay chạy tải/DAST công khai. Dữ liệu hiện hữu là dữ liệu thử. Không yêu cầu secret qua chat. PDF/A/B/C giữ nguyên.

- **Source/gates:** root HEAD`ce4ae4e`, main remote`8b9a7e7`. Source khác snapshot cũ do thay đổi đã có của người dùng. Root strict build **0W/0E**, **155unit+3architecture+289integration**, không fail/skip. Candidate Render`41f619b` dựa main mới: **105unit+3architecture+171integration**, compiler0W/E. Không gắn số đo image cũ cho source mới. [Root tests](evidence/G06-20261104-root-tests.log), [candidate](evidence/G06-20261104-candidate-tests.log).
- **Phase30 / D9-01:** read-only31/10 đã xong:100VU/5min một rồi hai API, mỗi API2CPU/4GiB, exits0. Fixed offered-load chưa chứng minh capacity/scaling tuyến tính. Read/write100VU/10min current image đã chuẩn bị, chưa chạy. Formal DEC-11/production còn mở. D9-02 giữ đóng, IT mới có query/pool/slowlog gates.
- **Phase31 / D9-03:** sau lazy Modal: Node149/Jest43, lint/typecheck/format0; BUILD_ID`DuFuwIA9Nwlu7KlpSfAmx`. Ba UI bị ảnh hưởng đạt, gồm mở/đóng/focus Modal;4hành trình không ảnh hưởng giữ từ UI7 trước trên source chỉ khác Modal/test. CWV6visits/18records (INP max24ms), bundle24/24 và Lighthouse7×3 toàn bộ required gates đạt, script max204244B≤204800B. Không nới budget; giữ lượt đỏ204917B để đối chiếu. [CWV](evidence/G06-20261104-cwv-summary.json), [Lighthouse](evidence/G06-20261104-lighthouse.json). Còn deployment/RUM phù hợp; local không thay production.
- **Phase32 / D9-04:** root runtime chuyển10.0.13→10.0.14 pin digest để vá GHSA-p3jp-9v83-76w9; candidate main mới đã có runtime14. Hai image mới **0H/0C**, TrivyDB04/11; tar config/layers khớp Docker identity. [Identity](evidence/G06-20261104-image-identity.json), [root scan](evidence/G06-20261104-root-api-scan.json), [candidate](evidence/G06-20261104-candidate-api-scan.json). Root scan đầu lỗi shared cache lock, chạy lại tuần tự exit0. MinIO server cũ vẫn có security gap; CLI/gateway không vá server. HTTPS staging/dynamic còn mở.
- **Phase33 / D9-05/06:** giữ đóng theo phạm vi task. Candidate14files Gitleaks0, compiler/format/hooks0; đã push nhánh riêng rồi fast-forward main`8b9a7e7→41f619b`, không mất commit người dùng. Render tự deploy`dep-dblhtudckfvc73fircbg` live12:05:42UTC. Chỉ thêm2biến DP và đổi endpoint; mọi giá trị khác được GET/compare bảo toàn. Health/ready/release/recipe/detail/3media đều200; release đúng41f619b. Restart đúng service qua CLI, cùng1encrypted key/fingerprint còn nguyên, không plaintext. [Render](evidence/G06-20261104-render-after-restart.json), [keyring](evidence/G06-20261104-neon-keyring-after-restart.json), [env delta](evidence/G06-20261104-render-env-update.json). Đây là hotfix schema5, không phải toàn bộ source G06/schema13.
- **Phase34 / D9-07:** backup Neon mới04/11 đồng bộ snapshot30tables/2043rows, archive204453B/SHA`04c1ca684c3e5648ca5c376368912e89a9c4f161a0d7bd0027fc4842f065f129`, list verified, thư mục ngoài repo ACL hạn chế. Trước deploy: key rows0/keymigration1/main5; sau deploy/restart key rows1 mã hóa;17public tables ngoài keyring count/hash khớp trước, không apply migration/seed/reset Neon. [Backup](evidence/G06-20261104-neon-backup.json). Giữ rehearsal/restore31/10, không nói archive mới đã restore. Còn lịch backup/retention/nơi lưu độc lập deployment.
- **Phase35 / D9-09/10:** giữ functional2API/quota/privacy/locks/workers và MinIO4node lab đúng hashes. Read-only cũ không chứng minh current image/capacity; chưa có MinIOproduction4+node/CDN, không suy4container cùngPC thành4failure domains.
- **Provider:** plugin installed/enabled nhưng Render Unknown tool/Vercel không expose kể cả reload. Đã dùng fallback CLI chính thức, user Authorize CLI và xác nhận My Workspace; đọc đúng service/source trước thay đổi. CLI không hỗ trợ env nên dùng API per-key được tài liệu Render xác nhận, secret đọc từ private file trong memory, không log/chat/Git. [Access](evidence/G06-20261104-provider-access.json). Endpoint mới`https://ron-christmas-browsers-midwest.trycloudflare.com`:301objects/300Neon references đủ,3SHA khớp/anonymous403. Giữ gateway/IAM/bucket/credentials; Quick Tunnel vẫn phụ thuộc PC/process. Vercel chưa điều khiển được.

### Điểm tiếp tục và bảo toàn

1. Work`tmp/package-G06/resume-20261104`, baseline1206files. Giữ mọi diff sẵn có; chỉ Dockerfile runtime, RecipesList/test Modal và tracking đã đổi trong lượt, nay thêm regression draft/cache chuẩn bị. Runners ở tmp, root index không stage hàng loạt.
2. Frontend final gates xanh; không rerun phần này khi chỉ sửa backend cache. Backend measured image36ac có545source hashes trong [binding trước cache fix](evidence/G06-20261104-backend-before-cache-fix.json), test155/3/289 và image0H/C còn đúng artifact đó.
3. Read/write lab đầu PGdefault100 đã HTTP500/SQLSTATE53300 dưới2API pool100mỗi bên; đã dừng, diagnostic100VU45s/abort15s xác nhận. Chỉ labPG sửa max_connections250; smoke5/30s và diagnostic100/45s exit0 (warm p50≈84,53ms, APIp95≈308,54ms,p99≈702,04ms, failureRate0). Giữ lỗi và cleanup; không thay Neon/Render pool.
4. Lab`benchmark-round2`, project`culinary-g06-2d56324bd70a`: lượt100VU10min bắt đầu12:05:44UTC, same image36ac/2CPU4GiB/API, fresh50recipes20categories,2upstreams. Metrics/cleanup đã xong;25samples cố định12:09:41→12:15:41UTC đo1743hits/45444misses=3,69%. Đang kiểm DBtrigger mọi draft write bump PublicCacheState và IContentMutation luôn evict public tags. Không gọi cache đạt từ read-only lịch sử89% trên image khác.
5. `DraftCacheIsolationTests.cs` đã tái hiện đỏ: generation871→874 khi chỉ tạo draft/composition. Lượt100VU10min image36ac exit0:43331iterations, APIp95=184,10ms/p99=360,07ms, cachedGETp50=34,16ms, failures0; cache window12:09:41→12:15:41UTC chỉ3,69%, không đạt80%. Đã dọn lab. Đang sửa local: migration14`20261104121500_PublishedContentCacheGeneration` thêm trigger chỉ theo nội dung published (không sửa migration cũ), pipeline so generation trước/sau để tránh evict khi public data không đổi, giữ lịch refresh nền như cũ; fresh generation read lỗi thì vẫn evict bảo thủ sau commit. Focused cache/privacy/Redis-outage/upgrade tests5/5 đạt. Strict cuối0W/E;7focused tests sau sửa fixture Hangfire storage đều đạt, gồm media/outbox. Full regression lần đầu có1timing-cache test và unit mock thiếu dependency mới; đã giữ deadline15s nhưng chờ cache thật hồi phục, thêm mock generation đúng test setup. Lượt tiếp theo lộ test hosts dùng JobStorage process-global nhầm DB (hangfire.job không tồn tại); đã bind chính storage fixture chuẩn bị, không thay worker production. Lượt đó bị ngắt theo user turn; full regression mới đang chạy, còn image/scan và benchmark lại. Không áp schema14 lên Neon/Render schema5.
6. Render hotfix live và restart đã kiểm; không deploy/restart lại chỉ để làm evidence. Cert thật`tmp/data-protection/certificate.base64` phải giữ, không regenerate/rotate hoặc đưa vào Git. Main remote41f619b, root localce4 giữ nguyênworktree. Hai push hooks đã pass sau chuẩn LF candidate; lỗi CJS/preflight wrapper cũ giữ log, không bypass hook/force push.
7. Prefix`G06-20261104-`; historical31/10files chỉ image378. Còn manifest/secret delta cuối và cập nhật sáu dòng progress khi backend cache/measurement kết thúc. D9-01/03/04/07/09/10[ ], D9-02/05/06[x]. Không gói khác/phase10/28/36.

## Phạm vi, đầu vào và artifact

- Đã đọc AGENTS, README gốc/điều phối, work-packages, progress, report/checkpoint G06, task và dependency liên quan. Đối chiếu manifest cũ: 701 inventory hashes không drift lúc bắt đầu (gồm13runtime logs ignore; manifest cuối tách riêng các log này). Không làm lại phần còn hợp lệ; không chạy gói khác hoặc phase10/28/36.
- PDF là chuẩn, tr.13/39–44; SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`. PDF nêu ≥100 users, API2vCPU/4GB, ngưỡng latency/cache và production traffic; **không cung cấp URL deployment hoặc chốt duration/read-write mix/tolerance scaling**. DEC-11 vẫn mở; không dùng lại protocol80–20/5+15phút/error1%/efficiency90% đã bị rút. DEC-10 giữ03:00UTC theo baseline Test Strategy§4.
- HEAD `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`, worktree có sẵn nhiều thay đổi. Không reset/restore/stage hàng loạt, sửa PDF, commit/push/deploy/publish. Không có ủy quyền publish/ref mới.
- Runtime: .NET10.0.403, Node22.23.3 tại `tmp/srs-audit/runtime/node-v22.23.3-win-x64`, Python3.13.7, Docker29.7.2, k61.4.2, Trivy0.74.0, Lighthouse13.5.0. Raw logs dùng UTC29/10; ngày checkpoint theo Asia/Bangkok30/10.
- Chủ máy đã giải phóng ổC; Docker phục hồi. Stack synthetic riêng `culinary-g06-20261029`, API2CPU/4GiB, PostgreSQL16.10/Redis7.4.5/MinIO/MailHog/collector0.137.0; Prometheus3.6/Grafana12.2 dùng provisioning có sẵn. Ports loopback ngẫu nhiên, DB/bucket/tài khoản thử riêng. Không dùng hay dừng stack `culinary-blog-*`.
- API image cuối: manifest-list `sha256:59647a0bbe14894ee59f7f50d093321761efd15239f59004938e0956f71bf4ab`, config `sha256:5eeeae423621477139d612691521d6c1ca4fbf88c5968b1d56277c8a8e131233`. Web: manifest-list `sha256:e4b7ae1a2d3d63418c94ccc8835d2cb8d95feff1f6bb6036fcc0399c9b662cbd`, config `sha256:9f3bd06cdc0f7031d63b6543946159cbee0c0f9cd532123a28795dd0b10f77ef`.
- Frontend BUILD_ID `HOABvUQUOl1ubTR8DFT1T`; browser run `6d0298daac9b`, lock SHA256 `d95534a4f7326b82ec82aac2f3ba005572b3fa7312cea6a646810e14e50f85d6`. [Browser identity](evidence/G06-20261030-browser-run.json).

## Phase 30

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Bằng chứng / còn mở |
|---|---|---|
| D9-01 | Mở — protocol/production | Diagnostic smoke mới5VU/30s:150API requests đều200,0errors, warm p50 **8,608ms**,p95 **18,600ms**,p99 **51,534ms**,cold p95 **32,670ms**, Output Cache **89,33%**; k6 exit0.175HTTP requests tính cả warm-up/Prometheus control; workloadGET90%warm/10%cold từ script có sẵn, không write/TLS/WAN. API2CPU/4GiB, synthetic rate override1.000.000global/1.000auth. Chỉ kiểm script/counter, không phải protocol≥100users/read-write/production. [Target](evidence/G06-20261030-smoke-target.json), [summary](evidence/G06-20261030-k6-smoke.json). DEC-11 chưa có protocol phù hợp để nghiệm thu load/stress/scaling. |
| D9-02 | Đạt phạm vi task | PostgreSQL Testcontainer10.000 recipes,1 warm-up+5 đo: list p95 **0,282ms**, search **31,563ms**, Title **0,056ms**, quickest **7,102ms**. EXPLAIN ANALYZE/BUFFERS lưu đầy đủ. Thêm B-tree `(Status,Title,Id)` và migration; title dùng index này, category/difficulty/published dùng composite có sẵn. Search match phần lớn dataset nên planner chọn Seq Scan+sort dù GIN tsvector/trigram tồn tại; không ép planner hoặc suy scan là thiếu index. Quickest biểu thức PrepTime+CookTime dưới100ms nên chưa thêm expression index. |

Đã review WHERE/ORDER BY của category/public/owner/admin, key/slug/author/difficulty/time indexes và GIN search; không thêm replica/partition tùy chọn. Base query AsNoTracking, phân trang trước mapping; MapRecipesAsync batch category/author/role/ingredient/step/image theo ID, projection các collection cần trả. **Đo SQL thực qua DbCommandInterceptor: 1 recipe và50 recipes đều11 read commands**; không tăng theo số recipe. Pool100/command timeout30s assertions đã chạy. Slow-log query `pg_sleep(0.15)` thật, >100ms, không SQL/params trong slow-event. Tất cả thuộc full240 IT pass. [Plans/counts](evidence/G06-20261030-query-evidence.txt).

## Phase 31

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Bằng chứng / còn mở |
|---|---|---|
| D9-03 | Mở — còn CWV/INP | **23/23 page entries≤200KiB gzip**, hai editor **193,2KiB** thay206,1KiB. Gate nay tính cả ancestor error/not-found/loading và global-error, ngoài layouts/JS/deduplicate/missing artifact; negative controls đã chạy. Lighthouse7routes×3runs trên build cuối: tất cả required gates pass, exit0; còn production CWV/INP evidence, static manifest/lab không thay nghiệm thu production. |

Tách schema Problem Details sang module chỉ tải khi parse lỗi; `axios` và `readProblemDetails` dynamic import. Env chỉ có một string default/URL nên dùng validation trực tiếp, giữ đầu vào hợp lệ/không hợp lệ; không đổi form Zod hoặc API semantics. Hai regression mới chứng minh schema không tải trong đường thành công, default/stripping/fallback giữ nguyên và env validation không kéo schema. Node138/138, Jest21/21, browser7/7, lint/typecheck/format đều0. [Budget](evidence/G06-20261030-bundle-budget.txt).

Lượt Lighthouse đầu của30/10 cho LCP/CLS đạt nhưng script transfer recipes222.091B/search219.078B/login245.005B làm gate exit1. Network cho thấy viewport-prefetch tải JS của các route khác và auth validation tải sẵn. Sửa Link/NavLink compat mặc định `prefetch=false` (caller vẫn có thể opt-in), login/register dùng resolver tải khi validate. Browser7/7 cuối chứng minh form/navigation giữ behavior; không tăng budget hoặc bỏ network gate. Lượt đỏ giữ tại `tmp/package-G06/resume-20261030/lighthouse/`.

Lighthouse13.5.0 desktop,21navigations/7routes, production-mode HTTP loopback, không cùng load/fault/build/scan; BUILD_ID cuối như trên. Median của3runs:

| Route | LCP ms | CLS | Script transfer B |
|---|---:|---:|---:|
| / |1619|0,0000474|191641|
| /recipes |855|0,0000441|202207|
| /categories |786|0,0000441|180779|
| /search |843|0,0000441|202207|
| /login |744|0,0000175|187457|
| /recipes/com-chien-hai-san |779|0,0001595|180779|
| /categories/banh-viet |758|0,0000441|184112|

TBT0, meta-description1 trên7routes; tất cả required assertions có dữ liệu và pass. Tool có `LanternError:NO_LCP` trong optional trace-insight logs, nhưng không có runtimeError ở các navigation reports và audit LCP thực vẫn có số; không suppress lỗi/gate. [Target](evidence/G06-20261030-lighthouse-target.json), [assertions](evidence/G06-20261030-lighthouse-summary.json), [log](evidence/G06-20261030-lighthouse.txt).

Mẫu Event Timing Chromium thật trên login/recipes/search:3page visits,9/3/3 trusted keyboard/pointer events; group theo interactionId, max observed **16ms** cả3visit, durationThreshold16ms, desktop1440×900 không throttle. Window start/end epoch và raw entries trong [interaction evidence](evidence/G06-20261030-interaction.json). Đây là chẩn đoán local ít hơn50interactions/visit, **không chứng nhận INP production/p75/RUM**, không lấy TBT thay INP. D9-03 giữ mở theo PDF§4.1.

## Phase 32

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Bằng chứng / còn mở |
|---|---|---|
| D9-04 | Mở — HTTPS staging | Build **hai image cuối thành công**, scan Trivy0.74.0 `image --input` tar: **API0 High/Critical, web0 High/Critical, cả hai exit0**. Không gắn scan image cũ cho source mới. Thiếu HTTPS staging có identity artifact/ủy quyền dynamic scan/TLS. |

Dependency locks không đổi: giữ npm0H/C (5Low), .NET vulnerable listing không finding; config11 scopes0H/C và triage worktree128 findings của checkpoint29/10 cho snapshot đó. History67 commits0leak còn hợp lệ vì HEAD không đổi. Scan source cuối1019Git-eligible files (gồm evidence): Gitleaks8.30.1 raw exit1,199candidates; đã triage182digest/fingerprint,14Render resource IDs,3technical prose,0unresolved; không đổi rule/allowlist và không gọi raw scanner exit0. Trivy config cuối11scopes0H/C,exit0. [Triage](evidence/G06-20261030-secret-triage.json), [raw redacted](evidence/G06-20261030-secret-findings.json), [snapshot hashes](evidence/G06-20261030-scan-snapshot-hashes.json). TLS1.2/1.3/redirect/HSTS31536000/headers/CORS/cookie/fail-fast có source+IT, chưa thay verification deployment. [Scan API](evidence/G06-20261030-scan-api.json), [web](evidence/G06-20261030-scan-web.json), [identity](evidence/G06-20261030-security-summary.json).

31/10: source697/locks/image không đổi; bổ sung Gitleaks8.30.1 trên **47file mới/thay đổi**, raw exit1/160candidates:157digest/fingerprint,2Render IDs,1nhãn Compose, **0unresolved**; [triage](evidence/G06-20261031-secret-triage.json). Render sau sửa tunnel đã read/media200 nhưng commit`e610783` khác G06; chưa dùng làm staging security acceptance.

## Phase 33

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Bằng chứng |
|---|---|---|
| D9-05 | Đạt phạm vi local/container của task | Live/ready/aggregate matrix thật và public chỉ status; HTTP/EF/custom traces và metrics tới collector, Prometheus, **dashboard Grafana provisioned thật và query datasource có frame dữ liệu**. Trace gốc `92c3942c9c2e7a7a04d594c2eccff12c` nối HTTP→EF→outbox Producer→WelcomeEmailJob Consumer, giữ correlation `g06-runtime-drill`; job124 thật đã gửi tới MailHog. Counter output hits38, business recipe.created2 tại snapshot, không dùng làm chứng nhận cache≥80% dưới tải. |
| D9-06 | Đạt phạm vi failure/restart của task | Full drill trên image cuối: Redis down→ready503/live200/public read200; MinIO down→aggregate503/ready200; SMTP down→register201; DB down→ready503/live200/500 Problem Details không stack/Npgsql, reconnect sau phục hồi; API/worker restart giữ user/refresh token và refresh200. Full240 IT có Hangfire persisted scheduled retries qua từng worker restart,4 attempts→Failed; media/outbox resilience, DB/Redis fallback/timeouts và storage/jobs thật. Media154 objects được backup/đọc lại/checksum sau restart và restore target sạch. |

Đã sửa thêm gap exporter: collector YAML có sẵn trước đây chỉ export trace ra debug, chưa gửi backend. Nay `otlphttp/traces` mặc định Seq development; production Compose bắt buộc `OTEL_TRACE_BACKEND_ENDPOINT` trỏ Jaeger/Tempo OTLP HTTP base URL, release example chỉ là placeholder `http://tempo:4318`, không phải URL deployment của người dùng. Không deploy. Render/release `-Template` exit0, missing-endpoint negative control exit1. [Seq docs](https://datalust.co/docs/tracing-from-opentelemetry-sdks), [collector env defaults](https://opentelemetry.io/docs/collector/configuration/#environment-variables).

Runtime dùng **chính YAML source cuối**, Seq riêng digest pin theo compose hiện có: query Seq API trả **20spans** chung TraceId `67c562766776f821b1d1c8836916a130`, đủ Server/Client/Producer/Consumer và welcome job154. Đây là backend export/query thật, bổ sung cho debug/Grafana evidence trước đó; không dùng Seq stack người dùng. [Seq summary không SQL/params](evidence/G06-20261030-seq-summary.json). Deployment production vẫn phải cung cấp endpoint backend thật; không suy local Seq thành production Jaeger/Tempo.

Outbox welcome/media lưu nullable TraceParent/CorrelationId; Producer khôi phục context gốc trước enqueue, Hangfire client filter lưu job parameters và server filter tạo Consumer/scope rồi dispose. Migration `20261029171904_JobTraceContext` chỉ thêm4 nullable columns; existing rows giữ NULL. Migration `20261029172426_RecipeTitleSortIndex` chỉ thêm index; upgrade/data-preservation test đã chạy. Telemetry đăng ký ActivitySource jobs. Test mới kiểm outbox sau ambient request kết thúc, original trace khác producer span, persisted job params, consumer parent/baggage và cleanup.

Giữ sửa batch catch/retry MediaOutboxDispatcher của checkpoint trước, không đổi HostOptions Ignore. StructuredLoggingTests dùng job thật thay ID giả để kiểm filter đọc shared storage; failed sink không crash, context/secret redaction đã chạy. Alert observation/receiver D9-08 vẫn thuộc phase36, không thực hiện ở G06. [Drill](evidence/G06-20261030-drill.txt), [events](evidence/G06-20261030-runtime-events.json), [OTLP/Grafana](evidence/G06-20261030-observability.json).

## Phase 34

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Bằng chứng / còn mở |
|---|---|---|
| D9-07 | Mở — operational tool/storage | Clock scheduler03:00UTC4 methods/7 scenarios pass; retention31-day removed/29-day kept. WAL replica/fsync/full_page_writes/synchronous_commit đềuon. **19 public tables: toàn bộ count/hash/schema khớp;154 media objects có SHA256 manifest và roundtrip verification thật.** Source writer freeze, target DB/MinIO sạch riêng. Local volumes tách biệt; chưa có independent production backup storage/deployment/tool availability evidence. |

Phát hiện ở lượt restore đầu: image cached có mc nhưng thiếu `find`; pipeline `find | sort` che lỗi và tạo manifest rỗng, nhánh empty-check cũng không phân biệt được lỗi command. **Các lượt trước `restore-verified.log` không chứng minh media checksum**, kể cả log exit0; không dùng để nghiệm thu bytes hoặc empty-bucket case. Sửa fail-fast `command -v find/sha256sum`, bỏ pipeline che lỗi, restore hỗ trợ checksum rỗng chỉ khi filesystem thật rỗng. Negative control image thiếu find nay exit1 trước backup; control bucket rỗng thật restore exit0 và file không nằm trong manifest rỗng bị reject exit1. [Controls](evidence/G06-20261030-restore-empty-controls.json). Image G06 tạm ghép busybox/find từ Redis cached, chỉ đổi temp helper, giữ production defaults.

Lượt cuối đo RPO tới recovery cut theo started-at: PostgreSQL **3,502s**, MinIO **2,502s**. Từ bắt đầu tạo restore target tới original API ready **13,275s**; từ lúc freeze xong qua backup/compare/restore tới original API ready **22,684s**, toàn workflow kể metadata **24,363s**. Đây là drill local, restore targets chưa nhận public traffic, không chứng nhận production RTO/RPO. [Metadata154checksums](evidence/G06-20261030-restore-metrics.json), [data/WAL](evidence/G06-20261030-restore-data.json), [negative control](evidence/G06-20261030-backup-negative-control.json).

Automatic approval review **từ chối đổi MinIO tool image mặc định trong compose/production/E2E** vì ảnh hưởng workflows/services chung chưa được chỉ định. Không thực hiện mutation đó, không dùng công cụ khác để lách. Đã dùng temporary E2E copy (chỉ đổi tool image, mc cùng version/commit) và backup-tool image synthetic có find; original runner/defaults giữ nguyên. Registry default minio/mc vẫn pull denied, Quay401 ở môi trường này; phải giải quyết availability trước vận hành. [Override identity](evidence/G06-20261030-browser-tool-override.json). Temporary E2E runner đã xóa.

## Phase 35

Cập nhật Data Protection mới nhất: [checkpoint Free/no disk](#data-protection-free-31102026); xem phạm vi reuse và gates mới trước khi dùng số liệu lịch sử.

Cập nhật mới nhất: [lượt chuẩn bị31/10](#chuan-bi-nghiem-thu-31102026); số đo cũ bên dưới giữ lịch sử, chỉ reuse phần còn khớp hash.

| Task | Kết quả | Còn mở |
|---|---|---|
| D9-09 | Mở — DEC-11/topology benchmark | Giữ JWT/cache/welcome dedup [30/10](evidence/G06-20261030-two-api.json). **31/10: ma trận hai API/Nginx đạt quota gốc100/10/5, private/public cache, RedLock/retry và hai worker khác nhau**; [evidence](evidence/G06-20261031-multi-instance.json). Còn benchmark throughput/latency trước-sau theo DEC-11 và môi trường/resource được chỉ định; không đóng task từ functional checks. |
| D9-10 | Mở — production topology | **31/10: lab thật bốn MinIO node/bốn volume mới**, Erasure4drives/parity2, đọc/ghi khi một node dừng, phục hồi/heal/checksum đạt; [evidence](evidence/G06-20261031-minio4.json). Cùng một host/đĩa, chưa phải production HA. Còn deployment MinIO4+nodes/CDN `_next/static`/upstream/persistence đúng artifact. Production template single node và demo qua tunnel không đủ. Giữ image multi-stage/non-root đã build/scan. |

## Gates và evidence dùng chung

Cwd repository, npm/Lighthouse cwd frontend. Docker chỉ stack synthetic; không dùng secret thật. [Manifest hiện tại](evidence/G06-20261030-manifest.json) gắn source/helper/log/artifact identity; report29/10 chỉ là lịch sử.

| Gate thực chạy | Actual / exit |
|---|---|
| `dotnet build backend/CulinaryBlog.slnx -c Release --no-restore -m:1 -nodeReuse:false -p:UseSharedCompilation=false -warnaserror -p:CustomAfterMicrosoftCommonTargets=D:/Culinary_Blog/scripts/srs-analyzers.targets` |0warning/0error,exit0; [strict](evidence/G06-20261030-strict.txt) |
| `dotnet test backend/CulinaryBlog.slnx -c Release --no-build --no-restore --logger trx ...` | **129unit+3architecture+240integration pass,0skip/0fail**,exit0. [Counts](evidence/G06-20261030-regression-counts.json), [log](evidence/G06-20261030-backend-regression.txt) |
| `dotnet format backend/CulinaryBlog.slnx --verify-no-changes --no-restore` |exit0; [log](evidence/G06-20261030-backend-format.txt) |
| `npm run build`, `npm run budget:check`, `npm test`, `npm run test:components`, lint/typecheck/format |build/budget0; Node138/138, Jest21/21,0skip; lint/typecheck/format0. Negative parser/env/budget controls giữ đủ. |
| Temporary runner cùng source `npm run test:e2e` |7/7 browser thật, build/backend identity ở run.json; [log](evidence/G06-20261030-browser.txt). Lượt đầu DLL lock do build đồng thời test được giữ trong tmp, đã chạy lại tuần tự sau full regression. |
| `python scripts/test-backup-scheduler.py`, `python tmp/package-G06/runtime.py drill/restore`, observe helper |scheduler4methods/7scenarios; drill/OTLP/Grafana/Seq và final154-object restore pass; empty/unmanifested controls pass; từng shell sửa `bash -n` exit0. Chỉ helper có credentials synthetic trong tmp. |
| Trivy0.74.0 `image --input` API/web tar |0H/C,exit0/0; không mount Docker socket. Config11scopes0H/C,exit0; full source secret scan199candidates triage0unresolved,raw exit1. Dependency/history scans giữ theo lock/HEAD không đổi. |
| Application coverage checkpoint29/10 |888/985=90,15%, branch68,35% còn hợp lệ: Application source không đổi, unit129 cuối pass. [Coverage](evidence/G06-coverage-final.txt) |

Lỗi môi trường/runner trước thành công vẫn giữ raw logs trong tmp và report29/10; không ghép isolated passes thành full suite. Report này có full backend run xanh độc lập và browser run xanh độc lập.

## Bổ sung 31/10/2026 — functional scaling và đường cloud

Đã đọc lại AGENTS/README/work-packages/progress/report, phase30–35 và dependency liên quan. **697/697 source/config vẫn khớp manifest30/10, PDF SHA256 khớp**; helper rehearsal/tunnel cũng khớp bản đã review. Không sửa application source, contract, migration, A/B/C; giữ build/regression/bundle/Lighthouse/scan/restore còn đúng artifact, không gọi các kết quả đó là lượt chạy mới. HEAD vẫn `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`. [Đối chiếu và phạm vi quyền](evidence/G06-20261031-resume.json), [manifest bổ sung](evidence/G06-20261031-manifest.json).

### Hai API qua Nginx — D9-09

`python tmp/package-G06/resume-20261031/multi_instance.py`, cwd repo, Python3.13.7/Docker29.7.2; helper .NET10.0.403 build0warning/0error. **Final exit0,9nhóm kiểm đạt**, dùng cached API image `sha256:59647a0bbe14894ee59f7f50d093321761efd15239f59004938e0956f71bf4ab`, mỗi API giới hạn2CPU/4GiB, PostgreSQL/Redis/MinIO/MailHog riêng. Không override quota; Nginx chỉ trong lab dùng `X-G06-Peer` để chọn TEST-NET peer rồi ghi đè `X-Forwarded-For`, hai API trust network của lab. Đây không phải cấu hình proxy production.

- Auth11requests:10×422 rồi429; upload6requests:5×401 rồi429; global101requests:100×200 rồi429. Cả hai upstream dùng cùng remaining/limit và429 có Retry-After/Problem Details; đổi forwarded header phía client không nhân quota. Đây là kiểm biên chức năng tuần tự trên loopback, không phải load100users.
- Mỗi owner/other-user đi qua cả hai upstream:6owner200 có `private,no-store`,6other-user403 không chứa recipe data và có `no-store`;6anonymous401,6public-draft404; không có Age. Public cache6reads cùng SHA256, warm responses có Age, đủ hai upstream. JWT của cùng owner được chấp nhận ở cả hai API.
- Hai Hangfire server dùng chung PostgreSQL. Khi helper giữ lease `culinary:sitemap`, job106 không ghi phiên bản mới, chuyển Scheduled retry. Thả lease và requeue: Succeeded đúng1lần, sitemap versions3→3→4. Cho từng API nghỉ có kiểm soát trong **stack thử**: job108/job110 được hai worker khác nhau xử lý, mỗi job tăng đúng1object version. RedLock.NET dùng một Redis lab và Hangfire `DisableConcurrentExecution` dùng PostgreSQL; không chứng nhận quorum nhiều Redis độc lập hoặc exactly-once mọi failure case.

[JSON](evidence/G06-20261031-multi-instance.json), [log](evidence/G06-20261031-multi-instance.txt), [helper build](evidence/G06-20261031-probe-build.txt). Các lượt harness đầu được giữ trong tmp: đòi `private` cả trên403 (framework trả `no-cache,no-store`), chu kỳ round-robin chẵn không đưa mỗi identity qua hai upstream, và đếm nhầm một debounced refresh độc lập vào job thử. Log phân biệt job103/job107; đã sửa harness/drain cả Scheduled rồi chạy **toàn ma trận** xanh. Không sửa ứng dụng hoặc bỏ kiểm cache/lock để pass.

### MinIO distributed lab — D9-10

`python tmp/package-G06/resume-20261031/minio_cluster.py`, cwd repo; **exit0**. Bốn container cùng image pin `minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e`, mỗi node0,5CPU/512MiB, bốn named volumes mới, không publish cổng. Runtime admin info xác nhận Erasure,4drives/parity2; bucket synthetic có versioning, hai object tổng40bytes. Khi dừng node4:3online/1offline, đọc object cũ và ghi/đọc object mới đạt. Bật node4:4online, heal quét2objects/sửa1object thiếu shard, sau heal0missing/0corrupted; SHA256 của cả hai object khớp trước/sau. [JSON topology/heal/mounts](evidence/G06-20261031-minio4.json), [log](evidence/G06-20261031-minio4.txt).

Lượt đầu parser nhầm server state`online` với drive state`ok`; đã giữ raw failure, dọn lab rồi chạy lại thành công. Bốn volume vẫn trên cùng Docker host/đĩa; không suy thành bốn failure domains, production durability, CDN hoặc performance acceptance.

### Neon/MinIO/Render sau quyền mới

- Người dùng cho phép **sửa tunnel và endpoint Render**, không bao gồm migration Neon thật. Đã áp script khôi phục gateway từ [bản sửa đã review](../../../operations/neon-minio-fix-preparation-2026-10-30.md), giữ bucket/volume/IAM/key. Gateway gốc `culinary-minio-tunnel-07c9fcb00712` và cloudflared tiếp tục chạy để phục vụ demo; private tunnel env chỉ đổi endpoint.
- Tunnel mới `https://involves-chambers-blocked-faculty.trycloudflare.com`: health200, signed list/read đạt; anonymous root/Admin/Console403. **301objects hiện có,300keys từ snapshot Neon đã kiểm đều còn,0missing**. Ba ảnh original/medium/thumbnail tải qua Render và tunnel có cùng SHA256; anonymous GET cả ba403. Không upload/delete object thật. [Tunnel](evidence/G06-20261031-tunnel-check.json), [key/checksum](evidence/G06-20261031-tunnel-media.json).
- Công cụ browser/native pipe không khả dụng; không có Render CLI/phiên API được cấu hình. Người dùng xác nhận tự đổi duy nhất endpoint rồi save/redeploy. Sau đó agent GET `/api/v1/release`, `/health/ready`, `/health`, list/detail và3media: **đều200**. Release trả commit`e6107839283767594c598f84e70dbf139a626655`, providerRender/service/instance identity, image digestsnull theo cơ chế provider; commit này **khác artifact source/image G06 local**. [Live read evidence](evidence/G06-20261031-render-readonly.json). Chưa kiểm upload/delete/job trên cloud hoặc dùng demo làm HTTPS staging G06. GET release trước lúc người dùng redeploy đã timeout; không lấy timeout đó kết luận dịch vụ down.
- Giữ [Neon chỉ đọc](../../../operations/neon-minio-readonly-check-2026-10-30.md) và [rehearsal clone5→13→5→13](../../../operations/neon-minio-fix-preparation-2026-10-30.md): helper/artifact hashes còn đúng, không chạy lại hoặc áp8migration lên Neon thật. Connection/data read đang tốt không có nghĩa schema của release G06 đã deploy.
- Automatic approval review từ chối probe phụ gửi **root credential qua tunnel công khai**; probe không chạy, không đổi công cụ để lách. Thay bằng signed GET dùng demo key đã được phép, anonymous denials và cấu hình gateway khớp template. Không cần truyền root credential để chứng minh đường đọc. Không chạy public load/dynamic scan, tạo tài nguyên cloud, commit hoặc push.

### Cleanup và gates lượt tiếp tục

Khởi động Docker Desktop để chạy kiểm; Docker tự bật13container người dùng theo restart policy có sẵn. Không recreate/stop/cleanup stack người dùng. IDs trước/sau từng lab khớp. Hai project `culinary-g06-20261031` và `culinary-g06-minio4-20261031` đều **0container/0volume/0network** sau cleanup; chỉ dọn tài nguyên synthetic có project label đã xác minh. Không dừng gateway/cloudflared vừa được cho phép sửa. Helper compile/syntax, kết quả JSON, links, source/PDF/helper hashes và checkpoint được kiểm lại; regression30/10 giữ theo source697không đổi.

[Validation31/10](evidence/G06-20261031-validation.json) kiểm JSON/links/checkbox/source hashes và không thấy giá trị credential thật đã cấu hình trong các hồ sơ mới. Đã tải lại **Gitleaks8.30.1** đúng [release chính thức](https://github.com/gitleaks/gitleaks/releases/tag/v8.30.1), kiểm archive checksum trước khi chạy; không cài toàn hệ thống. [Nguồn/checksum](evidence/G06-20261031-scanner-download.json). Quét delta các file mới/thay đổi so snapshot30/10, giữ scan cũ chỉ với file khớp hash; **raw exit1, findings đã triage0unresolved**, không thay rule/allowlist. [Delta/snapshot](evidence/G06-20261031-scan-delta.json), [triage](evidence/G06-20261031-secret-triage.json), [raw redacted](evidence/G06-20261031-secret-findings.json). Findings là digest/fingerprint, Render resource IDs và nhãn Compose synthetic, không gọi raw scanner exit0. Baseline hash inventory còn38generated/ignored outputs ngoài tập Git-eligible; đó là loại khỏi phạm vi scan Git, không phải xóa dữ liệu. Tracking/checksum/scan-result metadata được ghi sau snapshot, không có application source mới. D9-04 vẫn mở vì HTTPS staging/dynamic/TLS đúng artifact chưa đủ.

## Thực thi sau ủy quyền — 31/10/2026

<a id="authorized-rollout-31102026"></a>

Người dùng xác nhận dữ liệu hiện tại là dữ liệu thử và cho phép đưa bản sửa đã kiểm lên hệ thống, thử tải trong môi trường riêng xác định trước và thử hai backend. **Không còn chờ hỏi lại quyền deploy/Neon update đã mô tả.** Chưa có ngân sách/nơi backup độc lập/domain mới. [Hồ sơ quyền, tài nguyên và trình tự áp](../../../operations/g06-authorized-rollout-2026-10-31.md).

- **Phase34:** backup Neon mới cùng exported snapshot; restore riêng PostgreSQL18/localeC.UTF-8 so đủ28tables/2042rows/count/hash. Áp thật chỉ migration`20261031134534_PersistDataProtectionKeys`, ledger riêng1, keyrows0 trước bật provider. Main ledger vẫn5, dữ liệu28tables/2042rows không đổi. Không áp8migration nghiệp vụ còn lại hoặc reset/seed/xóa thật. Backup mới ngoài repo/ACL hạn chế vẫn cùng PC, không chứng nhận storage độc lập. [Backup](evidence/G06-20261031-rollout-neon-backup.json), [rehearsal](evidence/G06-20261031-rollout-neon-keyring-rehearsal.json), [apply thật](evidence/G06-20261031-rollout-neon-keyring-apply.json).
- **Phase32/33:** main đang deploy có10commit mới so nền G06, gồm sửa nghiệp vụ theo PDF. Thử merge full candidate có30conflicts, đã abort chỉ trong worktree riêng, chưa publish/promote full candidate. Tách hotfix14files từ đúng`e610783`, không sửa model/migration nghiệp vụ hoặc frontend. Commit`4cfe24348b29e82c0e622d73941ceacbfd7dfa2e` đã push nhánh riêng, main chưa đổi. [Diff review](https://github.com/Cookie1109/culinary_blog/compare/main...fix/render-free-keyring-20261031).
- **Gates hotfix riêng:** compiler0W/E, format0,79unit+3architecture+124IT0fail/skip; Linux API image`sha256:dacef4fe135dc6b129ed6fd848dc6f65ae24d91e1ab8546df28acfe1a808262a`scan0H/C; Gitleaks14files0findings. Backend/frontend pre-push formats đã đạt, không bỏ hook. Đây khác full G06 source745/129unit/242IT/image378; không tráo các count/artifact. Analyzer bổ sung G06 trên thin live baseline báo295style diagnostics ở Domain không đổi, chưa gọi gate đó pass; full G06 release còn phải hợp nhất dependency và giữ đủ gates.
- **Phase30/35:** chọn labloopback`culinary-g06-f54dfdfa36c0`, API G06image378,2CPU/4GiB mỗi API,50recipes/20categories/App13/key1; chỉ DB/Redis/MinIO synthetic riêng. Public-read100VU/5phút cho1API rồi2API; ghi counters đúng window và Docker resources, không trộn ảnh hưởng build/backup/fault/scan vào phép đo. Hai lượt đầu dừng trước tải do parser wrapper/collector path; đã giữ raw exit/log, sửa harness, xác nhận metrics trước chạy lại. Kết quả cuối sẽ được ghi sau khi run kết thúc; không suy wrapper exit0 cũ thành pass. Thử local được phép không tự duyệt DEC-11/tolerance hoặc chứng nhận production.
- **Phase31:** source frontend G06 không đổi,107hashes giữ evidence chuẩn bị; chưa đưa collector lên Vercel/live. Frontend của hotfix giữ đúng main, không lấy bundle/Lighthouse của G06 gắn cho live đó. Đo RUM cần đúng artifact/window/sample sau deploy.

### Checkpoint tiếp tục sau ủy quyền

1. Đọc checkpoint này và [hồ sơ rollout](../../../operations/g06-authorized-rollout-2026-10-31.md) trước ghi chú “chưa có quyền” cũ. Helpers/raw state ở `tmp/package-G06/authorized-rollout-20261031/`. Không chạy lại `apply_keyring_neon.py`: migration đã áp thật, table/history đã tồn tại. Đọc/đối chiếu trước mọi bước tiếp theo.
2. Dùng nhánh hotfix đã push để sửa Render trước, không cần áp main5→13 cho hotfix này. Cần truy cập Render để kiểm config/branch, đưa certificate private qua secret configuration, bậtDBmode trên binary mới và kiểm runtime/reset/restart. Browser host/native pipe unavailable, không có provider API credentials sẵn; các kết nối Render/Vercel được đề xuất nhưng chưa được xác nhận. Đây là thiếu kết nối thao tác, không phải thiếu quyền deploy. Không yêu cầu user gửi password/APIkey/certificate qua chat.
3. Full G06 source vẫn là snapshot745 trong manifestDP, original workspace/source ngoài phạm vi giữ nguyên. Full-release merge cần xử lý khác biệt với main mới và kiểm lại phần ảnh hưởng; không đẩy snapshotlocal`0568c2f` lên main hoặc gọi hotfix là toàn bộ G06. Giữ PDF/A/B/C, không thêm ngoại lệ analyzer/security/scaling.
4. Kết quả/cleanup benchmark đang chạy cần chốt đúng run before final; không dừng/xóa stack người dùng. Dữ liệu/volumes lab riêng chỉ dọn theo đúng project. Nơi lưu backup độc lập, MinIO server H/C, topology production4+node/CDN, formal load/scaling và RUM vẫn mở. D9-02/05/06 giữ[x],6task còn lại giữ[ ]. Không phase10/28/36/gói khác.

## Data Protection cho Render Free — 31/10/2026

<a id="data-protection-free-31102026"></a>

**Người dùng xác nhận Free, không có persistent disk. Đã thực hiện và kiểm bản sửa local; chưa migrate Neon, đổi Data Protection Environment hoặc deploy source mới.** Không cần mua disk cho phương án keyring này. G06 giữ3task đóng/6task mở; không thay PDF/A/B/C, không chạy gói khác hoặc phase10/28/36.

### Phần sửa và kiểm chứng

- **Phase32/34:** thêm provider EF Core10.0.13 chuẩn của ASP.NET Core; `DataProtectionKeyDbContext` cùng connection string Database, bảng `DataProtectionKeys`, ledger riêng `__DataProtectionMigrations`. Chỉ bật với `DataProtection__PersistKeysToDatabase=true` và certificate RSA/private key trong secret `DataProtection__CertificateBase64`; mặc định filesystem giữ nguyên. DB mode dùng certificate mã hóa XML keys, không fallback plaintext. Startup kiểm table/protect-unprotect/persist; thiếu migration sẽ dừng trước ready. [Runbook và cấu hình](../../../operations/render-data-protection-2026-10-31.md).
- **Migration:** `20261031134534_PersistDataProtectionKeys` độc lập; **AppDbContext vẫn13migration**, model/snapshot/test upgrade nghiệp vụ khớp byte trước sửa. SQL keyring idempotent đã áp2lần trong lab; không đụng ledger nghiệp vụ. Down từ chối drop khi có keys. [SQL review](evidence/G06-20261031-dp-keyring-up.sql), [patch riêng](evidence/G06-20261031-dp-data-protection-only.patch), [apply-check/roundtrip](evidence/G06-20261031-dp-patch-check.json). Patch dựa worktree trước lượt Free/no disk, không tuyên bố áp trực tiếp vào live`e610783`.
- **Certificate:** script PowerShell7 tạo RSA3072/PFX base64 trong thư mục Git-ignore, ACL user hiện tại; không in secret, không ghi đè. Candidate secret đã tạo local; smoke dùng certificate riêng. Giá trị private không có trong report/evidence. Giữ certificate ổn định qua replica/redeploy và backup riêng khỏi DB; mất private key sẽ không đọc được keyring cũ. [Negative control ghi đè](evidence/G06-20261031-dp-certificate-check.json).
- **Phase33/34:**2integration cases mới dùng Identity reset token thật: missing/public-only certificate bị từ chối; token hợp lệ qua DI provider mới; key XML mã hóa/không masterKey plaintext; backup PostgreSQL→restore sạch giữ key fingerprint và reset token; blocked KeyPath không bị ghi; rollback empty/nonempty đúng guard. Full regression **129unit+3architecture+242IT,0fail/0skip**, strict0warnings/0errors, format verify exit0. [Commands/counts/TRX hashes](evidence/G06-20261031-dp-gates.json).
- **Linux image thực:** API non-root UID1654,2CPU/4GiB, PostgreSQL/Redis synthetic riêng; thiếu key migration→startup fail; apply idempotent→ready; force-recreate sang containerID khác→ready, keyring SHA/count1 giữ nguyên, không tạo `/var/data/data-protection`. [Runtime](evidence/G06-20261031-dp-linux-smoke.json).
- **Phase35:** hai API container đồng thời cùng DB/certificate đều ready, dùng cùng encrypted keyring/count1/fingerprint; main migration13 không đổi. Đây là functional sharing, chưa phải benchmark scaling. [Hai replica](evidence/G06-20261031-dp-linux-shared-key-smoke.json). Cả hai lab dọn0container/volume/network;13container người dùng giữ nguyên IDs.
- **Phase32:** image API mới `sha256:378b9ff6130892e6a0e63fb2a72178a9b2a0fbefcc68f19995de48497715f397` build/Trivy0.74.0 exit0, **0High/Critical**; DB scan UpdatedAt31/10/2026 01:14:32UTC. NuGet audit không vulnerability. [Scan](evidence/G06-20261031-dp-api-scan.json), [DB metadata](evidence/G06-20261031-dp-trivy-db-metadata.json), [audit](evidence/G06-20261031-dp-nuget-audit.json). Secret delta có redacted findings/triage, không đổi rules/allowlists; [triage](evidence/G06-20261031-dp-secret-triage.json).
- **Phase30/31 và reuse:** query/N+1/pool/slowlog implementation không đổi; full242IT mới bao gồm các regression liên quan. Không gắn benchmark image API cũ cho API mới. Frontend107source hashes không đổi: giữ Node140/Jest37/UI7, bundle23/23, CWV18metric/6visits và Lighthouse21navigation của BUILD_ID`xdiPVekHnUeTXXctWK3dd`; không rebuild/ređo. Functional quota/privacy/workers/MinIO4node cũ chỉ reuse phần implementation không đổi; keyring sharing/recreate có runtime mới ở trên.

### Điều kiện triển khai và checkpoint tiếp tục

1. Bắt đầu từ [manifest DP mới](evidence/G06-20261031-dp-manifest.json), [validation](evidence/G06-20261031-dp-validation.json), rồi manifest chuẩn bị và Render follow-up để reuse theo hash. Source mới chỉ8file existing/6file mới thuộc backend/scripts; tracking5file và evidence mới. Helpers/raw logs ở `tmp/package-G06/data-protection-20261031/`; không publish private compose, PFX/base64 hoặc DB dump.
2. Render live vẫn`e610783`; endpoint MinIO đã được người dùng đổi và8GET/3SHA đạt trong follow-up trước, không gọi đó là bản sửa keyring đã deploy. Chỉ Save and deploy lại binary cũ không cài provider này. Không bật flag DB trên binary cũ để giả định lỗi đã hết.
3. Để áp candidate G06 đầy đủ: review/cho phép source release cùng migration nghiệp vụ **Neon5→13** và keyring migration riêng, backup mới/write-freeze/target/khả năng phục hồi. Rehearsal5→13→5→13 giữ dữ liệu trước đó không thay quyền áp thật. Xem [kế hoạch phát hành](../../../operations/g06-release-proposal.md). Không tự mua disk, deploy production, migrate hoặc chạy load/DAST công khai.
4. Cấu hình secrets bằng provider Environment/secret management; certificate đã có local, không yêu cầu user gửi giá trị qua chat và không chạy generator lại để thay key. Trước áp phải backup certificate tách DB; sau deploy kiểm release/ready/log/reset trên account/sink được phép và token qua redeploy. Không gửi test email tới người khác.
5. D9-07 vẫn cần backup storage độc lập/lịch/retention/RTO-RPO deployment và vận hành tool; D9-04 còn MinIO server High/Critical + HTTPS staging/DAST/TLS đúng artifact. D9-01/09 còn DEC-11/target/resource/window100users/scaling; D9-03 còn production RUM/sample; D9-10 còn production4+node/CDN/persistence. Không có task mới đủ toàn bộ acceptance để đóng.
6. Không commit/push vì chưa có ủy quyền; giữ toàn bộ thay đổi ngoài phạm vi. Gateway/cloudflared được giữ chạy theo quyền sửa tunnel trước; lab DP đã dọn, không stop/prune stack người dùng. Không chạy lại các gates hợp lệ chỉ vì checkpoint lịch sử nói đang chờ Disks.

## Kiểm Render sau Save and deploy — 31/10/2026

<a id="render-followup-31102026"></a>

**Cập nhật endpoint đã có kết quả đọc đạt; không còn chờ bước này.** Người dùng cung cấp log deployment Live. Agent GET release/ready/health/list/detail và3media:8requests đều200; commit vẫn`e6107839283767594c598f84e70dbf139a626655`; SHA256 original/medium/thumbnail khớp tunnel. Không đọc trực tiếp Environment và không suy smoke đọc thành nghiệm thu upload/delete/job hoặc G06 source đã deploy. [Live read](evidence/G06-20261031-render-followup-readonly.json).

Log startup có Redis/PostgreSQL Unhealthy/cancellation, sau đó ready200 và SQL SELECT1 thành công; lần kiểm mới cũng healthy. Không có bằng chứng lỗi dependency còn kéo dài, không tự tăng timeout hoặc đổi connection strings. Root `/`404 không phải endpoint nghiệp vụ bị hỏng.

**Gap mới cần xử lý trong phase32/34:** Data Protection keyring `/var/data/data-protection` báo Access denied; ứng dụng Live không chứng minh keyring dùng được. Dockerfile đúng commit live chuẩn bị/chown `/app/data-protection`; probe image local xác nhận writable ở đường dẫn đó, `/var` không writable. Chưa kiểm mount/ownership trên Render thật. [Source commit](evidence/G06-20261031-render-followup-live-dockerfile.txt), [probe](evidence/G06-20261031-render-followup-path-probe.txt), [phương án theo disk hiện có](../../../operations/render-data-protection-2026-10-31.md).

Đã hỏi người dùng Render Disks/mount path để chọn phương án; chưa có câu trả lời tại checkpoint này. Không tự đổi DataProtection__KeyPath, chạy root/chmod777, mua disk, triển khai DB provider hoặc migrate Neon. Chuyển sang `/app/data-protection` chỉ giải quyết quyền ghi nếu không có disk, chưa giải quyết key persistence. Giữ D9-04/D9-07 mở; không tạo waiver SRS hoặc thay checkbox các task đã đạt local.

Source739files khớp manifest chuẩn bị, không chạy lại frontend/backend/benchmark đã hợp lệ. Chỉ thêm HTTP read, probe quyền filesystem và cập nhật tracking. [Manifest follow-up](evidence/G06-20261031-render-followup-manifest.json) tham chiếu manifest chuẩn bị còn nguyên, thay trạng thái pending endpoint trong checkpoint cũ. Không commit/push hoặc deploy source mới.

Checkpoint tiếp tục: dùng manifest follow-up; hỏi/đọc thông tin Disks rồi chuẩn bị đúng phương án keyring. Nếu còn lỗi ảnh, chỉ kiểm lại release/media/tunnel bị ảnh hưởng; không chạy lại G06 local suite hoặc các gói/phase ngoài phạm vi.

## Bổ sung chuẩn bị nghiệm thu G06 — 31/10/2026

<a id="chuan-bi-nghiem-thu-31102026"></a>

Đây là checkpoint mới hơn phần runtime31/10 bên dưới. **Chưa đóng G06**: D9-02/05/06 giữ[x], D9-01/03/04/07/09/10 giữ[ ]. Chưa deploy source mới, migrate Neon thật, chạy load/DAST công khai, đổi topology hoặc storage. Có khôi phục gateway/tunnel với guard đã kiểm theo quyền sửa tunnel có sẵn. Giữ PDF/A/B/C; không đưa proposal thành quyết định được duyệt.

### Phần đã triển khai và kiểm chứng thêm

- **Phase30 / D9-01:** thêm `performance/k6/recipe-journey.js` cho2public GET→create draft→update If-Match→owned GET kiểm dữ liệu→delete If-Match, mỗi VU có Author/token synthetic riêng. Từ chối URL public hoặc run ID không hợp lệ trước HTTP; negative control `k6 inspect` exit107. [Protocol DEC-11 đề xuất](../dec-11-measurement-proposal.md) lưu dataset/resources/warm-up/load/stress/scaling-repeat/raw ratios và ngưỡng PDF. Duration/mix/scaling tolerance vẫn chưa duyệt, không đưa lại error1%/efficiency90% đã rút.
- **Smoke hành trình cuối:**5VU/10s loopback,45vòng hoàn tất/270business HTTP operations,0business failures; warm GETp50 **16.472ms**, APIp95 **57.519ms**,p99 **148.799ms**, k6exit0. Tổng HTTP bao gồm registration/warm-up/control được giữ trong JSON. Native API DLL giữ hash cũ, không áp2CPU/4GiB trong lượt wiring này; không chứng nhận100users/production/cache≥80%/scaling. K6summary phiên bản này ghi `thresholds:false` cho ngưỡng **không thất bại**, kết luận dựa cả CLIexit0/failureRate0, không đảo nghĩa field. [Summary](evidence/G06-20261031-preparation-journey-smoke.json), [log](evidence/G06-20261031-preparation-journey-smoke.log), [guard](evidence/G06-20261031-preparation-journey-guard.json).
- **Phase31 / D9-03:** thêm collector opt-in LCP/CLS/INP dùng public `web-vitals`4.2.4, cùng-origin POST, sendBeacon JSON text, attribution theo document entry, route whitelist che slug/ID và device cohort cố định. Receiver mặc định404, yêu cầu buildID/origin/JSON/schema6fields (application/json hoặc text/plain), giới hạn stream2048B kể cả Content-Length sai; không nhận URL/query/form/token/UA raw. Chỉ ghi metric JSON vào stdout, không tạo DB/service analytics. Summarizer lọc build/window, dedup name/id, tách route/device và nearest-rankp75; luôn giữ acceptance pending deployment/sample adequacy. [Cấu hình và xuất log](../../../operations/web-vitals.md).
- **Frontend cuối:** BUILD_ID`xdiPVekHnUeTXXctWK3dd`, run`97897f892eb9`, lockSHA`86b486bd0c8318cd00445b6182aa333d37c79e6e2aded59455c862815ba7db3a`; Node140/140, Jest37/37; UI7/7 tái dùng từ build`9JeyYnBpCBsxU9luSXP8H`/run`434fab819477` vì chỉ3file telemetry đổi, xem [reuse/diff](evidence/G06-20261031-preparation-artifact-reuse.json). CWV pipeline6controlled visits desktop/mobile nhận18metric, đủ cả3loại; max INP local **16ms**. Đây là field-like pipeline lab, không phải RUM production. Bundle23/23entries gồm error/loading boundaries≤200KiB, max **195,6KiB**. Lighthouse7routes×3runs **21navigations/7routes, toàn bộ required gates pass, exit0**; các số đo theo từng route trong [assertions](evidence/G06-20261031-preparation-lighthouse.json). [Browser identity](evidence/G06-20261031-preparation-browser-run.json), [CWV summary](evidence/G06-20261031-preparation-cwv-summary.json), [budget](evidence/G06-20261031-preparation-bundle-budget.log).
- **Điều tra budget:** collector đầu dùng Next hook kéo thêm metric không cần, network script205698B; chuyển public SDK3metric còn205032B, vẫn vượt200KiB232B. Rút gọn riêng collector mới: một transport sendBeacon JSON text, route matcher cho browser URL, capture pathname tại document entry; server rewrite paths được che thành other. Sau các lượt205032→204866→204824B, dùng beacon JSON text; test origin/schema/bytes giữ nguyên và bổ sung case text/plain. Không trì hoãn thu metric để né Lighthouse, tăng budget hoặc sửa code ngoài phạm vi. Run`1b9adf9a7b7f` có E2E7/7 và CWV18 nhưng Lighthouse được dừng có kiểm soát sau khi đã tái hiện3lượt recipes205032B; giữ raw reports/cleanup, thay các gates CWV/bundle/Lighthouse bằng build cuối, giữ UI7 theo reuse mapping.
- **Phase32 / D9-04:** npm phát hiện High `follow-redirects`1.16.1; đã cập nhật duy nhất lock entry thành1.16.2 theo [advisory](https://github.com/advisories/GHSA-mrwp-f3q8-3g9p). So lock với snapshot cũ: chỉ thêm `web-vitals`4.2.4 và đổi entry này; không broad audit-fix/force hoặc nâng Next. Audit cuối **0H/0C,36Moderate/3Low** thuộc dev tooling graph, đã ghi triage giới hạn; không gọi full audit clean. [Audit](evidence/G06-20261031-preparation-npm-audit-final.json), [triage](evidence/G06-20261031-preparation-npm-triage.json).
- **Image/config:** web candidate `sha256:182d5990753d8da1e2c4476be9be698359d64ced4f90d3a6892951065cf74931`; Trivy0.74.0 image scan **0High/0Critical,exit0**, tool candidate0H/C; hai Dockerfile mới/đổi config scan0H/C (40checks pass). DB được refresh31/10, metadata UpdatedAt`2026-10-31T01:14:32.625573468Z`; các scan tar readonly/offline sau refresh, không mount Docker socket. [Web scan](evidence/G06-20261031-preparation-web-scan.json), [tool scan](evidence/G06-20261031-preparation-patched-tool-scan-final.json), [config](evidence/G06-20261031-preparation-config-scan.json), [DB metadata](evidence/G06-20261031-preparation-trivy-db-metadata.json). Artifact local chưa publish/deploy và không phải release Render`e610783`. Native standalone và Docker image build riêng; chưa chứng nhận performance/HTTPS của image trên deployment.
- **MinIO server gap thực tế:** image demo đọc identity`sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e` trùng server cũ có findings trong scan candidate đầu. Các advisory [unsigned-trailer CVE-2026-40344/41145](https://github.com/minio/minio/security/advisories/GHSA-9c4q-hq6p-c237), [OIDC CVE-2026-33322](https://github.com/minio/minio/security/advisories/GHSA-5cx5-wh4m-82fh), [S3 Select CVE-2026-39414](https://github.com/advisories/GHSA-h749-fxx7-pwpg) chưa có bản vá Community được nhà cung cấp công bố. Không chạy exploit, không suy package finding thành deployment đã bị khai thác hoặc dùng điều kiện OIDC/gateway làm waiver. **CLI mới không vá server**, nên không báo toàn stack0H/C. Cần review storage/version được bảo trì và kế hoạch migration; không tự mua license/chuyển nhà cung cấp/fork server.
- **Gateway source:** thêm chặn mode có header`UNSIGNED-PAYLOAD-TRAILER`; Nginx isolated/stub10cases pass (signed normal/read204, trailer/case/comma403, anonymous/otherbucket/admin/rootprefix403, health200). Đây là kiểm routing/header, không kiểm exploit hoặc signature validity ở MinIO. **Đã áp guard khi khôi phục đúng gateway hiện hữu theo quyền sửa tunnel trước đó**. Khi tiếp tục sau ngắt phiên, Docker/gateway/cloudflared đã dừng;13user container IDs và107frontend source hashes còn khớp. Gateway giữ nguyên ID/IAM/bucket/credentials, tunnel mới`https://retail-buying-measuring-helping.trycloudflare.com` signed-read301objects, đủ300snapshot keys,3media SHA khớp snapshot Render trước đó; anonymous media403. Dashboard Render không kết nối được (browser host/native pipe unavailable), nên cập nhật endpoint Render vẫn cần thực hiện/kiểm release. GET release hiện tại200 vẫn là`e610783`, ready200 không chứng minh MinIO mới đã nối. Đã gửi yêu cầu người dùng chỉ đổi endpoint và restart đúng release, chờ kết quả. [Runtime resume](evidence/G06-20261031-preparation-runtime-resume.json), [restore](evidence/G06-20261031-preparation-tunnel-restore.log), [media](evidence/G06-20261031-preparation-tunnel-media-check.json). [Cases/cleanup](evidence/G06-20261031-preparation-gateway-check.json).
- **Phase33 / D9-05/06:** không sửa backend. Giữ strict build0W/0E,129unit+3architecture+240IT, query/N+1/pool/slowlog và telemetry/failure evidence30/10, multi-instance31/10 theo hashes còn khớp. Không chạy lại các lab đã hợp lệ hoặc phase36.
- **Phase34 / D9-07:** thêm candidate `infra/operations/minio-tools.Dockerfile` với MC official commit`7394ce0dd2a80935aded936b09fa12cbb3cb8096`, Go1.27.3, dependency locks và Alpine3.24.3 pin digest; GNU coreutils/findutils, chỉ binaryMC/không MinIOserver, user1000:1000. Candidate cũ136H/C bị loại; candidate cuối`sha256:00ecdeeba1186561b111b290c7877e4fc24f087bd31d456f642f19434739dda0` scan0H/C cả trước/sau refresh DB. Backup→restore2objects gồm empty/nested/spaced key,2SHA khớp/versioning/retention-tool capability pass, clean target riêng. Giữ scheduler/19tables/154SHA/rehearsal Neon cũ; chưa đổi default image chung, chown volume hiện hữu hoặc chạy schedule deployment. [Roundtrip](evidence/G06-20261031-preparation-backup-tool.json), [log](evidence/G06-20261031-preparation-patched-tool-roundtrip.log), [runbook candidate](../../../operations/minio-backup-tool.md).
- **Phase35 / D9-09/10:** giữ hai API/shared quota/privacy/locks/two-worker và MinIO4node/quorum/heal evidence31/10. Bổ sung protocol/runbook để review; không suy4container một host thành4production failure domains/CDN hay smoke thành scaling tuyến tính. [Kế hoạch phát hành cụ thể](../../../operations/g06-release-proposal.md).

### Khắc phục output k6 sau secret gate

Secret scan đầu phát hiện5JWT của tài khoản lab trong `setup_data` của legacy summary (raw report private trong tmp, lab đã dọn); chưa commit/push. Đã bỏ setup_data khỏi artifact công bố, chỉ giữ metrics/checks đã đo. Sửa runner để setup chỉ trả category ID, mỗi VU login control và giữ JWT trong memory riêng; `handleSummary()` xuất allowlist runId/profile/metrics. Không coi JWT là noncredential hoặc thêm allowlist. [Findings đầu đã redacted](evidence/G06-20261031-preparation-initial-secret-findings.json).

Chạy lại đúng phần auth/output: lab`9a98888d7b32`,5VU/5s,CLIexit0; actual JSON mới chỉ3keys đã duyệt, cả custom và legacy summary không có JWT, legacy setup chỉ có category. Business assertions/thresholds giữ nguyên, không chạy lại frontend/build/UI/Lighthouse. [Run/privacy assertions](evidence/G06-20261031-preparation-journey-safe-run.json), [metrics](evidence/G06-20261031-preparation-journey-safe-summary.json), [cleanup](evidence/G06-20261031-preparation-journey-safe-cleanup.json). Source performance mới dùng evidence này; số đo5VU/10s trước giữ lịch sử diagnostic, không phải nghiệm thu100users.

### Gates, bảo toàn và điều kiện còn mở

Full lint/typecheck/format/tests liên quan **đều exit0 (Node140/Jest37; UI7 tái dùng từ434fab819477, CWV6visits mới)**. Backend source/PDF/A/B/C giữ nguyên; frontend gates cũ chỉ còn lịch sử vì source/bundle đã đổi. Tận dụng fixture disposable có sẵn của `run-e2e.mjs` cho hồi quy G06, không thực hiện gói G05 hoặc các phase ngoài scope. Runtime/artifact/command exits/source hashes/evidence hashes và cleanup tại [manifest mới](evidence/G06-20261031-preparation-manifest.json). Temporary runner được archive riêng trong tmp, không để trong source. Synthetic resources đã dọn đúng label; user container IDs giữ nguyên. Lượt tiếp tục có khôi phục Docker/gateway/tunnel đã dừng, theo evidence runtime-resume và quyền trước đó. Secret delta có redacted findings/triage và hash snapshot riêng; **xem [delta](evidence/G06-20261031-preparation-scan-delta.json), [redacted findings](evidence/G06-20261031-preparation-secret-findings.json) và [triage](evidence/G06-20261031-preparation-secret-triage.json); raw exit được giữ nguyên, yêu cầu0unresolved, không đổi rules/allowlists**.

| Task | Đã có thêm | Điều kiện thật sự còn thiếu |
|---|---|---|
| D9-01 | Read/write runner và draft protocol; local smoke xanh | Duyệt DEC-11, target/artifact/resources/window được phép; đo100users/load/stress/cache đúng window và production yêu cầu PDF |
| D9-03 | Collector/receiver/aggregator và toàn bộ gate frontend mới | Deploy đúng artifact có quyền, retention/export log và cửa sổ mẫu production đủ đại diện cho CWV/INP |
| D9-04 | Vá dependency, scan candidate/config, guard đã áp cho demo | Xử lý High/Critical của storage server; staging HTTPS đúng candidate và quyền dynamic/TLS scan; chưa lấy demo cũ thay G06 |
| D9-07 | Tool image đủ tiện ích/scan0H/C, roundtrip sạch | Review/publish/override đúng service được phép, storage backup độc lập, lịch/retention và RTO/RPO deployment |
| D9-09 | Functional2API trước đó; draft scaling protocol | Duyệt cách đánh giá scaling/target/resources và chạy benchmark so1→2instance đúng protocol |
| D9-10 | Lab4node đã đạt, giữ nguyên evidence | Topology production4+node/persistent/CDN có identity, storage security plan và quyền provision/deploy |

### Checkpoint tiếp tục sau lượt chuẩn bị

1. Đọc manifest`G06-20261031-preparation-manifest.json` trước các manifest lịch sử. Dùng hash reuse backend/functional labs; frontend mới phải dùng đúng BUILD_ID/source snapshot trong manifest này. Không rebuild/retest phần còn hợp lệ chỉ vì report có lịch sử nhiều lượt.
2. Review proposal DEC-11 và release plan; chỉ xin/chấp nhận quyền cho target và hành động cụ thể. Quyền tunnel/Render endpoint không bao gồm source deploy, Neon migration hoặc public load/DAST. Không yêu cầu secret trực tiếp; cấu hình qua provider Environment/Secrets.
3. MinIO server security là gap mới, CLI0H/C không khép gap. Guard đã áp và tunnel được khôi phục theo quyền trước đó, xem URL mới/evidence; cần kiểm Render sau cập nhật endpoint. Không tự thay defaultMC chung, chuyển volume hay mua tài nguyên.
4. Neon clone rehearsal5→13→5→13 còn hợp lệ. Live source G0613migrations khác Neon5; áp thật cần backup/write-freeze/target/release được duyệt. Không reset/seed dữ liệu thật.
5. Giữ9task trạng thái3đóng/6mở; không tự chạy gói khác, phase10/28/36, commit hoặc push. Helpers/runs đã giữ trong `tmp/package-G06/preparation-20261031/`; artifact runtime được chỉ rõ trong manifest để chỉ kiểm lại phần bị ảnh hưởng.


Lighthouse network script max204775B, còn25B so budget loopback; cần đo lại khi deploy vì provider/build/header khác. [Bảng giá trị](evidence/G06-20261031-preparation-lighthouse-route-values.json).

## Checkpoint tiếp tục 31/10/2026 — runtime trước lượt chuẩn bị

<a id="checkpoint-tiep-tuc-31102026"></a>

1. Bắt đầu từ [manifest31/10](evidence/G06-20261031-manifest.json); dùng manifest30/10 cho source/build/scan/regression,31/10 cho helper/new runtime/tracking. Không dựng lại các lab vừa xanh hoặc áp migration Neon thật chỉ để làm lại bằng chứng.
2. **D9-01/09:** functional hai API/quota/privacy/RedLock/worker đã bổ sung; còn protocol DEC-11 và target/resource/artifact được chỉ định cho load/stress/scaling. Không tự phục hồi mix/tolerance bị rút hoặc chạy tải trên demo công khai.
3. **D9-03:** còn CWV/INP trên production artifact G06 với cửa sổ mẫu phù hợp. Giữ bundle/Lighthouse/Event Timing local30/10; chưa deploy instrumentation/RUM.
4. **D9-04:** Render demo đã đọc khỏe, nhưng release`e610783` khác source G06; vẫn cần HTTPS staging đúng artifact và quyền dynamic/TLS scan. Không suy ba media200 thành security acceptance.
5. **D9-07:** giữ scheduler/restore/checksum/rehearsal hiện có; cần tool image vận hành được review/cấp quyền và backup storage độc lập/lịch chạy thật. Không tự thay default image chung đã bị review từ chối ở30/10.
6. **D9-10:** lab4node/quorum/heal đã kiểm; còn topology MinIO4+node production persistent/CDN theo PDF và deployment identity. Demo hiện tại vẫn single-node qua Quick Tunnel, phụ thuộc PC/process; URL phiên có thể đổi khi tunnel được tạo lại.
7. Helpers31/10 ở `tmp/package-G06/resume-20261031/`: `multi_instance.py`, `Probe/`, `minio_cluster.py`, `render_check.py`, `tunnel_media_check.py`. Hai Python lab refuse tài nguyên cùng project đã có và tự dọn đúng project. Không dùng `runtime.py setup` cũ để tái tạo config31/10. Probe chỉ nhận loopback DB`g06`/user`g06` và Redis loopback, credentials synthetic; private files không commit.
8. Không có task mới đủ toàn bộ acceptance để đóng: **D9-02/05/06 giữ[x], D9-01/03/04/07/09/10 giữ[ ]**. Chỉ chạy các phase30–35, không phase10/28/36/gói khác. Không commit/push.

## Checkpoint 30/10/2026 — lịch sử

1. Đối chiếu manifest30/10 với diff/artifact; giữ những gates còn đúng hash. Không chạy lại toàn bộ vì checkpoint29/10 từng Docker500: môi trường đó đã phục hồi và full regression/image/restore hiện có kết quả mới.
2. D9-01/09: cần protocol DEC-11 (dataset/mix/duration/cách đánh giá scaling) và production artifact/resource/network được chỉ định. Không tự đưa lại tolerance bị rút. Người dùng xác nhận chưa biết URL; PDF không chứa URL để agent tự suy ra.
3. D9-03: giữ bundle23/23 (cả boundaries), Lighthouse cuối7×3pass và local Event Timing16ms; cần CWV/INP production với cửa sổ mẫu phù hợp. Không lấy TBT/local samples thay production INP.
4. D9-04: cần HTTPS staging đúng artifact và quyền dynamic scan/TLS. Không scan endpoint demo bất kỳ hoặc tự deploy để lấy bằng chứng.
5. D9-07: giữ scheduler/retention/19tables/154checksums/RPO local; cần image công cụ vận hành có đủ find/sha256sum/mc và storage backup độc lập trên deployment. Thay defaults chung từng bị automatic review từ chối; chỉ có temp synthetic fallback trong lượt này.
6. D9-10: cần production MinIO4+nodes/CDN/Nginx persisted topology có identity. Không tự provision/mua cloud hoặc suy file config là deployment thật.
7. Helpers `tmp/package-G06/runtime.py`, `web.mjs`, `resume-20261030/observe.py`, `Dockerfile.backup-tool`; `runtime.py setup` tái tạo compose nên sẽ mất temp Prometheus/Grafana/backup-tool/Seq/api2/Nginx override. Tái tạo đúng helper hash và config trước chạy; host port đổi sau API restart, đọc lại `compose port`.
8. Cleanup stack G06 và manifest cuối được ghi dưới đây. Không commit/push vì chưa có ủy quyền tương ứng; không tự chuyển gói.

## Cleanup và bảo toàn

- `runtime.py cleanup` exit0; xác minh **0container/0volume/0network** mang project label `culinary-g06-20261029`. Hai web processes đã xác minh parent/command rồi dừng. Browser run `6d0298daac9b` tự dọn stack; temporary runner không còn. [Cleanup](evidence/G06-20261030-cleanup-summary.json), [remaining](evidence/G06-20261030-cleanup-remaining.json), [browser cleanup](evidence/G06-20261030-browser-cleanup.json).
- Container IDs/names/running state của stack người dùng trước/sau cleanup không đổi. Không prune daemon/image/cache, restore đè hoặc stop stack người dùng. Loopback URLs trong evidence là endpoint của lượt thử đã dọn, không phải staging/production được bàn giao.
- Manifest ghi source/helper/tracking/evidence hashes và changed-from-resume; source ngoài danh sách G06 giữ theo inventory. Security snapshot1019files có hash riêng; cập nhật hồ sơ cuối chỉ thêm kết quả/định danh/checkpoint, không đổi source sau scan. PDF/A/B/C giữ nguyên. Không commit/push/deploy.
