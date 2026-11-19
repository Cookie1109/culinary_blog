# Cloudinary — đã hủy phương án

Người dùng ngày 26/10/2026 rút lại quyết định Cloudinary và yêu cầu giữ nguyên MinIO. Adapter, SDK, CLI copy/probe, test và cấu hình Cloudinary đã được gỡ; không dùng các lệnh storage-probe/storage-migrate hoặc nhập credentials Cloudinary theo tài liệu cũ.

Storage hiện hành là MinIO. Cấu hình backend dùng ObjectStorage__Endpoint, ObjectStorage__AccessKey, ObjectStorage__SecretKey, ObjectStorage__BucketName; Docker local giữ cấu hình/volume hiện có. backend/.env dành cho cấu hình Render vẫn thiếu endpoint và credentials MinIO cloud thật cùng PostgreSQL/Redis/hostname/release metadata; không dùng thông số local làm dịch vụ cloud.

Nguồn quyết định và bằng chứng cũ được giữ để truy vết tại [yêu cầu đã rút](../specs/srs-completion-phases/approvals/2026-10-26-cloudinary-storage.md) và checkpoint cuối [report G04](../specs/srs-completion-phases/reports/package-G04-result.md). Không có dữ liệu MinIO người dùng nào đã chuyển sang Cloudinary; không cần migration ngược. Xem [README](../../README.md) để chạy hệ thống MinIO hiện hành.
