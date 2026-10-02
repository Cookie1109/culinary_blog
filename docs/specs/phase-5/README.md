# Phase 5 — Publishing, discovery và cache

**Trạng thái:** **Accepted / Closed**  
**Baseline:** [Phase 0](../phase-0/README.md)  
**Ngày nghiệm thu:** 03/10/2026

## Phạm vi và đầu ra bàn giao

| Nhóm | Kết quả bàn giao | Bằng chứng chính |
|---|---|---|
| P5-01–06 — lifecycle | Publish kiểm tra toàn aggregate; giữ `PublishedAt` và slug lần đầu; unpublish/archive/unarchive; public chỉ trả Published; audit metrics create/publish/unpublish | `Recipe`, `ContentService`, `RecipeCompositionApiTests` |
| P5-07–14 — discovery | `unaccent` + `pg_trgm`, search vector/trigger, list/search Published-only, filter AND, sort allowlist, pagination `data/meta`, indexes và cảnh báo query >100 ms | migrations `RecipeSearchVector`, `RecipeDiscoveryIndexes`; `ContentService`; `RecipeDiscoveryApiTests` |
| P5-15–20 — cache | TTL đúng policy; key versioned chứa toàn bộ tham số chuẩn hóa; private endpoint không dùng public cache; invalidation theo tag; metrics hit/miss/latency/failure; Redis down fallback DB | `PublicCacheKeys`, `RedisApplicationCache`, `PublicCacheInvalidationFilter`; integration tests |
| P5-21–24 — sitemap/revalidation | Job 02:00 UTC, retry 2 lần, chỉ public content, giữ artifact cũ khi lỗi; callback revalidation có secret/debounce; không dùng sitemap ping đã bị Google ngừng hỗ trợ | `SitemapGenerationJob`, `PublicContentRefreshScheduler`; backend/frontend tests |
| P5-25–29 — QA/performance | Test tiếng Việt và input biên; contract filter/sort/page; isolation Guest/Author/Admin; hit/miss/invalidation/outage; benchmark 10.000 recipes và `EXPLAIN ANALYZE` | `RecipeDiscoveryApiTests`, `RecipeDiscoveryPerformanceTests`, `RecipeCompositionApiTests`, `RedisApplicationCacheTests` |

Các đầu ra yêu cầu của phase đã có đủ:

- Recipe lifecycle hoàn chỉnh.
- Public API listing/detail/category/search ổn định.
- Redis cache và invalidation không dùng chung dữ liệu private.
- Sitemap và frontend revalidation flow.

## Kết quả QA

| Gate | Kết quả ngày 03/10/2026 | Trạng thái |
|---|---|---|
| Search tiếng Việt | `pho` tìm được `Phở bò truyền thống`; prefix và multi-term có ranking; Draft không xuất hiện | Đạt |
| Search input biên | Chuỗi chỉ có ký tự đặc biệt và truy vấn không có kết quả trả page rỗng, không lỗi SQL | Đạt |
| Filter/sort/pagination | Các filter áp dụng theo AND; page/meta đúng; sort ngoài allowlist trả `400` | Đạt |
| Cache isolation | Guest, Author và Admin nhận cùng tập Published từ public cache; Draft chỉ xuất hiện ở `/me` hoặc `/admin` đúng quyền | Đạt |
| Cache invalidation | Detail/list/search/category được prime cache rồi unpublish; mọi public read đều loại recipe ngay sau mutation | Đạt |
| Redis outage | Dừng Redis container trong lúc test; `GET /api/v1/recipes` vẫn trả `200` từ PostgreSQL | Đạt |
| Lifecycle | Publish thiếu composition trả `400 RECIPE_PUBLISH_INCOMPLETE`; publish/unpublish/archive/unarchive và metadata ổn định | Đạt |
| Sitemap/revalidation | Sitemap chỉ chứa Published, giữ artifact trước khi upload lỗi, retry đúng 2; frontend callback/robots wiring pass | Đạt |
| Phase 5 acceptance suite | 13/13 integration tests pass trên PostgreSQL/Redis thật | Đạt |

## Báo cáo performance và execution plan

Profile chuẩn dùng để nghiệm thu:

- Docker Desktop 29.7.2, Linux kernel WSL2 6.6.87.2.
- Docker memory 7,68 GB; host có 12 logical CPU.
- PostgreSQL 16.10 Testcontainer; kết nối local loopback, không có network WAN.
- Benchmark bổ sung 10.000 Published recipes vào seed nền, đã chạy `ANALYZE` trước đo.
- Mỗi query warm-up 1 lần, sau đó đo 5 lần bằng `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)`; budget p95 ≤100 ms.

Kết quả đại diện:

| Query | p95 | Plan review | Kết quả |
|---|---:|---|---|
| Listing theo status/category/difficulty, sort PublishedAt, limit 50 | 0,210 ms | `Index Scan` qua `IX_Recipes_Status_CategoryId_Difficulty_PublishedAt` | Đạt |
| Search FTS + trigram, ranking, limit 50 | 19,343 ms | PostgreSQL chọn `Seq Scan` cho dataset 10k vì cost thấp hơn bitmap index; GIN FTS/trigram tồn tại và predicate trigram dùng toán tử `%` tương thích index | Đạt |

Public list/search không có N+1: page query và các lookup category/count/author/role/ingredient/step/image đều chạy theo batch `IN/ANY` trên toàn page. Số query không tăng theo số recipe trong page. Cả hai query chính nằm dưới budget 100 ms; lựa chọn plan search cần được đo lại khi cardinality và phân bố dữ liệu production thay đổi.

## Exit gate

| Tiêu chí | Bằng chứng | Kết luận |
|---|---|---|
| Publish thiếu ingredient/step trả đúng lỗi; publish hợp lệ xuất hiện public | Domain + lifecycle integration test | Đạt |
| Unpublish/archive biến mất khỏi API/cache/search trong policy | Cache được prime trước mutation và kiểm tra lại detail/list/search/category | Đạt |
| `pho` tìm được `phở` | PostgreSQL integration test với `unaccent`/FTS thật | Đạt |
| Không lộ private content qua cache | Test Guest/Author/Admin và private endpoints | Đạt |
| Redis down không làm core read API crash | Test dừng Redis container, API vẫn `200`; multiplexer dùng `AbortOnConnectFail=false` và cache fail-fast | Đạt |
| Query không N+1 và đạt baseline latency | Batched mapping review + benchmark 10k/`EXPLAIN ANALYZE` | Đạt |

Không còn exception mở cho Exit gate Phase 5.

## Lệnh tái kiểm

```powershell
Set-Location backend
dotnet test tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj `
  --configuration Release `
  --filter "FullyQualifiedName~RecipeDiscoveryApiTests|FullyQualifiedName~RecipeDiscoveryPerformanceTests|FullyQualifiedName~RecipeCompositionApiTests|FullyQualifiedName~RedisApplicationCacheTests|FullyQualifiedName~SitemapGenerationJobTests"

Set-Location ../frontend
npm test
npm run lint
npm run typecheck
npm run build
```
