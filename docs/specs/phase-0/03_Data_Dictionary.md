# Data Dictionary v1

> Trạng thái: Accepted for implementation  
> Naming: JSON `camelCase`; C# `PascalCase`; PostgreSQL table/column naming do EF convention được cấu hình thống nhất ở Phase 1.
> Các khác biệt với câu chữ PDF được giải thích tại [00_SRS_Conformance_and_Resolution.md](./00_SRS_Conformance_and_Resolution.md).

## 1. Kiểu dùng chung

| Tên | Kiểu | Quy tắc |
|---|---|---|
| EntityId | UUID v4 | Server sinh; không sequential ID public |
| UserId | string | ASP.NET Core Identity key |
| Timestamp | ISO-8601 UTC | DB `timestamptz`; API kết thúc `Z` |
| Version | int64 | Bắt đầu 1, tăng mỗi mutation aggregate; map ETag |
| Slug | string | lowercase ASCII, dấu cách thành `-`, unique, tối đa theo entity |
| Url | absolute HTTPS URL | Local dev có thể HTTP; không nhận `javascript:`/unsafe scheme |
| Money | Không dùng v1 | Không có commerce trong scope |

## 2. Enums

### RecipeStatus

| JSON | DB | Ý nghĩa |
|---|---:|---|
| `draft` | 0 | Chỉ owner/Admin xem |
| `published` | 1 | Public/search/sitemap |
| `archived` | 2 | Ẩn public, owner/Admin xem |

### RecipeDifficulty

| JSON | DB | Ý nghĩa |
|---|---:|---|
| `easy` | 1 | Dễ |
| `medium` | 2 | Trung bình |
| `hard` | 3 | Khó |
| `expert` | 4 | Chuyên gia |

### Roles

`Author`, `Admin`. Guest không phải persisted role.

## 3. Audit/soft-delete contract

Áp dụng cho Category, Recipe, RecipeStep, RecipeIngredient và RecipeImage khi phù hợp:

| Field | DB type | Null | Quy tắc |
|---|---|:---:|---|
| id | uuid | Không | PK |
| createdAt | timestamptz | Không | Set ở insert |
| updatedAt | timestamptz | Có | Set ở update |
| createdBy | string | Có | User hoặc system context |
| updatedBy | string | Có | User hoặc system context |
| isDeleted | boolean | Không | Default false |
| deletedAt | timestamptz | Có | Có khi soft delete |
| deletedBy | string | Có | Actor soft delete |
| version | bigint | Không | Default 1, concurrency token |

## 4. ApplicationUser

| Field | Type/length | Null | Editable | Validation/ghi chú |
|---|---|:---:|:---:|---|
| id | string | Không | Không | Identity PK |
| email | varchar(256) | Không | Không trong v1 profile | Normalized unique; không public tùy view |
| displayName | varchar(100) | Không | Có | 2–100, trim, reject active HTML |
| avatarUrl | varchar(500) | Có | Có | Safe HTTP(S) URL hoặc managed asset |
| bio | text | Có | Có | Tối đa 2.000 ký tự, render escaped/sanitized |
| isActive | boolean | Không | Admin | Default true |
| emailConfirmed | boolean | Không | System/provider | Có thể được set bởi external verified identity; v1 không có confirmation endpoint riêng |
| createdAt | timestamptz | Không | Không | UTC |
| roles | string[] | N/A | Admin process | Response projection từ Identity roles |

Register request thống nhất: `{ displayName, email, password }`. Không yêu cầu `userName` public; Identity UserName có thể dùng normalized email nội bộ.

## 5. RefreshToken

| Field | Type | Null | Quy tắc |
|---|---|:---:|---|
| id | uuid | Không | PK |
| userId | string | Không | FK cascade theo user retention policy |
| tokenHash | char(64) | Không | SHA-256 hex của raw token ngẫu nhiên 512-bit, unique; không lưu raw token |
| familyId | uuid | Không | Revoke family khi reuse |
| expiresAt | timestamptz | Không | CreatedAt + 7 ngày |
| revokedAt | timestamptz | Có | Null khi active |
| replacedByTokenHash | char(64) | Có | Link rotation |
| createdAt | timestamptz | Không | UTC |
| createdByIp | varchar(45) | Có | IPv4/IPv6 |
| userAgentHash | varchar(64) | Có | Không cần lưu raw UA nếu không cần |

## 6. Category

| Field | Type/length | Null | Validation/constraint |
|---|---|:---:|---|
| id | uuid | Không | PK |
| name | varchar(100) | Không | 2–100; unique toàn bảng, kể cả soft-deleted rows |
| slug | varchar(120) | Không | Unique toàn bảng; không đổi khi rename và không tái sử dụng |
| description | text | Có | Tối đa 2.000 |
| imageUrl | varchar(500) | Có | Safe URL/managed asset |
| orderIndex | int | Không | ≥0, default 0 |
| recipeCount | int | N/A | Response projection; chỉ Published public |
| audit fields | — | — | Theo mục 3 |

