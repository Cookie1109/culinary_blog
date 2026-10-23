# Giao việc theo 7 gói

Mục tiêu: giảm số lần giao lại, đọc nguồn chung và dựng môi trường lặp. **PDF SRS vẫn là tiêu chuẩn; giữ đủ 38 phase/85 task, owner và acceptance hiện tại.** Đây là cách điều phối, không phải kế hoạch viết lại hệ thống. Không cam kết mỗi gói chỉ cần một phiên hoặc quota giảm theo một tỷ lệ cố định.

## Chọn gói

Các số trong cột phase là chỉ mục của [README](README.md). Mỗi phase thuộc đúng một gói hoặc hàng chờ riêng; không chuyển owner task.

| Gói | Phạm vi | Phase | Số task | Đầu vào / thứ tự |
|---|---|---|---|---|
| G01 | Nguồn yêu cầu, điểm còn mở và môi trường | 01,02,03 | 9 | Tái dùng01 → xử lý02 và làm03 độc lập |
| G02 | Kiến trúc, validation, quyền và đăng nhập | 04,05,06,07,08,09 | 13 | Đầu vào liên quan từ G01;04 →05 →06/07 →08 →09 |
| G03 | Routes, rendering, ISR, cache và rate limit | 11,12,13,14,15,16 | 14 | Phần cần dùng của G01/G02;11 →12 →13 →14 →15 →16 |
| G04 | Nghiệp vụ, media, jobs, SEO, schema và API | 17,18,19,20,21,22 | 15 | Phần cần dùng của G02/G03;17 →18/19/20 →21 →22 |
| G05 | Coverage, integration, E2E, UX và CI | 23,24,25,26,27,29 | 14 | Contract G04 ổn định và auth G02;23 →24 →25 →26 →27 →29 |
| G06 | Hiệu năng, bảo mật, vận hành, backup và scaling | 30,31,32,33,34,35 | 9 | Gates liên quan G05;30/31/32/33 →34 sau33 →35 sau30/33/34 |
| G07 | Tài liệu cuối, traceability và bàn giao | 37,38 | 7 |37 dùng evidence G04/G05;38 review mọi phase01–37, gồm hàng chờ |

Tổng trong 7 gói: **35 phase/81 task**. Ba phase còn lại có **4 task** trong hàng chờ bên dưới. Không yêu cầu mọi phần của một gói phải xanh mới cho làm phần độc lập của gói sau, nhưng phải nêu chính xác dependency nào đã đủ và dependency nào còn thiếu theo bảng README.

## Hàng chờ điều kiện riêng

| Phase | Task gốc | Đầu vào / điều kiện | Cách giao và evidence |
|---|---|---|---|
| 10 | D3-05,D3-07 | Sau08/09; Google OAuth app, redirect URI/domain, tài khoản thử và credential phù hợp | Giao riêng phase10 khi sẵn điều kiện; dùng report phase hoặc dẫn evidence chung; mock không chứng minh provider thật |
| 28 | D8-10 | Sau02/27 ở phần liên quan; browser/device minimum, NVDA/VoiceOver và người thao tác phù hợp | Giao riêng phase28 khi sẵn điều kiện; giữ kết quả theo từng thiết bị/browser, không suy JSDOM là screen reader |
| 36 | D9-08 | Sau02/33/34; tiêu chí SLA đã chốt, deployment/monitor/receiver được phép và đủ thời gian quan sát | Giao riêng phase36 khi sẵn điều kiện; không tự coi thời gian chờ là SLA đạt |

Đây là các yêu cầu vẫn bắt buộc theo phạm vi SRS, không phải danh sách miễn kiểm. Việc tạo bảng không tự đổi trạng thái phase10/28/36 thành Chờ môi trường: chỉ cập nhật trạng thái sau khi kiểm điều kiện thực tế.

Các phần thiếu RUM/HTTPS staging/production topology/Rich Results/UAT vẫn nằm tại task và phase gốc trong G04/G06/G07. Report ghi phần đã làm, phần chờ, điều kiện cần và việc độc lập có thể tiếp tục; không tạo owner thứ hai. Chuẩn bị được gói bàn giao không có nghĩa G07 hoặc toàn bộ SRS đã đạt. Gom gói không cấp quyền deploy, gửi email/alert, dùng tài khoản người khác hoặc ký UAT.

## Phạm vi từng gói và checkpoint

### G01 — Nguồn và môi trường

