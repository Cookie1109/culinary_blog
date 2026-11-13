// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Audit;

/// <summary>Carries list audit logs query data.</summary>
/// <param name="Level">The level supplied to this operation.</param>
/// <param name="Search">The search supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
public sealed record ListAuditLogsQuery(string? Level, string? Search, int Page, int PageSize) : IRequest<AuditLogsEnvelope>;
