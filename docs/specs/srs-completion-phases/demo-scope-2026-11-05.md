# Phạm vi chuẩn bị demo G06/G07 — 05/11/2026

Người dùng yêu cầu “bỏ hết những phần đó đi” sau bảng các bước bổ sung có thể bỏ. Áp dụng cho những bước bổ sung dưới đây; PDF SRS và đính chính A/B/C tiếp tục là tiêu chuẩn. Đây là thay đổi kế hoạch chuẩn bị demo, không phải kết quả kiểm thử hoặc miễn trừ yêu cầu PDF.

| Phần bổ sung | Cách xử lý từ nay |
|---|---|
| Sao lưu toàn bộ ảnh lên Google Drive, download/restore cloud và lịch upload ảnh | Bỏ khỏi kế hoạch. Không tiếp tục OAuth/upload hoặc bật lịch. Giữ dữ liệu, bản sao và khóa đã tạo. |
| Tìm 100 người thật để kiểm thử tải | Bỏ. Dùng người dùng giả lập khi chạy phép đo đã được phép; yêu cầu ≥100 concurrent users và các ngưỡng PDF vẫn giữ. |
| Chờ cố định 28 ngày để thu CWV/INP | Bỏ. Không tự đặt thời gian chờ này. Vẫn cần số đo hợp lệ và nêu rõ môi trường/mẫu; thiếu bằng chứng thì ghi chưa nghiệm thu. |
| Tạo mới SBOM/provenance và nhiều hồ sơ phát hành riêng chỉ phục vụ demo | Bỏ. Dùng thông tin phiên bản/build và bằng chứng sẵn có. Không tắt gate hoặc xóa đầu ra của pipeline hiện hữu. |
| Điền bảng 74 mã với toàn bộ metadata chi tiết hoặc lặp nhiều báo cáo | Bỏ yêu cầu biểu mẫu chi tiết. Gộp thành một bản tổng hợp ngắn: phạm vi, phiên bản demo, kiểm thử đã chạy, lỗi và yêu cầu còn mở. Giữ bảng cũ làm tài liệu tham chiếu. |
| Biên bản UAT/chữ ký nghiệm thu riêng để chuẩn bị demo | Bỏ bước lập biên bản/chữ ký riêng. Vẫn kiểm thử năm luồng chính và UX trên đúng bản demo; không tự ký hoặc nhận đã được giảng viên chấp thuận. |

## Ưu tiên demo trên PC — 10/11/2026

Theo yêu cầu người dùng, ưu tiên chuẩn bị và trình diễn các luồng trên trình duyệt PC. PDF trang7 loại ứng dụng di động native iOS/Android khỏi phạm vi, nhưng trang42 NFR-USE-001 vẫn yêu cầu website responsive cho mobile320–767px, tablet768–1199px và desktop≥1200px, kiểm bằng Chrome DevTools và BrowserStack iOS/Android. Vì PDF có yêu cầu này, không bỏ kiểm tra responsive hoặc tự miễn trừ các phép kiểm chưa có bằng chứng. Không cần xây ứng dụng điện thoại riêng hay chuyển phần demo chính sang điện thoại. Điều chỉnh ưu tiên demo này không mở thực hiện phase28 hoặc gói khác.

## Phần bắt buộc vẫn giữ

- PDF trang40–41: ngưỡng hiệu năng API/cache, ≥100 concurrent users, scaling, CWV/INP và dung lượng JavaScript.
- PDF trang39–43: quan sát hệ thống, bảo mật/HTTPS, xử lý lỗi, pg_dump tự động mỗi ngày03:00 giữ30ngày, WAL, dữ liệu MinIO trên volume persistent và refresh token trong DB. Google Drive không phải kho bắt buộc trong PDF.
- PDF trang43: chất lượng/review code, coverage/kiểm thử, năm luồng đăng ký–đăng nhập–tạo công thức–xuất bản–tìm kiếm, README/setup, tài liệu API, ADR và CHANGELOG.
- PDF trang44: shared state/lock/worker, upstream nhiều API, MinIO distributed4+node cho production và CDN. Phần production thiếu môi trường hoặc bằng chứng vẫn chưa nghiệm thu.

Các task hỗn hợp D9-07/D10-02/03/04 được giản lược phần bổ sung, không tự đóng toàn task. D9-02/05/06 giữ trạng thái cũ; D9-01/03/04/07/09/10 còn mở theo [checkpoint G06](reports/package-G06-result.md#checkpoint-05112026-after-backup-skip). G07 chưa chạy; cập nhật phạm vi tài liệu không phải thực hiện phase37/38. Không thay đổi hệ thống, dữ liệu, PDF hoặc A/B/C trong lượt chỉnh kế hoạch này.

Khi báo cáo, phân biệt bản demo chức năng với nghiệm thu toàn bộ SRS. Chỉ dùng kết luận toàn bộ SRS khi các yêu cầu bắt buộc có đủ bằng chứng.
