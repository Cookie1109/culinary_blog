# ADR-008: Full-text search tiếng Việt

- Trạng thái: Accepted
- Ngày: 09/09/2026
- Owner: Technical Lead
- Liên quan: GAP-016

## Bối cảnh

PostgreSQL chuẩn không bảo đảm có text-search configuration `vietnamese`; SRS yêu cầu tìm có/không dấu và prefix matching.

## Quyết định

- V1 dùng PostgreSQL `simple` configuration kết hợp `unaccent` normalization.
- Dùng trigger-maintained search vector từ Title có trọng số cao hơn Description, đúng phạm vi FR-SRCH-001.
- GIN index trên search vector.
- Prefix query được tạo từ token đã normalize/sanitize; không ghép raw query string thành SQL.
- `pg_trgm` dùng cho fallback/fuzzy title matching và suggestion, có threshold đo bằng test corpus.
- Custom Vietnamese dictionary là cải tiến Post-v1 trừ khi spike chứng minh bắt buộc.

## Hệ quả

- Không stemming tiếng Việt sâu nhưng deployment tái lập và đáp ứng tìm không dấu.
- Cần corpus có dấu/không dấu/typo và EXPLAIN ANALYZE trước release.

## Tiêu chí xác minh

- “pho” tìm “phở”, “bun bo” tìm “bún bò” theo relevance acceptance.
- Query đặc biệt không tạo syntax/SQL injection.
- Search query chính dùng index trên dataset chuẩn.
