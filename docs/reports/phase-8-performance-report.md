# Báo cáo Performance Phase 8

> Ngày chạy: 04/10/2026 (Asia/Bangkok)  
> Phạm vi: P8-05 đến P8-09  
> Kết quả: **Local Accepted**

## Profile đo

- Release build chạy qua Docker Compose và Nginx tại `http://localhost:8080`.
- API: giới hạn 2 CPU/4 GiB; PostgreSQL: 2 CPU/4 GiB; Redis: 1 CPU/1 GiB.
- Host Windows có 12 logical processors.
- Dataset synthetic: 50 recipe, 5 author, 20 category; 17 recipe ở trạng thái Published; không dùng PII production.
- Load generator và stack cùng localhost; không giả lập WAN latency.
- Global API rate limit được nới riêng trong `performance/compose.performance.yaml` để đo năng lực API/cache. Cấu hình mặc định không đổi; kiểm rate limit thuộc P8-13.

## K6 smoke, load và stress

| Profile | VU tối đa | Iteration | Lỗi | Warm p50 | Warm p95 | Warm p99 | Cold p50 | Cold p95 | Cold p99 | Cache hit |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Smoke | 5 | 150 | 0% | 2.83 ms | 5.10 ms | 5.47 ms | 6.25 ms | 7.42 ms | 7.63 ms | 90.00% |
| Load | 100 | 29,900 | 0% | 2.63 ms | 7.89 ms | 18.90 ms | 5.95 ms | 12.78 ms | 24.44 ms | 89.93% |
| Stress ramp | 100 | 23,039 | 0% | 2.33 ms | 4.80 ms | 8.53 ms | 5.51 ms | 8.62 ms | 12.46 ms | 89.95% |

Các profile đều đạt budget warm-cache p50 ≤150 ms, API p95 ≤500 ms, p99 ≤1 s, error rate <1% và cache hit rate ≥80%. Hit rate được tính từ delta counter ứng dụng `cache_hits_total` và `cache_misses_total` trên Prometheus, không suy đoán từ response time.

## Release frontend

Production build và `npm run budget:check` đạt trên toàn bộ route. Route lớn nhất là 175.8 KB First Load JS gzip, dưới budget 200 KB.

Lighthouse CI chạy 3 lượt cho mỗi route `/`, `/recipes`, `/categories`, `/search`, một recipe detail và một category detail lấy từ dataset thật:

| Route | Performance median | LCP median | CLS median | TBT median | Script transfer median | Meta description |
|---|---:|---:|---:|---:|---:|---:|
| `/` | 99 | 864 ms | 0 | 0 ms | 190.9 KB | 1 |
| `/recipes` | 99 | 909 ms | 0 | 0 ms | 193.8 KB | 1 |
| `/categories` | 99 | 867 ms | 0 | 0 ms | 185.0 KB | 1 |
| `/search` | 99 | 862 ms | 0 | 0 ms | 194.3 KB | 1 |
| Recipe detail | 99 | 872 ms | 0 | 0 ms | 190.9 KB | 1 |
| Category detail | 99 | 841 ms | 0 | 0 ms | 190.9 KB | 1 |

Tất cả route đạt LCP ≤2.5 s, CLS ≤0.1, TBT ≤200 ms, script transfer ≤200 KB và audit bắt buộc `meta-description = 1`. HTML release cũng được kiểm tra trực tiếp: thẻ description nằm trước `</head>` trên đủ 6 route. INP là field metric nên tiếp tục được theo dõi bằng RUM ở staging/production; TBT là lab proxy trong Lighthouse.

## Kết luận P8-09

Không có budget nào thất bại và không có bottleneck đo được cần thay đổi query, index hoặc cache. Theo nguyên tắc P8-09, không thực hiện tối ưu speculative. Cần chạy lại cùng harness trên immutable staging artifact trước go/no-go vì kết quả localhost không bao gồm WAN, TLS và tải hạ tầng dùng chung.
