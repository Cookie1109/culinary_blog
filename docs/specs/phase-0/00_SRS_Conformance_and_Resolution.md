# SRS Conformance and Conflict Resolution Baseline v1

> Nguồn chuẩn: `SRS_Culinary_Blog_v1.0.0.pdf` (71 trang)  
> Trạng thái: Accepted for implementation  
> Ngày chốt: 11/09/2026  
> Phạm vi: chỉ giải quyết mâu thuẫn/thiếu rõ ràng nội tại của SRS; mọi yêu cầu không mâu thuẫn trong PDF vẫn bắt buộc.

## 1. Quy tắc ưu tiên

Khi hai đoạn trong PDF không thể cùng được hiện thực, baseline chọn theo thứ tự:

1. Bảo mật, phân quyền và không làm lộ Draft/Archived.
2. Tính đúng trên PostgreSQL 16, khả năng chạy nhiều API instance và độ bền dữ liệu.
3. Contract tổng hợp tại Chương 8 và Application Error Codes tại Phụ lục B.
4. Use case chi tiết tại Chương 3.
5. Mô tả kiến trúc hoặc ví dụ triển khai.

Ngoại lệ phải được nêu rõ trong bảng dưới đây. Quyết định ở đây là phần bổ sung chính thức cho SRS v1.0.0; không được tự thay đổi lại trong code, OpenAPI hoặc test mà không có Change Request.

## 2. Ma trận mâu thuẫn và quyết định

