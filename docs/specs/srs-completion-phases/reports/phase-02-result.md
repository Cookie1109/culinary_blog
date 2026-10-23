# Kết quả Phase 02 — Quyết định và mâu thuẫn SRS

- **Trạng thái hiện tại: Chờ quyết định; D0-04 mở lại.** Theo chỉ dẫn lấy PDF làm chuẩn và chỉnh A/B/C, [đính chính](../approvals/2026-10-23-abc-correction.md) thay hiệu lực11 quyết định đã duyệt trước đây. D0-02/D0-03 vẫn đóng công việc rà/ghi nhận.
- Expected hiện tại ở register/inventory. Các checkpoint dưới đây là lịch sử, không còn cho phép miễn PDF hoặc dùng protocol cũ để nghiệm thu. Chưa sửa runtime, migration hay deployment.

## Checkpoint trước chấp thuận — commit7e3e42a

Phần dưới giữ nguyên kết quả/lệnh và điều kiện chờ tại thời điểm checkpoint, trước phản hồi duyệt; trạng thái hiện tại và kiểm lại nằm ở phần cập nhật sau chấp thuận cuối report.

- **Trạng thái checkpoint: Chờ quyết định.** Đã lập hồ sơ đối chiếu và đề xuất; chưa chấp thuận11 đề xuất mới, chưa đóng tiêu chí đo thống nhất của D0-04.
- Ngày thực hiện:23/10/2026, Asia/Bangkok; Codex. Người dùng ghi “Phase01” nhưng liên kết file phase02; thực hiện theo file được liên kết như đã nêu đầu phiên, không chạy lại phase01 hay triển khai phase03.
- HEAD trước: `b48d2465f6ad5c5b0bcdefac8981b57637d10535`, nhánh `main`, index trống. Có138 đường dẫn thay đổi/chưa tracked từ trước. Snapshot bytes/status tại `tmp/srs-decisions/before.json`, SHA256 `8fa747b4fcd10cb6b8d80ce9cdcfe7dc263f7992129bd8cd1da9feaf7d0797f0`.
- Sau: commit chứa report truy bằng `git log -1 -- docs/specs/srs-completion-phases/reports/phase-02-result.md`; không chèn SHA tương lai/tự tham chiếu. Source được review là dirty worktree, không thể tái hiện chỉ bằng HEAD cũ.
- Runtime thực tế: Windows PowerShell; Python3.13.7; Node22.18.0; .NET SDK10.0.402. Chỉ đọc file và nguồn chính thức trên web; không DB/container/provider login/SMTP thật, không đọc secrets từ `.env`.
- Phụ thuộc đã đọc: [report01](phase-01-result.md), [inventory](../acceptance-inventory.md), README điều phối/progress/checklist/mẫu report; Conformance/Decision Log/Product Baseline/Data Dictionary/Error Catalog/Routes/Test Strategy và ADR. Runtime/provider/UAT của phase01 chưa được chứng nhận, không suy thành pass ở đây.
- Áp dụng Postman API Engineer → API Documentation đã đọc trong phiên: tái dùng contract local, giữ rõ expected/source/proposal, không sửa OpenAPI hoặc publish Postman. PDF skill dùng đọc SRS; không sửa/tạo PDF.

### Kết quả từng task

| Task | Requirement/acceptance/nguồn | Kết quả | Evidence/điều kiện mở |
|---|---|---|---|
| D0-02 | GAP-001–033; SRS toàn bộ và baseline;12 INV-G của report01 | Đã rà đủ33 GAP, giữ baseline Accepted/Amended hiện có;12 nhóm đều có decision reference và owner | [Decision register](../decision-register.md):33 dòng review,25 mục DEC; INV-G05/07 đã có quyết định tại Conformance§3, không xin duyệt lại |
| D0-03 | NFR-PERF-003,MAINT-001/002,SCALE-001,SEO-003;CT-HW-02 | Đã ghi riêng7 nhóm cache/ping/.NET analyzers/ESLint/frontend tests/browser/lock, có nguồn, phương án, lý do, tác động, cách kiểm và trạng thái | DEC-01–07;2 đề xuất ping/lock chờ duyệt; tool/ruleset nguyên văn chưa được miễn, acceptance triển khai vẫn mở |
| D0-04 | NFR-REL-001/003,PERF-002,SCALE-001–003;tr.40/43–44 | Đã kiểm số học uptime và tìm baseline backup03:00UTC; có protocol SLA/load/scaling cụ thể, giữ production topology nguyên văn | DEC-08–12; **còn chờ duyệt DEC-09/11**, chưa chốt cửa sổ/tolerances, checkbox D0-04 giữ mở |

