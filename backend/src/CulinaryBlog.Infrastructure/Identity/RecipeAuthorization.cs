using System.Security.Claims;
using CulinaryBlog.Domain.Recipes;
using Microsoft.AspNetCore.Authorization;

namespace CulinaryBlog.Infrastructure.Identity;

public sealed class RecipeOwnerRequirement : IAuthorizationRequirement;

internal sealed class RecipeAuthorizationHandler : AuthorizationHandler<RecipeOwnerRequirement, Recipe>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RecipeOwnerRequirement requirement,
        Recipe recipe)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (Guid.TryParse(id, out var userId) &&
            (recipe.AuthorId == userId || context.User.IsInRole("Admin")))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
