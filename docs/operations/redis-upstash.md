# Redis Upstash cho demo Culinary Blog

Rà soát tổng hợp mới nhất và file import chung8 biến: [Render Neon/Upstash review](render-neon-upstash-review.md). Đã bổ sung kiểm AUTH/PING chỉ đọc; kết quả16 phép kiểm bên dưới vẫn là bằng chứng của lượt kiểm trước. Chưa đổi Render.

Cập nhật26/10/2026, Asia/Bangkok. **Đã xác thực và kiểm thử Upstash thật bằng thư viện Redis của backend. Đã chuẩn bị file import và cập nhật cấu hình Redis trong các bản nháp riêng. Chưa thay Environment trên Render, chưa redeploy hoặc khởi động backend với Upstash.** Database Upstash đã được người dùng tạo trước; không tạo database/gói trả phí khác.

## Kết nối và phạm vi

Backend dùng `GetConnectionString("Redis")`, tương ứng biến **`ConnectionStrings__Redis`**. Giữ nguyên StackExchange.Redis qua **TCP/TLS cổng6379**, user `default`, database0. Không thay cache sang HTTP REST, không thêm Upstash SDK vào backend, không thay nghiệp vụ.

Chuỗi dùng cú pháp StackExchange.Redis, không phải URL Redis hoặc các biến JavaScript REST:

```text
<endpoint>.upstash.io:6379,user=default,password=<private-password>,ssl=True,sslHost=<endpoint>.upstash.io,abortConnect=False,connectTimeout=5000,syncTimeout=5000,asyncTimeout=5000,connectRetry=1
```

Giá trị thật do `ConfigurationOptions.ToString(includePassword:true)` tạo và đã parse lại/so password, user, TLS thành công; thứ tự các tùy chọn có thể khác ví dụ. Không tắt certificate validation. Không đặt giá trị này vào frontend/Vercel hoặc tên biến NEXT_PUBLIC_*.

Redis hiện phục vụ distributed cache, output cache/tag, khóa RedLock, debounce và các phép Lua/counter. Không sao chép key Redis local sang Upstash trong nhiệm vụ này. Cache sẽ được tạo khi ứng dụng được phép sử dụng; không copy khóa/counter cũ. PostgreSQL, MinIO, root `.env` và Docker Compose giữ nguyên. Metadata/tài khoản/token ứng dụng ở PostgreSQL không bị sửa.

## Credential riêng

Người dùng cho phép dùng credential đã cung cấp. File riêng có ACL cho người dùng hiện tại/SYSTEM/Administrators, được Git ignore và nằm trong thư mục `tmp` đã bị Docker build context loại:

```text
D:\Culinary_Blog\tmp\upstash\runtime.env
D:\Culinary_Blog\tmp\upstash\render-redis-connection.txt
D:\Culinary_Blog\tmp\upstash\render-redis.env
```

`runtime.env` chứa credential phục vụ kiểm chứng; không import file này vào Render. `render-redis-connection.txt` chỉ chứa Value của connection string. **`render-redis.env` chỉ có một biến ConnectionStrings__Redis và là file để import.** Không gửi nội dung các file này vào chat/Git/log. Chuỗi người dùng gửi bằng text đã xác thực được cả TCP và REST trong lần kiểm này; không suy diễn mọi REST token đều có thể thay password TCP.

Lượt REST đầu dùng chuỗi chép từ ảnh trả401; sau khi người dùng cung cấp chuỗi text chính xác, REST PING trả200/PONG, file credential riêng đã cập nhật theo kết quả đó. Không tiếp tục thử đoán password.

## Kiểm thử đã thực thi

- Build `tools/UpstashProbe`: thành công,0 warning/0 error, tham chiếu Infrastructure để dùng cùng NuGet dependencies của backend.
- Handshake TCP6379 bằng Python xác nhận certificate/hostname và TLS1.3. Không xác thực bằng handshake này; AUTH/PING được xác minh riêng qua StackExchange.Redis.
- StackExchange.Redis AUTH/PING thành công với user default/TLS; SET NX, GET, TTL và DEL đạt.
- `IDistributedCache` thực: ghi/đọc/xóa đạt. Set/tag và Lua hash/PEXPIRE, sorted-set/PEXPIRE đạt.
- `IOutputCacheStore` Redis thực: ghi/đọc và evict theo tag đạt.
- RedLock thực: lấy khóa, chặn chủ thứ hai khi khóa đang giữ, giải phóng và lấy lại đạt.
- Cả16 điều kiện trong probe đạt; namespace ngẫu nhiên `culinary:upstash-probe:<uuid>:` được dọn, cache demo có TTL. Không FLUSHDB/FLUSHALL, không đọc/xóa key ứng dụng có sẵn. RedLock dùng resource demo riêng, được dispose/giải phóng sau kiểm.
- Các bản nháp `backend/.env` và `tmp/render/backend-render.env.draft` chỉ đổi dòng `ConnectionStrings__Redis`; kiểm byte phần còn lại không thay đổi. Không đổi database/MinIO/seed/worker flags.

