using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Audit;

public sealed record AuditLogEntryDto(
    string Id,
    DateTimeOffset Timestamp,
    string Level,
    string EventName,
    string Message,
    string? UserId,
    string? CorrelationId,
    string? RequestPath,
    string? RequestMethod,
    int? StatusCode,
    IReadOnlyDictionary<string, string>? Properties);

public sealed record TelemetryStatusDto(
    bool SeqConfigured,
    bool OtlpConfigured,
    bool OperationalNetworkRestricted);

public sealed record AuditLogsEnvelope(
    IReadOnlyCollection<AuditLogEntryDto> Data,
    PageMeta Meta,
    TelemetryStatusDto Telemetry);

public interface IAuditLogService
{
    Task<AuditLogsEnvelope> ListAuditLogsAsync(
        string? level,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
