// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using MediatR;

namespace CulinaryBlog.Application.Behaviors;

/// <summary>Represents the authorization behavior.</summary>
/// <param name="currentActor">The current actor supplied to this operation.</param>
/// <param name="unitOfWork">The unit of work supplied to this operation.</param>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class AuthorizationBehavior<TRequest, TResponse>(IApplicationActor currentActor, IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <summary>Handles the authorization behavior.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="next">The next supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var rule = request switch
        {
            CreateCategoryCommand r => new AccessRule(Admin: true, ClaimedAdmin: r.IsAdmin),
            UpdateCategoryCommand r => new AccessRule(Admin: true, ClaimedAdmin: r.IsAdmin),
            DeleteCategoryCommand r => new AccessRule(Admin: true, ClaimedAdmin: r.IsAdmin),
            GetAdminRecipeQuery => new AccessRule(Admin: true),
            ListAdminRecipesQuery => new AccessRule(Admin: true),
            ListAuditLogsQuery => new AccessRule(Admin: true),
            GetCurrentUserQuery r => new AccessRule(UserId: r.UserId),
            UpdateProfileCommand r => new AccessRule(UserId: r.UserId),
            ChangePasswordCommand r => new AccessRule(UserId: r.UserId),
            GetMyRecipeQuery r => new AccessRule(UserId: r.UserId.ToString(), RecipeId: r.Id, OwnerOnly: true),
            ListMyRecipesQuery r => new AccessRule(UserId: r.UserId.ToString()),
            CreateRecipeCommand r => new AccessRule(UserId: r.UserId.ToString()),
            UpdateRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            DeleteRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            PublishRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            UnpublishRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            ArchiveRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            UnarchiveRecipeCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.Id),
            CreateIngredientCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            UpdateIngredientCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            DeleteIngredientCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            CreateStepCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            UpdateStepCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            DeleteStepCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            UploadImageCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            UpdateImageCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            DeleteImageCommand r => new AccessRule(UserId: r.UserId.ToString(), ClaimedAdmin: r.IsAdmin, RecipeId: r.RecipeId),
            _ => null,
        };
        if (rule is not null)
        {
            var actor = await currentActor.GetAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new AuthProblemException("AUTH_TOKEN_MISSING", "Authentication is required.");
            var admin = actor.Roles.Contains("Admin");
            if (!actor.IsActive || (!admin && !actor.Roles.Contains("Author")) ||
                ((rule.Admin || rule.ClaimedAdmin) && !admin) ||
                (rule.UserId is not null && rule.UserId != actor.Id))
            {
                throw new ContentProblemException("FORBIDDEN", "Access denied.", ContentProblemKind.Forbidden);
            }

            if (rule.RecipeId.HasValue)
            {
                var recipe = await unitOfWork.Recipes.GetByIdAsync(rule.RecipeId.Value, cancellationToken).ConfigureAwait(false)
                    ?? throw new ContentProblemException("RECIPE_NOT_FOUND", "Recipe was not found.", ContentProblemKind.NotFound);
                if ((rule.OwnerOnly || !admin) && recipe.AuthorId.ToString() != actor.Id)
                {
                    throw new ContentProblemException("FORBIDDEN", "You cannot edit this recipe.", ContentProblemKind.Forbidden);
                }
            }
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }

    private sealed record AccessRule(bool Admin = false, string? UserId = null, bool ClaimedAdmin = false, Guid? RecipeId = null, bool OwnerOnly = false);
}
