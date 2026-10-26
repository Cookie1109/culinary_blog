# ADR-011 — Runtime release metadata theo môi trường triển khai

Ngày26/10/2026. Trạng thái: được người dùng duyệt **có điều kiện khớp PDF**, đã đối chiếu và triển khai local; chưa deploy. Không sửa hiệu lực các đính chính A/B/C hoặc đóng gate SRS nào.

## Căn cứ và phạm vi

SRS71 trang, SHA256 `8a82ccf3771918797f304117179c7a3b1086abfed8735ca42e9cd070f3461063`:

- Trang15, CONS-009: đóng gói Docker, Dockerfile multi-stage, Compose cho Development. Cùng trang cho phép PostgreSQL/Redis/MinIO dạng managed service hoặc VPS trong production.
- Trang41–42, NFR-SEC-005/007: HTTPS/CORS và secrets qua môi trường, không đưa vào Git.
- Trang43, NFR-MAINT-003: ADR và tài liệu phát hành.
- Trang52, §6.5: hệ thống containerized, Docker Compose Development/Production.

Không có quy định `CommitSha`, `ApiImageDigest`, `WebImageDigest` hoặc bắt buộc kiểm hai image digest lúc API startup trong PDF. Ba trường này là quy ước bổ sung của release tooling trong repository. Việc hỗ trợ metadata khác không loại yêu cầu Docker/Compose hoặc thay nghiệm thu production.

Người dùng đã duyệt: “Đồng ý với điều kiện phải khớp với PDF”, cho việc điều chỉnh metadata Render/Vercel; không duyệt deploy, seed hoặc migration. Backend Render và frontend Vercel là môi trường demo được người dùng chọn. Không gọi mô hình này là bằng chứng đạt §6.5 production Docker Compose, reverse proxy, SLA hoặc toàn bộ SRS.

## Quyết định

1. `Release:Provider` mặc định **Docker**. Giữ kiểm SHA40 ký tự và hai image digest đúng định dạng trong Production. Workflow `release-images`, release-gate/deploy scripts, Dockerfile/production Compose và immutable-artifact gates giữ nguyên.
2. Chỉ opt-in bằng `Release__Provider=Render`. Khi đó guard yêu cầu `RENDER=true`, `RENDER_GIT_COMMIT` đúng SHA40, `RENDER_SERVICE_ID` hợp lệ và `RENDER_INSTANCE_ID` có giá trị. Đây là metadata do Render cấp. Không tự phát hiện nền tảng để bỏ yêu cầu Docker.
3. Với Render, `Release:CommitSha` trống sẽ dùng `RENDER_GIT_COMMIT`; nếu người vận hành khai báo explicit thì phải trùng SHA nền tảng. Dùng revision source/service/instance để truy vết; **không gọi service/instance ID là Docker digest hoặc Render deploy ID**.
4. `Release:WebDeploymentId` tùy chọn dành cho ID Vercel thật dạng `dpl_...`. Bỏ trống trả null, không suy ID từ domain/alias và không đoán. Khi có giá trị thì kiểm format. Giá trị được người vận hành khai báo; guard không xác thực với Vercel API và không chứng minh alias đang trỏ bản đó.
5. `/api/v1/release` giữ envelope data/meta và ba field commitSha/apiImageDigest/webImageDigest; thêm provider/apiServiceId/apiInstanceId/webDeploymentId. Trong Render, hai digest luôn null vì không có bằng chứng image digest; Development có thể có commit/digest null như trước. Schema response riêng tại `docs/operations/release-metadata.schema.json`.
6. Giữ nguyên tất cả guard secrets, AllowedHosts, HTTPS/CORS và maintenance announcement. Không sửa auth/TLS/rate limit/MinIO, không áp migration/seed. Provider lạ bị từ chối; không thêm đường “None” hoặc tắt guard.

## Vì sao frontend ID không phải điều kiện khởi động backend

PDF không yêu cầu ràng buộc runtime API vào một artifact frontend. Hai dịch vụ triển khai độc lập; thông tin chưa được cung cấp phải biểu diễn bằng null. Đường Docker release có yêu cầu hai image thật vẫn giữ nguyên và release tooling vẫn kiểm cùng bundle. Môi trường Render không được dùng để làm gate Docker xanh; hỗ trợ demo không xác nhận production promotion.

## Giới hạn và kiểm chứng

Guard kiểm định dạng/nhất quán cấu hình, không tự xác thực attestation hoặc gọi control-plane API. Bằng chứng source Render lấy từ biến nền tảng, không thay thế image digest và UAT. Các biến môi trường vẫn thuộc miền tin cậy của người vận hành.

Regression xác minh Docker mặc định, Render opt-in, revision mismatch, thiếu metadata, provider lạ và các guard secrets/HTTPS. HTTP endpoint kiểm cả response Docker cũ và Render không bịa digest. Không kết nối database/Redis/MinIO để chạy các kiểm này. Runtime Render chưa được cập nhật; schema Neon5/11 migration vẫn là blocker riêng.

Nguồn nền tảng: [Render default environment variables](https://render.com/docs/environment-variables), [Vercel deployment ID/URL](https://vercel.com/docs/rest-api/deployments/get-a-deployment-by-id-or-url). Không đưa provider token vào backend để lấy metadata lúc startup.
