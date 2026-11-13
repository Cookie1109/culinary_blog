// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Audit;

/// <summary>Carries audit log entry dto data.</summary>
/// <param name="Id">The identifier of the requested resource.</param>
/// <param name="Timestamp">The timestamp supplied to this operation.</param>
/// <param name="Level">The level supplied to this operation.</param>
/// <param name="EventName">The event name supplied to this operation.</param>
/// <param name="Message">The message supplied to this operation.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="CorrelationId">The correlation id supplied to this operation.</param>
/// <param name="RequestPath">The request path supplied to this operation.</param>
/// <param name="RequestMethod">The request method supplied to this operation.</param>
/// <param name="StatusCode">The status code supplied to this operation.</param>
/// <param name="Properties">The properties supplied to this operation.</param>
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
