// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Domain.Recipes;
using Microsoft.AspNetCore.Authorization;

namespace CulinaryBlog.Infrastructure.Identity;

/// <summary>Represents the recipe authorization handler.</summary>
internal sealed class RecipeAuthorizationHandler : AuthorizationHandler<RecipeOwnerRequirement, Recipe>
{
    /// <summary>Handles requirement.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="requirement">The requirement supplied to this operation.</param>
    /// <param name="resource">The resource supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RecipeOwnerRequirement requirement,
        Recipe resource)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        if (!string.IsNullOrWhiteSpace(id) &&
            (resource.AuthorId == id || context.User.IsInRole("Admin")))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
