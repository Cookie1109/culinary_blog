# Kết quả Phase 01 — Inventory acceptance và contract

- **Trạng thái: Đạt trong phạm vi tài liệu D0-01/D0-05.** Không xác nhận 74 yêu cầu đã đạt runtime; các acceptance thực thi và quyết định của phase sau vẫn mở.
- Ngày thực hiện: **23/10/2026, Asia/Bangkok**; agent Codex theo yêu cầu người dùng, không có người ký UAT/reviewer độc lập.
- HEAD trước thực hiện: `32e203e0b558446c51d6379c71dbec60644319c9`, nhánh `main`, remote `origin` trỏ repo `Cookie1109/culinary_blog`. Đầu vào có **143 đường dẫn thay đổi/chưa tracked**; index ban đầu không có staged change. Không reset/restore/clean hoặc sửa PDF đang chuyển thư mục.
- Fingerprint snapshot các đường dẫn/status/SHA256 file đầu vào: `bcb23196bf26c86aea47e5c30f3f3bf1a1be628f73591f9d030b0a766f1c5775` (SHA256 của `tmp/srs-inventory/before.json`). Evidence source trong inventory phản ánh **worktree dirty này**, gồm một số file chưa tracked từ trước; commit tài liệu không mang theo các thay đổi application đó. Không thể dùng riêng HEAD cũ để tái hiện trạng thái source đã review.
- Phiên bản sau: commit chứa report này có thể truy bằng `git log -1 -- docs/specs/srs-completion-phases/reports/phase-01-result.md`; không chèn SHA tự tham chiếu chưa tồn tại. Không build image/deploy/publish hoặc migrate DB.
- Runtime thực tế: Windows PowerShell, Python **3.13.7**, Node trên PATH **22.18.0**, .NET SDK **10.0.402**. Không chạy Node22.18 để chứng nhận baseline22.23.3. `.mise.toml` còn pin Node20.19.5/.NET10.0.401; đối chiếu ADR-010/package engines là đầu vào D1-01 của phase03, giữ nguyên trong lượt này.
- Profile/dataset: chỉ đọc file local; không DB/container/SMTP/provider test; không đọc/in secrets từ `.env`.
- Phụ thuộc: không có phase trước. Đã đọc README điều phối, progress, checklist gốc, mẫu report; đối chiếu SRS, Traceability Matrix, Conformance, Decision Log GAP-001–033, Product Baseline, Data Dictionary, Error Catalog, Route Map và báo cáo trạng thái lịch sử. Đọc file phase02 để đặt đúng owner quyết định, không thực hiện phase02.
- Áp dụng skill PDF (trích xuất và render kiểm bảng nguồn), Postman API Engineer → API Documentation (tái dùng OpenAPI/DTO/test, ghi contract và sai khác local). Không tạo workspace/mock hay publish Postman.

## Kết quả từng task

| Task | Requirement/acceptance/trang nguồn | Kết quả | Evidence/điều kiện còn mở |
|---|---|---|---|
| D0-01 | 34FR tr.17–39;30NFR tr.40–45;10CONS tr.14–15 | Đã tách **347 acceptance** với ID duy nhất, trang, ưu tiên, API/job/screen, expected, loại kiểm, hiện trạng/evidence và decision reference | [Inventory](../acceptance-inventory.md). Metadata trên mỗi bảng áp dụng cho mọi dòng. Có happy/alternative/error/actor/concurrency/dependency cases; mọi runtime row giữ `Chưa kiểm runtime`. Must của NFR/tóm tắt FR được phân biệt với MoSCoW nguyên văn |
| D0-05 | UI/network/provider/hardware Chương5 tr.46–49;architecture/Compose Chương6 tr.50–53;data Chương7 tr.54–60;API Chương8 tr.61–66;AppendixA/B/C tr.67–71 | Đã lập **207 contract rows**, có request/response/nullability/enums/limits/FK/index/audit/concurrency/soft-delete; có đủ **45 OpenAPI operations** hiện tại, gồm supporting/supplemental operations | Inventory CT-UI/HTTP/EXT/HW/ARCH/DATA/API/SUP/QUERY/SCHEMA/ERR/GLOSS. **12 nhóm INV-G01–12** chuyển đúng owner, không tự duyệt sai khác. Endpoint matrix có run evidence vẫn do phase24 làm |

## Thay đổi và quyết định

