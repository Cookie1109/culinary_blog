# Kế hoạch hoàn thành 4 yêu cầu chức năng

Ngày lập: **02/11/2026**, Asia/Bangkok. Trạng thái: **kế hoạch, chưa thực thi**.

## 1. Phạm vi và giả định

Hoàn thành và kiểm chứng bốn yêu cầu:

1. Upload ảnh công thức.
2. Trang thêm/sửa công thức và vòng đời Draft → Published → Archived/Deleted.
3. Trang danh sách người dùng gồm Admin và Author.
4. Logging và trang xem nhật ký hệ thống.

**G06 và G07 do người dùng thực hiện riêng.** Kế hoạch này không thực hiện benchmark/scaling production, RUM, security deployment, backup production hoặc ký nghiệm thu toàn bộ SRS. Kết quả của kế hoạch là chức năng và bằng chứng trên bản build local xác định, để người dùng tiếp tục G06/G07.

Giả định triển khai:

- Chỉ Admin được xem danh sách người dùng và nhật ký hệ thống. Author tiếp tục xem/cập nhật hồ sơ của mình theo chức năng hiện có.
- Danh sách người dùng chỉ phục vụ xem, tìm kiếm tên/email, lọc vai trò và phân trang; không thêm tạo/xóa tài khoản, đổi quyền hoặc khóa tài khoản.
- Giữ mô hình hiện tại: `RecipeStatus` gồm Draft, Published, Archived; xóa công thức dùng `IsDeleted`. Không thêm enum Deleted hoặc đổi sang hard delete trong phạm vi này.
- Giữ kiến trúc, phong cách UI và hợp đồng đang dùng. Tái sử dụng phần đã triển khai, chỉ sửa sai sót được kiểm chứng.
- Dùng dữ liệu và dịch vụ thử riêng; không thay đổi dữ liệu, migration hoặc cấu hình của stack người dùng để lấy bằng chứng.

## 2. Điểm xuất phát

| Yêu cầu | Hiện trạng đã quan sát | Công việc chính |
|---|---|---|
| Upload ảnh | Có UI, API, MinIO, validation, ảnh chính và xử lý nền | Kiểm chứng đường UI → API → storage/job thật; sửa lỗi nếu có |
| Quản lý công thức | Có wizard, CRUD, quyền sở hữu, ETag và lifecycle | Kiểm chứng chuyển trạng thái, hiển thị công khai, xóa mềm và xung đột |
| Danh sách người dùng | Chưa có trang/API | Bổ sung contract, backend, UI và kiểm thử phân quyền |
| Logging | Có Serilog, request/pipeline logging, audit middleware và trang Admin | Kiểm chứng sự kiện thật, thông tin truy vết, bảo mật và khả năng đọc lại |

Baseline kiểm tra ngày 02/11/2026: backend unit **129/129**, architecture **3/3**, frontend Node **140/140**, Jest **37/37** đạt. Đây là kết quả trước thay đổi; không thay kết quả trên bản bàn giao sau khi thực hiện kế hoạch.

Điểm lưu ý: `AuditLogService` kết hợp rolling log files với dữ liệu suy ra từ các trường Created/Updated/Published/Deleted của entity. Snapshot của entity không tự chứng minh đủ lịch sử từng lần sửa hoặc chuyển trạng thái.

## 3. Thứ tự thực hiện

### Bước 0 — Chốt baseline và ma trận nghiệm thu

- [ ] Ghi HEAD, danh sách thay đổi có sẵn, runtime và hash các file thuộc phạm vi; giữ nguyên thay đổi ngoài phạm vi.
- [ ] Đối chiếu contract hiện tại, test và evidence liên quan; xác định bằng chứng nào còn khớp source.
- [ ] Lập ma trận yêu cầu → UI/API → tình huống → test/evidence, gồm Guest, Author A, Author B và Admin.
- [ ] Chuẩn bị DB/Redis/MinIO và tài khoản synthetic riêng bằng harness có sẵn; SMTP thử chỉ dùng khi harness yêu cầu.

**Đạt khi:** mỗi yêu cầu có tiêu chí kiểm tra cụ thể, môi trường thử tách biệt và các file dự kiến sửa được xác định.

### Bước 1 — Bổ sung danh sách người dùng

**Hợp đồng đề xuất**

- API: `GET /api/v1/admin/users`.
- Query: `search`, `role`, `page`, `pageSize`; `role` nhận Admin/Author theo chuẩn biểu diễn thống nhất với contract hiện tại.
- Phân trang mặc định 20, tối đa 100; kiểm tra đầu vào theo quy ước API hiện có.
- Response theo envelope `data` + `meta` hiện tại. Chỉ trả `id`, `displayName`, `email`, `roles`, `isActive`, `createdAt`.
- Thứ tự ổn định: `createdAt` giảm dần, sau đó `id` làm tie-break.
- Lọc theo membership: tài khoản có cả Admin và Author xuất hiện một lần; mảng `roles` giữ đúng các vai trò hiện có.
- Dùng `AdminPolicy` và kiểm tra quyền trong Application theo pattern đang có. Guest nhận 401; Author nhận 403; Admin hợp lệ nhận 200.
- Dữ liệu riêng tư, không lưu vào public/shared cache; không trả password hash, security stamp, refresh/reset token hoặc thông tin xác thực nội bộ.

