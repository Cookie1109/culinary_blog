// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Users;
using MediatR;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the admin users endpoints.</summary>
internal static class AdminUsersEndpoints
{
    /// <summary>Maps admin users endpoints.</summary>
    /// <param name="endpoints">The endpoints supplied to this operation.</param>
    /// <returns>The map admin users endpoints result.</returns>
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/admin/users", ListAsync)
            .RequireAuthorization("AdminPolicy")
            .WithTags("Admin")
            .WithName("ListAdminUsers")
            .WithSummary("List users (Admin)")
            .WithDescription("Private user directory. Search matches literal text in name/email (max 100 characters). Role is Admin or Author. Page defaults to 1; pageSize defaults to 20 and ranges from 1 to 100. Ordered by createdAt descending, then id. Users with multiple roles appear once with all their roles.")
            .Produces<PageEnvelope<AdminUserDto>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Response.Headers.CacheControl = "private, no-store";
                return await next(context).ConfigureAwait(false);
            });
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        CancellationToken cancellationToken,
        string? search = null,
        string? role = null,
        int page = 1,
        int pageSize = 20) =>
        Results.Ok(await sender.Send(new ListAdminUsersQuery(search, role, page, pageSize), cancellationToken).ConfigureAwait(false));
}
