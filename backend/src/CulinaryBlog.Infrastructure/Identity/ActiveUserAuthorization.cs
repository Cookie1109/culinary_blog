using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CulinaryBlog.Infrastructure.Identity;

public sealed class ActiveUserRequirement : IAuthorizationRequirement;

internal sealed class ActiveUserAuthorizationHandler(UserManager<ApplicationUser> userManager)
    : AuthorizationHandler<ActiveUserRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (Guid.TryParse(userId, out var id))
        {
            var user = await userManager.FindByIdAsync(id.ToString()).ConfigureAwait(false);
            if (user?.IsActive == true)
            {
                context.Succeed(requirement);
            }
        }
    }
}
