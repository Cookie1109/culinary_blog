using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Presentation;

internal sealed class RecipeOwnershipFilter(AppDbContext dbContext, IAuthorizationService authorizationService)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var id = Guid.Parse(context.HttpContext.Request.RouteValues["id"]!.ToString()!);
        var recipe = await dbContext.Recipes.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, context.HttpContext.RequestAborted)
            .ConfigureAwait(false)
            ?? throw new ContentProblemException("RECIPE_NOT_FOUND", "Recipe was not found.", ContentProblemKind.NotFound);
        var result = await authorizationService.AuthorizeAsync(
            context.HttpContext.User, recipe, "RecipeOwnerPolicy").ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new ContentProblemException("FORBIDDEN", "You cannot edit this recipe.", ContentProblemKind.Forbidden);
        }

        return await next(context).ConfigureAwait(false);
    }
}