### Thay đổi và quyết định

- Tạo [decision-register.md](../decision-register.md), không thay trạng thái Accepted trong baseline cũ. Có nguồn, impact, acceptance/test owner và phương án ưu tiên;11 đề xuất được gom thành góiA/B/C để người có quyền review, chưa có chữ ký/approval mới.
- Tạo [verify-decisions.py](../verify-decisions.py) để kiểm coverage33 GAP/25 DEC/12 INV-G, ID tham chiếu, status proposal, local links/bảng,3 hash baseline không đổi và phép tính SLA. Đây là kiểm tài liệu, không phê duyệt quyết định hoặc chứng minh ứng dụng đúng.
- Bổ sung reference decision register và kết quả tìm lại baseline vào inventory; giữ347 acceptance/207 CT rows và mọi runtime status chưa kiểm. Không sửa report01 lịch sử. Nếu chỉ thêm reference mà expected/runtime không đổi thì không hạ trạng thái phase01.
- Cập nhật đúng dòng02 của progress, task D0-02/D0-03 và handoff của file phase02; D0-04 còn mở. D0-03 đánh đóng **công việc ghi nhận/đối chiếu**, không đồng nghĩa ping/lock hoặc công cụ khác SRS đã được chấp thuận.
- Không sửa code, API/status/schema, migration, CI hay môi trường. Bản file phase02 tồn tại nhưng chưa tracked từ trước; commit sẽ ghi file giao việc là mới, không có nghĩa nội dung chỉ dẫn gốc được tạo toàn bộ ở lượt này.

### Những điểm đã có căn cứ, không cần duyệt lại

- Conformance§3: public health aggregate-only và chưa bật VerifiedAuthor trongv1. Inventory trước đã ghi câu hỏi nhưng thiếu reference này; nay DEC-17/19 giải đáp phần quyết định, runtime vẫn cần test.
- Test Strategy§4: backup hằng ngày03:00UTC/retention30 ngày. Scheduler hiện sleep86400 chưa chứng minh đúng lịch; phase34 thực hiện. UTC+7 tương ứng10:00 Bangkok.
- GAP-013: Redis-backed Output Cache có yêu cầu cụ thể; cache-aside hiện tại không được gọi là đã đạt. Giữ yêu cầu theo baseline, chưa có waiver.
- ADR-002/Error Catalog/GAP-007: làm rõ status theo từng cause và owner logout; không lấy OpenAPI skeleton mâu thuẫn làm quyền tự đảo baseline.
- SRS static-analysis/test tools và production MinIO4+nodes/CDN vẫn bắt buộc nếu chưa có CR. Local/source/JSDOM/template không thay evidence tương ứng.

### Kiểm tra thực tế

Cwd chung **`D:\Culinary_Blog`**. Kết quả kiểm cấu trúc dưới đây đã chạy; phần commit/push được xác minh trong transcript và SHA của commit chứa report.

