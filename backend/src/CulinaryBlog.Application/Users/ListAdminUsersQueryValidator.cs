// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using FluentValidation;
using MediatR;

namespace CulinaryBlog.Application.Users;

/// <summary>Represents the list admin users query validator.</summary>
internal sealed class ListAdminUsersQueryValidator : AbstractValidator<ListAdminUsersQuery>
{
    /// <summary>Initializes a new instance of the <see cref="ListAdminUsersQueryValidator"/> class.</summary>
    public ListAdminUsersQueryValidator()
    {
        this.RuleFor(query => query.Search).MaximumLength(100);
        this.RuleFor(query => query.Role).Must(role => role is null or "Admin" or "Author");
        this.RuleFor(query => query.Page).GreaterThan(0)
            .Must((query, page) => (long)(page - 1) * query.PageSize <= int.MaxValue);
        this.RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
