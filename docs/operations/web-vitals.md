# Thu CWV/INP cho D9-03

Bộ thu mặc định tắt. Đây là chuẩn bị đo, không phải bằng chứng production đã đạt INP. Không cần tài khoản dịch vụ analytics mới.

## Deployment đang thu từ05/11/2026

Frontend chính `https://culinary-blog.vercel.app` đã bật bộ thu tại deployment `dpl_HCgPP4wkGH9ybFHAi6xWgF2e1b96`, source/build label `b4f3376e8001c283faf1773c87cf0e4ccc3b06e1`. Backend Render giữ41f619b. Đây là bản CWV tối thiểu dựa trên frontend đang chạy, khác snapshot G06 đầy đủ trong root. [Identity và gates](../specs/srs-completion-phases/reports/package-G06-result.md#checkpoint-05112026-closure).

CLI Vercel đã được người dùng đăng nhập. Có thể xuất log metric theo deployment vào thư mục private có ACL:

```powershell
vercel logs --deployment dpl_HCgPP4wkGH9ybFHAi6xWgF2e1b96 --query web-vital --since 30m --limit 1000 --json
```

Kết quả CLI65.11 là JSONL envelope `type=log`, metric nằm trong từng dòng JSON của `data.message`. Chỉ trích metric có `event=web-vital`, đúng deployment/build/window và schema đã định nghĩa; bỏ envelope/requestId/domain/trace/header trước khi chia sẻ. Nếu summary còn cursor thì tiếp tục paging, không báo đủ mẫu từ1000dòng bị cắt. Retention/quota của provider phải được kiểm theo plan, không coi stdout là nơi lưu vĩnh viễn.

Loại `id=g06-staged-routing-control` và toàn cửa sổ kiểm tự động05/11 `05:26:18–05:26:59.999Z` khỏi RUM. Đây là bốn lượt login có điều khiển, không phải người dùng thật. Provider có thể ghi beacon pagehide sau khi browser observer đã kết thúc; cần đối chiếu log thay vì chỉ đếm request thấy trong Playwright. Giữ exit1 của observer cũ trong report. Người dùng cần mở document mới sau deploy và đổi tab/đóng document sau tương tác; tab cũ có thể vẫn chạy JavaScript của deployment trước.

## Cấu hình sau khi được phép triển khai

- Đặt `WEB_VITALS_ENABLED=true` **khi build và khi chạy** frontend. Đổi cờ cần build/deploy lại để các trang prerender nhất quán. Docker hỗ trợ `--build-arg WEB_VITALS_ENABLED=true`; runtime phải được inject cờ tương ứng qua cấu hình môi trường được duyệt. Không tự thay Environment cloud từ tài liệu này.
- Đặt `WEB_VITALS_BUILD_ID` thành định danh artifact/deployment công khai đã kiểm. Nếu bỏ trống, receiver dùng `VERCEL_GIT_COMMIT_SHA`, sau đó `RELEASE_COMMIT_SHA`; các fallback chỉ định danh source, cần đối chiếu deployment/config riêng trước nghiệm thu. Không điền secret vào build ID. Không có ID hợp lệ thì receiver503.
- `SITE_URL` phải là origin HTTPS chính xác của deployment cần đo. Preview phải có cấu hình origin riêng; không mở CORS wildcard để thu các preview khác. Cờ tắt thì receiver404, không ghi metric.

## Payload và nơi lưu

Client dùng các hàm public `onLCP/onCLS/onINP` của Google `web-vitals`4.2.4 để chỉ tải ba metric cần đo, gửi tới POST cùng origin `/api/web-vitals`. Dùng sendBeacon với JSON text để gửi cả lúc pagehide. Browser thiếu API hoặc queue đầy có thể mất sample; không tạo sample giả hoặc chặn thao tác người dùng. Không gửi form values, token, email, URL/query, user agent nguyên văn, DOM targets hoặc performance entries.

Payload chỉ có `name/id/value/route/device/navigationType`: route thuộc danh sách pattern cố định, slug/ID bị che; device là mobile/desktop suy từ browser UA, không phải census thiết bị. `route` là **route vào document**, không gán INP cuối visit cho route soft-navigation cuối cùng. Metric ID do Web Vitals sinh để khử trùng metric trong visit, không phải định danh user lâu dài.

Receiver chấp nhận application/json hoặc text/plain cho JSON text của beacon, kiểm origin/schema và giới hạn2048bytes trên stream, kể cả khi Content-Length thiếu/sai. Server tự thêm `build/receivedAt`, ghi mỗi record JSON vào stdout với `event=web-vital`, trả204/no-store. Không có DB hay external collector mới. Provider/container log retention và quyền export cần được xác nhận khi triển khai; stdout local không chứng minh đã có lưu trữ vận hành production. Dữ liệu từ một public receiver vẫn cần review chất lượng/mức đại diện trước nghiệm thu.

## Tổng hợp có cửa sổ mẫu

Export **chỉ các dòng metric JSON** từ log của deployment được phép đọc vào file riêng, không xuất toàn bộ request/auth logs. Ví dụ log local:

```powershell
rg '^\{"event":"web-vital"' next.log > metrics.jsonl
node frontend/scripts/summarize-web-vitals.mjs metrics.jsonl BUILD_ID 2026-10-31T00:00:00Z 2026-11-01T00:00:00Z > cwv-summary.json
```

Thay window bằng thời gian thực của đợt đo, không dùng ngày ví dụ làm evidence. Script lọc đúng build/window, giữ record mới nhất theo name/id, tách route/device/metric, tính nearest-rank p75 và báo số mẫu/missing metrics. Ngưỡng vẫn LCP2500ms, CLS0,1, INP200ms. `withinThreshold` là mô tả cohort đã nhận, **không tự kết luận D9-03 đạt**, không thêm một số ngày/số mẫu tối thiểu thành ngoại lệ SRS. Missing INP phải giữ thiếu, không lấy TBT thay.

`frontend/scripts/check-web-vitals.mjs` kiểm đường browser→beacon→receiver→log→summary trên loopback, cả desktop/mobile. Đây là kiểm pipeline local với visits được điều khiển, tách khỏi RUM production và Lighthouse. Khi source thay đổi, phải chạy lại build/bundle/browser/Lighthouse tương ứng; không dùng build cũ để chứng nhận code mới.

Trạng thái/gates mới nhất: [report G06](../specs/srs-completion-phases/reports/package-G06-result.md).
