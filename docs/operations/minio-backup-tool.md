# Công cụ sao lưu MinIO — candidate G06

Ngày31/10/2026. Candidate chỉ được build và thử trên storage synthetic local. Chưa publish, đổi default `MINIO_MC_IMAGE`, chown volume hiện hữu hoặc thay lịch backup của môi trường đang dùng.

## Build và định danh

```powershell
docker build -f infra/operations/minio-tools.Dockerfile -t culinary-g06-minio-tools:patched .
docker image inspect culinary-g06-minio-tools:patched --format '{{.Id}} {{.Config.User}}'
```

Dockerfile lấy source MC chính thức commit `7394ce0dd2a80935aded936b09fa12cbb3cb8096`, kiểm dependency bằng `go.mod`/`go.sum` lưu cùng repo, build với Go1.27.3 và runtime Alpine3.24.3 được pin digest. Chỉ đưa binary `mc`, giấy phép và GNU coreutils/findutils vào runtime; không chứa MinIO server. Dependency đã vá gồm compress, Prometheus parser, x/net, x/crypto và gRPC. Dùng `-mod=readonly`; không chạy update dependency khi khởi động container.

Candidate đã kiểm có image ID `sha256:00ecdeeba1186561b111b290c7877e4fc24f087bd31d456f642f19434739dda0`, user `1000:1000`. Image ID local không phải registry manifest digest; cần review/publish theo quyền riêng trước khi dùng trên host khác. Repo MC đã được lưu trữ read-only; rebuild này không tạo cam kết hỗ trợ upstream.

## Kết quả và giới hạn

- Candidate ban đầu tái dùng image server cũ bị scan phát hiện136High/Critical; đã loại khỏi phương án vận hành. Candidate cuối scan0High/Critical theo database/version ghi trong [report G06](../specs/srs-completion-phases/reports/package-G06-result.md).
- `minio-backup.sh` và `restore-minio-drill.sh` chạy nguyên bản LF trên hai MinIO synthetic riêng: hai object gồm file rỗng và key lồng thư mục/có khoảng trắng, hai dòng SHA256 khớp sau restore, versioning được bật. GNU `find`, `sha256sum`, `touch -d` có đủ. Harness dọn đúng tài nguyên có label của lượt thử.
- Chưa có storage backup ngoài host, lịch chạy trên deployment hoặc đo RPO/RTO thật. Candidate CLI không vá MinIO server đang chạy.

## Cấu hình sau khi được phép triển khai

1. Review image/digest, scan và kiến trúc CPU của host đích. Chỉ override `MINIO_MC_IMAGE` cho service backup đã chỉ định; không sửa defaults chung để tự áp sang demo/production/E2E.
2. Cấp đường dẫn backup độc lập, quyền UID/GID1000 và quota/retention. Với volume mới có thể khởi tạo ownership trong bước provision được duyệt. Không chown, xóa hoặc mount ghi một volume hiện hữu chỉ để thử candidate.
3. Cấu hình `MINIO_ENDPOINT`, `MINIO_BUCKET`, `MINIO_ROOT_USER`, `MINIO_ROOT_PASSWORD` qua Environment/Secrets hoặc secret file có quyền đọc hạn chế; không đưa giá trị vào commit/chat/log. Dùng identity có quyền backup/restore đúng phạm vi khi vận hành; kiểm compatibility với script trước áp.
4. Mount scripts read-only và backup storage theo quyền đã duyệt; runtime cần `/tmp` ghi được cho MC config. Giữ lịch03:00UTC, retention30ngày theo SRS và kiểm bằng log/manifest thực tế. Restore vào target sạch được chỉ định; không dùng bucket dữ liệu thật làm target drill.
5. Kiểm restore/checksum, tuổi bản sao gần nhất, dung lượng, cảnh báo lỗi và RTO/RPO trên deployment. Đủ các bằng chứng này mới xét đóng D9-07.

## Gap bảo mật MinIO server

Image server demo đã đọc identity là `sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e`, cùng image cũ được quét trong lab. MinIO công bố Community chưa có bản vá cho [unsigned-trailer authentication bypass CVE-2026-40344/41145](https://github.com/minio/minio/security/advisories/GHSA-9c4q-hq6p-c237), [OIDC algorithm confusion CVE-2026-33322](https://github.com/minio/minio/security/advisories/GHSA-5cx5-wh4m-82fh) và [S3 Select memory exhaustion CVE-2026-39414](https://github.com/advisories/GHSA-h749-fxx7-pwpg). Điều kiện khai thác khác nhau; phát hiện package không chứng minh deployment bị khai thác. Không chạy exploit trên demo.

Template gateway đã thêm chặn header có `UNSIGNED-PAYLOAD-TRAILER`, kiểm Nginx isolated với upstream stub. Đây chỉ là defense cho upload mode đó, không thay việc vá server hoặc miễn gate High/Critical. Ngày31/10 khi tiếp tục, Docker Desktop/gateway/cloudflared đã ngừng; đã khôi phục đúng gateway ID theo quyền sửa tunnel trước đó và áp guard đã qua10case/config test. Saved config nay khớp template. Tunnel mới `https://retail-buying-measuring-helping.trycloudflare.com` đọc đủ300key từ snapshot Neon và3media có SHA256 khớp bằng demo identity; credential/bucket/IAM giữ nguyên. Endpoint Render cần cập nhật riêng; xem report G06, không dùng URL cũ làm trạng thái hiện tại.

Trước nghiệm thu production cần chọn storage/version được bảo trì và có bản vá phù hợp, review compatibility/chi phí/migration rồi cấp quyền thực hiện. Không tự mua AIStor, đổi nhà cung cấp hoặc fork server trong lượt G06 này. Nếu chọn giải pháp khác MinIO thì phải đối chiếu yêu cầu PDF và quyết định chính thức, không tự đặt ngoại lệ mới.