**Triển khai và kiểm chứng**

- [ ] Cập nhật OpenAPI trước, bổ sung query/DTO/validator/handler và implementation truy vấn theo kiến trúc hiện tại.
- [ ] Dùng bảng Identity/role hiện có, phân trang tại DB, không gọi truy vấn role riêng cho từng dòng. Dự kiến không cần migration; kiểm tra schema trước khi quyết định.
- [ ] Sinh lại TypeScript types và bổ sung authenticated API client.
- [ ] Tạo `/dashboard/users`, mục điều hướng dành cho Admin trên desktop/mobile và guard truy cập trực tiếp.
- [ ] Hiển thị bảng/card, vai trò, trạng thái, ngày tạo, tìm kiếm/lọc/phân trang và loading/empty/error/retry; đổi bộ lọc đưa về trang 1.
- [ ] Test dữ liệu Admin/Author/đa vai trò, tìm kiếm, role filter, thứ tự, phân trang, đầu vào sai, 401/403 và response không chứa trường nhạy cảm.
- [ ] Browser test Admin xem được danh sách, tìm/lọc/phân trang; Author không tải được dữ liệu khi mở URL trực tiếp hoặc gọi API.

**Đạt khi:** Admin xem đúng danh sách từ DB thật và dùng được các thao tác trên UI; Guest/Author bị chặn tại backend; dữ liệu nhạy cảm không được trả về.

### Bước 2 — Kiểm chứng upload ảnh và hoàn thiện lỗi còn lại

- [ ] Từ wizard tạo công thức, upload JPEG/PNG/WebP/AVIF hợp lệ; kiểm tra object và metadata trong MinIO/DB thử.
- [ ] Xác nhận giới hạn 5 × 1024 × 1024 bytes, MIME/signature không khớp, file rỗng và file hỏng bị từ chối; lỗi giữ được lựa chọn để thử lại và không để lại metadata/object mồ côi.
- [ ] Kiểm tra Author chỉ upload cho công thức của mình; Author khác/Guest bị chặn; Admin thao tác theo quyền hiện có.
- [ ] Kiểm tra ảnh đầu tiên làm ảnh chính, đổi ảnh chính, alt/order và xóa ảnh chính tự chọn ảnh thay thế; UI tải lại vẫn đúng.
- [ ] Chạy worker thật, giải mã kiểm kích thước thumbnail/medium theo contract; khi job lỗi vẫn xem được ảnh gốc và retry đúng.
- [ ] Dừng storage trong môi trường thử riêng: lỗi theo contract, form không mất dữ liệu, phục hồi và upload lại được.
- [ ] Kiểm tra ảnh của Draft/Archived/Deleted không bị Guest truy cập bằng URL media hoặc object storage.
- [ ] Kiểm tra xóa ảnh và xử lý đồng thời/ETag: không xóa nhầm object, không làm sai ảnh chính hoặc version.

**Đạt khi:** UI → API → MinIO → job → hiển thị hoạt động trên build thử hiện tại; các ca lỗi/quyền có assertion và không làm hỏng dữ liệu.

### Bước 3 — Kiểm chứng thêm/sửa và vòng đời công thức

- [ ] Tạo Draft qua UI với thông tin cơ bản, nguyên liệu, bước nấu và ảnh; tải lại hoặc đăng nhập lại vẫn giữ nội dung đã lưu.
- [ ] Sửa từng nhóm dữ liệu; đầu vào không hợp lệ có thông báo cạnh trường và không ghi một phần ngoài ý muốn.
- [ ] Author A sửa được công thức của mình; Author B bị từ chối; Admin sửa được theo chính sách hiện tại. Kiểm cả UI và API trực tiếp.
- [ ] Hai phiên sửa cùng công thức: request dùng version cũ bị từ chối theo contract; UI giữ input và cho tải lại bản mới.
- [ ] Publish bị chặn nếu thiếu điều kiện bắt buộc; publish hợp lệ xuất hiện ở home/list/category/search/detail sau invalidation hoàn tất.
- [ ] Unpublish đưa về Draft; archive từ các trạng thái được phép ẩn khỏi public; unarchive đưa về Draft theo implementation hiện tại.
- [ ] Xóa mềm làm công thức biến mất khỏi danh sách/detail/media theo chính sách hiện tại; kiểm cờ xóa và tác vụ dọn ảnh liên quan.
- [ ] Kiểm thao tác chuyển trạng thái sai, nhấn lặp và lỗi API không tạo thông báo thành công giả hoặc mất nội dung.
- [ ] Kiểm confirmation, trạng thái đang xử lý và hiển thị danh sách trên desktop/mobile.

