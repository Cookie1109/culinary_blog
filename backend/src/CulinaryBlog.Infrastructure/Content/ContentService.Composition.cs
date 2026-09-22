using System.Text.Json;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CulinaryBlog.Infrastructure.Content;

internal sealed partial class ContentService
{
    private const long MaximumImageBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> ImageExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

    public async Task<ChildMutationDto<IngredientDto>> CreateIngredientAsync(
        Guid recipeId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        IngredientWriteRequest request,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.MarkCompositionChanged();
        var ingredients = await dbContext.RecipeIngredients.Where(item => item.RecipeId == recipeId)
            .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        var targetIndex = Math.Min(request.OrderIndex, ingredients.Count);
        var ingredient = RecipeIngredient.Create(
            Guid.NewGuid(), recipeId, request.Name, request.Quantity, request.Unit, request.Notes, targetIndex);
        ingredients.Insert(targetIndex, ingredient);
        for (var index = 0; index < ingredients.Count; index++)
        {
            ingredients[index].MoveTo(index);
        }
        dbContext.RecipeIngredients.Add(ingredient);
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return new ChildMutationDto<IngredientDto>(ToIngredientDto(ingredient), recipe.Version);
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<ChildMutationDto<IngredientDto>> UpdateIngredientAsync(
        Guid recipeId,
        Guid ingredientId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        IngredientWriteRequest request,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var ingredients = await dbContext.RecipeIngredients.Where(item => item.RecipeId == recipeId)
            .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        var ingredient = ingredients.SingleOrDefault(item => item.Id == ingredientId)
            ?? throw NotFound("INGREDIENT_NOT_FOUND", "Ingredient was not found.");
        var targetIndex = Math.Min(request.OrderIndex, ingredients.Count - 1);
        ingredient.Update(request.Name, request.Quantity, request.Unit, request.Notes, targetIndex);
        ingredients.Remove(ingredient);
        ingredients.Insert(targetIndex, ingredient);
        for (var index = 0; index < ingredients.Count; index++)
        {
            ingredients[index].MoveTo(index);
        }
        recipe.MarkCompositionChanged();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return new ChildMutationDto<IngredientDto>(ToIngredientDto(ingredient), recipe.Version);
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<long> DeleteIngredientAsync(
        Guid recipeId,
        Guid ingredientId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var ingredients = await dbContext.RecipeIngredients.Where(item => item.RecipeId == recipeId)
            .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
        var ingredient = ingredients.SingleOrDefault(item => item.Id == ingredientId)
            ?? throw NotFound("INGREDIENT_NOT_FOUND", "Ingredient was not found.");
        if (recipe.Status == RecipeStatus.Published && ingredients.Count <= 1)
        {
            throw Conflict("RECIPE_STATE_CONFLICT", "A published recipe must keep at least one ingredient. Unpublish it first.");
        }

        ingredient.Delete();
        var remaining = ingredients.Where(item => item.Id != ingredientId).ToList();
        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].MoveTo(index);
        }
        recipe.MarkCompositionChanged();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return recipe.Version;
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<ChildMutationDto<StepDto>> CreateStepAsync(
        Guid recipeId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        StepWriteRequest request,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var number = await dbContext.RecipeSteps.CountAsync(item => item.RecipeId == recipeId, cancellationToken).ConfigureAwait(false) + 1;
        var step = RecipeStep.Create(Guid.NewGuid(), recipeId, number, request.Title, request.Description, request.TimerMinutes, request.ImageUrl);
        dbContext.RecipeSteps.Add(step);
        recipe.MarkCompositionChanged();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        return new ChildMutationDto<StepDto>(ToStepDto(step), recipe.Version);
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<ChildMutationDto<StepDto>> UpdateStepAsync(
        Guid recipeId,
        Guid stepId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        StepWriteRequest request,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var steps = await dbContext.RecipeSteps.Where(item => item.RecipeId == recipeId)
            .OrderBy(item => item.StepNumber).ToListAsync(cancellationToken).ConfigureAwait(false);
        var step = steps.SingleOrDefault(item => item.Id == stepId)
            ?? throw NotFound("STEP_NOT_FOUND", "Recipe step was not found.");
        var targetNumber = request.StepNumber ?? step.StepNumber;
        if (targetNumber > steps.Count)
        {
            throw new ContentProblemException("VALIDATION_ERROR", "stepNumber must be within the current step range.", ContentProblemKind.BadRequest);
        }

        step.Update(step.StepNumber, request.Title, request.Description, request.TimerMinutes, request.ImageUrl);
        if (targetNumber != step.StepNumber)
        {
            foreach (var item in steps)
            {
                item.MoveTo(item.StepNumber + 100_000);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            steps.Remove(step);
            steps.Insert(targetNumber - 1, step);
            for (var index = 0; index < steps.Count; index++)
            {
                steps[index].MoveTo(index + 1);
            }
        }

        recipe.MarkCompositionChanged();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new ChildMutationDto<StepDto>(ToStepDto(step), recipe.Version);
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<long> DeleteStepAsync(
        Guid recipeId,
        Guid stepId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {\n        try\n        {    
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        var steps = await dbContext.RecipeSteps.Where(item => item.RecipeId == recipeId)
            .OrderBy(item => item.StepNumber).ToListAsync(cancellationToken).ConfigureAwait(false);
        var step = steps.SingleOrDefault(item => item.Id == stepId)
            ?? throw NotFound("STEP_NOT_FOUND", "Recipe step was not found.");
        if (recipe.Status == RecipeStatus.Published && steps.Count <= 1)
        {
            throw Conflict("RECIPE_STATE_CONFLICT", "A published recipe must keep at least one step. Unpublish it first.");
        }

        step.Delete();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var remaining = steps.Where(item => item.Id != stepId).ToList();
        foreach (var item in remaining)
        {
            item.MoveTo(item.StepNumber + 100_000);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        for (var index = 0; index < remaining.Count; index++)
        {
            remaining[index].MoveTo(index + 1);
        }

        recipe.MarkCompositionChanged();
        await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return recipe.Version;
    \        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<ChildMutationDto<RecipeImageDto>> UploadImageAsync(
        Guid recipeId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        Stream content,
        long length,
        string contentType,
        string? altText,
        bool isPrimary,
        CancellationToken cancellationToken)
    {
        if (length is <= 0 or > MaximumImageBytes)
        {
            throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size must be between 1 byte and 5 MB.", ContentProblemKind.BadRequest);
        }

        if (!ImageExtensions.ContainsKey(contentType))
        {
            throw new ContentProblemException("FILE_MIME_INVALID", "Only JPEG, PNG and WebP images are accepted.", ContentProblemKind.BadRequest);
        }

        var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
        EnsureVersion(recipe, expectedVersion);
        recipe.MarkCompositionChanged();

        var temporaryPath = Path.GetTempFileName();
        string? objectKey = null;
        try
        {
            await CopyToLimitedFileAsync(content, temporaryPath, cancellationToken).ConfigureAwait(false);
            var detectedContentType = await DetectImageContentTypeAsync(temporaryPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(contentType, detectedContentType, StringComparison.OrdinalIgnoreCase))
            {
                throw new ContentProblemException("FILE_SIGNATURE_INVALID", "Image signature does not match its MIME type.", ContentProblemKind.BadRequest);
            }

            var imageId = Guid.NewGuid();
            objectKey = $"recipes/{recipeId:N}/{imageId:N}/original{ImageExtensions[detectedContentType]}";
            await using (var upload = File.OpenRead(temporaryPath))
            {
                await fileStorage.UploadAsync(objectKey, upload, upload.Length, detectedContentType, cancellationToken).ConfigureAwait(false);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var existingImages = await dbContext.RecipeImages.Where(item => item.RecipeId == recipeId)
                .OrderBy(item => item.OrderIndex).ToListAsync(cancellationToken).ConfigureAwait(false);
            var makePrimary = isPrimary || existingImages.Count == 0;
            if (makePrimary)
            {
                foreach (var current in existingImages.Where(item => item.IsPrimary))
                {
                    current.UpdateMetadata(current.AltText, false, current.OrderIndex);
                }
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            var image = RecipeImage.Create(imageId, recipeId, objectKey, detectedContentType, altText, makePrimary, existingImages.Count);
            dbContext.RecipeImages.Add(image);
            dbContext.MediaOutbox.Add(CreateOutbox(MediaOutboxTypes.ResizeImage, new ResizeImagePayload(imageId)));
            await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new ChildMutationDto<RecipeImageDto>(ToImageDto(image), recipe.Version);
        }
        catch (ContentProblemException)
        {
            if (objectKey is not null)
            {
                await TryDeleteCompensationAsync(objectKey).ConfigureAwait(false);
            }
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (objectKey is not null)
            {
                await TryDeleteCompensationAsync(objectKey).ConfigureAwait(false);
            }
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeImages"))
        {
            if (objectKey is not null)
            {
                await TryDeleteCompensationAsync(objectKey).ConfigureAwait(false);
            }
            throw Conflict("IMAGE_PRIMARY_CONFLICT", "Choose another primary image before clearing the current primary.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            if (objectKey is not null)
            {
                await TryDeleteCompensationAsync(objectKey).ConfigureAwait(false);
            }
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception)
        {
            if (objectKey is not null)
            {
                await TryDeleteCompensationAsync(objectKey).ConfigureAwait(false);
            }
            throw new ContentProblemException("FILE_UPLOAD_FAILED", "The image could not be stored.", ContentProblemKind.ServiceUnavailable);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    public async Task<ChildMutationDto<RecipeImageDto>> UpdateImageAsync(
        Guid recipeId,
        Guid imageId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        ImageMetadataRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
            EnsureVersion(recipe, expectedVersion);
            var images = await dbContext.RecipeImages.Where(item => item.RecipeId == recipeId)
                .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
            var image = images.SingleOrDefault(item => item.Id == imageId)
                ?? throw NotFound("IMAGE_NOT_FOUND", "Recipe image was not found.");
            var makePrimary = request.IsPrimary ?? image.IsPrimary;
            if (makePrimary)
            {
                var currentPrimary = images.SingleOrDefault(item => item.IsPrimary && item.Id != imageId);
                if (currentPrimary is not null)
                {
                    currentPrimary.UpdateMetadata(currentPrimary.AltText, false, currentPrimary.OrderIndex);
                    await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            else if (image.IsPrimary && images.Count > 0)
            {
                throw Conflict("IMAGE_PRIMARY_CONFLICT", "Choose another primary image before clearing the current primary.");
            }

            var targetIndex = Math.Min(request.OrderIndex ?? image.OrderIndex, images.Count - 1);
            images.Remove(image);
            images.Insert(targetIndex, image);
            for (var index = 0; index < images.Count; index++)
            {
                var item = images[index];
                item.UpdateMetadata(item.Id == imageId ? request.AltText : item.AltText, item.Id == imageId ? makePrimary : item.IsPrimary, index);
            }
            recipe.MarkCompositionChanged();
            await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new ChildMutationDto<RecipeImageDto>(ToImageDto(image), recipe.Version);
        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeImages"))
        {
            throw Conflict("IMAGE_PRIMARY_CONFLICT", "Choose another primary image before clearing the current primary.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<long> DeleteImageAsync(
        Guid recipeId,
        Guid imageId,
        Guid userId,
        bool isAdmin,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var recipe = await FindRecipeForMutationAsync(recipeId, userId, isAdmin, cancellationToken).ConfigureAwait(false);
            EnsureVersion(recipe, expectedVersion);
            var image = await dbContext.RecipeImages.IgnoreQueryFilters().SingleOrDefaultAsync(
                item => item.Id == imageId && item.RecipeId == recipeId, cancellationToken).ConfigureAwait(false)
                ?? throw NotFound("IMAGE_NOT_FOUND", "Recipe image was not found.");
            if (image.IsDeleted)
            {
                return recipe.Version;
            }
            var wasPrimary = image.IsPrimary;
            image.Delete();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (wasPrimary)
            {
                var replacement = await dbContext.RecipeImages.Where(item => item.RecipeId == recipeId)
                    .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                replacement?.UpdateMetadata(replacement.AltText, true, replacement.OrderIndex);
            }

            var remainingImages = await dbContext.RecipeImages.Where(item => item.RecipeId == recipeId)
                .OrderBy(item => item.OrderIndex).ThenBy(item => item.CreatedAt).ToListAsync(cancellationToken).ConfigureAwait(false);
            for (var index = 0; index < remainingImages.Count; index++)
            {
                var item = remainingImages[index];
                item.UpdateMetadata(item.AltText, item.IsPrimary, index);
            }

            dbContext.MediaOutbox.Add(CreateOutbox(
                MediaOutboxTypes.DeleteObjects,
                new DeleteObjectsPayload(new[] { image.ObjectKey, image.MediumObjectKey, image.ThumbnailObjectKey }.OfType<string>().ToArray())));
            recipe.MarkCompositionChanged();
            await SaveRecipeMutationAsync(recipe, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return recipe.Version;
        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeImages"))
        {
            throw Conflict("IMAGE_PRIMARY_CONFLICT", "Choose another primary image before clearing the current primary.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
    }

    public async Task<MediaFileDto> OpenMediaAsync(
        Guid imageId,
        string variant,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var image = await dbContext.RecipeImages.AsNoTracking().SingleOrDefaultAsync(item => item.Id == imageId, cancellationToken)
            .ConfigureAwait(false) ?? throw NotFound("IMAGE_NOT_FOUND", "Recipe image was not found.");
        var recipe = await dbContext.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == image.RecipeId, cancellationToken)
            .ConfigureAwait(false) ?? throw NotFound("RECIPE_NOT_FOUND", "Recipe was not found.");
        if (recipe.Status != RecipeStatus.Published && !isAdmin && recipe.AuthorId != userId)
        {
            throw NotFound("IMAGE_NOT_FOUND", "Recipe image was not found.");
        }

        var (objectKey, contentType) = variant.ToLowerInvariant() switch
        {
            "original" => (image.ObjectKey, image.ContentType),
            "medium" when image.MediumObjectKey is not null => (image.MediumObjectKey, "image/webp"),
            "thumbnail" when image.ThumbnailObjectKey is not null => (image.ThumbnailObjectKey, "image/webp"),
            _ => throw NotFound("IMAGE_NOT_FOUND", "Image variant was not found."),
        };
        try
        {
            return new MediaFileDto(await fileStorage.OpenReadAsync(objectKey, cancellationToken).ConfigureAwait(false), contentType);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ContentProblemException("FILE_UPLOAD_FAILED", "The image storage service is unavailable.", ContentProblemKind.ServiceUnavailable);
        }
    }

    private static IngredientDto ToIngredientDto(RecipeIngredient item) =>
        new(item.Id, item.Name, item.Quantity, item.Unit, item.Notes, item.OrderIndex);

    private static StepDto ToStepDto(RecipeStep item) =>
        new(item.Id, item.StepNumber, item.Title, item.Description, item.TimerMinutes, item.ImageUrl);

    private static RecipeImageDto ToImageDto(RecipeImage item) => new(
        item.Id,
        MediaUrl(item.Id, "original"),
        item.MediumObjectKey is null ? null : MediaUrl(item.Id, "medium"),
        item.ThumbnailObjectKey is null ? null : MediaUrl(item.Id, "thumbnail"),
        item.AltText,
        item.IsPrimary,
        item.OrderIndex,
        item.ProcessingStatus.ToString().ToLowerInvariant());

    private MediaOutboxMessage CreateOutbox<T>(string type, T payload) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Payload = JsonSerializer.Serialize(payload),
        CreatedAt = timeProvider.GetUtcNow(),
        NextAttemptAt = timeProvider.GetUtcNow(),
    };

    private static async Task CopyToLimitedFileAsync(Stream source, string path, CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true);
        var buffer = new byte[64 * 1024];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > MaximumImageBytes)
            {
                throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size cannot exceed 5 MB.", ContentProblemKind.BadRequest);
            }
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }
    }

    private static Task<string> DetectImageContentTypeAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var input = File.OpenRead(path);
            cancellationToken.ThrowIfCancellationRequested();
            using var codec = SKCodec.Create(input);
            if (codec is null || codec.Info.Width <= 0 || codec.Info.Height <= 0 ||
                (long)codec.Info.Width * codec.Info.Height > 40_000_000)
            {
                throw new ContentProblemException(
                    "FILE_SIGNATURE_INVALID", "The uploaded image dimensions are invalid or too large.", ContentProblemKind.BadRequest);
            }
            var contentType = codec?.EncodedFormat switch
            {
                SKEncodedImageFormat.Jpeg => "image/jpeg",
                SKEncodedImageFormat.Png => "image/png",
                SKEncodedImageFormat.Webp => "image/webp",
                _ => null,
            };
            input.Position = 0;
            using var decoded = SKBitmap.Decode(input);
            if (decoded is null)
            {
                throw new ContentProblemException(
                    "FILE_SIGNATURE_INVALID", "The uploaded file cannot be decoded as an image.", ContentProblemKind.BadRequest);
            }
            return Task.FromResult(contentType ?? throw new ContentProblemException(
                "FILE_SIGNATURE_INVALID", "The uploaded file is not a supported image.", ContentProblemKind.BadRequest));
        }
        catch (ContentProblemException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new ContentProblemException("FILE_SIGNATURE_INVALID", "The uploaded file is not a valid image.", ContentProblemKind.BadRequest);
        }
    }

    private async Task TryDeleteCompensationAsync(string objectKey)
    {
        try
        {
            await fileStorage.DeleteAsync(objectKey, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // The daily reconciliation job reports objects left behind after a failed compensation.
        }
    }
}
