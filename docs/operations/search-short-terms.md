# Tìm kiếm món ăn tiếng Việt và trải nghiệm sử dụng

Cập nhật27/10/2026 theo yêu cầu tối ưu tìm kiếm trên Vercel. Đây là bản mở rộng
của sửa từ ngắn trước đó, giữ PostgreSQL FTS/unaccent/pg_trgm, GIN, ts_rank,
Published-only và các bộ lọc. Không đổi schema hoặc chạy migration/seed.

## Thứ tự khớp

1. Chuẩn hóa Unicode NFKC, khoảng trắng và hoa/thường; giữ chữ gốc để hiển thị.
2. Khớp đủ mọi từ nguyên vẹn trong tiêu đề/mô tả. Nếu nhập dấu tiếng Việt và có
   tập khớp đúng dấu trong các bộ lọc hiện tại, ưu tiên tập đó (cá khác cà, bò khác bơ).
3. Nếu không có, tìm tiền tố. Từ ngắn đã được nhận diện như ga không mở rộng sang
   gạo/gấc/gan, kể cả trong truy vấn nhiều từ. Vẫn hỗ trợ ph→phở và ví dụ prefix
   pho:* & bo:* của FR-SRCH-001 trang36–37 PDF.
4. Chỉ khi không có khớp tốt hơn mới tìm tên gần đúng. Từng từ đều phải khớp;
   dung sai gõ sai chỉ áp dụng từ có ít nhất4 ký tự, qua pg_trgm word similarity.
5. Mặc định tìm từ khóa theo độ phù hợp ts_rank; duyệt không từ khóa theo mới nhất.
   Người dùng vẫn có thể chọn mới nhất, nấu nhanh nhất hoặc tên A–Z.

Không trộn các món chỉ giống một phần vào tập đã khớp đủ. Phở bò không bị thêm
bò kho chỉ vì có chữ bò. Món có từ khóa ở mô tả vẫn hợp lệ: xôi xéo có mỡ gà,
súp cua có gà xé. Cache tìm kiếm dùng namespace v2, TTL/tag hiện có được giữ,
không flush Redis hoặc thay dữ liệu.

## Giao diện

- Gợi ý đúng phạm vi: tìm tên món và mô tả, hỗ trợ nhập không dấu.
- Mặc định “Phù hợp nhất” khi có từ khóa, reset về trang1 khi đổi từ khóa/bộ lọc.
- Hiển thị tên và mô tả dạng văn bản thường, không tô từ khóa hoặc thêm nhãn giải thích.
  Đoạn mô tả dài vẫn lấy phần quanh từ khóa, không dùng HTML thô.
- Xóa từ khóa riêng hoặc bỏ từng filter; xóa từ khóa giữ filter khác.
- Không gửi yêu cầu khi đang ghép dấu IME; báo lỗi từ khóa1 ký tự/quá100/chỉ ký hiệu.
- Giữ lịch sử Back/Forward, tránh cuộn trang khi đổi filter; về vùng kết quả khi
  tìm hoặc chuyển trang. Chặn submit trùng và thông báo trạng thái tải.
- Phân biệt không có kết quả với lỗi kết nối. Retry giữ URL/từ khóa/filter;
  URL quá số trang có nút về trang đầu. Phân trang gọn với Trước/Sau và dấu lược.
- Bộ lọc mobile có nút xem kết quả và vùng chạm rộng hơn; giữ phong cách hiện tại.

## Kiểm chứng và giới hạn

Dùng PostgreSQL16/Redis7 Testcontainers cho đúng SQL, không ghi Neon/Upstash.
Có regression cho ga/gà/GÀ/Unicode tổ hợp, gà nướng/phở bò, cá/cà, bò/bơ,
prefix/typo/all-word AND, Published privacy/rank/filter/paging. Frontend kiểm
DOM tương tác, SSR validation/error, lịch sử và IME; build/lint/typecheck/audit
và các gate CI giữ nguyên. Bằng chứng cuối, commit/CI/runtime ghi ở report vận hành.

Các con số8/32 của phiên bản sửa từ ngắn là checkpoint lịch sử. Không dùng deploy
hoặc cải tiến này để đóng toàn G04: conflict400/422, Category Name50/100 và
Google sitemap ping retired còn mở; source/migration G04 khác chưa rollout.