**Đạt khi:** vòng đời và quyền đúng ở DB/API/UI; mọi điểm hiển thị công khai phản ánh trạng thái mới sau thời gian xử lý theo contract; các nhánh lỗi giữ dữ liệu người dùng.

### Bước 4 — Kiểm chứng logging và trang nhật ký

- [ ] Gửi request có CorrelationId xác định; kiểm log request chứa method/path/status/duration, CorrelationId và UserId khi xác thực.
- [ ] Kiểm Command/Query được ghi log và lỗi được ghi đúng mức; kiểm ngữ cảnh job nền liên quan tới upload/dọn ảnh.
- [ ] Thực hiện chuỗi tạo → sửa hai lần → publish → unpublish → publish → archive → unarchive → delete; đối chiếu từng sự kiện thật với người thao tác, đối tượng, thời gian và CorrelationId.
- [ ] Chứng minh các lần sửa/chuyển trạng thái riêng biệt được ghi nhận; không lấy dữ liệu suy từ snapshot cuối để thay sự kiện còn thiếu. Nếu phát hiện thiếu, sửa đúng đường ghi/đọc log hiện có.
- [ ] Admin tra được chuỗi thao tác từ trang nhật ký bằng bộ lọc/tìm kiếm/phân trang hiện có; không nhân đôi sự kiện khi kết hợp file và fallback DB.
- [ ] Kiểm lỗi API/validation/quyền có request log tương ứng; mutation thất bại không được ghi thành nghiệp vụ thành công.
- [ ] Kiểm log không chứa password, Authorization header, access/refresh/reset token hoặc nội dung multipart nhạy cảm.
- [ ] Kiểm rolling files và volume log local: restart API trong harness có persistence, đọc lại được sự kiện trước restart; không tuyên bố đây là retention production.
- [ ] Kiểm Guest/Author không đọc được API/trang nhật ký Admin; kiểm sink lỗi theo bộ test hiện có.

**Đạt khi:** truy được các thao tác thật và lỗi bằng dữ liệu log trên build hiện tại, có đủ ngữ cảnh, không lộ thông tin xác thực, quyền đọc được thực thi tại backend.

### Bước 5 — Hồi quy và bàn giao

- [ ] Chạy test liên quan sau từng thay đổi; bổ sung test cho hành vi mới/lỗi được chứng minh, tái sử dụng test hiện có.
- [ ] Cập nhật ma trận endpoint đang dùng bởi verifier cho API users và happy/error/authorization cases; giữ evidence lịch sử và nêu rõ source mới.
- [ ] Chạy kiểm tra chung trên source cuối: backend build/unit/architecture/integration, coverage gate Application ≥80%, frontend Node/Jest, lint/typecheck/format, generated types, production build và analyzers theo cấu hình repo.
- [ ] Chạy Playwright harness với backend/storage thật trên source cuối, gồm luồng upload/lifecycle và users mới; kiểm nhật ký cho các thao tác bằng integration và browser tương ứng.
- [ ] Ghi command/cwd/runtime/exit code, số pass/fail/skip, test IDs, source/build identity và evidence; ca chưa chạy hoặc môi trường lỗi giữ trạng thái chưa xác minh.
- [ ] Tạo báo cáo kết quả riêng cho bốn yêu cầu, danh sách lỗi còn lại và bước demo tái hiện. Mỗi yêu cầu chỉ được đánh đạt khi đủ tiêu chí tương ứng.
- [ ] Bàn giao danh sách file/hash đã thay đổi và các phép đo/bằng chứng G06/G07 có thể mất hiệu lực. Người dùng dùng bản chức năng cuối này để tiếp tục hai gói; không tự đóng task G06/G07.

**Đạt khi:** cả bốn hàng trong ma trận nghiệm thu đều đạt trên cùng source cuối, không còn lỗi chức năng thuộc phạm vi; những giới hạn của môi trường thử được ghi cụ thể.

## 4. Điều kiện kết thúc

| Yêu cầu | Bằng chứng tối thiểu để đóng |
|---|---|
| Upload ảnh | Browser upload và xử lý job/storage thật; assertion file sai/quyền/ảnh chính/xóa/privacy/failure |
| Quản lý công thức | Browser tạo/sửa/lifecycle; API quyền/ETag/validation; public visibility/invalidation và xóa mềm |
| Danh sách người dùng | API đúng dữ liệu/roles/paging/privacy/401/403; browser Admin thao tác và Author bị chặn |
| Logging | Log thật của chuỗi thao tác; context/redaction/persistence local; Admin tra được, Guest/Author bị chặn |

Kết luận bàn giao chỉ xác nhận **bốn yêu cầu chức năng đã đạt trong môi trường và artifact được ghi nhận**. Việc hoàn tất toàn bộ SRS và nghiệm thu cuối do người dùng tiếp tục qua G06/G07 cùng các điều kiện còn mở liên quan.
