# G07 — Bàn giao bản demo môn học

Ngày 11/11/2026, Asia/Bangkok. **Đã bàn giao hồ sơ demo miễn phí.** Sáu task đã hoàn tất theo phạm vi ghi dưới đây, gồm D10-04 sau xác nhận UAT của người dùng. D10-05 còn điều kiện vận hành production. Kết luận release production theo SRS là **No-Go**. [Checklist G07](../../srs-completion-checklist.md#d10-tài-liệu-cuối-uat-và-nghiệm-thu), [bằng chứng G07](evidence/G07-20261111-handoff-checks.json).

## Phiên bản và khởi tạo

<a id="d10-01-setup"></a>

**D10-01 — Đạt với profile đã có image MinIO phù hợp.** Dùng mã từ `git archive 41f619b`, cấu hình `.env.example`, project Docker Compose riêng, volume rỗng và cổng loopback riêng. Lượt `docker compose up --detach --build --wait` đầu tiên thất bại khi tải image `minio/mc` đã ghim. Quay cũng trả 401 với hai tag MinIO Community được thử. Lượt dùng MinIO server đã có trong cache và MinIO Client `culinary-g06-minio-tools:patched` đã xác minh hoàn tất trong **224,07 giây**, exit 0, có build API/Web và restore package. Trang chủ, health, Scalar, OpenAPI 3.1.1 và API danh sách công thức đều trả 200. PostgreSQL mới áp **5 migration**, seed **100 công thức, 20 danh mục, 5 tác giả**. Project thử đã dọn sạch; container ngoài project giữ nguyên. README và Compose local đã có tùy chọn dùng image đã nạp khi registry từ chối. Máy chưa có nguồn image phù hợp vẫn cần chuẩn bị image trước khi chạy. [README](../../../../README.md#chạy-với-docker), [công cụ MinIO Client](../../../operations/minio-backup-tool.md), [bằng chứng setup](evidence/G07-20261111-handoff-checks.json).

<a id="d10-02-artifact"></a>

**D10-02 — Đã định danh bản đang chạy.** API Render Free trả commit `41f619bd1d91079dbd37ee73c10c44fbbc5a6298` từ `/api/v1/release`. Deployment `dep-dbpppj0n74is738lsrjg` là bản đã áp cấu hình MinIO. Frontend Vercel deployment `dpl_CzWEgVuacUVkQFs9oKSweUPb1YHM` có source `df0dbb927e0430a485d47570e8849a11340065cb`. Trước và sau lượt Playwright, kiểm commit backend và SHA-256 của một asset được tham chiếu trên trang `/recipes`, khớp manifest asset của bản Vercel đã ghi. CI GitHub bên dưới thuộc commit `41f619b`. Worktree local có các thay đổi và bài kiểm riêng, bao gồm sửa phiên đăng nhập và bài test quản trị. [Identity Vercel](evidence/G06-20261110-layout-deployment.json), [identity Render](evidence/G06-20261111-free-demo-checks.json), [ADR](../../phase-0/adr/ADR-011-release-metadata-providers.md).

## Kiểm thử demo và truy vết

Phần này lưu kết quả kiểm live trước PR #31 trên các artifact cũ. Trạng thái sau sửa, UAT được chấp nhận và CI cuối nằm ở mục [cập nhật UAT và hồi quy](#cập-nhật-uat-và-hồi-quy-ngày-11112026).

**D10-03 — Đã tổng hợp kết quả.** Bảng traceability có đủ **34 FR** với endpoint/job, màn hình và phép kiểm. Inventory có 30 NFR, 10 CONS, 347 điều kiện kiểm chứng và 209 dòng hợp đồng. Hai lệnh `python -B docs/specs/srs-completion-phases/verify-inventory.py` và `python -B docs/specs/srs-completion-phases/verify-decisions.py` đều exit 0. Ma trận runtime G05 local có 47 operation. Job backend công khai của `main` báo 28 operation đã ánh xạ trên runtime inventory 45. [Ma trận FR](../../phase-0/06_Traceability_Matrix.md), [ma trận API local](package-G05-endpoint-matrix.md), [CI backend](https://github.com/Cookie1109/culinary_blog/actions/runs/41825925509/job/126560512540).

**D10-04 — Đã kiểm bản demo, còn lỗi cần xử lý.** Playwright Chromium 156, chạy từ gốc repo bằng `node tmp/g07-20261111/check-live.mjs`, `--followup` và `--login-only`, exit lần lượt 1, 1 và 0. Dùng tài khoản thử địa chỉ `example.invalid` và dữ liệu công thức riêng. Hai công thức thử đã xóa qua API và xác minh khách nhận 404. Các tài khoản thử được giữ lại. [Kết quả từng lượt](evidence/G07-20261111-handoff-checks.json).

| Luồng | Kết quả trên bản demo |
|---|---|
| CJ-01 · Đăng ký, tự đăng nhập, hồ sơ | HTTP 201, phiên và trang hồ sơ đúng. Email chào mừng thực tế thuộc kiểm MailHog của G05 local. |
| CJ-02 · Đăng nhập và vòng đời phiên | Đăng nhập bằng email từ browser context mới và vào dashboard thành công. Lượt đầu bị gián đoạn do chuyển trang khi đăng xuất. Lượt bổ sung đợi về trang chủ và tái hiện `/api/auth/session` trả phiên đã đăng nhập sau khi thao tác đăng xuất thành công. GET session trên demo vẫn trả `Set-Cookie`. Bản sửa sẵn có trong worktree bỏ header này ở GET session. Chạy `npm --prefix frontend run test:components -- --runTestsByPath tests/auth-cookie-lifecycle.test.jsx` đạt 4/4. |
| CJ-03 · Tạo nháp | Tạo nháp, nguyên liệu, bước và tải PNG đều HTTP 201. Khách đọc công thức nháp nhận 404. |
| CJ-04 · Xuất bản và khám phá | Xuất bản HTTP 200. Trang chi tiết và trang tìm kiếm HTTP 200, hiển thị đúng công thức thử. |
| CJ-05 · Quyền sở hữu và Admin | Author khác nhận 403 khi đọc nháp, 403 khi gọi API Admin, UI từ chối mở bản nháp. Luồng Admin có quyền dương đã được kiểm trong bài test local. Phiên Admin trên bản demo chưa được kiểm trong lượt này. |

Quét **9 cặp trang/viewport** trên 320, 768 và 1440 px. Không có tràn ngang. `/recipes` có `aria-prohibited-attr`, `/search?q=pho` có `label-content-name-mismatch` ở cả ba chiều rộng. Trang đăng nhập thiếu landmark `main` trong DOM đã kiểm. Phần tương tác thật với screen reader/thiết bị của phase 28 và UAT với Product Owner chưa có bằng chứng trong lượt này.

## Vận hành và quyết định bàn giao

**D10-05 — Đã rà vận hành demo, còn gate production.** DNS/HTTPS và health public hoạt động. Kết quả G06 xác nhận TLS 1.2, CORS, ảnh sau khôi phục và sai khác HSTS. Lịch PostgreSQL được kiểm lại đang bật hằng ngày **03:00 UTC**, lượt gần nhất đã ghi kết thúc 0. API maintenance trả `enabled=false`. Kiểm template `./scripts/release-gate.ps1 -EnvFile infra/production/release.env.example -Template` đạt. Kiểm phát hiện bí mật GitHub thuộc CI của commit đã định danh. Alert thật, SLA, thông báo bảo trì trước 48 giờ và kết quả release gate với manifest production vẫn thiếu. Runbook có quy trình rollback theo artifact, Vercel đã ghi deployment trước `dpl_6UHCvBm31hSkn3AWF79uvVDeqV4R`, còn Render có bằng chứng phục hồi cùng build trong G06. Quyết định **No-Go production** vì còn lỗi phiên, trợ năng, MinIO mức High và các điều kiện G06. [Bằng chứng G06](package-G06-result.md#free-demo-closeout-20261111), [backup](../../../operations/neon-backup-windows.md), [go/no-go và rollback](../../../release/go-no-go-and-rollback.md).

**D10-06 — Đã review diff và phiên bản.** Mã GitHub `main`, nhánh frontend Vercel và worktree có thay đổi là ba phạm vi kiểm chứng riêng. Thay đổi G07 trong workspace gồm hướng dẫn setup, hai biến image cho Compose local, cập nhật ADR/changelog, checklist và hồ sơ bàn giao. Commit, push và rollout thuộc một lượt phát hành riêng. `git diff --check` và kiểm tra liên kết áp dụng cho các thay đổi G07.

**D10-07 — Đã kết luận phạm vi nghiệm thu.** Demo có bằng chứng cho các luồng chức năng nêu trên. Nghiệm thu toàn bộ SRS còn thiếu D10-04/05, các tiêu chí tải, scaling, topology và bảo mật G06, Google thật (phase 10), browser/thiết bị và screen reader (phase 28), SLA/alert/maintenance (phase 36), cùng các điều kiện đã ghi trong [tiến độ](../progress.md). UAT do Product Owner đã được xác nhận trong phần cập nhật bên dưới; hai issue GitHub đã đóng sau PR #31. Các kết quả và đính chính A/B/C dùng đúng phạm vi đã ghi trong [decision register](../decision-register.md).

## Hai issue GitHub

| Issue | Quyết định | Bằng chứng và phần còn thiếu |
|---|---|---|
| [#25 — Functional Regression & UAT](https://github.com/Cookie1109/culinary_blog/issues/25) | **Đã đóng, completed** | PR #31 đã merge; 34 FR có traceability, seed 100 công thức, runtime 45/45 API, bốn luồng E2E trên stack thật và UAT được người dùng chấp nhận. [Job browser đạt](https://github.com/Cookie1109/culinary_blog/actions/runs/42970222052/job/130358990255). |
| [#29 — Code Quality & Test Coverage](https://github.com/Cookie1109/culinary_blog/issues/29) | **Đã đóng, completed** | SonarAnalyzer và ArchUnit.NET đã tích hợp, Application line coverage **81,78%** (413/505), 105 unit test và 3 test kiến trúc đạt; build 0 warning/error, ESLint và Prettier đạt. [Backend CI](https://github.com/Cookie1109/culinary_blog/actions/runs/42970222052/job/130356726954), [frontend CI](https://github.com/Cookie1109/culinary_blog/actions/runs/42970222052/job/130358303170). |

GitHub REST xác nhận cả hai issue đã đóng với `state_reason=completed` sau khi merge [PR #31](https://github.com/Cookie1109/culinary_blog/pull/31). Commit trên `main` là `4b9102f4b37482a76f25cdbafbfb50b1a1b26dd0`, ngày 11/11/2026.

## Cập nhật UAT và hồi quy ngày 11/11/2026

Người dùng xác nhận **đã thử và chấp nhận** bản UAT cô lập tại `http://localhost:18080`, project `culinary-issues-25-29-uat`. Image API `sha256:f9335b163755e3c6fbe914a1b0d7b789923589330277cf8e7a48070d591d3d86`, Web `sha256:a8470a8d92ce8cc6d7b654b0ba5b93c2aae3746a987db99bbecd2c0243125d0c`. [Tiêu chí và bản UAT](../../phase-0/07_Critical_Journey_Acceptance.md#kiểm-thử-chấp-nhận-cho-issue-25). Đây là chấp nhận bản demo môn học, không phải chữ ký nghiệm thu production hoặc toàn bộ SRS.

Trên commit cuối PR #31 `03c08b3ee1b7f50727ff8bca3e36f62f2c7ce30b`, 105 unit test, 3 test kiến trúc (gồm ArchUnit.NET), 180 integration test và 4 luồng E2E Playwright trên stack cô lập đã đạt. Application line coverage **81,78%** (413/505). Ma trận runtime đối chiếu **45/45** operation OpenAPI với test đã chạy. Frontend đạt ESLint, Prettier, TypeScript, build, 135 Node test và 22 component test. **CI GitHub đạt cả 6 job:** backend, frontend, functional-browser, security, Dependency Review và Docker Compose/Trivy. Các sửa đổi phiên đăng xuất, migration E2E, bộ lọc truy cập và form đăng nhập trong test audit được ghi trong PR. Hai ảnh API/Web đạt gate Trivy sau khi cập nhật gói apt và base image zlib đã vá. [PR #31](https://github.com/Cookie1109/culinary_blog/pull/31), [CI commit cuối](https://github.com/Cookie1109/culinary_blog/actions/runs/42970222052).

**D10-04 đạt phạm vi demo/UAT người dùng.** Kiểm thử và UAT xác nhận các luồng chức năng chính; những lỗi ghi ở lượt live cũ trên `41f619b`/`df0dbb9` là bằng chứng lịch sử, không bị ghi đè. Thiết bị và screen reader thực tế của phase 28 vẫn chưa có bằng chứng. **D10-05 còn mở theo SRS production:** Render Free không đáp ứng profile 2 vCPU/4 GiB hoặc nhiều instance, MinIO một node không đạt topology phân tán, HSTS API Render và một số điều kiện alert/SLA/maintenance/recovery thực tế chưa xác minh. Các sai khác và nguồn cụ thể ở [báo cáo G06](package-G06-result.md#free-demo-closeout-20261111).

Sau merge, Vercel hoàn tất triển khai commit `4b9102f` và Render `/api/v1/release` trả đúng commit này. Lượt kiểm phát hiện gateway MinIO và tiến trình Quick Tunnel trên máy đã dừng, trong khi MinIO vẫn khỏe. Đã khởi động lại đúng gateway hiện hữu, kiểm đọc ảnh có xác thực khớp byte local và các đường root/admin vẫn trả 403. Chỉ đổi `ObjectStorage__Endpoint` trên Render, giữ nguyên credentials, bucket, volume và gói Free; triển khai lại build hiện có bằng [`deploy_only`](https://api-docs.render.com/reference/create-deploy). Deployment `dep-dbq0l0hsrm7s7394vme0` đã live. `/health`, `/health/ready`, `/health/live`, trang công thức, API qua Vercel và ảnh mẫu đều trả 200; ảnh qua API và proxy Vercel khớp SHA-256. GET session trên website không trả `Set-Cookie`. [Bằng chứng đóng issue, CI và kiểm live](evidence/G07-20261111-issues-25-29-closeout.json).

Ảnh demo vẫn phụ thuộc máy đang chạy Docker, gateway và Quick Tunnel. Lượt dừng tiến trình vừa kiểm chứng làm API aggregate health và ảnh bị gián đoạn; URL tunnel thay đổi khi tạo phiên mới. Giới hạn này được giữ trong hồ sơ vận hành miễn phí và không đáp ứng cam kết uptime/topology production của SRS.
