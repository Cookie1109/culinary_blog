# Phase 0 — Baseline đặc tả và kiến trúc

> Nguồn: [Kế hoạch dự án](../Culinary_Blog_Project_Plan.md) và [Phân tích SRS](../SRS_Culinary_Blog_Analysis.md)  
> Ngày chuẩn bị: 09/09/2026  
> Ngày chốt baseline: 11/09/2026  
> Trạng thái tổng thể: **Accepted for implementation**

## Mục đích

Thư mục này chứa toàn bộ đầu ra Phase 0 đã hợp nhất từ SRS. `00_SRS_Conformance_and_Resolution.md` là tài liệu ưu tiên khi hai đoạn trong PDF tự mâu thuẫn; các yêu cầu PDF không mâu thuẫn vẫn bắt buộc nguyên vẹn.

## Danh mục đầu ra

| Hạng mục | Tệp | Công việc Phase 0 | Trạng thái |
|---|---|---|---|
| SRS conformance và giải mâu thuẫn | [00_SRS_Conformance_and_Resolution.md](./00_SRS_Conformance_and_Resolution.md) | GAP-001–033 | Accepted |
| Phạm vi, FR và backlog v1 | [01_Product_Baseline.md](./01_Product_Baseline.md) | P0-01–P0-05 | Accepted |
| Decision log GAP-001–033 | [02_Decision_Log.md](./02_Decision_Log.md) | P0-01–P0-15 | Accepted |
| Data dictionary | [03_Data_Dictionary.md](./03_Data_Dictionary.md) | P0-15 | Accepted |
| OpenAPI skeleton | [openapi.v1.yaml](./openapi.v1.yaml) | P0-16–P0-19 | Accepted |
| Error catalog | [04_Error_Catalog.md](./04_Error_Catalog.md) | P0-17–P0-18 | Accepted |
| Route map và wireframe | [05_Routes_and_Wireframes.md](./05_Routes_and_Wireframes.md) | P0-20 | Accepted |
| Traceability matrix | [06_Traceability_Matrix.md](./06_Traceability_Matrix.md) | P0-21 | Accepted |
| Acceptance criteria | [07_Critical_Journey_Acceptance.md](./07_Critical_Journey_Acceptance.md) | P0-22 | Accepted |
| Threat model | [08_Threat_Model.md](./08_Threat_Model.md) | P0-23 | Accepted |
| Test strategy | [09_Test_Strategy.md](./09_Test_Strategy.md) | Phase 0 deliverable | Accepted |
| 9 Architecture Decision Records | [adr/](./adr/) | P0-06–P0-14 | Accepted |

## ADR

| ADR | Quyết định | Trạng thái |
|---|---|---|
| [ADR-001](./adr/ADR-001-soft-delete-and-retention.md) | Soft delete, không hard purge trong v1 | Accepted |
| [ADR-002](./adr/ADR-002-http-status-and-errors.md) | HTTP status và error contract | Accepted |
| [ADR-003](./adr/ADR-003-postgresql-concurrency.md) | Concurrency với `Version bigint` | Accepted |
| [ADR-004](./adr/ADR-004-refresh-token-security.md) | Refresh token body/sessionStorage | Accepted |
| [ADR-005](./adr/ADR-005-google-oauth.md) | Google OAuth flow | Accepted |
| [ADR-006](./adr/ADR-006-object-visibility.md) | MinIO private/public delivery | Accepted |
| [ADR-007](./adr/ADR-007-distributed-cache.md) | Redis-backed cache | Accepted |
| [ADR-008](./adr/ADR-008-vietnamese-full-text-search.md) | FTS tiếng Việt | Accepted |
| [ADR-009](./adr/ADR-009-transactional-outbox.md) | Transactional Outbox | Accepted |

## Kết quả thực hiện P0-01–P0-23

| Task | Kết quả |
|---|---|
| P0-01 | Baseline công nhận 34 FR; cách đếm được ghi trong Product Baseline |
| P0-02 | Các chức năng thiếu đã phân loại Must/Should/Post-v1 |
| P0-03 | Publish invariant đã chốt: ingredient + step, không bắt buộc ảnh |
| P0-04 | Soft delete đã chốt; không hard purge/restore trong v1 |
| P0-05 | Author tự publish trong v1; moderation ngoài scope |
| P0-06–P0-14 | Đã tạo và chốt 9 ADR ở trạng thái Accepted |
| P0-15 | Data dictionary đã chuẩn hóa |
| P0-16 | OpenAPI v1 skeleton đã tạo |
| P0-17 | Response/error contract đã định nghĩa |
| P0-18 | Application error catalog đã tạo |
| P0-19 | Public recipe API được tách khỏi `/me/recipes` |
| P0-20 | Route map và low-fidelity wireframes đã tạo |
| P0-21 | Traceability matrix theo module/FR đã tạo |
| P0-22 | 5 critical journeys có acceptance criteria |
| P0-23 | STRIDE-oriented threat model và security acceptance đã tạo |

## Review khi thay đổi baseline

Mọi Change Request phải được Product Owner review phần scope/business rule, Technical Lead review ADR/data/API, Frontend review browser contract và QA review traceability/acceptance. Không sửa trực tiếp code contract nếu chưa cập nhật bộ tài liệu này.

## Sign-off

| Vai trò | Người duyệt | Quyết định | Ngày | Ghi chú |
|---|---|---|---|---|
| Project directive | Chủ dự án | Accepted | 11/09/2026 | Cho phép chốt tối ưu các mâu thuẫn PDF |
| Product Owner | _Chưa gán tên_ | Baseline effective | 11/09/2026 | Owner cho thay đổi business tiếp theo |
| Technical Lead | _Chưa gán tên_ | Baseline effective | 11/09/2026 | Owner cho thay đổi kiến trúc tiếp theo |
| Frontend/QA | _Chưa gán tên_ | Baseline effective | 11/09/2026 | Thực thi contract và gates đã chốt |

## Exit gate Phase 0

- [x] Mọi GAP-001–033 có quyết định, vị trí PDF và lý do.
- [x] Đã tạo request/response/error contract để review.
- [x] Đã tạo ADR cho soft delete, concurrency, token, Google, FTS, cache, object visibility và jobs.
- [x] Must-have backlog có acceptance criteria ở mức critical journey.
- [x] Baseline v1 có hiệu lực theo chỉ đạo chuẩn hóa ngày 11/09/2026.
- [x] Contract, schema, threat model và test strategy đã đồng bộ.

Phase 0 được xem là **Closed**. Tên cá nhân giữ trống không làm vô hiệu baseline; khi team được thành lập, người nhận vai trò chịu trách nhiệm quản lý Change Request tiếp theo.