| GAP | Vị trí trong PDF | Mâu thuẫn/thiếu rõ ràng | Quyết định cuối | Lý do |
|---|---|---|---|---|
| GAP-001 | 1.5 trang 10; 2.2 trang 12; Chương 3 trang 17-39 | SRS nói 27 FR, nhưng tổng nhóm `7+5+10+4+2+3+3` là 34. | Baseline có **34 FR**. | Giữ toàn bộ mã FR đã được SRS liệt kê; loại 7 FR sẽ làm mất yêu cầu có định danh và acceptance riêng. |
| GAP-002 | FR chi tiết trang 18-37; Chương 8 trang 61-66; Phụ lục A trang 67 | Validation dùng lẫn 400 và 422. | Mọi lỗi parse/field/query/domain precondition dùng **400**. | Chương 8 và Phụ lục A là contract tổng hợp; một mã duy nhất giúp OpenAPI/client/test ổn định. |
| GAP-003 | FR-RCP-004 trang 30-31; Phụ lục A-B trang 67-68 | Concurrency conflict dùng 409 trong FR nhưng 422 trong phụ lục. | Stale version dùng **409 Conflict**; thiếu `If-Match` dùng **400**. | Lost update là xung đột trạng thái resource; không thêm 428 vì SRS không liệt kê mã đó. |
| GAP-004 | FR-AUTH-001 trang 17-18; API 8.1 trang 61; Data Model 7.7 trang 58-59 | Request đăng ký lúc dùng `fullName/userName`, lúc dùng `displayName`; response lúc có token, lúc chỉ có user. | Request `{displayName,email,password}`; Identity `UserName=NormalizedEmail` nội bộ; response 201 chứa `{accessToken,refreshToken,expiresAt,user}`. | Khớp data model/API naming và vẫn giữ hành vi auto-login bắt buộc của FR-AUTH-001. |
| GAP-005 | FR-AUTH-003 trang 19-20; 5.3 trang 47; API 8.1 trang 61 | Auth.js callback, backend callback, `ExternalLoginInfo` và `idToken` cùng xuất hiện. | Auth.js sở hữu Authorization Code + PKCE/callback; backend nhận `{idToken}` tại `POST /auth/google` và tự xác minh. | Chỉ một component sở hữu callback; backend vẫn kiểm chứng issuer/audience/expiry/email verification trước khi cấp token nội bộ. |
| GAP-006 | FR-AUTH-002/004 trang 18-21; NFR-SEC-002 trang 41; Data 7.8 trang 59-60 | Refresh token được mô tả 512-bit và 128-bit; schema thiếu family ID nhưng yêu cầu revoke family. | Token ngẫu nhiên **512-bit**, DB chỉ lưu SHA-256; thêm `FamilyId`; TTL 7 ngày; reuse revoke cả family. | Chọn mức entropy cao hơn, thỏa yêu cầu thấp hơn và làm reuse detection có thể hiện thực. |
| GAP-007 | FR-AUTH-005 trang 21-22; 5.2 trang 47; API 8.1 trang 62 | Logout vừa yêu cầu bearer hợp lệ vừa cho phép khi access token hết hạn; refresh token được quy định trong body. | `POST /auth/logout` nhận `{refreshToken}` trong body; bearer là optional. Nếu bearer còn hạn phải khớp owner; nếu hết hạn, refresh token hợp lệ vẫn được revoke; luôn idempotent 204. | Giữ contract body/no-cookie của PDF và bảo đảm người dùng vẫn đăng xuất được khi access token hết hạn. |
| GAP-008 | FR-RCP-007 trang 32-33; NFR-REL-003 trang 43; Data 7.1 trang 54; API 8.3 trang 64 | Recipe delete vừa hard delete vừa soft delete. | Recipe **soft delete** (`IsDeleted=true`); không purge vật lý trong v1. | NFR durability, BaseEntity và API tổng hợp đều yêu cầu soft delete; đây là lựa chọn an toàn hơn và có nhiều bằng chứng hơn trong SRS. |
| GAP-009 | FR-CAT-005 trang 26-27; API 8.2 trang 63; Data 7.1 trang 54 | Category được mô tả xóa khỏi DB nhưng API ghi soft delete. | Category soft delete, chỉ khi không còn Recipe chưa bị xóa tham chiếu. | Khớp BaseEntity/global query filter và tránh mất dữ liệu. |
| GAP-010 | FR-RCP-005 trang 31; Phụ lục A-B trang 67-68 | Publish lúc chỉ cần 1 step, lúc cần 1 ingredient và 1 step. | Publish cần **ít nhất 1 ingredient và 1 step**; không bắt buộc primary image. | Chọn invariant đầy đủ hơn được error code chính thức mô tả; không tự thêm điều kiện ảnh ngoài PDF. |
| GAP-011 | Tên FR-RCP-006 trang 32; flow/API trang 32, 64 | Tên có Archive/Unarchive nhưng chỉ đặc tả archive. | V1 có `/archive` theo SRS; thêm `/unarchive` là supporting endpoint, chuyển Archived về Draft. | Hoàn tất đúng năng lực đã nằm trong tên FR, không tạo module nghiệp vụ mới. |
| GAP-012 | FR-RCP-008 trang 33-34; API 8.4 trang 64 | Set primary có path `/primary` riêng hoặc PATCH metadata chung. | Dùng `PATCH /recipes/{id}/images/{imageId}` cho `altText/isPrimary/orderIndex`. | Khớp Chương 8, giảm endpoint trùng và cho phép đổi primary nguyên tử. |
| GAP-013 | FR-CAT/RCP/SRCH trang 23-37; NFR-PERF-003 trang 40; NFR-SCALE-001 trang 44 | IMemoryCache/Output Cache/Redis và TTL 60/15/5/1 phút không thống nhất. | Redis-backed Output Cache: category 30 phút, detail 5 phút, list 15 phút, search 1 phút. | Ưu tiên NFR định lượng và yêu cầu stateless/horizontal scaling; một shared store tránh lệch cache giữa instance. |
| GAP-014 | FR-CAT-002 trang 24-25; FR-RCP-001 trang 27-28; NFR-SCALE-001 trang 44; UI ISR trang 46 | Public URL trả dữ liệu khác theo optional identity nhưng lại dùng shared cache/ISR. | Public `/recipes`, `/categories/{slug}`, `/recipes/search` chỉ trả Published; thêm private `/me/recipes` và `/me/recipes/{id}`. | Ngăn cache poisoning và rò Draft/Archived; vẫn bảo toàn quyền owner/Admin qua endpoint private. |
| GAP-015 | FR-RCP-004 trang 30-31; Data 7.1 trang 54 | `bytea [Timestamp]` được dùng như SQL Server rowversion trên PostgreSQL. | Dùng application-managed `Version bigint`, map ETag/If-Match và tăng nguyên tử. | PostgreSQL không tự tăng `bytea`; bigint rõ ràng, portable và dễ test. |
| GAP-016 | FR-SRCH-001 trang 36-37; PostgreSQL 16 constraint | SRS gọi text-search config `vietnamese`, nhưng PostgreSQL chuẩn không bảo đảm có config này. | Dùng `simple` + chuỗi đã `unaccent`, trigger-maintained weighted `tsvector`, GIN; `pg_trgm` fallback cho tiêu đề. | Triển khai tái lập trên PostgreSQL 16 và vẫn đạt acceptance “pho” tìm “phở”. |
| GAP-017 | FR-RCP-010 trang 35-36; Data 7.3 trang 57; API 8.5 trang 65 | Step dùng `DurationMinutes` hoặc `TimerMinutes`, có/không có `Title`; POST lúc tự sinh/lúc nhận StepNumber. | Schema dùng `title`, `description`, `timerMinutes`; POST tự gán StepNumber cuối; PUT có thể đổi vị trí bằng renumber transaction. | Khớp data model/API đầy đủ và giữ hành vi append của FR chi tiết. |
| GAP-018 | FR-RCP-009 trang 35; Data 7.4 trang 57; API 8.6 trang 65 | Ingredient dùng `SortOrder` hoặc `OrderIndex`. | Dùng `orderIndex`. | Khớp data model và Chương 8; tránh hai tên cho cùng ý nghĩa. |
| GAP-019 | FR-CAT-003 trang 25; Data 7.6 trang 58; FR-RCP-003 trang 29; Data 7.2 trang 55 | Category name tối đa 50 hoặc 100; cook time `>0` hoặc `>=0`. | Category name 2-100; cookTime `>=0`; prepTime `>0`. | Khớp schema và hỗ trợ món không cần nấu. |
| GAP-020 | FR-AUTH-007 trang 23; API 8.1 trang 62; Data 7.7 trang 59 | `fullName`/`displayName`; profile có hoặc không có `bio`. | Dùng `displayName`, `avatarUrl`, `bio`; email và username không đổi qua endpoint này. | Khớp ApplicationUser và API tổng hợp; `fullName` được xem là tên cũ của `displayName`. |
| GAP-021 | FR-RCP-001/002 trang 28-29; 5.2 trang 47; Chương 8 trang 61 | List response dùng `items/totalCount` hoặc `{data,meta}`. | Mọi list dùng `{data:[...],meta:{page,pageSize,total,totalPages,hasNextPage,hasPreviousPage}}`; resource đơn dùng `{data:{...}}`. | Khớp convention Chương 8 và tạo một SDK contract duy nhất. |
| GAP-022 | Môi trường 2.4.1 trang 13; FR-FILE trang 38; quyền Draft/Archived trang 13, 29 | Bucket `public-read` làm ảnh private vẫn truy cập được. | Bucket private; Published media qua controlled public proxy/CDN, private media qua presigned URL ngắn hạn sau authorization. | Quyền nội dung là yêu cầu bảo mật cốt lõi; bucket public mâu thuẫn trực tiếp với nó. |
| GAP-023 | Phụ thuộc 2.6.2 trang 16; FR-JOB trang 38-39; Hangfire PostgreSQL trang 48 | SRS nói job fire-and-forget có thể mất dù queue dùng PostgreSQL. | Hangfire PostgreSQL persistence bắt buộc; transactional outbox cho event sau DB commit; handler idempotent. | Đóng khoảng trống dual-write và phù hợp NFR durability/retry. |
| GAP-024 | Browser matrix 2.4.3 trang 14 và 5.4 trang 49 | Hai bộ minimum browser khác nhau. | Minimum: Chrome 112, Firefox 113, Edge 112, Safari 16; mobile giữ iOS/Android 14+/90+ nếu cao hơn theo platform. | Ưu tiên bảng giao diện phần cứng xuất hiện sau và mới hơn; không thay bằng “latest” để test tái lập. |
| GAP-025 | Cover/1.4 trang 1, 9; Architecture 6.1 trang 50; environment 2.4 trang 13-14 | Next.js 15 và 14+; Node 20/22; image `latest`. | .NET 10, PostgreSQL 16, Redis 7, Next.js 15, Node 20 LTS baseline; pin exact patch/package/image digest khi tạo lockfile; production không dùng `latest`. | Giữ major version rõ nhất của PDF và bảo đảm build tái lập. |
| GAP-026 | CONS-001/008 trang 14-15; NFR-MAINT-004 trang 43-44 | Domain không dependency ngoài, nhưng một câu cho phép FluentValidation ở Domain. | Domain chỉ .NET BCL; FluentValidation chỉ ở Application pipeline. | Phù hợp Clean Architecture và chính vị trí validator được SRS mô tả ở Application. |
| GAP-027 | Bảng NFR trang 40; NFR-SEC-001-007 trang 41-42 | Bảng tổng hợp ghi 6 security requirements nhưng thực tế có 7 mã. | Baseline có **7 NFR-SEC**. | Giữ mọi requirement có mã; số 6 là lỗi đếm tương tự GAP-001. |
| GAP-028 | ERD tóm tắt 6.4 trang 52; Data 7.5 trang 58 | RecipeImage vừa được ghi là owned columns trong Recipes, vừa có entity/table và quan hệ 1:N. | RecipeImage là **entity/table riêng** `RecipeImages`, quan hệ 1:N với Recipe. | Một Recipe có nhiều ảnh, mỗi ảnh có ID/URL/order riêng; owned columns không biểu diễn đúng collection và mâu thuẫn trực tiếp Chương 7.5. |
| GAP-029 | FR-RCP-001 trang 28; API 8.3 trang 63 | Filter chi tiết dùng `maxCookTime/minServings`; bảng API dùng `minPrepTime/maxPrepTime`. | Hỗ trợ hợp nhất `categoryId`, `difficulty`, `maxCookTime`, `minServings`, `minPrepTime`, `maxPrepTime`; kết hợp AND. | Không loại bỏ khả năng nào đã được PDF nêu; chi phí query/index nhỏ và contract rõ. |
| GAP-030 | FR-RCP-009 trang 35; Data 7.4 trang 57 | Ingredient Name tối đa 100 hoặc varchar(200). | Name dài **1-200** ký tự. | Ưu tiên schema dữ liệu chi tiết; vẫn bao hàm mọi giá trị hợp lệ theo giới hạn 100 cũ. |
| GAP-031 | FR-RCP-003 trang 29-30; Phụ lục B trang 68; Data 7.2 trang 54-55 | Recipe slug trùng lúc trả 409, lúc tự thêm suffix; Title được phép trùng. | Tự thêm suffix `-2`, `-3` cho collision thông thường; 409 chỉ dùng khi race/unresolved unique conflict. | Cho phép nhiều recipe cùng title như data model quy định và vẫn bảo vệ unique index khi concurrent race. |
| GAP-032 | Architecture 6.4 trang 52; Data 7.1 và 7.7 trang 54, 58-59 | “Tất cả entities” kế thừa BaseEntity UUID nhưng ApplicationUser kế thừa `IdentityUser<string>`. | ApplicationUser không kế thừa BaseEntity; giữ string Identity key và khai báo audit fields riêng. | Tránh hai khóa chính/kiểu ID xung đột và giữ đúng ASP.NET Core Identity model. |
| GAP-033 | Out-of-scope 1.2.3 trang 7; NFR-SEO-001 trang 44 | Rating system ngoài v1 nhưng phần SEO kỳ vọng star-rating rich snippet. | Không tạo rating/aggregateRating giả trong JSON-LD v1; chỉ xuất field có dữ liệu thật. | Structured data giả vi phạm tính đúng dữ liệu; rating chỉ được thêm cùng Change Request cho rating system. |