| Runtime/cwd | Lệnh/thao tác thực tế | Loại evidence | Expected/actual | Exit code/kết quả |
|---|---|---|---|---|
| Git CLI/root | `git status --short --branch`, `git rev-parse HEAD`, `git diff --cached --stat` | Repo | HEAD/138 dirty paths/index trống như trên |0 |
| Python3.13.7+pypdf6.21.1/root | `python -` đọc PdfReader và trích các trang13,37,39–45,47–48,55,60,63,65 | Source PDF |71 trang/hash giống inventory; excerpts tại tmp/srs-decisions/source.txt |0 |
| PowerShell/rg/root | `Get-Content`, `rg -n` trên baseline/ADR/DTO/EF/config/jobs/backup/production topology | Static review | Tìm quyết định cũ và source mismatch; một lần dò sai thư mục reliability đã tìm lại infra/operations |0 ở các lượt đọc cuối; không coi lỗi dò path là lỗi nghiệp vụ |
| Web primary sources | Đọc Google Search Central, Microsoft Learn, Hangfire, JSON Schema/OpenAPI;URLs/ngày ở W1–W5 register | External documentation | Ping retired; store cache khác nhau; lock có giới hạn; annotation khác runtime proof | Tools trả nội dung; không HTTP/provider test ứng dụng |
| Python3.13.7/root | `python docs/specs/srs-completion-phases/verify-decisions.py` | Documentation regression |33 GAP,25 DEC,12 mappings,11 proposals,20 local links; bảng/hash/số học hợp lệ |0/PASS |
| Python3.13.7/root | `python docs/specs/srs-completion-phases/verify-inventory.py` | Inventory regression |34FR/30NFR/10CONS;347 acceptance/207 contracts/45 operations/80 local links; giữ trạng thái runtime |0/PASS |
| Git CLI/root | `git diff --check` phần tài liệu; staged diff check trước commit | Diff review | Chỉ file phase02/phần tham chiếu liên quan, không whitespace errors |0/PASS ở worktree và staged diff |
| Python/hashlib/root | So snapshot trước/sau, loại đúng các file giao việc | Preservation |Trong138 dirty paths cũ, chỉ file giao việc phase02 đổi bytes;137 đường dẫn khác giữ nguyên;347 dòng acceptance và tiến độ01/03–38 không đổi |0/PASS |

Số học đã chạy bằng Python:0,5%×365×24=43,8 giờ; năm366 ngày=43,92 giờ;30 ngày=216 phút; downtime3,65 giờ/năm tương ứng uptime99,958333…%. Script verifier kiểm lại bằng Decimal. Đây là phép tính, không phải số đo uptime.

Không chạy unit/integration/frontend/build/browser/provider/production/benchmark/restore/CI: chỉ đổi tài liệu, không có runtime behavior được sửa. Các command có thật trong checklist mục6, package/project/scripts đã được đọc để handoff; không dùng lệnh dự kiến như kết quả test mới. Không ký UAT, gửi alert, provision hoặc deploy.

### Cập nhật theo dõi và điều kiện còn mở

- D0-02/D0-03 đóng ở phạm vi review/ghi nhận theo task; **D0-04 vẫn mở** vì tiêu chí SLA/load chưa thống nhất. Phase02=`Chờ quyết định`; phase03–38 không thay trạng thái.
- 11 đề xuất còn chờ: DEC-02/07/09/11/13/15/18/20/21/22/25. GóiA là API/UI clarification;góiB là sai khác SRS/baseline;góiC là tiêu chí vận hành. Approval phải chỉ rõ phương án/ID/ngày/nguồn. Chưa có approval không cho phép owner sửa theo đề xuất hoặc đóng nguyên văn.
- Người dùng đã yêu cầu commit/push tài liệu; điều đó không phải phê duyệt các waiver. Chưa chỉnh33 GAP, chưa âm thầm đổi key Identity/enum type/PUT semantics hoặc giảm topology production.
- Handoff tới03/04/05/08–10/11/14–22/28–36 theo owner trong register. Các phần baseline rõ và công việc độc lập vẫn có thể được giao tiếp; không chạy chúng ngay lượt này.

### Bàn giao

Đọc decision register và góiA/B/C để chấp nhận/điều chỉnh từng ID. Sau approval, chỉ cập nhật trạng thái quyết định/acceptance reference cần thiết, ghi nguồn chấp thuận rồi chạy lại verifier; triển khai vẫn do các phase owner. Nếu không duyệt, giữ baseline và ghi tác động/phương án nguyên văn.

Phần hồ sơ đã sẵn cho review; chưa tuyên bố hoàn tất mọi quyết định hoặc nghiệm thu SRS. Kiểm tài liệu đạt không thay approval, runtime/provider/production evidence.

## Checkpoint lịch sử sau chấp thuận — 23/10/2026 (đã được thay thế)

