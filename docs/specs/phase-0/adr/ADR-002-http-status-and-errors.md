# ADR-002: HTTP status và error contract

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Frontend Lead, QA Lead
- Liên quan: GAP-002, GAP-003, GAP-021

## Bối cảnh

SRS dùng lẫn 400/422 cho validation, 409/422 cho concurrency và hai pagination envelopes. Điều này làm OpenAPI, frontend và tests không ổn định.

## Quyết định

- Mọi lỗi dùng `application/problem+json` theo Problem Details.
- Field/syntax/request validation, thiếu `If-Match` và domain precondition dùng `400 Bad Request`.
- Sai/thiếu authentication dùng 401; có identity nhưng thiếu quyền dùng 403.
- Không tìm thấy hoặc bị soft-delete dùng 404.
- Duplicate, invalid state transition và stale concurrency dùng 409.
- Rate limit dùng 429 với `Retry-After`; dependency tạm unavailable dùng 503.
- `problem.type` chứa application error code ổn định; `traceId` và `correlationId` luôn có.
- List response dùng `data` và `meta`; resource response dùng `data`.

## Hệ quả

- Cần map exception/domain result tập trung.
- Frontend xử lý theo error code, không parse message.
- Quyết định này giải mâu thuẫn giữa Chương 3, Chương 8 và Phụ lục A-B; OpenAPI/test phải dùng duy nhất mapping đã chốt.

## Tiêu chí xác minh

- Contract tests cho mọi status/error code.
- Không trả stack trace/internal exception ở production.
- Correlation ID khớp response và logs.