## 3. Quyết định bổ sung để bảo toàn yêu cầu PDF

- `GET /health` chỉ trả trạng thái tổng hợp công khai; chi tiết từng dependency chỉ mở trong internal/ops context. `/health/live` và `/health/ready` vẫn public cho probe.
- Admin có quyền vào Hangfire Dashboard. Structured application audit/log view dành cho Admin; raw Seq/OTLP infrastructure vẫn cần operational authentication/network control.
- Google login, profile, category delete và archive giữ mức **Should Have** đúng MoSCoW của SRS. Chúng thuộc v1 khi Must-have đã đạt gate, không được làm chậm release Must-have.
- Policy `VerifiedAuthor` không được bật trong v1 vì SRS yêu cầu email confirmed nhưng không đặc tả send/confirm/resend endpoint. V1 dùng `AuthorPolicy`; bật `VerifiedAuthor` chỉ sau Change Request bổ sung vòng đời xác nhận email.
- Email confirmation, password reset, Admin deactivate/reactivate, public author profile, moderation, bulk reorder và restore soft-deleted resource không phải FR đầy đủ của SRS v1. Chúng ở backlog sau v1 hoặc cần Change Request riêng.
- Recipe không có ảnh vẫn có thể publish; frontend dùng ảnh mặc định cho card/OG/JSON-LD. Nếu sau này bắt buộc ảnh, đó là thay đổi nghiệp vụ.
- Sitemap job chạy 02:00 UTC và retry 2 lần. Welcome email retry đúng lịch 1, 5, 30 phút. Resize/delete retry tối đa 3 lần.

## 4. Quy tắc thay đổi

Mọi thay đổi sau baseline phải nêu: requirement bị ảnh hưởng, lý do, thay đổi API/data/security/test, người duyệt và ngày hiệu lực. Code, migration, OpenAPI và test phải trace về SRS hoặc quyết định trong tài liệu này.
