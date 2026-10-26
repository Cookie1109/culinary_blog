# Tim kiem tu ngan tieng Viet

Ngay27/10/2026. Ban sua theo yeu cau tim "ga"/"gà" khong tra ve mon chi co
"gao", "gac" hoac "gan" trong tieu de/mo ta.

- Voi mot tu don2 ky tu sau bo dau, neu co khop nguyen tu trong cong thuc Published,
  dung khop nguyen tu va khong mo rong bang trigram cho truy van do.
- Neu chua co khop nguyen tu, giu prefix/trigram de "ph" van tim duoc "pho".
- Truy van dai hon va nhieu tu giu prefix, vi du `pho:* & bo:*` theo
  FR-SRCH-001, SRS trang36-37. Giu unaccent, FTS/GIN, rank, filter, paging,
  Published-only va cau hinh FTS hien co. Khong doi schema, seed hay credentials.
- Kiem nguyen tu dung truy van EXISTS tren tap Published truoc cac bo loc khac;
  mot tu da duoc nhan dien khong bi bien thanh tu khac khi doi category.
- Xoi xeo co "mo ga" va sup cua co "ga xe" van la ket qua hop le.

Regression PostgreSQL16/Redis7 trong Testcontainers rieng:

- Truoc sua:2 test ga/gà that bai, mong3 nhung thuc te7 ket qua.
- Sau sua:8/8 test RecipeDiscoveryApiTests tren source tach tu main;0 fail/skip.
- Cung ban sua tren workspace G04:32/32 test cung class;0 fail/skip.
- Test bao phu khong dau/co dau, loc category, mo ta, loai gao/gac/gan,
  Draft privacy va prefix2 ky tu chua hoan chinh. Khong dung Neon de seed/test.

Lenh kiem ban chuan bi push:

```powershell
dotnet test backend/tests/CulinaryBlog.IntegrationTests/CulinaryBlog.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~RecipeDiscoveryApiTests
```

Deploy frontend/backend thanh cong chua dong goi G04. Report workspace hien giu
9/15 task dong; D6-01/02/03/04/09/13 con mo vi400/422, Category Name50/100 va
Google sitemap ping da ngung. Source deploy tach rieng giu5 migration; cac thay
doi G04 va6 migration pending trong workspace khong nam trong ban sua nay.
Khong dung checkpoint local de chung nhan production SRS hoac mien gate CI.