- Nguồn: người dùng trả lời trực tiếp “Duyệt gói A”, “Duyệt gói B”, “Duyệt gói C”; bản ghi approval lưu IDs/call/revision và phạm vi cụ thể. Phê duyệt này độc lập với yêu cầu commit/push đã có trước.
- HEAD đầu lượt cập nhật: `7e3e42aa5c7e96c0f4688a7c8a274572401a707b`.137 đường dẫn thay đổi/chưa tracked có sẵn được snapshot tại `tmp/srs-approval/before.json`, SHA256 `1dad79bd7796220e5a8aca8513487e2f3548422fc92b6ec007d2778c04b42ac6`.
- Cập nhật11 DEC thành **Đã duyệt**, mapping INV-G, phạm vi amendment ưu tiên cho các phần baseline mâu thuẫn, bảng effective acceptance trong inventory, và verifier kiểm approval provenance/IDs. Các file baseline lịch sử được giữ bytes để đối chiếu; khi thực thi, owner đọc amendment mới thay vì chọn câu chữ cũ.
- **D0-04 đã đóng**: SLA99,5%/30 ngày UTC và load/scaling protocol đã có xác nhận; backup03:00UTC và production topology giữ theo phạm vi register. Đóng quyết định không đóng bằng chứng đo.
- D0-02/D0-03/D0-04 đều đạt; dòng02 progress và file phase02 cập nhật Đạt. Dòng01/03–38 giữ nguyên. Các ghi chú chờ trong checkpoint trước là lịch sử.

| Task | Kết quả sau xác nhận | Evidence |
|---|---|---|
| D0-02 |33 GAP đã review;11 amendment/clarification được duyệt, có ngày/nguồn/phạm vi | Register và APP-2026-10-23-ABC |
| D0-03 | Cache/tools/browser theo baseline; ping và lock alternatives có approval góiB | DEC-01–07 và approval |
| D0-04 | Target/cửa sổ/cách tính SLA,timezone backup và tiêu chí load/scaling đã thống nhất | DEC-08–12, approval góiC; chưa có uptime/benchmark mới |

Cwd `D:\Culinary_Blog`, Python3.13.7/PowerShell; cùng môi trường đọc tài liệu, không dùng credential/dataset thật.

| Kiểm lại sau approval | Expected/actual | Kết quả |
|---|---|---|
| `python docs/specs/srs-completion-phases/verify-decisions.py` |33 GAP/25 DEC/12 mappings;11 approved/0 pending,26 local links,package/provenance hợp lệ |0/PASS |
| `python docs/specs/srs-completion-phases/verify-inventory.py` |34FR/30NFR/10CONS;347 acceptance/207 contracts/45 operations/81 local links;runtime chưa kiểm |0/PASS |
| Git scoped diff và staged diff check | Chỉ cập nhật hồ sơ/chấp thuận/phần theo dõi thuộc phase02,không whitespace errors |0/PASS worktree và staged |
| Snapshot/hash trước-sau |137 đường dẫn có sẵn giữ nguyên;347 acceptance rows và progress01/03–38 không đổi |0/PASS |

Verifier đã phát hiện một nhãn provenance góiC bị ghi thành góiB trong bảng tóm tắt (exit1); đã sửa đúng góiC, kiểm lại exit0. Kiểm tra này không thay nội dung approval của người dùng.

Bàn giao: dùng11 quyết định đã chấp thuận làm expected cho các phase owner, đặc biệt17/18/19/20/21/22/30/35/36. Không tự chạy phase tiếp theo trong lượt xác nhận này. Application tests/provider/production/UAT chưa chạy vì không có code/runtime change; không đổi trạng thái các loại evidence đó thành pass.

## Đính chính hiện tại — PDF là tiêu chuẩn, 23/10/2026

- Người dùng xác định PDF là tiêu chuẩn, sau đó yêu cầu “Thế chỉnh lại các gói ABC đi”. [SRS-2026-10-23-ABC-CORRECTION](../approvals/2026-10-23-abc-correction.md) ghi nguồn/phạm vi và thay hiệu lực11 ID cũ. Không diễn giải thành phê duyệt một protocol mới.
- HEAD đầu lượt `866394388c07371638364e5145c235bfcb0a2aca`, sau commit CI có sẵn của người dùng. Snapshot136 file thay đổi/chưa tracked: `tmp/srs-abc-correction/before.json`, SHA256 `8df5ef7e5ca8dd3fc882f20ec6c4b20b71ff1125146bfa3b52a7757cea099603`. Index trống trước stage; không đưa thay đổi có sẵn vào commit hồ sơ.
- Đối chiếu trực tiếp PDF bằng pypdf các trang37/40/43–47/55/58–59/61/63/65; excerpts ở `tmp/srs-abc-correction/page-N.txt`. PDF71 trang/hash giữ nguyên. Kiểm lại nguồn chính thức Google W1: endpoint ping đã ngừng/404; không gọi provider endpoint hoặc tự cấp waiver.

