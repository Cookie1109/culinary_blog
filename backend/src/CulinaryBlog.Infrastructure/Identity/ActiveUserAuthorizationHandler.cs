// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the active user authorization handler.</summary>
/// <param name="userManager">The user manager supplied to this operation.</param>
internal sealed class ActiveUserAuthorizationHandler(UserManager<ApplicationUser> userManager)
    : AuthorizationHandler<ActiveUserRequirement>
{
    /// <summary>Handles requirement.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="requirement">The requirement supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var user = await userManager.FindByIdAsync(userId).ConfigureAwait(false);
            if (user?.IsActive == true)
            {
                context.Succeed(requirement);
            }
        }
    }
}
