# ADR-009: Transactional Outbox cho background work

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Operations
- Liên quan: GAP-023

## Bối cảnh

DB commit và enqueue Hangfire/upload MinIO là các hệ thống riêng. Nếu process crash giữa hai thao tác, business data có thể tồn tại nhưng job bị mất hoặc object trở thành orphan.

## Quyết định

- Dùng Transactional Outbox cho domain events cần reliable dispatch: welcome email, image-resize metadata, cache/revalidation event và sitemap signal.
- Business data và OutboxMessage được commit cùng PostgreSQL transaction.
- Dispatcher claim message bằng lock an toàn, enqueue/execute handler idempotent, sau đó đánh dấu processed.
- Handler có idempotency key là OutboxMessage ID/business event ID.
- Object upload vẫn xảy ra trước DB metadata commit; nếu DB fail, ghi/trigger compensation cleanup. Khi metadata commit, outbox chịu trách nhiệm resize.
- Object delete/purge dùng outbox/job idempotent; object không tồn tại được xem là thành công.
- Failed messages có retry/backoff, attempt/error metadata, alert và manual replay runbook.

## Hệ quả

- Thêm table, dispatcher và vận hành nhưng đóng khoảng trống dual-write quan trọng.
- Eventual consistency phải được UI thể hiện, ví dụ thumbnail pending.

## Tiêu chí xác minh

- Crash sau commit trước dispatch vẫn xử lý event khi service trở lại.
- Replay message không gửi email/tạo metadata/xóa object sai nhiều lần.
