# ADR-010 — Supported Node.js runtime

- Status: Accepted
- Date: 04/10/2026
- Decision owners: Technical Lead / Frontend / Operations
- Amends: GAP-025

## Context

Baseline ban đầu pin Node.js 20.19.5. Node 20 đã end-of-life ngày 24/03/2026 và không còn nhận bản vá bảo mật. Phase 8 container scan cũng chứng minh runtime image cũ còn Critical/High findings trong tooling đi kèm.

## Decision

- Chuyển build/runtime/CI sang Node.js 22.23.3 LTS và pin image digest.
- Runtime image xóa npm/npx vì production chỉ chạy `node server.js`; package manager chỉ tồn tại ở build stage.
- Khi Node 22 hết security support, phải nâng sang LTS còn hỗ trợ qua một security amendment tương tự; không giữ runtime EOL chỉ để bảo toàn baseline lịch sử.
- Lockfile và release build phải được kiểm chứng lại sau mỗi lần đổi runtime major/patch.

## Consequences

- Giữ Next.js 15 và contract ứng dụng hiện tại, chỉ thay runtime được hỗ trợ.
- Giảm attack surface của production image và loại các CVE từ npm CLI không cần thiết.
- CI, tài liệu prerequisite và container digest phải thay đổi đồng bộ.
