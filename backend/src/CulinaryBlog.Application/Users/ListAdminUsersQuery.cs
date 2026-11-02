using CulinaryBlog.Application.Content;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Users;

public sealed record ListAdminUsersQuery(string? Search, string? Role, int Page = 1, int PageSize = 20)
    : IRequest<PageEnvelope<AdminUserDto>>;

internal sealed class ListAdminUsersQueryValidator : AbstractValidator<ListAdminUsersQuery>
{
    public ListAdminUsersQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Role).Must(role => role is null or "Admin" or "Author");
        RuleFor(query => query.Page).GreaterThan(0)
            .Must((query, page) => (long)(page - 1) * query.PageSize <= int.MaxValue);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

internal sealed class ListAdminUsersQueryHandler(IAdminUserDirectory directory)
    : IRequestHandler<ListAdminUsersQuery, PageEnvelope<AdminUserDto>>
{
    public async Task<PageEnvelope<AdminUserDto>> Handle(ListAdminUsersQuery request, CancellationToken cancellationToken)
    {
        if (!await directory.IsCurrentUserAdminAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new ContentProblemException("FORBIDDEN", "Only active administrators can list users.", ContentProblemKind.Forbidden);
        }

        return await directory.ListAsync(request.Search?.Trim(), request.Role, request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
    }
}