| Phần | Expected hiện tại / thay đổi | Điều kiện còn mở / owner |
|---|---|---|
| A: DEC-13/15/18/22/25 | Optional/query theo nguồn; API có message khi rỗng; OriginalUrl NOT NULL; route gốc và slug301. Rút quy tắc null/merge/conflict400/alias307–308 như tiêu chuẩn đã duyệt | Omit/null/empty/merge và query conflict chưa được PDF quy định đủ;17/18/22. Kiểm schema/storage URL và route/rendering thực;11/19/21/22 |
| B: DEC-02/07/20/21 | Trả về RedLock, `IdentityUser<string>`/varchar450, Instructions text NOT NULL, enums smallint. Ping vẫn trong acceptance, không thay bằng sitemap/robots | Provider ping đã ngừng: ghi vướng mắc riêng, chưa đạt nguyên văn;20/21/38. Migration/upgrade dữ liệu và concurrency chưa thực thi;20/22/35 |
| C: DEC-09/11 | Bỏ protocol30ngày/80–20/5+15phút/error1%/efficiency90%; giữ SLO,100users,2vCPU4GB và scaling tuyến tính trong PDF | Mâu thuẫn99,5% với3,65h/năm, kỳ đo/maintenance/missing data và protocol load/scaling chưa thống nhất;30/35/36/38 |
| Theo dõi | D0-02/D0-03 đóng review/ghi nhận; **D0-04 mở lại**, tiến độ02=`Chờ quyết định` | Không tự đóng phần chưa rõ; tiến độ01/03–38 giữ nguyên |

Sửa register, bảng expected inventory, README điều phối, checklist, file giao việc, progress và verifier; thêm bản đính chính và gắn nhãn lịch sử cho approval cũ. Nội dung approval cũ được giữ nguyên ngoài banner hết hiệu lực. Các checkpoint trước trong report là lịch sử, không phải chỉ dẫn hiện tại. Baseline ngoài A/B/C chưa được audit lại toàn bộ; không coi nhãn Accepted cũ là chứng nhận khớp nguyên văn PDF. Backup03:00UTC là quy ước T§4, PDF chỉ ghi03:00.

Cwd `D:\Culinary_Blog`, PowerShell/Python3.13.7; áp dụng skill API Documentation và PDF để đối chiếu nguồn. Không sửa OpenAPI/types/backend/frontend/migration, không chạy deployment hoặc phase thực thi.

| Lệnh/kiểm tra thực tế | Expected/actual | Kết quả |
|---|---|---|
| `python docs/specs/srs-completion-phases/verify-decisions.py` |33 GAP/25 DEC/12 INV mappings;11 quyết định cũ được thay hiệu lực,5 mục Chờ làm rõ SRS; package/provenance/source guards, trạng thái D0-04, Markdown/local links và3 hash baseline hợp lệ |0/PASS |
| `python docs/specs/srs-completion-phases/verify-inventory.py` |34FR/30NFR/10CONS;347 acceptance/207 contracts/45 OpenAPI operations;82 local links; PDF hash không đổi |0/PASS |
| Python/hashlib/subprocess so snapshot và `git show HEAD:<path>` |136 file có sẵn không đổi;347 acceptance rows/207 CT rows nguyên vẹn; progress01/03–38 không đổi; approval cũ chỉ thêm banner |0/PASS |
| `git diff --check` với danh sách10 file hồ sơ; review diff và index trước commit | Chỉ hồ sơ A/B/C/theo dõi/verifier; không lỗi whitespace hoặc file ngoài scope |0/PASS |

Không chạy unit/integration/frontend/browser/benchmark/restore vì chỉ sửa hồ sơ. Kiểm tài liệu không chứng minh hệ thống đạt SRS. Commit/push tiếp tục theo ủy quyền người dùng; commit chỉ mô tả công việc bằng tiếng Việt không dấu, không ghi đã hoàn thành phase.
