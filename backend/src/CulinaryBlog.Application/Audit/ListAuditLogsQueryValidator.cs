// Culinary Blog. Authorship is recorded in the repository history.

using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Audit;

/// <summary>Represents the list audit logs query validator.</summary>
internal sealed class ListAuditLogsQueryValidator : AbstractValidator<ListAuditLogsQuery>
{
    /// <summary>Initializes a new instance of the <see cref="ListAuditLogsQueryValidator"/> class.</summary>
    public ListAuditLogsQueryValidator()
    {
        this.RuleFor(request => request.Page).GreaterThan(0);
        this.RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        this.RuleFor(request => request.Search).MaximumLength(100);
    }
}
