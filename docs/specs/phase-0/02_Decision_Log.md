# Decision Log - GAP-001 đến GAP-033

> Trạng thái: Accepted for implementation  
> Ngày chốt: 11/09/2026  
> Giải thích đầy đủ về vị trí và lý do: [00_SRS_Conformance_and_Resolution.md](./00_SRS_Conformance_and_Resolution.md)

| GAP | Quyết định baseline | Owner thực thi | Tài liệu chi tiết | Trạng thái |
|---|---|---|---|---|
| GAP-001 | Dùng 34 FR; “27” là lỗi đếm | PO/QA | Product Baseline | Accepted |
| GAP-002 | Validation/domain precondition dùng 400 | TL/FE/QA | ADR-002 | Accepted |
| GAP-003 | Stale concurrency dùng 409; thiếu If-Match dùng 400 | TL/FE/QA | ADR-002/003 | Accepted |
| GAP-004 | Register dùng `displayName,email,password`; trả phiên auto-login | PO/TL/FE | Data Dictionary/OpenAPI | Accepted |
| GAP-005 | Auth.js sở hữu code+PKCE/callback; backend verify ID token | TL/FE | ADR-005 | Accepted |
| GAP-006 | Refresh token 512-bit, SHA-256 hash, FamilyId và rotation | TL | ADR-004 | Accepted |
| GAP-007 | Refresh/logout nhận token trong body; logout cho phép access token hết hạn | TL/FE | ADR-004/OpenAPI | Accepted |
| GAP-008 | Recipe soft delete; không purge vật lý trong v1 | PO/TL | ADR-001 | Accepted |
| GAP-009 | Category soft delete khi không còn Recipe liên quan | PO/TL | ADR-001 | Accepted |
| GAP-010 | Publish cần ít nhất 1 ingredient và 1 step; không bắt buộc ảnh | PO/TL/QA | Product Baseline | Accepted |
| GAP-011 | Bổ sung `/unarchive`, Archived chuyển về Draft | PO/TL/FE | OpenAPI | Accepted |
| GAP-012 | PATCH image metadata duy nhất để đổi alt/primary/order | TL/FE | OpenAPI | Accepted |
| GAP-013 | Redis-backed cache; TTL theo NFR-PERF-003 | TL | ADR-007 | Accepted |
| GAP-014 | Public chỉ Published; private dùng `/me/recipes` | TL/FE/QA | ADR-007/OpenAPI | Accepted |
| GAP-015 | Application-managed `Version bigint` làm ETag | TL | ADR-003 | Accepted |
| GAP-016 | `simple + unaccent`, GIN; `pg_trgm` fallback | TL | ADR-008 | Accepted |
| GAP-017 | Step dùng `title`, `description`, `timerMinutes`; server quản lý số thứ tự | PO/TL/FE | Data Dictionary/OpenAPI | Accepted |
| GAP-018 | Ingredient/image dùng `orderIndex` | TL/FE | Data Dictionary | Accepted |
| GAP-019 | Category name tối đa 100; cookTime cho phép 0 | PO/TL/QA | Data Dictionary | Accepted |
| GAP-020 | Profile sửa `displayName`, `avatarUrl`, `bio` | PO/TL/FE | Data Dictionary/OpenAPI | Accepted |
| GAP-021 | Response dùng resource envelope `data`; list dùng `data/meta` | TL/FE/QA | OpenAPI/Error Catalog | Accepted |
| GAP-022 | Bucket private; public/private delivery theo trạng thái | TL/Ops | ADR-006 | Accepted |
| GAP-023 | Hangfire PostgreSQL + Transactional Outbox cho reliable dispatch | TL/Ops | ADR-009 | Accepted |
| GAP-024 | Browser minimum dùng bảng 5.4: Chrome 112, Firefox 113, Edge 112, Safari 16 | PO/FE/QA | Test Strategy | Accepted |
| GAP-025 | .NET 10, Next.js 15, Node 20 LTS; pin exact patch/image digest | TL/FE/Ops | Project Plan | Accepted |
| GAP-026 | FluentValidation chỉ ở Application; Domain chỉ dùng .NET BCL | TL | Project Plan/tests | Accepted |
| GAP-027 | Dùng 7 NFR-SEC; số 6 là lỗi đếm | TL/QA | Conformance/Test Strategy | Accepted |
| GAP-028 | RecipeImage là entity/table riêng, quan hệ 1:N | TL | Data Dictionary | Accepted |
| GAP-029 | Hợp nhất toàn bộ filter Recipe được PDF nêu | TL/FE/QA | OpenAPI | Accepted |
| GAP-030 | Ingredient name dài 1-200 | PO/TL/QA | Data Dictionary/OpenAPI | Accepted |
| GAP-031 | Recipe slug tự thêm suffix; 409 chỉ cho race chưa resolve | TL | Data Dictionary/OpenAPI | Accepted |
| GAP-032 | ApplicationUser dùng Identity string key, audit fields riêng | TL | Data Dictionary | Accepted |
| GAP-033 | Không xuất rating giả trong JSON-LD v1 | PO/FE/QA | Route/Acceptance | Accepted |

## Blocking status

Không còn GAP chưa có quyết định. Việc triển khai chỉ được phép dùng các contract đã chốt trong OpenAPI, Data Dictionary và Error Catalog. Thay đổi tiếp theo phải qua Change Request.

## Change record

| Ngày | GAP/ADR | Thay đổi | Căn cứ |
|---|---|---|---|
| 09/09/2026 | GAP-001-026 | Tạo baseline đề xuất lần đầu | Phân tích SRS |
| 11/09/2026 | GAP-001-033 | Chốt toàn bộ quyết định, bổ sung 7 GAP bị bỏ sót và đồng bộ tài liệu | Chỉ đạo chuẩn hóa theo SRS nguồn |
