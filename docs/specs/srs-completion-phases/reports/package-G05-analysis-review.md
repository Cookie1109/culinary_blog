# G05 — Đề nghị review diagnostic theo ngữ cảnh

**Trạng thái: người dùng đã duyệt ngày 29/10/2026: “Duyệt 4 vị trí có kiểm chứng”. Đã áp dụng đúng bốn vị trí bên dưới; strict gate đã pass0/0, negative controls pass và browser nguồn cuối7/7 (xem report G05).** Không thay PDF SRS hoặc A/B/C, không giảm coverage, không bỏ test và không tắt rule trên toàn project. Phê duyệt sửa diện rộng type/documentation ngày 29/10/2026 đã được thực hiện riêng.

Sau sửa lỗi thực và style/documentation, strict build còn đúng hai lỗi ở Infrastructure (`tmp/package-G05/analyzers-strict-review.log`). Đề nghị ghi nhận phạm vi hẹp sau:

| Rule/vị trí | Kiểm soát đã triển khai và kiểm chứng | Đề nghị |
|---|---|---|
| S5332 — `Email/SmtpClientFactory.cs`, object initializer duy nhất | Factory từ chối SMTP plaintext tới host bên ngoài; options validation cũng chặn. `ProductionConfigurationGuard` từ chối mọi SMTP không TLS khi email production bật, kể cả localhost. Plaintext chỉ còn cho loopback/localhost/MailHog capture đã có trong môi trường thử. | Pragma riêng quanh initializer đó, có comment dẫn policy/tests; không bỏ kiểm TLS production hoặc tắt S5332 nơi khác. |
| S6444 — `Content/ContentService.SearchPredicates.cs`, một expression tree | Pattern là literal đã Regex.Escape; Npgsql chuyển predicate thật sang SQL `~*`, không chạy regex .NET. Timeout overload bốn tham số không được Npgsql10 hỗ trợ. DB command timeout được đặt rõ 30s; regex chạy trong ứng dụng đã có timeout1s. | Pragma riêng một dòng tạo expression, có comment dẫn kiểm SQL và giới hạn; các regex .NET khác vẫn chịu S6444. |

`analysis-controls.trx`: **11/11 pass, 0 skip**, gồm 7 policy SMTP không mở kết nối/gửi email, 3 production TLS cases và SQL translation của chính expression production. `profile-security-current.log`: **3/3 pass**, gồm client validation avatar và giữ dữ liệu sau lỗi server.

Ngoài hai vị trí backend, Airbnb `no-script-url` báo đúng hai **literal dữ liệu thử bị từ chối**: `scripts/check-profile-browser.mjs` và `tests/ux-ui-behavior.test.mjs`, cùng input `javascript:alert(1)`. Đề nghị `eslint-disable-next-line` đúng hai literal với lý do negative fixture; giữ nguyên dữ liệu xấu/assertion chặn submit, không tắt rule ở mã sản phẩm hoặc xóa test.

Căn cứ translator chính thức: [Npgsql10 Regex translator](https://github.com/npgsql/efcore.pg/blob/v10.0.0/src/EFCore.PG/Query/ExpressionTranslators/Internal/NpgsqlRegexTranslator.cs), chỉ dịch IsMatch hai/ba tham số. Kiểm thực tế dùng package10.0.3 đã pin và `ToQueryString()` trên expression production.

Sau duyệt vẫn phải chạy negative controls chứng minh các rule còn fail ngoài phạm vi review, strict analyzer/lint toàn bộ và regression trên source cuối. Đây là review diagnostic có bằng chứng; không phải kết luận CI hoặc toàn SRS đã đạt.