Chi tiết không-secret trong `redis-upstash-evidence.json`; report đầy đủ nhưng không in credential ở `tmp/upstash/tcp-check.json`, `rest-check.json`, `render-preparation.json`. Việc kiểm chỉ chạy probe Redis độc lập, không chạy host API, EF migration, seed, Hangfire hay kết nối PostgreSQL/MinIO.

**Chưa kiểm:** network/credential trong runtime Render; HTTP cache/rate limiter/debounce của API end-to-end; toàn bộ ứng dụng phối hợp Neon/Upstash/MinIO sau triển khai. Lua probe chứng minh các nhóm lệnh cần thiết, không phải nghiệm thu toàn bộ hành vi rate limiting. `backend/.env` là bản nháp biến môi trường; ASP.NET Core hiện không tự đọc file `.env` này khi chạy native.

## Áp dụng trên Render — thao tác riêng còn chờ

Không có connector quản trị Render/Upstash trong phiên. Việc chuyển PostgreSQL trước đó vẫn còn bước review schema ứng dụng5/11 migration và Hangfire; kết quả Redis không tự giải quyết các phần đó. Chưa có phê duyệt riêng cho đổi Environment/redeploy backend Render trong lượt này.

Sau khi review các đầu vào backend và duyệt bước thay cấu hình:

1. Mở **Render Dashboard → culinary-blog-api → Environment → Edit**.
2. Chọn **Add from .env**, nạp `D:\Culinary_Blog\tmp\upstash\render-redis.env`. Chỉ thêm/cập nhật Key **`ConnectionStrings__Redis`**, giữ mọi biến khác. Nếu nhập tay, copy một dòng trong `render-redis-connection.txt` vào Value của đúng Key này.
3. Không nhập `UPSTASH_REDIS_REST_URL`/`UPSTASH_REDIS_REST_TOKEN` để thay connection string Redis của ASP.NET Core. Không import nguyên backend/.env khi database/biến release còn đang review.
4. Save/redeploy chỉ sau khi người dùng duyệt thay môi trường. Sau đó kiểm Logs/health Redis, thử đọc công thức hai lần để tạo/hit cache, rồi thử mutation/demo invalidation trong kế hoạch kiểm API được phép. Không bật worker/seed để thử Redis.
5. Nếu lỗi `WRONGPASS`, kiểm credential TCP/user default và việc secret có bị reset hay không. Nếu timeout, kiểm endpoint/cổng6379/TLS và network từ Render; không tắt TLS/certificate validation. Nếu quota/throttling, xem Usage/Logs của Upstash, không tự đổi gói.

Redis Docker local vẫn là môi trường Development trong Compose. Không bật stack local, reset cache local hoặc di chuyển volume trong nhiệm vụ này.

## Chạy lại kiểm chứng

Chỉ chạy khi chủ động cho phép ghi các key demo riêng lên Upstash:

```powershell
dotnet build tools/UpstashProbe/UpstashProbe.csproj --nologo
dotnet tools/UpstashProbe/bin/Debug/net10.0/UpstashProbe.dll D:\Culinary_Blog\tmp\upstash\runtime.env
```

Probe không in giá trị credential/connection string, chỉ ghi connection string vào file riêng. Chạy lại sẽ dùng namespace ngẫu nhiên mới, không quét nội dung key khác. Script chuẩn bị bản nháp: `python tools/upstash/prepare_render.py --env-file D:\Culinary_Blog\tmp\upstash\runtime.env`; chỉ dùng sau probe đạt.

Nguồn đối chiếu: [Upstash Redis compatibility](https://upstash.com/docs/redis/overall/compatibility), [Upstash REST API](https://upstash.com/docs/redis/features/restapi), [StackExchange.Redis configuration](https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/Configuration.md). Orbit discovery đã thử nhưng không khả dụng (HTTPError); đối chiếu tài liệu chính chủ và kiểm SDK trực tiếp, không dùng kết quả đó làm bằng chứng REST/TCP hoạt động.
