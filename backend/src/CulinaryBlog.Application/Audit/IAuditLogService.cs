// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Audit;

/// <summary>Defines the audit log service contract.</summary>
public interface IAuditLogService
{
    /// <summary>Lists audit logs.</summary>
    /// <param name="level">The level supplied to this operation.</param>
    /// <param name="search">The search supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<AuditLogsEnvelope> ListAuditLogsAsync(
        string? level,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
