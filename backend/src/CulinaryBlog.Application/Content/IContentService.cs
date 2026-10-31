// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.Application.Content;

/// <summary>Defines the content service contract.</summary>
public interface IContentService
{
    /// <summary>Lists categories.</summary>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken);

    /// <summary>Gets category.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <param name="sort">The sort supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<CategoryDetailEnvelope> GetCategoryAsync(string slug, int page, int pageSize, CancellationToken cancellationToken, string? sort = null);

    /// <summary>Creates category.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<CategoryDto> CreateCategoryAsync(CategoryWriteRequest request, CancellationToken cancellationToken);

    /// <summary>Updates category.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<CategoryDto> UpdateCategoryAsync(Guid id, CategoryWriteRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes category.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Creates recipe.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> CreateRecipeAsync(string userId, RecipeCreateRequest request, CancellationToken cancellationToken);

    /// <summary>Updates recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> UpdateRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        RecipeUpdateRequest request,
        CancellationToken cancellationToken);

    /// <summary>Publishes recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> PublishRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken);

    /// <summary>Unpublishes recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> UnpublishRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken);

    /// <summary>Archives recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> ArchiveRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken);

    /// <summary>Unarchives recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> UnarchiveRecipeAsync(
        Guid id,
        string userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken);

    /// <summary>Gets published recipe.</summary>
    /// <param name="slug">The slug supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> GetPublishedRecipeAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Lists published recipes.</summary>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<RecipeDto>> ListPublishedRecipesAsync(int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Gets my recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> GetMyRecipeAsync(Guid id, string userId, CancellationToken cancellationToken);

    /// <summary>Lists my recipes.</summary>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="status">The status supplied to this operation.</param>
    /// <param name="q">The q supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<RecipeDto>> ListMyRecipesAsync(
        string userId,
        RecipeStatus? status,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Gets admin recipe.</summary>
    /// <param name="id">The identifier of the requested resource.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<RecipeDto> GetAdminRecipeAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists admin recipes.</summary>
    /// <param name="status">The status supplied to this operation.</param>
    /// <param name="authorId">The identifier of the user performing or owning the operation.</param>
    /// <param name="q">The q supplied to this operation.</param>
    /// <param name="page">The page supplied to this operation.</param>
    /// <param name="pageSize">The page size supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<PageEnvelope<RecipeDto>> ListAdminRecipesAsync(
        RecipeStatus? status,
        string? authorId,
        string? q,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Creates ingredient.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<IngredientDto>> CreateIngredientAsync(
        Guid recipeId, string userId, bool isAdmin, long expectedVersion, IngredientWriteRequest request, CancellationToken cancellationToken);

    /// <summary>Updates ingredient.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="ingredientId">The ingredient id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<IngredientDto>> UpdateIngredientAsync(
        Guid recipeId, Guid ingredientId, string userId, bool isAdmin, long expectedVersion, IngredientUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes ingredient.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="ingredientId">The ingredient id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<long> DeleteIngredientAsync(
        Guid recipeId, Guid ingredientId, string userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    /// <summary>Creates step.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<StepDto>> CreateStepAsync(
        Guid recipeId, string userId, bool isAdmin, long expectedVersion, StepWriteRequest request, CancellationToken cancellationToken);

    /// <summary>Updates step.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="stepId">The step id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<StepDto>> UpdateStepAsync(
        Guid recipeId, Guid stepId, string userId, bool isAdmin, long expectedVersion, StepUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes step.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="stepId">The step id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<long> DeleteStepAsync(
        Guid recipeId, Guid stepId, string userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    /// <summary>Uploads image.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="content">The content supplied to this operation.</param>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="contentType">The content type supplied to this operation.</param>
    /// <param name="altText">The alt text supplied to this operation.</param>
    /// <param name="isPrimary">The is primary supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<RecipeImageDto>> UploadImageAsync(
        Guid recipeId,
        string userId,
        bool isAdmin,
        long expectedVersion,
        Stream content,
        long length,
        string contentType,
        string? altText,
        bool isPrimary,
        CancellationToken cancellationToken);

    /// <summary>Updates image.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="imageId">The image id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<ChildMutationDto<RecipeImageDto>> UpdateImageAsync(
        Guid recipeId, Guid imageId, string userId, bool isAdmin, long expectedVersion, ImageMetadataRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes image.</summary>
    /// <param name="recipeId">The identifier of the recipe being accessed.</param>
    /// <param name="imageId">The image id supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="expectedVersion">The recipe version used to reject concurrent changes.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<long> DeleteImageAsync(
        Guid recipeId, Guid imageId, string userId, bool isAdmin, long expectedVersion, CancellationToken cancellationToken);

    /// <summary>Performs the open media operation.</summary>
    /// <param name="imageId">The image id supplied to this operation.</param>
    /// <param name="variant">The variant supplied to this operation.</param>
    /// <param name="userId">The identifier of the user performing or owning the operation.</param>
    /// <param name="isAdmin">The is admin supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    Task<MediaFileDto> OpenMediaAsync(
        Guid imageId, string variant, string? userId, bool isAdmin, CancellationToken cancellationToken);
}
