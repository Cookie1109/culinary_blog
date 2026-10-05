# Changelog

Tất cả thay đổi đáng chú ý của Culinary Blog được ghi tại đây. Dự án dùng Semantic Versioning; ngày phát hành chỉ được điền sau production smoke thành công.

## [Unreleased]

### Release gate

- Chờ ZAP trên staging HTTPS, browser UAT và Product Owner sign-off.
- Chờ go/no-go, production deployment và theo dõi tăng cường 24–48 giờ.

## [1.0.0-rc.1] - 2026-10-05

### Added

- Vòng đời xác thực local, refresh-token rotation/replay protection và phân quyền Guest/Author/Admin.
- Category, Recipe Draft/composition/media, publish/archive/delete, tìm kiếm tiếng Việt và public discovery.
- Next.js public web, dashboard Author/Admin, SEO/JSON-LD/sitemap, responsive và accessibility coverage.
- OpenTelemetry/Prometheus/Grafana/Alertmanager, health probes, audit logs, backup/restore và failure drills.
- Immutable release images, provenance/SBOM, production Compose/TLS Nginx, artifact promotion gate và release evidence.

### Security

- Production fail-fast cho secret/host/origin/release identity; CORS allowlist, TLS 1.2+, HSTS, CSP và rate limits.
- Dependency, container, secret, license và local dynamic scans không còn Critical/High mở.

### Known deviations

- Google OAuth là Should-have và chưa thuộc release candidate hiện tại.
- Welcome email dùng transactional outbox hosted worker thay Hangfire; retry/observability vẫn được giữ theo deviation đã duyệt.

[Unreleased]: docs/release/v1.0.0-release-notes.md
[1.0.0-rc.1]: docs/release/v1.0.0-release-notes.md

