# Metadata Render — ban va tach rieng

Ban va nay duoc tach tu source GitHub `7ed7a5835ab6d79ec72c9aa6d21fae72a24d206e`. Giu nguyen5 EF migration va dependency cua source do; khong dua6 migration dang phat trien hoac cac thay doi G04 khac vao commit.

## Cau hinh sau khi source moi duoc trien khai theo phe duyet

Them mot bien trong Render → culinary-blog-api → Environment:

```text
Release__Provider=Render
```

Render tu cap `RENDER=true`, `RENDER_GIT_COMMIT`, `RENDER_SERVICE_ID`, `RENDER_INSTANCE_ID`; khong nhap cac gia tri gia. De trong/xoa `Release__CommitSha` neu dang co SHA cu; neu khai bao thi phai trung revision Render. Khong can dien hai Docker image digest cho provider Render. Neu co ID frontend Vercel that, co the khai bao tuy chon `Release__WebDeploymentId=dpl_...`; bo trong tra null.

Giu cac bien ket noi Neon/Upstash, MinIO va cac secrets da duoc kiem. Trong giai doan kiem ket noi, giu:

```text
Database__ApplyMigrationsOnStartup=false
AdminSeed__Enabled=false
BackgroundJobs__Enabled=false
```

Ban va cung ngan Hangfire tu chuan bi schema khi jobs tat. Khong chay seed, migration hoac worker de thu metadata.

Provider Docker van la mac dinh, giu yeu cau SHA va hai image digest. Xem [ADR-011](../specs/phase-0/adr/ADR-011-release-metadata-providers.md) va [response schema](release-metadata.schema.json). Ho tro metadata Render khong thay nghiem thu Docker Compose production theo SRS; cac gate release/security/UAT khong duoc mien.

## Kiem chung tren source tach rieng

27 regression da dat, gom metadata Docker/Render, guard secrets/HTTPS/maintenance/CORS/auth cache, HTTP release endpoint va startup storage offline. Build khong co warning/error. Khong su dung database/Redis/MinIO that trong cac test nay.

Sau lan deploy duoc nguoi dung cho phep, kiem `/api/v1/release`: provider Render, commitSha trung Source tren Render, service/instance ID duoc cap that, hai image digest null. Kiem `/health/live`, `/health/ready` va API doc recipe theo ke hoach demo; health ket noi khong thay cho kiem schema ung dung.

Trong luc chuan bi commit/push nay, chua thay Environment Render, chua merge vao main, chua deploy hay thay du lieu cloud. Push nhanh rieng khong cap nhat branch main ma Web Service dang theo doi. Auto-deploy tren Render chua xac minh duoc vi Computer Use dung khi khong xac dinh URL trinh duyet du chac chan.
