# Phase 32 — Kiểm security scans và HTTPS deployment

> Đích giao việc: GPT 6.1 Sol. Trạng thái ban đầu: **Chưa bắt đầu**. Đây là tài liệu thực hiện, chưa phải bằng chứng công việc đã đạt.

## Giao việc bằng prompt này

```text
Thực hiện Phase 32 theo docs/specs/srs-completion-phases/phase-32-security-and-https.md trong D:\Culinary_Blog.
Đọc AGENTS.md, quy tắc trong docs/specs/srs-completion-phases/README.md và progress.md trước khi sửa.
Đối chiếu checklist gốc, report bàn giao của phase phụ thuộc và mã nguồn hiện tại; không coi số test cũ là bằng chứng mới.
Chỉ thực hiện các task được giao trong phase này. Sửa và kiểm chứng đến khi đạt phạm vi được giao.
Nếu vướng quyết định/credential/môi trường, ghi rõ điều kiện còn thiếu và tiếp tục phần độc lập của chính phase; không giả kết quả.
Cập nhật report, progress.md và checkbox gốc chỉ theo bằng chứng. Không tự chạy phase tiếp theo.
```

## Đầu vào và giới hạn

- Checklist nguồn: [srs-completion-checklist.md](../srs-completion-checklist.md), task `D9-04`.
- Phụ thuộc kỹ thuật: [Phase 16](phase-16-rate-limit-and-headers.md), [Phase 19](phase-19-media-storage-and-jobs.md), [Phase 22](phase-22-schema-migrations-openapi.md), [Phase 29](phase-29-ci-and-static-analysis.md)
- Loại công việc: **Scans/HTTPS staging**. Xem quy tắc xử lý phần pending trong README.
- Yêu cầu liên quan theo ma trận gốc: `NFR-SEC-004`, `NFR-SEC-005`, `NFR-SEC-007`. Đóng phase này không tự đóng toàn bộ FR/NFR/CONS đó.
- Giữ thay đổi nhỏ theo use case; phần đã đạt chỉ cần kiểm chứng. Không mở rộng sang phase khác để sửa tiện thể.

Các file/vùng mã cần đọc đầu tiên (đường dẫn thư mục hoặc tên có thể thay đổi; tìm implementation hiện tại trước khi sửa):

- `.github/workflows/security-dynamic.yml`
- `infra/nginx/`
- `infra/production/`
- `backend/src/CulinaryBlog.Infrastructure/Configuration/`
- `frontend/next.config.mjs`
- `docs/reports/`

## Task gốc phải đóng

- [ ] **D9-04 · Kiểm chứng:** quét dependencies backend/frontend, secret history/worktree, images/config và dynamic security trên staging HTTPS đúng artifact; ghi scan version/report/findings/remediation, không suy “0 High/Critical” từ báo cáo cũ. Đóng Critical/High; finding còn lại được triage theo release gate hiện có. Kiểm CORS exact origins, TLS ≥1.2, HTTP redirect, HSTS 31536000, CSP/security headers/cookie policy và production fail-fast secrets.

## Trình tự thực hiện trong phase

1. Chạy dependency/secrets/history/images/config scans trên bản hiện tại, redaction output.
2. Kiểm HTTPS/TLS/HSTS/CORS/CSP/security/cookie headers và fail-fast config trong deployment test phù hợp.
3. Chạy dynamic security staging HTTPS khi có endpoint/ủy quyền, triage findings và giữ môi trường test riêng.

## Cách kiểm chứng

- Report tool version/artifact/findings; Critical/High được đóng, phần còn lại theo gate repo.
- Không coi scan cũ/local HTTP là staging HTTPS hoặc tắt check để pass.
- Lấy lệnh có thật trong package/project/scripts hiện tại và checklist mục 6; report phải ghi lệnh đã chạy, cwd, runtime, exit code, expected/actual. Không ghi một lệnh dự kiến là kết quả đã chạy.
- Chạy regression đủ cho thay đổi; thêm test có ý nghĩa nếu phát hiện/sửa lỗi, không thêm test chỉ lặp cách triển khai.

## Điều kiện hoàn thành và bàn giao

- [ ] Có evidence mới hoặc pending rõ phần staging thiếu.
- [ ] Không deploy production hoặc quét host ngoài phạm vi được phép.
- [ ] Report có kết quả của từng task, file/code/test/decision, bằng chứng và các điều kiện còn mở; không chứa secrets.
- [ ] Cập nhật `progress.md` và checkbox task gốc tương ứng nếu đủ điều kiện. Nếu task có một phần chưa đạt, để ô gốc mở.

Tạo report **`reports/phase-32-result.md`** từ [mẫu report](reports/README.md). Report chưa tồn tại ở thời điểm chia phase; không tạo nội dung PASS trước khi thực hiện.

[Quay về điều phối](README.md) · [Trước](phase-31-frontend-performance-and-inp.md) · [Tiếp theo để tham khảo](phase-33-observability-and-resilience.md) — liên kết tiếp theo không phải chỉ thị tự thực hiện.
