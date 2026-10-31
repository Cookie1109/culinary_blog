// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the http application actor.</summary>
/// <param name="accessor">The accessor supplied to this operation.</param>
/// <param name="users">The users supplied to this operation.</param>
internal sealed class HttpApplicationActor(IHttpContextAccessor accessor, UserManager<ApplicationUser> users) : IApplicationActor
{
    /// <summary>Gets the http application actor.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<ApplicationActor?> GetAsync(CancellationToken cancellationToken)
    {
        var principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var id = principal.FindFirst("sub")?.Value ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = id is null ? null : await users.FindByIdAsync(id).ConfigureAwait(false);
        return user is null ? null : new ApplicationActor(user.Id.ToString(), user.IsActive, (await users.GetRolesAsync(user).ConfigureAwait(false)).ToArray());
    }
}
