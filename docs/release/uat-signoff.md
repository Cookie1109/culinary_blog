# UAT và defect sign-off — v1.0.0

> Trạng thái: **Pending**. Tài liệu mẫu không phải là chữ ký. Product Owner phải thực hiện/ủy quyền UAT trên staging và điền đầy đủ định danh artifact.

## Session identity

| Trường | Giá trị |
|---|---|
| Staging URL | Chờ điền |
| Git commit SHA | Chờ điền |
| API image digest | Chờ điền |
| Web image digest | Chờ điền |
| `staging-deployment.json` checksum/link | Chờ điền |
| Test window UTC | Chờ điền |
| Product Owner / QA lead | Chờ điền |

Trước UAT, đối chiếu `/api/v1/release` với release bundle. Nếu lệch bất kỳ trường nào, dừng session và deploy lại đúng artifact.

## Must-have journeys

| ID | Journey/acceptance | Kết quả | Evidence/defect |
|---|---|---|---|
| CJ-01 | Guest đăng ký, tự đăng nhập, xem/cập nhật profile; welcome email lỗi không rollback user | Pending | |
| CJ-02 | Login, refresh rotation/replay protection, logout; lockout/rate limit không enumerate account | Pending | |
| CJ-03 | Author tạo Draft hoàn chỉnh với ingredient/step/image, validation và autosave/recovery | Pending | |
| CJ-04 | Publish → public listing/detail/search/category/sitemap/metadata; unpublish/archive biến mất khỏi public/cache | Pending | |
| CJ-05 | Author khác bị 403; owner đúng quyền; Admin override/audit; direct URL/API không bypass | Pending | |
| UX | Public/dashboard responsive; keyboard/focus/error/loading/empty states; không có blocker accessibility | Pending | |

## Defect register

| ID | Severity | Mô tả | Journey | Owner | Due date | Trạng thái / risk acceptance |
|---|---|---|---|---|---|---|
| — | — | Chưa ghi nhận | — | — | — | — |

Severity gate: Critical/High mở = No-Go. Medium chỉ được phép với owner, due date, workaround/impact và chữ ký risk acceptance của Product Owner + Technical Lead. Low có thể đưa backlog.

## Sign-off

- [ ] Tất cả Must-have pass trên đúng artifact.
- [ ] Defect register đã triage theo gate trên.
- [ ] Không dùng dữ liệu/PII thật trong staging.
- [ ] Product Owner chấp nhận phạm vi known deviations/release notes.

Decision: **Pending / Accept / Reject**

Product Owner: ______ · Signature/reference: ______ · UTC time: ______

QA Lead: ______ · Signature/reference: ______ · UTC time: ______