- Thêm [acceptance-inventory.md](../acceptance-inventory.md): mở rộng traceability hiện có xuống điều kiện kiểm cụ thể; giữ cả khác biệt nguyên văn PDF và baseline. Không lấy 27FR/6SEC do lỗi đếm trong SRS làm tổng; dùng34FR/7SEC theo GAP-001/027.
- Thêm [verify-inventory.py](../verify-inventory.py): verifier Python chuẩn, không package mới; kiểm đúng tập74 mã, ID không trùng, metadata/cells không rỗng, bảng Markdown, nhóm contract bắt buộc,45 operations OpenAPI, local links và SHA256 SRS. Đây là kiểm cấu trúc tài liệu, **không tự chứng minh tính đầy đủ ngữ nghĩa chỉ từ count**; phần ngữ nghĩa được review thủ công từng chương.
- Cập nhật checkbox D0-01/D0-05 của [checklist gốc](../../srs-completion-checklist.md), checkbox task/handoff trong [file phase01](../phase-01-acceptance-inventory.md), và đúng dòng01 cùng gap/handoff của [progress](../progress.md). Không đổi trạng thái phase02–38 hay các task owner khác.
- API/data/migration ảnh hưởng: **không sửa**. Chỉ ghi sai khác source→contract cần phase sau xử lý. Không nâng framework, thêm endpoint, sửa analyzer, chạy migration hoặc sửa application tests.
- Phần có sẵn được tái dùng: FR traceability, GAP/ADR, OpenAPI, DTOs, EF configuration/snapshot, route config, test suites/harness. Không viết lại contract hoặc dùng generated schema làm bằng chứng runtime.
- Phạm vi Git: chỉ tài liệu của công việc inventory và các file đầu vào điều phối cần cho liên kết/handoff; các thay đổi backend/frontend/OpenAPI/Postman và PDF/bài giảng có từ trước được giữ ngoài commit. Các file điều phối/checklist đã tồn tại nhưng chưa tracked trước lượt này; Git sẽ ghi chúng là file mới, không phải mọi nội dung lịch sử trong đó đều được thực hiện ở lượt này.

## Kiểm tra thực tế

Tất cả lệnh dưới đây chạy từ **`D:\Culinary_Blog`**, không phải lệnh dự kiến. Các kết quả lỗi dò đường dẫn đầu phiên không được ghi thành application-test fail/pass.

