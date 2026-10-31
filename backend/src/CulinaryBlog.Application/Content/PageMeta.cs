// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Carries page meta data.</summary>
/// <param name="Page">The page supplied to this operation.</param>
/// <param name="PageSize">The page size supplied to this operation.</param>
/// <param name="Total">The total supplied to this operation.</param>
/// <param name="TotalPages">The total pages supplied to this operation.</param>
/// <param name="HasNextPage">The has next page supplied to this operation.</param>
/// <param name="HasPreviousPage">The has previous page supplied to this operation.</param>
public sealed record PageMeta(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);
