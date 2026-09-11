# Application Error Catalog v1

> Trạng thái: Accepted for implementation  
> Error body: RFC Problem Details `application/problem+json`  
> `type` dùng URI ổn định `https://culinaryblog.local/problems/{code}`; `code` lặp lại riêng để client dùng thuận tiện.
> Quy tắc 400/409 đã được chốt từ các mâu thuẫn ở Chương 3, Chương 8 và Phụ lục A-B của PDF; xem [00_SRS_Conformance_and_Resolution.md](./00_SRS_Conformance_and_Resolution.md).

## 1. Schema chuẩn

```json
{
  "type": "https://culinaryblog.local/problems/VALIDATION_ERROR",
  "title": "Request validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/recipes",
  "code": "VALIDATION_ERROR",
  "errors": {
    "title": ["Title must contain 5 to 200 characters."]
  },
  "correlationId": "...",
  "traceId": "..."
}
```

`detail` có thể localize và không được client dùng làm branching logic. Production không trả stack trace, SQL, object key nội bộ hoặc token.

## 2. Common

| Code | HTTP | Khi dùng |
|---|---:|---|
| VALIDATION_ERROR | 400 | Field/query/body validation |
| MALFORMED_REQUEST | 400 | JSON/multipart không parse được |
| PRECONDITION_REQUIRED | 400 | Thiếu `If-Match` trên mutation yêu cầu concurrency |
| CONCURRENCY_CONFLICT | 409 | ETag/version stale |
| RATE_LIMIT_EXCEEDED | 429 | Vượt rate limit; có `Retry-After` |
| RESOURCE_NOT_FOUND | 404 | Resource chung không tồn tại/đã soft-delete |
| INTERNAL_ERROR | 500 | Unhandled; message generic |
| DEPENDENCY_UNAVAILABLE | 503 | Dependency bắt buộc tạm unavailable |

## 3. Authentication/User

| Code | HTTP | Khi dùng |
|---|---:|---|
| AUTH_EMAIL_EXISTS | 409 | Email đã đăng ký |
| AUTH_INVALID_CREDENTIALS | 401 | Email/password sai, không phân biệt trường hợp |
| AUTH_ACCOUNT_LOCKED | 423 | Lockout còn hiệu lực |
| AUTH_ACCOUNT_DISABLED | 403 | `IsActive=false` |
| AUTH_TOKEN_MISSING | 401 | Thiếu access token |
| AUTH_TOKEN_EXPIRED | 401 | Access token hết hạn |
| AUTH_TOKEN_INVALID | 401 | JWT signature/issuer/audience/format sai |
| AUTH_REFRESH_TOKEN_EXPIRED | 401 | Refresh token hết hạn |
| AUTH_REFRESH_TOKEN_REVOKED | 401 | Token đã revoke/reuse; family bị revoke |
| AUTH_REFRESH_TOKEN_INVALID | 401 | Token không tìm thấy/không hợp lệ |
| AUTH_GOOGLE_TOKEN_INVALID | 400 | Google ID token invalid/expired/audience sai |
| AUTH_GOOGLE_EMAIL_UNVERIFIED | 400 | Google email chưa verified |
| AUTH_EXTERNAL_ACCOUNT_CONFLICT | 409 | External identity không thể auto-link an toàn |
| USER_NOT_FOUND | 404 | User mục tiêu không tồn tại |

## 4. Category

| Code | HTTP | Khi dùng |
|---|---:|---|
| CATEGORY_NOT_FOUND | 404 | Category không tồn tại/deleted |
| CATEGORY_NAME_EXISTS | 409 | Active normalized name trùng |
| CATEGORY_SLUG_EXISTS | 409 | Slug collision không resolve được |
| CATEGORY_DELETE_HAS_RECIPES | 409 | Category còn recipe theo retention policy |

## 5. Recipe và child resource

| Code | HTTP | Khi dùng |
|---|---:|---|
| RECIPE_NOT_FOUND | 404 | Recipe không tồn tại/deleted/ẩn theo non-disclosure policy |
| RECIPE_FORBIDDEN | 403 | Authenticated nhưng không phải owner/Admin |
| RECIPE_SLUG_EXISTS | 409 | Slug conflict |
| RECIPE_PUBLISH_INCOMPLETE | 400 | Thiếu ingredient/step hoặc dữ liệu Recipe bắt buộc |
| RECIPE_INVALID_TRANSITION | 409 | Chuyển trạng thái không hợp lệ |
| RECIPE_STATE_CONFLICT | 409 | Mutation làm Published recipe vi phạm invariant |
| RECIPE_CONCURRENCY_CONFLICT | 409 | Recipe aggregate version stale |
| INGREDIENT_NOT_FOUND | 404 | Ingredient không thuộc recipe/không tồn tại |
| STEP_NOT_FOUND | 404 | Step không thuộc recipe/không tồn tại |
| STEP_NUMBER_CONFLICT | 409 | StepNumber duplicate/renumber conflict |
| IMAGE_NOT_FOUND | 404 | Image không thuộc recipe/không tồn tại |
| IMAGE_PRIMARY_CONFLICT | 409 | Không thể bảo đảm đúng một primary |

## 6. File/Job/Search

| Code | HTTP | Khi dùng |
|---|---:|---|
| FILE_SIZE_EXCEEDED | 400 | File >5 MB |
| FILE_MIME_INVALID | 400 | MIME ngoài allowlist |
| FILE_SIGNATURE_INVALID | 400 | Magic bytes/decode không khớp |
| FILE_UPLOAD_FAILED | 503 | Storage unavailable/upload fail |
| FILE_PROCESSING_FAILED | 500 | Async resize failed; thường hiển thị qua resource state thay vì HTTP request |
| SEARCH_QUERY_INVALID | 400 | `q` rỗng, <2 hoặc quá dài |
| SORT_FIELD_INVALID | 400 | Sort field không thuộc allowlist |

## 7. Mapping rule

- 404 vs 403 cho private Recipe: endpoint biết ID và user đã authenticated có thể trả 403 theo SRS; public slug endpoint luôn trả 404 cho non-Published để tránh disclosure.
- DB unique violation phải map về code domain cụ thể, không trả raw constraint name.
- Timeout/Redis outage không mặc định trả 503 nếu có thể fallback an toàn.
- `errors` chỉ có cho field validation; conflict dùng structured extensions như `currentVersion` khi không làm lộ dữ liệu.
