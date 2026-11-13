// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Users;

/// <summary>Represents the list admin users query handler.</summary>
/// <param name="directory">The directory supplied to this operation.</param>
internal sealed class ListAdminUsersQueryHandler(IAdminUserDirectory directory)
    : IRequestHandler<ListAdminUsersQuery, PageEnvelope<AdminUserDto>>
{
    /// <summary>Handles the list admin users query handler.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
