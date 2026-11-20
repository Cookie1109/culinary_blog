# DEC-11 — Protocol đo đề xuất để duyệt

Ngày31/10/2026. **Chưa được phê duyệt, không thay decision register hoặc A/B/C.** Phục vụ D9-01/D9-09 trong G06. PDF giữ quyền ưu tiên; không đặt error tolerance, efficiency cutoff hoặc giảm các ngưỡng SRS.

## Target và dữ liệu

- Một artifact source/image có SHA/digest đã review. Single API giới hạn2vCPU/4GiB, pool100; ghi tài nguyên/region/version của DB/Redis/MinIO, worker count, TLS/network và tài nguyên load generator. Môi trường phải được chủ sở hữu cho phép chạy tải. Local diagnostics không thay production evidence theo SRS§4.1.
- Dataset/tài khoản synthetic riêng: mỗi VU một Author, run ID riêng; không đọc/ghi recipe của người dùng để thử mutation. Backup/fault/Lighthouse không chạy cùng benchmark. Định danh và đếm dữ liệu trước/sau, xóa toàn bộ chỉ trong stack thử được xác nhận là disposable.
- Rate overrides chỉ trong profile benchmark; lưu actual config. Quota gốc100/10/5 được kiểm riêng, không thay production policy để né giới hạn.

## Workload và trình tự đề xuất

1. Smoke5VU/30s: kiểm assertions, token, ETag, counters và resource identity; lỗi wiring phải sửa trước load.
2. Public-read profile: dùng `performance/k6/public-api.js` để đo warm/cold/cache counters. Profile hiện có là90%warm/10%cold,100VU/5phút; đây là lựa chọn script, **không phải mix SRS đã duyệt**. Giữ raw throughput/errors/cache baseline/final, không suy counters tích lũy thành hit rate của window.
3. Read/write journey: `performance/k6/recipe-journey.js`. Mỗi vòng gồm2public GET, tạo draft, cập nhật If-Match, đọc draft bằng owner và xóa draft If-Match; nghỉ1giây. Mỗi VU có token riêng. Tỷ lệ thực tế do6operation/vòng quyết định:3reads/3writes nếu vòng hoàn tất; ghi mọi vòng dở/lỗi, không coi tỷ lệ danh nghĩa là observed mix. Registration/login đầu mỗi VU/warm-up là control, tách khỏi latency trend nghiệp vụ. Chưa bao phủ publish/upload/job latency; không gọi đây là mọi endpoint.
4. Đề xuất warm-up3phút rồi steady100VU/10phút cho journey; stress50→100→150 theo stage đã lưu. Duration/mix/phạm vi endpoint này cần được chốt trước nghiệm thu. Runner journey hiện chỉ cho **loopback** và run ID `g06-<12hex>`; không có quyền chạy trên URL cloud từ tài liệu này.
5. Scaling: cùng artifact/dataset/mix/network, so1API và2API (mỗi instance2vCPU/4GiB) qua Nginx. Chạy cùng100VU để so latency, sau đó tải tăng tương ứng để đo capacity. Lặp3lượt/mỗi topology, chạy tuần tự, lưu từng lượt và biến thiên; không chỉ chọn lượt đẹp nhất. Công bố RPS2/RPS1 và RPS2/(2×RPS1). **Chưa đặt tolerance để kết luận “tuyến tính/không degradation”**; phần này vẫn cần quyết định DEC-11.

## Ngưỡng và kết luận

- Giữ warm cached GETp50≤150ms; APIp95≤500ms/p99≤1000ms; Redis steady-state hit≥80%; tối thiểu100concurrent users trên profile yêu cầu. Không dùng p95 từ lỗi HTTP nhanh để báo đạt.
- Mỗi endpoint/scenario phải có count/errors/status và latency, warm/cold riêng; lưu achieved VUs, throughput, dropped iterations, CPU/RAM/OOM/DBpool/Redis/job backlog. Missing samples/counters hoặc generator saturation là thiếu bằng chứng.
- Chưa có error budget được duyệt: mọi unexpected HTTP/business error đều phải ghi và điều tra; script giữ `rate==0`, không tự cho phép1%error.
- Không tính tỷ lệ cache từ journey thiếu Prometheus window; không trộn kết quả public-read và journey để tự tạo một weighted score. Chốt phạm vi/mix dùng cho cache≥80% trước khi nghiệm thu.
- Bảng verdict phải phân biệt: ngưỡng PDF đạt/thất bại theo phép đo; protocol/deployment/sample chưa đủ; và acceptance scaling còn chờ DEC-11. Lượt diagnostic xanh không đóng D9-01/09.

## Điều kiện thực thi ngoài local còn thiếu

Target/staging/production cụ thể và artifact được phép đưa lên; CPU/RAM/plan/quota đã xác nhận; quyền load/stress theo window và dataset; quyền deploy/migration nếu cần đưa G06 lên; cách đánh giá sai số scaling. Agent chuẩn bị kỹ thuật, người dùng chỉ cần phê duyệt phạm vi/nguồn lực sau khi xem kế hoạch phát hành, không gửi password/API key qua chat.

## Xuất kết quả không mang credential

JWT chỉ ở memory từng VU; setup chỉ trả category ID. Dùng `SUMMARY_PATH` với `handleSummary()` để ghi `runId/profile/metrics`, không dump toàn bộ data object hoặc response. Ví dụ trên lab loopback đã được chỉ định: `k6 run -e BASE_URL=http://127.0.0.1:PORT -e BENCHMARK_RUN_ID=g06-012345abcdef -e PROFILE=smoke -e SUMMARY_PATH=journey-metrics.json performance/k6/recipe-journey.js`. Thay PORT/runID bằng giá trị lab thực. Legacy `--summary-export` không dùng làm report chuẩn; regression còn kiểm cả legacy setup_data không có JWT. Profile dài cần xác nhận vòng đời token/refresh trong protocol, không tăng TTL production để che lỗi. [k6 custom summary chính thức](https://grafana.com/docs/k6/latest/results-output/end-of-test/custom-summary/).
