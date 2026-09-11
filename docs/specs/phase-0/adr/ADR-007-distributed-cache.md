# ADR-007: Distributed cache và public/private isolation

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owners: Technical Lead, Frontend Lead, QA Lead
- Liên quan: GAP-013, GAP-014

## Bối cảnh

SRS dùng lẫn IMemoryCache/Output Cache/Redis và cho public URL trả dữ liệu tùy optional identity, tạo nguy cơ rò private content.

## Quyết định

- Dùng Redis-backed cache cho shared API state; không dùng per-node IMemoryCache cho business response.
- `/recipes`, `/categories/{slug}`, `/recipes/search` chỉ Published và an toàn cho public cache.
- `/me/recipes` cùng preview Draft/Archived là private, mặc định không shared-cache.
- Key format: `cb:v1:{resource}:{normalized-parameters}`; không chứa raw secret/PII.
- TTL: category 30 phút, recipe detail 5 phút, recipe list 15 phút, search 1 phút.
- Mutation invalidate tag/key sau DB commit; TTL là safety net.
- Redis lỗi thì fallback DB và emit metric/log, không fail core read.
- Next.js ISR/public cache dùng cùng visibility boundary và được revalidate sau lifecycle mutation.

## Hệ quả

- Public/private API tách rõ, giảm cache poisoning/leak.
- Cần invalidation service, metrics và outage tests.

## Tiêu chí xác minh

- Guest không bao giờ nhận Draft từ cache đã warm bởi Author/Admin.
- Redis down vẫn đọc được DB trong performance degraded mode.
