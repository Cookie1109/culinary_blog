# G06 — Các đầu vào còn thiếu sau triển khai CWV

**Cập nhật 11/11/2026.** D9-01/04/09/10 đã đóng cho báo cáo môn học miễn phí. Các điều kiện dưới đây được giữ để đối chiếu khi nghiệm thu SRS. Kết quả và kiểm tra demo mới nhất ở [báo cáo G06](../specs/srs-completion-phases/reports/package-G06-result.md#free-demo-closeout-20261111).

Checkpoint 10/11/2026, Asia/Bangkok. D9-03/D9-07 đã đóng theo [review nghiệm thu](../specs/srs-completion-phases/reports/package-G06-result.md#acceptance-review-10112026). D9-01/04/09/10 còn các điều kiện nghiệm thu SRS.

| Điều kiện nghiệm thu | Có sẵn và có thể tận dụng | Cần chốt trước bước phụ thuộc |
|---|---|---|
| Tải và so một/hai backend | PC hiện có; hai API giới hạn 2CPU/4GiB; runner synthetic, load100VU10min và stress đã kiểm | Chốt [protocol DEC-11](../specs/srs-completion-phases/dec-11-measurement-proposal.md), đặc biệt cách đánh giá scaling; chỉ định target đáp ứng tài nguyên và yêu cầu production trong PDF. Không tải Render Free công khai. |
| Hiệu năng frontend — đã nghiệm thuD9-03 | Productiondf0: Lighthouse7×3/42assertions đạt;18acceptedmetrics/6lượt tương tác thật trênbrowser trong ngưỡng;58assets20pagesbudget/SHA | Phạm vi route/window/artifact đã ghi rõ; không cần chờRUM hoặc phát hành mọi dirtysource. Đổi bản liên quan thì kiểm lại. Không nhận mẫu điều khiển làorganicRUM. |
| BackupDB/durability — đã nghiệm thuD9-07 | Task03UTC/30ngày bật/actionthực chạy0;30tables2044rows/5migrations restore/hash match,301mediarestore/SHAbinding, WAL/tokencontinuity; [runbook](neon-backup-windows.md) | Máy cần bật/userđăng nhập/Docker/mạng hoạt động. KhoDB Windows độc lập vớiNeon; không claimRPO/RTO mới hoặc cứu dữ liệu khi mất mọi ổ/bản sao. PDF không buộc khoảnhngoàiPC/Drive; không tiếp tục ảnh/Drive. |
| Máy chủ ảnh an toàn, bốn node | 301ảnh hiện có, persistent volume, gateway; lab4node/quorum/heal đã kiểm | Bản MinIO server được vá và topology production4+node có storage/persistence, đường truy cập ổn định. Không mặc định phải mua bốn máy vật lý; cần thiết kế theo tài nguyên thực tế và ghi rõ các node có chung điểm lỗi nào. Bốn container thử trên PC không tự trở thành bằng chứng production. |

## Sao lưu ảnh/Drive đã dừng theo người dùng

Người dùng yêu cầu bỏ phần sao lưu ảnh/Drive. Không tải thêm, tải xuống/restore cloud, bật lịch hoặc retention; không tự tiếp tục theo quyền cũ. Một upload bản mã hóa đã hoàn tất trước khi yêu cầu dừng, chưa có clouddownload/restore. Không xóa bản đã tạo hoặc khóa; không yêu cầu đăng nhập lại. Bản nháp automation đã lưu trong tmp, không cài lịch Windows.

PDF trang43 NFR-REL-003 không bắt buộc Drive hoặc upload toàn bộ ảnh lên Drive. Ngày10/11 đã đủ lịchDB03UTC/30ngày/action0, NeonWAL/rowhash/tokencontinuity, restore vàrunbook nênD9-07 đóng; không miễn trừ các yêu cầuPDF. Restore301ảnh/301SHA trước được đối chiếu với301object hiện tại:300metadata không đổi,1đổiLastModified đã đọc lạiSHA khớp; không backupảnh mới. Điều kiện máy/user/Docker phải hoạt động vẫn được công bố; không tự bổ sung kịch bản mất mọi ổ/bản sao thành gate mới.

## Giới hạn MinIO hiện tại

Image công cụ `mc` đã vá không thay image server. Các finding High/Critical của server vẫn mở theo [hồ sơ công cụ/storage](minio-backup-tool.md). Guard của gateway chỉ giảm một đường tấn công, không thay gate vá server.

AIStor Free hiện chỉ hỗ trợ một node. Bản server đã vá có thể được đánh giá riêng cho D9-04. Nghiệm thu D9-10 cần bản được hỗ trợ trên topology production từ bốn node. [Tài liệu chính thức](https://docs.min.io/aistor/operations/licenses/#minio-aistor-free-community-use).

## Trạng thái phát hành

Frontend Vercel đã phát hànhdf0dbb9 bổ sung sửaCLS/search và accessibility trên base đang chạy; đúng deployment đã kiểm stage/primary/CDN và18controlledCWV. Không phát hành toàn snapshot root có thay đổi của nhiều gói. Backend Render vẫn41f619b, Neon vẫn5migration nghiệp vụ; snapshot G06 local có14migration. Việc phát hành snapshot đầy đủ cần đối chiếu source/dependency, rehearsal và backup đúng artifact trước áp; không đưa khác biệt này thành lỗi dữ liệu đã xác nhận hoặc tự reset Neon.
