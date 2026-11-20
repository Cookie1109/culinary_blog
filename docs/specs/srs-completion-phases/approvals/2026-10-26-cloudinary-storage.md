# Yêu cầu đổi storage cloud sang Cloudinary — 26/10/2026

**Đã rút lại, không còn hiệu lực.** Cùng ngày, người dùng chỉ đạo: “Giữ nguyên MinIO đi. Ban nãy quyết định chuyển sang Cloudinary là một sai lầm”. MinIO tiếp tục là storage của dự án; không triển khai/chuyển dữ liệu sang Cloudinary. Nội dung bên dưới chỉ giữ lịch sử yêu cầu đã bị thay thế; xem checkpoint MinIO mới nhất trong report G04.

Người dùng yêu cầu trong chat: “Tôi muốn chuyển từ MinIO sang Cloudinary. Dự án hiện tại có chuyển được không? Nếu có, hãy triển khai”. Sau đó xác nhận dùng thông tin trong ảnh để điền file riêng: “Bạn điền giúp tôi đi, ảnh Key tôi gửi rồi đó”. Không chép credentials vào tài liệu này.

Phạm vi đã ủy quyền: triển khai adapter/config/test Cloudinary để thay MinIO cho deployment cloud, chuẩn bị chuyển dữ liệu giữ byte/key và quyền riêng tư. MinIO local/nguồn cũ giữ lại để không mất dữ liệu và hỗ trợ copy/rollback. Không có quyền xóa bucket, tự chuyển dữ liệu người dùng, tạo dịch vụ trả phí, commit/push hoặc tự nhận Render đã deploy.

PDF vẫn là baseline yêu cầu; lựa chọn provider Cloudinary là thay đổi do người dùng yêu cầu rõ, cần báo cáo riêng với bằng chứng provider mới. Không dùng kết quả MinIO làm bằng chứng Cloudinary thật, không sửa PDF, không thay đính chính A/B/C hoặc miễn trừ các gate khác. Yêu cầu tài nguyên production, PostgreSQL/Redis, persistence, release metadata và source conflicts của G04 còn nguyên trạng cần nghiệm thu riêng.

Xem [hướng dẫn vận hành](../../../operations/cloudinary-storage.md) và checkpoint cuối [G04](../reports/package-G04-result.md).
