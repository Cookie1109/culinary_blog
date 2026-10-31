// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Audit;

/// <summary>Carries audit logs envelope data.</summary>
/// <param name="Data">The data supplied to this operation.</param>
/// <param name="Meta">The meta supplied to this operation.</param>
/// <param name="Telemetry">The telemetry supplied to this operation.</param>
public sealed record AuditLogsEnvelope(
    IReadOnlyCollection<AuditLogEntryDto> Data,
    PageMeta Meta,
    TelemetryStatusDto Telemetry);