- Đọc report01/02 và register hiện tại; tái dùng inventory01 đã kiểm, chỉ kiểm lại phần bị thay đổi. Không lập lại danh mục347 acceptance từ đầu.
- Giữ hiệu lực [đính chính A/B/C](approvals/2026-10-23-abc-correction.md). D0-04 còn mở; không chọn phương án trái PDF để đóng quyết định. Thiếu quyết định không chặn kiểm runtime/môi trường03 độc lập.
- Kiểm môi trường, format và baseline test03 trên worktree hiện tại; không lấy test lịch sử làm kết quả mới. Checkpoint chỉ rõ phần nguồn đã đủ cho G02 và các contract còn cần làm rõ.

### G02 — Kiến trúc và auth

- Làm04–06 cho handlers/pipeline/quyền/transaction/logging, sau đó07–09 cho local auth, Google session/refresh và logout owner; giữ phụ thuộc trong README cho từng phần.
- Kiểm chứng code đang có trước sửa; dùng chung fixtures/test environment và chỉ sửa gap được chứng minh. Provider thật thuộc phase10 riêng.
- Checkpoint: validation/quyền/rollback và vòng đời token có evidence, phần chưa đạt được ghi riêng; bàn giao đầu vào routes/cache của G03.

### G03 — HTTP, rendering và cache

- Thực hiện11–16 theo thứ tự phụ thuộc; kiểm HTTP status/rendering/ISR, mutation→revalidation, cache/privacy/failure và quota/headers chia sẻ.
- Dùng lại production build khi source liên quan chưa đổi; sau thay đổi phải build/kiểm lại phần chịu ảnh hưởng. Local production-mode HTTP không tự chứng minh production deployment.
- Checkpoint: nối được evidence backend→Next→cache và phân biệt single/multi-instance; không làm lại auth hay nghiệp vụ ngoài phần dependency cần dùng.

### G04 — Nghiệp vụ và contract

- Sau17, làm18/19/20 ở phần độc lập,21 sau20,22 sau đủ đầu vào. Bao phủ composition/search/media/jobs/SEO rồi đối chiếu schema/migration/API/types.
- Dùng PDF và register đã đính chính; RedLock/Identity string/Instructions NOT NULL/smallint và message search không được thay bằng ngoại lệ A/B/C cũ. Phần chưa rõ giữ mở trước khi sửa contract phụ thuộc.
- Checkpoint: contract nhất quán với behavior đã kiểm, migration thử trên DB riêng, tác động/known gaps đủ cho G05. Ghi ping provider đã ngừng là vướng mắc, không nhận sitemap/robots làm waiver.

### G05 — Kiểm thử và CI

- Dùng code/contract G04 ổn định để làm23–27 rồi29. Thêm test thiếu có giá trị; tái dùng happy/error/role cases thay vì dựng suite mới cho mỗi phase.
- Coverage Application≥80%, integration endpoint matrix, năm luồng E2E, UX/a11y và CI vẫn giữ đúng điều kiện gốc. Phase28 có thiết bị/screen reader thật nằm ở hàng chờ.
- Checkpoint: regression/gates trên đúng source/artifact, log/count/skip có nguyên nhân; chỉ chạy lại suite khi source/dependency liên quan đổi hoặc có thất bại chưa giải thích.

### G06 — Phi chức năng và vận hành

- Chuẩn bị môi trường synthetic một lần cho các bài30–35 tương thích; cô lập tài nguyên/dataset mỗi phép đo, ghi rõ profile để không nhiễu số đo. Restore/failure drill dùng target thử riêng.
- Sau33 làm34;35 cần kết quả30/33/34 và shared cache/quota/jobs trước đó. Không dùng việc gom gói để chạy bài scaling khi đầu vào chưa đủ.
- Checkpoint: phân biệt benchmark local/staging/production, lab/RUM, config/evidence thật. D0-04/protocol, credential, topology hoặc cửa sổ mẫu thiếu thì ghi mở đúng task. SLA quan sát thuộc36, không chờ thụ động trong phiên agent.

### G07 — Bàn giao

- Tổng hợp37 từ report/evidence hiện có, kiểm artifact/config/docs và acceptance còn thiếu; không viết lại báo cáo các gói.
- Phase38 phải review cả 10/28/36 và các phần môi trường/quyết định còn mở. Hỗ trợ UAT và kết luận đúng phạm vi; không tự ký thay người nghiệm thu.
- Checkpoint: manifest/evidence liên kết được tới từng acceptance, danh sách pass/fail/pending và bước tiếp theo cụ thể. Chỉ kết luận toàn SRS đạt khi đủ điều kiện gốc.

## Quy trình để giảm đọc và chạy lặp

