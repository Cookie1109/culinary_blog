// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Features.Recipes.Commands;
using FluentValidation;

namespace CulinaryBlog.Application.Content;

/// <summary>Represents the request metadata validator.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
internal sealed class RequestMetadataValidator<TRequest> : AbstractValidator<TRequest>
{
    /// <summary>Initializes a new instance of the <see cref="RequestMetadataValidator{TRequest}"/> class.</summary>
    public RequestMetadataValidator()
    {
        this.RuleFor(request => request).Custom((value, context) =>
        {
            (IReadOnlyCollection<(string Field, Guid Value)> Ids, long? Version) metadata = value switch
            {
                UpdateCategoryCommand request => ([("Id", request.Id)], (long?)null),
                DeleteCategoryCommand request => ([("Id", request.Id)], (long?)null),
                GetMyRecipeQuery request => ([("Id", request.Id)], (long?)null),
                GetAdminRecipeQuery request => ([("Id", request.Id)], (long?)null),
                OpenMediaQuery request => ([("ImageId", request.ImageId)], (long?)null),
                CreateIngredientCommand request => ([("RecipeId", request.RecipeId)], request.ExpectedVersion),
                UpdateIngredientCommand request => ([("RecipeId", request.RecipeId), ("IngredientId", request.IngredientId)], request.ExpectedVersion),
                DeleteIngredientCommand request => ([("RecipeId", request.RecipeId), ("IngredientId", request.IngredientId)], request.ExpectedVersion),
                CreateStepCommand request => ([("RecipeId", request.RecipeId)], request.ExpectedVersion),
                UpdateStepCommand request => ([("RecipeId", request.RecipeId), ("StepId", request.StepId)], request.ExpectedVersion),
                DeleteStepCommand request => ([("RecipeId", request.RecipeId), ("StepId", request.StepId)], request.ExpectedVersion),
                UploadImageCommand request => ([("RecipeId", request.RecipeId)], request.ExpectedVersion),
                UpdateImageCommand request => ([("RecipeId", request.RecipeId), ("ImageId", request.ImageId)], request.ExpectedVersion),
                DeleteImageCommand request => ([("RecipeId", request.RecipeId), ("ImageId", request.ImageId)], request.ExpectedVersion),
                UpdateRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                PublishRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                UnpublishRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                ArchiveRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                UnarchiveRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                DeleteRecipeCommand request => ([("Id", request.Id)], request.ExpectedVersion),
                _ => ([], null),
            };
            foreach (var (field, id) in metadata.Ids)
            {
                if (id == Guid.Empty)
                {
                    context.AddFailure(field, $"{field} must not be empty.");
                }
            }

            if (metadata.Version is <= 0)
            {
                context.AddFailure("ExpectedVersion", "ExpectedVersion must be greater than zero.");
            }
        });
    }
}
