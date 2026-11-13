// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Users;

/// <summary>Carries list admin users query data.</summary>
/// <param name="Search">The search supplied to this operation.</param>
/// <param name="Role">The role supplied to this operation.</param>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
public sealed record ListAdminUsersQuery(string? Search, string? Role, int Page = 1, int PageSize = 20)
    : IRequest<PageEnvelope<AdminUserDto>>;