1. Đầu lượt đọc AGENTS.md, README, progress, mục gói được giao và checkpoint hiện tại nếu có. Chỉ đọc các file phase thuộc gói và report dependency liên quan; không nạp toàn bộ report38 phase mỗi lượt.
2. Giữ các thay đổi có sẵn. Lập danh sách task trong gói: đã có evidence còn phù hợp / cần kiểm / cần sửa / thiếu điều kiện. Đọc phần PDF/inventory/checklist/register cần dùng; không bỏ nguồn chỉ để tiết kiệm context.
3. Kiểm phần hiện có trước sửa, làm theo phụ thuộc. Khi gặp điểm chặn, ghi ảnh hưởng và tiếp tục task độc lập trong gói; không suy approval hoặc bỏ acceptance để đi tiếp.
4. Sau thay đổi, chạy kiểm tra liên quan. Dùng một lần chạy chung cho nhiều task khi cùng source/artifact, config, môi trường và đủ assertions; report mapping mỗi task tới evidence đó. Chạy regression/gates bắt buộc ở checkpoint thích hợp, không chạy lại chỉ vì sang phase nhỏ.
5. Cập nhật **một report** `reports/package-GNN-result.md` theo [mẫu](reports/README.md), từng dòng phase đã xử lý và checkbox gốc. Giữ report cũ, không sao chép evidence sang nhiều report phase. Phase chưa đụng tới giữ trạng thái cũ.
6. Tiếp tục task còn làm được trong gói mà không yêu cầu người dùng giao từng phase. Khi cần đổi phiên, lưu checkpoint; khi gói hết việc có thể làm với đầu vào hiện tại, bàn giao các phần còn mở. Không hứa7 gói tương đương đúng 7 phiên.

Một report gói có thể chứa phase Đạt và phase Chờ; trạng thái tổng phải phản ánh phần còn mở. Không đánh toàn gói Đạt vì phần local xanh hoặc vì đã đọc đủ file. Bảng task→phase trong progress vẫn là nguồn owner duy nhất. Gói chỉ là phạm vi giao việc, không phải owner thay thế.

## Prompt dùng ngay

Thay `G01` bằng gói muốn giao. Giao G01 hiện tại sẽ tái dùng phần01 đã có, không chạy lại toàn bộ phần lập inventory.

```text
Thực hiện gói G01 theo docs/specs/srs-completion-phases/work-packages.md.
Đọc AGENTS.md, README điều phối, progress.md và checkpoint mới nhất của gói nếu có.
PDF SRS là tiêu chuẩn; giữ đính chính A/B/C hiện tại, không tự đặt ngoại lệ mới.
Chỉ đọc sâu các phase/task của gói và dependency liên quan. Kiểm phần đã có trước,
chỉ sửa phần thiếu hoặc thất bại, giữ mọi thay đổi có sẵn ngoài phạm vi.
Tiếp tục các phase trong gói khi đủ đầu vào, không dừng xin giao lại từng phase.
Ghi mở phần thiếu quyết định/môi trường và làm phần độc lập còn lại trong gói.
Chạy kiểm tra liên quan sau sửa, gom regression chung khi phù hợp và giữ đủ gates.
Cập nhật reports/package-G01-result.md, từng dòng phase đã xử lý trong progress.md
và checkbox task gốc theo bằng chứng. Nếu cần đổi phiên, ghi checkpoint để tiếp tục.
Không tự chạy gói khác hoặc phase10/28/36. Commit/push theo ủy quyền hiện có;
nếu commit, mô tả việc đã làm bằng tiếng Việt không dấu, không nói hoàn thành phase.
```

Prompt tiếp tục cùng gói khi đổi phiên:

```text
Tiếp tục gói G01 theo docs/specs/srs-completion-phases/work-packages.md.
Đọc AGENTS.md, README, progress và reports/package-G01-result.md, nhất là checkpoint.
Đối chiếu git diff/runtime/artifact với evidence đã có; chỉ kiểm lại phần bị ảnh hưởng.
Tiếp tục bước còn mở trong phạm vi gói khi đủ đầu vào, không làm lại phần còn hợp lệ.
Giữ PDF làm chuẩn, cập nhật cùng report/tiến độ/checkbox theo bằng chứng thực tế.
```

Khi điều kiện cho10/28/36 đã sẵn, người dùng có thể giao phase tương ứng bằng prompt trong file phase. Không cần chờ mọi gói hoàn thành mới xử lý hàng chờ, nhưng phải đủ dependency và phạm vi ủy quyền.

[README điều phối](README.md) · [Tiến độ](progress.md) · [Checklist85 task](../srs-completion-checklist.md)
