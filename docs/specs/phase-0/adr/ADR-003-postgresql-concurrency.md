# ADR-003: Optimistic concurrency trên PostgreSQL

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owner: Technical Lead
- Liên quan: GAP-003, GAP-015

## Bối cảnh

SRS dùng `bytea [Timestamp]` như SQL Server rowversion, nhưng PostgreSQL không tự cung cấp semantics đó cho cột bytea.

## Quyết định

- Aggregate cần concurrency dùng `Version bigint NOT NULL DEFAULT 1`.
- Mỗi update thành công tăng `Version` trong cùng statement/transaction.
- Read trả `ETag: "{version}"` và body có thể chứa `version` phục vụ form state.
- Mutation yêu cầu `If-Match`; thiếu header trả 400 `PRECONDITION_REQUIRED`, stale value trả 409 `CONCURRENCY_CONFLICT`.
- Child mutation của Recipe kiểm tra/increment version Recipe để một aggregate có concurrency boundary thống nhất.

## Hệ quả

- Contract rõ, dễ test, không phụ thuộc system column `xmin`.
- Client phải giữ/refresh ETag sau mọi mutation.
- Cần xử lý atomic update và tránh tăng version ngoài ý muốn bởi read/maintenance job.

## Phương án không chọn

- PostgreSQL `xmin`: ít schema hơn nhưng là implementation detail và wraparound/serialization cần lưu ý.
- UUID token: hoạt động nhưng khó quan sát thứ tự version hơn.

## Tiêu chí xác minh

- Hai update cùng ETag chỉ một request thành công.
- Stale request không ghi đè và trả current ETag nếu policy cho phép.