## 7. Recipe

| Field | Type/length | Null | Validation/constraint |
|---|---|:---:|---|
| id | uuid | Không | PK |
| title | varchar(200) | Không | 5–200 |
| slug | varchar(220) | Không | Unique toàn bảng; collision thêm `-2`, `-3`; bất biến sau publish đầu tiên |
| description | text | Không | 1–2.000 |
| instructions | text | Có | Legacy/summary; detailed instructions dùng steps |
| prepTime | int | Không | >0 phút |
| cookTime | int | Không | ≥0 phút |
| servings | int | Không | >0 |
| difficulty | RecipeDifficulty | Không | Enum hợp lệ |
| status | RecipeStatus | Không | Default draft |
| categoryId | uuid | Không | FK restrict |
| authorId | string | Không | FK Identity user |
| publishedAt | timestamptz | Có | Set lần publish đầu và giữ nguyên khi republish để ổn định `datePublished` |
| searchVector | tsvector | Có | Trigger/generated theo ADR-008 |
| nutrition | RecipeNutrition | Có | Owned columns |
| audit fields | — | — | Theo mục 3; version là aggregate ETag |

## 8. RecipeNutrition

Tất cả field nullable decimal, không âm, tính trên một serving:

| JSON field | DB column | Unit |
|---|---|---|
| calories | Nutrition_Calories decimal(8,2) | kcal |
| protein | Nutrition_Protein decimal(8,2) | g |
| carbohydrates | Nutrition_Carbohydrates decimal(8,2) | g |
| fat | Nutrition_Fat decimal(8,2) | g |
| fiber | Nutrition_Fiber decimal(8,2) | g |
| sodium | Nutrition_Sodium decimal(8,2) | mg |

## 9. RecipeIngredient

| Field | Type/length | Null | Validation/constraint |
|---|---|:---:|---|
| id | uuid | Không | PK |
| recipeId | uuid | Không | FK Recipe |
| name | varchar(200) | Không | 1–200 |
| quantity | decimal(10,3) | Có | >0 nếu có; null biểu thị “vừa đủ” |
| unit | varchar(50) | Có | Trim; có thể null khi quantity null |
| notes | varchar(500) | Có | Render escaped |
| orderIndex | int | Không | ≥0; unique không bắt buộc nhưng normalize khi reorder |
| audit fields | — | — | Child mutation tăng Recipe version |

## 10. RecipeStep

| Field | Type/length | Null | Validation/constraint |
|---|---|:---:|---|
| id | uuid | Không | PK |
| recipeId | uuid | Không | FK Recipe |
| stepNumber | int | Không | >0; unique `(recipeId, stepNumber)`; liên tục từ 1 |
| title | varchar(200) | Không | 1–200 |
| description | text | Không | 1–2.000 |
| timerMinutes | int | Có | ≥0 nếu có |
| imageUrl | varchar(500) | Có | Managed/safe URL |
| audit fields | — | — | Child mutation tăng Recipe version |

## 11. RecipeImage

| Field | Type/length | Null | Validation/constraint |
|---|---|:---:|---|
| id | uuid | Không | PK |
| recipeId | uuid | Không | FK Recipe |
| objectKey | varchar(500) | Không | Internal key, không do client chọn |
| originalUrl | varchar(500) | Có | Projection/delivery URL, không phải storage identity |
| mediumUrl | varchar(500) | Có | Pending cho đến khi job xong |
| thumbnailUrl | varchar(500) | Có | Pending cho đến khi job xong |
| altText | varchar(200) | Có | Nên bắt buộc trước publish |
| isPrimary | boolean | Không | Tối đa một active primary/recipe |
| orderIndex | int | Không | ≥0 |
| processingStatus | string | Không | `pending`, `ready`, `failed` |
| audit fields | — | — | Theo mục 3 |

## 12. OutboxMessage

| Field | Type | Null | Quy tắc |
|---|---|:---:|---|
| id | uuid | Không | Event/idempotency key |
| occurredAt | timestamptz | Không | Business event time |
| type | varchar(200) | Không | Versioned message type |
| payload | jsonb | Không | Không chứa secret/raw token |
| processedAt | timestamptz | Có | Null khi pending |
| attemptCount | int | Không | Default 0 |
| nextAttemptAt | timestamptz | Có | Backoff |
| error | text | Có | Sanitized diagnostic |

## 13. Pagination/query contract

- `page`: int, default 1, minimum 1.
- `pageSize`: int, default 12, 1–50.
- `sort`: allowlist; `-` prefix là descending; default `-createdAt`.
- Response `meta`: page, pageSize, total, totalPages, hasNextPage, hasPreviousPage.
- Recipe filters: categoryId, difficulty, maxCookTime, minServings, minPrepTime, maxPrepTime; kết hợp AND.
- Search `q`: trim/normalize, 2–100 ký tự.