| Runtime | Lệnh/thao tác đã chạy | Loại evidence | Expected / actual | Exit code / kết quả | Log/run |
|---|---|---|---|---|---|
| Git CLI | `git status --short`, `git diff --cached --stat`, `git rev-parse HEAD`, `git remote -v` | Local repository | Worktree dirty/index trống/HEAD và remote xác định như trên | 0; trạng thái đọc thành công | Transcript; snapshot paths/hash `tmp/srs-inventory/before.json` (ignored) |
| Python3.13.7 + pypdf6.21.1 sẵn trong tmp/pdf-tools | `python -` trích từng page từ PDF vào pages.json/source.txt | PDF source extraction |71 trang; SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063` | Lần đầu exit1 do console cp1252 khi in tiếng Việt **sau khi extraction đã ghi file**; các lượt đọc page dùng stdout UTF-8 exit0 | `tmp/srs-inventory/pages.json`, `source.txt`; report lưu hash để kiểm lại bản nguồn |
| Python3.13.7 + pypdf6.21.1 | `python -` với `PdfReader`, `extract_text()` và regex chuẩn hóa mã qua whitespace; so tập mã với heading inventory | Independent source completeness | Trích đủ74 mã từ PDF, tập mã bằng inventory, không missing/extra | **0, PASS**. Lượt thử trên layout-text exit1 vì text các cột chen giữa mã, chỉ nhận66; chuyển plain-text để kiểm độc lập, không giảm yêu cầu | `tmp/srs-inventory/plain.txt`; stdout74=74, missing=[]/not-extracted=[] |
| Poppler bundled | `pdftoppm.exe -f 63 -l 63 -scale-to 1800 -png -singlefile docs/specs/SRS_Culinary_Blog_v1.0.0.pdf tmp/srs-inventory/page-63` | PDF visual review | Render bảng API tr.63, kiểm cột/table thực; thấy bảng vượt mép phải từ nguồn | 0; đã mở ảnh xem, không tự bịa phần bị cắt; dùng FR/baseline bổ sung có dẫn nguồn | `tmp/srs-inventory/page-63.png` (ignored) |
| PowerShell/rg | `Get-Content` và `rg -n` trên nguồn spec/baseline/DTO/config/endpoint/test/package/scripts | Static/source review | Định vị implementation/test và sai khác có căn cứ |0 cho các lần đọc nguồn cuối; dò sai đường dẫn Enums/HealthEndpoints/AuditInterceptor đã tìm lại theo cây source, không coi là lỗi chương trình | Reference E-AUTH…E-OPENAPI và INV-G01–12 trong inventory |
| Python3.13.7 | `python docs/specs/srs-completion-phases/verify-inventory.py` | Documentation regression |74 mã đúng tập,347 acceptance,207 contracts,45 operations,79 local links/hash/table hợp lệ | **0, PASS** ở lượt cuối. Lượt đầu phát hiện pipe Markdown chưa escape và report chưa tạo; đã sửa/tạo rồi kiểm lại | Kết quả tổng hợp ở đây; stdout cuối trong transcript |
| Git CLI | `git diff --check` và `git diff --cached --check` sau stage đúng danh sách | Documentation diff/whitespace | Không whitespace error trong phần được stage | **0** ở lượt cuối | Transcript staged review; không suy app regression từ diff check |
| Python3.13.7/hashlib | So snapshot trước-sau, loại các file phase01 đã sửa | Worktree preservation | Trong143 đường dẫn cũ, chỉ3 tài liệu được giao đổi bytes;140 đường dẫn còn lại giữ nguyên, không file mới ngoài scope (trừ tmp ignored) | **0** ở lượt cuối; không sửa backend/frontend/PDF/Postman | `tmp/srs-inventory/before.json`; command kiểm và count ở transcript |

Không chạy unit/integration/frontend/browser/production HTTP/provider/performance/scans/CI/UAT: không sửa code/runtime, các kiểm này không chứng minh thêm tính đúng của thay đổi tài liệu. Không lấy75/3/104 hoặc41/99 trong report cũ làm kết quả mới. Các phép đo và môi trường bắt buộc đã được đưa vào từng acceptance và owner, chưa đạt runtime thì vẫn mở.

## Gap bàn giao và cập nhật theo dõi

| Nhóm | Nội dung cần tiếp nhận | Phase tiếp nhận |
|---|---|---|
| INV-G01–03 | PUT partial/replacement; nested composition create khác DTO; sortBy/sortOrder/page defaults khác bảng API |02 quyết định;17/18 nghiệp vụ;22 contract/migration khi cần |
| INV-G04–07 | Google/inactive status mapping; health entries/privacy; empty search hint; VerifiedAuthor |02 quyết định;05/08/10/18/22/33 theo inventory |
| INV-G08–11 | Identity Guid so string GAP-032; Instructions nullable/enums storage; image URL nullability; RT IP/UA schema/retention |02 quyết định,19 media,22 schema/upgrade/types; không migrate dữ liệu hiện có lúc inventory |
| INV-G12 | Response tokens writeOnly và nutrition schema mở trong OpenAPI |22 D6-15;02 nếu đổi baseline |

- Đóng **D0-01, D0-05** vì sản phẩm của chúng là inventory đã review/kiểm, không phải implementation của74 requirements.
- **D0-02/03/04 vẫn mở**: GAP approvals và câu hỏi mới; Output Cache/caching mechanism, Google ping, analyzer/test tools, browser minimum, singleton lock, uptime99.5% vs3.65h/năm, timezone backup03:00 và scope production scaling. Chỉ rõ decision reference trước test, không tự ký thay người nghiệm thu.
- Giữ nguyên task D1–D10 và trạng thái phase02–38. G01–G09 cũ không được đóng lại bởi report này.
- Progress dòng01=`Đạt (tài liệu)`; ngày23/10/2026, HEAD đầu phiên+dirty reference và link report này. Các gap có owner/impact trong inventory/report và progress, không làm mất evidence cũ của phase khác.

## Bàn giao

- Người nhận phase02 đọc các dòng `INV-G01–12`, các decision còn mở và giữ GAP-001–033; không mặc nhiên chấp nhận mọi sai khác source so PDF. Tạo decision-register theo file được giao ở lượt sau.
- Người nhận phase03 xác nhận runtime pinned/host và test stack riêng trước baseline. `.mise.toml` lệch Node baseline là việc của D1, không bị sửa tiện thể trong phase01.
- Người viết test dùng ID acceptance/CT để map tới method/run mới; evidence locator hiện tại chỉ là điểm vào. Chạy verifier từ root bằng `python docs/specs/srs-completion-phases/verify-inventory.py`; các lệnh application có thật được liệt kê trong inventory/checklist mục6 nhưng **chưa chạy lần này**.
- Không cần credential/thiết bị để đóng inventory tài liệu. Google/browser/device/staging/RUM/uptime/backup/scaling/UAT là điều kiện của các owner phase tiếp theo, chưa được xác minh ở đây.
- Kết luận đúng phạm vi: inventory/handoff đủ dùng cho quyết định và viết test; chưa nghiệm thu hệ thống hoặc chạy phase tiếp theo.
