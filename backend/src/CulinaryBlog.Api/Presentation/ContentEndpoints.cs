// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using CulinaryBlog.Application.Audit;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Commands;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Domain.Recipes;
using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.WebUtilities;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the content endpoints.</summary>
internal static class ContentEndpoints
{
    private enum RecipeLifecycleAction
    {
        Publish,
        Unpublish,
        Archive,
        Unarchive,
    }

    /// <summary>Maps content endpoints.</summary>
    /// <param name="endpoints">The endpoints supplied to this operation.</param>
    /// <returns>The map content endpoints result.</returns>
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var categories = endpoints.MapGroup("/api/v1/categories").WithTags("Categories");
        categories.MapGet("/", ListCategoriesAsync).AllowAnonymous()
            .CacheOutput("Categories")
            .WithSummary("List categories")
            .Produces<DataEnvelope<IReadOnlyCollection<CategoryDto>>>();
        categories.MapGet("/{slug}", GetCategoryAsync).AllowAnonymous()
            .CacheOutput("Categories")
            .WithSummary("View a category and its published recipes")
            .Produces<CategoryDetailEnvelope>();
        categories.MapPost("/", CreateCategoryAsync).RequireAuthorization("AdminPolicy")
            .WithSummary("Create a category (Admin)")
            .WithDescription("Category names must contain between 2 and 50 characters and must not contain HTML.")
            .Produces<DataEnvelope<CategoryDto>>(StatusCodes.Status201Created);
        categories.MapPut("/{id:guid}", UpdateCategoryAsync).RequireAuthorization("AdminPolicy")
            .WithSummary("Update a category (Admin)")
            .WithDescription("Category names must contain between 2 and 50 characters and must not contain HTML.")
            .Produces<DataEnvelope<CategoryDto>>();
        categories.MapDelete("/{id:guid}", DeleteCategoryAsync).RequireAuthorization("AdminPolicy")
            .WithSummary("Delete an empty category (Admin)")
            .Produces(StatusCodes.Status204NoContent);

        endpoints.MapGet("/api/v1/sitemap.xml", GetSitemapAsync).AllowAnonymous()
            .WithTags("Sitemap").WithSummary("Download the generated sitemap")
            .Produces<string>(contentType: "application/xml");

        var recipes = endpoints.MapGroup("/api/v1/recipes").WithTags("Recipes");
        recipes.MapGet("/", ListPublishedRecipesAsync).AllowAnonymous()
            .CacheOutput("RecipeList")
            .WithSummary("List, filter and sort published recipes")
            .Produces<PageEnvelope<RecipeDto>>();
        recipes.MapGet("/search", SearchPublishedRecipesAsync).AllowAnonymous()
            .CacheOutput("Search")
            .WithSummary("Search published recipes")
            .WithDescription("Searches recipe titles only with every word required. Description-only matches are excluded. Prefers exact words and supplied Vietnamese accents; prefixes and approximate names are fallbacks. Default keyword order is relevance.")
            .Produces<PageEnvelope<RecipeDto>>();
        recipes.MapGet("/{slug}", GetPublishedRecipeAsync).AllowAnonymous()
            .CacheOutput("RecipeDetail")
            .WithSummary("View a published recipe")
            .Produces<DataEnvelope<RecipeDto>>()
            .Produces(StatusCodes.Status301MovedPermanently);
        recipes.MapPost("/", CreateRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Create a draft recipe")
            .Produces<DataEnvelope<RecipeDto>>(StatusCodes.Status201Created);
        recipes.MapPut("/{id:guid}", UpdateRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Update a recipe")
            .Produces<DataEnvelope<RecipeDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPatch("/{id:guid}/publish", PublishRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Publish a recipe")
            .Produces<DataEnvelope<RecipeDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPatch("/{id:guid}/unpublish", UnpublishRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Return a published recipe to draft")
            .Produces<DataEnvelope<RecipeDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPatch("/{id:guid}/archive", ArchiveRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Archive a recipe")
            .Produces<DataEnvelope<RecipeDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPatch("/{id:guid}/unarchive", UnarchiveRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Restore an archived recipe to draft")
            .Produces<DataEnvelope<RecipeDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapDelete("/{id:guid}", DeleteRecipeAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Soft-delete a recipe")
            .Produces(StatusCodes.Status204NoContent)
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPost("/{id:guid}/ingredients", CreateIngredientAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Add an ingredient")
            .Produces<RecipeMutationEnvelope<IngredientDto>>(StatusCodes.Status201Created)
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPut("/{id:guid}/ingredients/{ingredientId:guid}", UpdateIngredientAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Update an ingredient")
            .Produces<RecipeMutationEnvelope<IngredientDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapDelete("/{id:guid}/ingredients/{ingredientId:guid}", DeleteIngredientAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Delete an ingredient")
            .Produces(StatusCodes.Status204NoContent)
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPost("/{id:guid}/steps", CreateStepAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Add a preparation step")
            .Produces<RecipeMutationEnvelope<StepDto>>(StatusCodes.Status201Created)
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPut("/{id:guid}/steps/{stepId:guid}", UpdateStepAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Update a preparation step")
            .Produces<RecipeMutationEnvelope<StepDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapDelete("/{id:guid}/steps/{stepId:guid}", DeleteStepAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Delete a preparation step")
            .Produces(StatusCodes.Status204NoContent)
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapPost("/{id:guid}/images", UploadImageAsync)
            .WithSummary("Upload a recipe image")
            .WithDescription("JPEG, PNG, WebP or AVIF; maximum 5 MB. Image resizing runs in the background.")
            .Produces<RecipeMutationEnvelope<RecipeImageDto>>(StatusCodes.Status201Created)
            .RequireAuthorization("AuthorPolicy")
            .AddEndpointFilter<RecipeOwnershipFilter>()

            .RequireRateLimiting("upload")
            .DisableAntiforgery();
        recipes.MapPatch("/{id:guid}/images/{imageId:guid}", UpdateImageAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Update image metadata or select the primary image")
            .Produces<RecipeMutationEnvelope<RecipeImageDto>>()
            .AddEndpointFilter<RecipeOwnershipFilter>();
        recipes.MapDelete("/{id:guid}/images/{imageId:guid}", DeleteImageAsync).RequireAuthorization("AuthorPolicy")
            .WithSummary("Delete a recipe image")
            .Produces(StatusCodes.Status204NoContent)
            .AddEndpointFilter<RecipeOwnershipFilter>();

        endpoints.MapGet("/api/v1/media/{imageId:guid}/{variant}", OpenMediaAsync).AllowAnonymous()
            .WithTags("Images").WithSummary("Read an original, medium or thumbnail image")
            .WithDescription("Published recipe images are public; private images require an owner or Admin JWT.")
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream");

        var mine = endpoints.MapGroup("/api/v1/me/recipes").RequireAuthorization("AuthorPolicy").WithTags("MyRecipes");
        mine.MapGet("/", ListMyRecipesAsync)
            .WithSummary("List the current author's recipes")
            .Produces<PageEnvelope<RecipeDto>>();
        mine.MapGet("/{id:guid}", GetMyRecipeAsync)
            .WithSummary("View an owned recipe, including drafts")
            .Produces<DataEnvelope<RecipeDto>>();

        var admin = endpoints.MapGroup("/api/v1/admin/recipes").RequireAuthorization("AdminPolicy").WithTags("Admin");
        admin.MapGet("/", ListAdminRecipesAsync)
            .WithSummary("List all recipes (Admin)")
            .Produces<PageEnvelope<RecipeDto>>();
        admin.MapGet("/{id:guid}", GetAdminRecipeAsync)
            .WithSummary("View any recipe (Admin)")
            .Produces<DataEnvelope<RecipeDto>>();

        var adminAudit = endpoints.MapGroup("/api/v1/admin/audit-logs").RequireAuthorization("AdminPolicy").WithTags("Admin");
        adminAudit.MapGet("/", ListAdminAuditLogsAsync)
            .WithSummary("List audit logs and telemetry status (Admin)")
            .Produces<AuditLogsEnvelope>();

        return endpoints;
    }

    private static async Task<IResult> GetSitemapAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var content = await sender.Send(new GetSitemapQuery(), cancellationToken).ConfigureAwait(false);
        return content is null ? Results.NotFound() : Results.Stream(content, "application/xml; charset=utf-8");
    }

    private static async Task<IResult> ListCategoriesAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var categories = await sender.Send(new GetCategoriesQuery(), cancellationToken).ConfigureAwait(false);
        return Results.Ok(new { data = categories });
    }

    private static async Task<IResult> SearchPublishedRecipesAsync(
        string? q,
        string? category,
        string? difficulty,
        int? maxTime,
        string? sort,
        string? sortBy,
        string? sortOrder,
        int? page,
        int? pageSize,
        Guid? categoryId,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        ISender sender,
        CancellationToken cancellationToken)
    {
        RecipeDifficulty? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(difficulty, true, out var parsed) || !Enum.IsDefined(parsed))
            {
                throw new ContentProblemException("VALIDATION_ERROR", "Difficulty is invalid.", ContentProblemKind.Validation);
            }

            parsedDifficulty = parsed;
        }

        return Results.Ok(await sender.Send(
            new SearchPublishedRecipesQuery(
                q,
                category,
                parsedDifficulty,
                maxTime,
                sort,
                page ?? 1,
                pageSize ?? 12,
                categoryId,
                maxCookTime,
                minServings,
                minPrepTime,
                maxPrepTime,
                sortBy,
                sortOrder),
            cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetCategoryAsync(
        string slug,
        int? page,
        int? pageSize,
        string? sort,
        string? sortBy,
        string? sortOrder,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var category = await sender.Send(new GetCategoryQuery(slug, page ?? 1, pageSize ?? 12, sort, sortBy, sortOrder), cancellationToken).ConfigureAwait(false);
        return Results.Ok(category);
    }

    private static async Task<IResult> CreateCategoryAsync(
        CategoryWriteRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var category = await sender.Send(new CreateCategoryCommand(request, principal.IsInRole("Admin")), cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/v1/categories/{category.Slug}", new { data = category });
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid id,
        CategoryWriteRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return Results.Ok(new
        {
            data = await sender.Send(new UpdateCategoryCommand(id, request, principal.IsInRole("Admin")), cancellationToken).ConfigureAwait(false),
        });
    }

    private static async Task<IResult> DeleteCategoryAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteCategoryCommand(id, principal.IsInRole("Admin")), cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    /// <summary>Creates a draft recipe.</summary>
    /// <remarks>Optional ingredients, steps and nutrition are saved atomically with the recipe. Step numbers follow array order.</remarks>
    private static async Task<IResult> CreateRecipeAsync(
        RecipeCreateRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new CreateRecipeCommand(GetUserId(principal), request), cancellationToken)
            .ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Created($"/api/v1/me/recipes/{recipe.Id}", new { data = recipe });
    }

    private static async Task<IResult> UpdateRecipeAsync(
        Guid id,
        RecipeUpdateRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipe = await sender.Send(new UpdateRecipeCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
            SetEtag(httpContext, recipe.Version);
            return Results.Ok(new { data = recipe });
        }
        catch (ContentProblemException exception)
            when (exception.Code == "RECIPE_CONCURRENCY_CONFLICT" && exception.CurrentVersion is not null)
        {
            return ConcurrencyProblem(httpContext, exception);
        }
    }

    private static async Task<IResult> DeleteRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await sender.Send(
                new DeleteRecipeCommand(
                    id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)),
                cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (ContentProblemException exception)
            when (exception.Code == "RECIPE_CONCURRENCY_CONFLICT" && exception.CurrentVersion is not null)
        {
            return ConcurrencyProblem(httpContext, exception);
        }
    }

    private static Task<IResult> PublishRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeRecipeLifecycleAsync(id, RecipeLifecycleAction.Publish, principal, sender, httpContext, cancellationToken);

    private static Task<IResult> UnpublishRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeRecipeLifecycleAsync(id, RecipeLifecycleAction.Unpublish, principal, sender, httpContext, cancellationToken);

    private static Task<IResult> ArchiveRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeRecipeLifecycleAsync(id, RecipeLifecycleAction.Archive, principal, sender, httpContext, cancellationToken);

    private static Task<IResult> UnarchiveRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ChangeRecipeLifecycleAsync(id, RecipeLifecycleAction.Unarchive, principal, sender, httpContext, cancellationToken);

    private static async Task<IResult> ChangeRecipeLifecycleAsync(
        Guid id,
        RecipeLifecycleAction action,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var recipe = action switch
            {
                RecipeLifecycleAction.Publish => await sender.Send(new PublishRecipeCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken)
                    .ConfigureAwait(false),
                RecipeLifecycleAction.Unpublish => await sender.Send(new UnpublishRecipeCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken)
                    .ConfigureAwait(false),
                RecipeLifecycleAction.Archive => await sender.Send(new ArchiveRecipeCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken)
                    .ConfigureAwait(false),
                RecipeLifecycleAction.Unarchive => await sender.Send(new UnarchiveRecipeCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken)
                    .ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(action)),
            };
            SetEtag(httpContext, recipe.Version);
            return Results.Ok(new { data = recipe });
        }
        catch (ContentProblemException exception)
            when (exception.Code == "RECIPE_CONCURRENCY_CONFLICT" && exception.CurrentVersion is not null)
        {
            return ConcurrencyProblem(httpContext, exception);
        }
    }

    private static async Task<IResult> GetPublishedRecipeAsync(
        string slug,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new GetPublishedRecipeQuery(slug), cancellationToken).ConfigureAwait(false);
        if (!string.Equals(slug, recipe.Slug, StringComparison.Ordinal))
        {
            httpContext.Response.Headers.Location = $"/api/v1/recipes/{Uri.EscapeDataString(recipe.Slug)}";
            return Results.StatusCode(StatusCodes.Status301MovedPermanently);
        }

        httpContext.Features.Get<IOutputCacheFeature>()?.Context.Tags.Add($"recipe:{slug}");
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListPublishedRecipesAsync(
        Guid? categoryId,
        string? difficulty,
        int? maxCookTime,
        int? minServings,
        int? minPrepTime,
        int? maxPrepTime,
        string? sort,
        string? sortBy,
        string? sortOrder,
        int? page,
        int? pageSize,
        string? category,
        int? maxTime,
        ISender sender,
        CancellationToken cancellationToken)
    {
        RecipeDifficulty? parsedDifficulty = null;
        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            if (!Enum.TryParse<RecipeDifficulty>(difficulty, true, out var parsed) || !Enum.IsDefined(parsed))
            {
                throw new ContentProblemException(
                    "VALIDATION_ERROR", "difficulty is invalid.", ContentProblemKind.Validation);
            }

            parsedDifficulty = parsed;
        }

        return Results.Ok(await sender.Send(
            new ListPublishedRecipesQuery(
                categoryId,
                parsedDifficulty,
                maxCookTime,
                minServings,
                minPrepTime,
                maxPrepTime,
                sort,
                page ?? 1,
                pageSize ?? 12,
                category,
                maxTime,
                sortBy,
                sortOrder),
            cancellationToken).ConfigureAwait(false));
    }

    private static async Task<IResult> GetMyRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new GetMyRecipeQuery(id, GetUserId(principal)), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListMyRecipesAsync(
        string? status,
        string? q,
        int? page,
        int? pageSize,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListMyRecipesQuery(GetUserId(principal), ParseRecipeStatus(status), q, page ?? 1, pageSize ?? 12), cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> GetAdminRecipeAsync(
        Guid id,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await sender.Send(new GetAdminRecipeQuery(id), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListAdminRecipesAsync(
        string? status,
        string? authorId,
        string? q,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(new ListAdminRecipesQuery(ParseRecipeStatus(status), authorId, q, page ?? 1, pageSize ?? 12), cancellationToken).ConfigureAwait(false));

    private static RecipeStatus? ParseRecipeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<RecipeStatus>(status, true, out var parsed) || !Enum.IsDefined(parsed))
        {
            throw new ContentProblemException(
                "VALIDATION_ERROR", "status is invalid.", ContentProblemKind.Validation);
        }

        return parsed;
    }

    private static async Task<IResult> ListAdminAuditLogsAsync(
        string? level,
        string? search,
        int? page,
        int? pageSize,
        ISender sender,
        CancellationToken cancellationToken) =>
        Results.Ok(await sender.Send(
            new ListAuditLogsQuery(
                level, search, page ?? 1, pageSize ?? 20),
            cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> CreateIngredientAsync(
        Guid id,
        IngredientWriteRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateIngredientCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, result.RecipeVersion);
        return Results.Created(
            $"/api/v1/recipes/{id}/ingredients/{result.Resource.Id}",
            new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
    }

    private static async Task<IResult> UpdateIngredientAsync(
        Guid id,
        Guid ingredientId,
        IngredientUpdateRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateIngredientCommand(id, ingredientId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, result.RecipeVersion);
        return Results.Ok(new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
    }

    private static async Task<IResult> DeleteIngredientAsync(
        Guid id,
        Guid ingredientId,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var version = await sender.Send(new DeleteIngredientCommand(id, ingredientId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, version);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateStepAsync(
        Guid id,
        StepWriteRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateStepCommand(id, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, result.RecipeVersion);
        return Results.Created(
            $"/api/v1/recipes/{id}/steps/{result.Resource.Id}",
            new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
    }

    private static async Task<IResult> UpdateStepAsync(
        Guid id,
        Guid stepId,
        StepUpdateRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateStepCommand(id, stepId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, result.RecipeVersion);
        return Results.Ok(new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
    }

    private static async Task<IResult> DeleteStepAsync(
        Guid id,
        Guid stepId,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var version = await sender.Send(new DeleteStepCommand(id, stepId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, version);
        return Results.NoContent();
    }

    private static async Task<IResult> UploadImageAsync(
        Guid id,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!httpContext.Request.HasFormContentType)
        {
            throw new ContentProblemException("FILE_MIME_INVALID", "A multipart image upload is required.", ContentProblemKind.BadRequest);
        }

        const long maximumBytes = 5 * 1024 * 1024;
        if (httpContext.Request.ContentLength > maximumBytes + (64 * 1024))
        {
            throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size cannot exceed 5 MB.", ContentProblemKind.BadRequest);
        }

        var boundary = Microsoft.Net.Http.Headers.HeaderUtilities.RemoveQuotes(
            Microsoft.Net.Http.Headers.MediaTypeHeaderValue.Parse(httpContext.Request.ContentType!).Boundary).Value;
        if (string.IsNullOrEmpty(boundary) || boundary.Length > 128)
        {
            throw new ContentProblemException("VALIDATION_ERROR", "A valid multipart boundary is required.", ContentProblemKind.Validation);
        }

        var temporaryDirectory = Directory.CreateTempSubdirectory("culinary-upload-");
        var temporaryPath = Path.Combine(temporaryDirectory.FullName, "content");
        try
        {
            var reader = new MultipartReader(boundary, httpContext.Request.Body) { BodyLengthLimit = 6 * 1024 * 1024 };
            string? contentType = null;
            string? altText = null;
            var isPrimary = false;
            long length = 0;
            while (await reader.ReadNextSectionAsync(cancellationToken).ConfigureAwait(false) is { } section)
            {
                if (!Microsoft.Net.Http.Headers.ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition))
                {
                    throw new InvalidDataException("Multipart content disposition is invalid.");
                }

                var name = Microsoft.Net.Http.Headers.HeaderUtilities.RemoveQuotes(disposition.Name).Value;
                if (name == "file")
                {
                    if (contentType is not null)
                    {
                        throw new InvalidDataException("Only one file is allowed.");
                    }

                    contentType = section.ContentType ?? "application/octet-stream";
                    await using var output = File.Create(temporaryPath);
                    var buffer = new byte[64 * 1024];
                    int count;
                    while ((count = await section.Body.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        length += count;
                        if (length > maximumBytes)
                        {
                            throw new ContentProblemException("FILE_SIZE_EXCEEDED", "Image size cannot exceed 5 MB.", ContentProblemKind.BadRequest);
                        }

                        await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    using var fieldReader = new StreamReader(section.Body);
                    var field = new char[4097];
                    var count = await fieldReader.ReadBlockAsync(field.AsMemory(), cancellationToken).ConfigureAwait(false);
                    if (count > 4096)
                    {
                        throw new InvalidDataException("Multipart field is too large.");
                    }

                    var value = new string(field, 0, count);
                    if (name == "altText")
                    {
                        altText = value;
                    }

                    if (name == "isPrimary")
                    {
                        isPrimary = bool.TryParse(value, out var parsed) && parsed;
                    }
                }
            }

            if (contentType is null)
            {
                throw new InvalidDataException("The file field is required.");
            }

            await using var content = File.OpenRead(temporaryPath);
            var result = await sender.Send(
                new UploadImageCommand(
                    id,
                    GetUserId(principal),
                    principal.IsInRole("Admin"),
                    ReadEtag(httpContext),
                    content,
                    length,
                    contentType,
                    altText,
                    isPrimary),
                cancellationToken).ConfigureAwait(false);
            SetEtag(httpContext, result.RecipeVersion);
            return Results.Created(
                $"/api/v1/recipes/{id}/images/{result.Resource.Id}",
                new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
        }
        catch (InvalidDataException exception)
        {
            throw new ContentProblemException("VALIDATION_ERROR", exception.Message, ContentProblemKind.Validation);
        }
        finally
        {
            File.Delete(temporaryPath);
            temporaryDirectory.Delete();
        }
    }

    private static async Task<IResult> UpdateImageAsync(
        Guid id,
        Guid imageId,
        ImageMetadataRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateImageCommand(id, imageId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext), request), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, result.RecipeVersion);
        return Results.Ok(new { data = result.Resource, meta = new { recipeVersion = result.RecipeVersion } });
    }

    private static async Task<IResult> DeleteImageAsync(
        Guid id,
        Guid imageId,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var version = await sender.Send(new DeleteImageCommand(id, imageId, GetUserId(principal), principal.IsInRole("Admin"), ReadEtag(httpContext)), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, version);
        return Results.NoContent();
    }

    private static async Task<IResult> OpenMediaAsync(
        Guid imageId,
        string variant,
        ClaimsPrincipal principal,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var file = await sender.Send(new OpenMediaQuery(imageId, variant, GetOptionalUserId(principal), principal.IsInRole("Admin")), cancellationToken).ConfigureAwait(false);
        httpContext.Response.Headers.CacheControl = "private, no-store";
        return Results.Stream(file.Content, file.ContentType, enableRangeProcessing: true);
    }

    private static string GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ContentProblemException("AUTH_TOKEN_INVALID", "The access token is invalid.", ContentProblemKind.BadRequest);
        }

        return value;
    }

    private static string? GetOptionalUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static long ReadEtag(HttpContext context)
    {
        var value = context.Request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ContentProblemException(
                "PRECONDITION_REQUIRED",
                "If-Match is required.",
                ContentProblemKind.BadRequest);
        }

        var unquoted = value.Trim();
        if (unquoted.StartsWith('W'))
        {
            throw new ContentProblemException("VALIDATION_ERROR", "If-Match must be a strong quoted ETag.", ContentProblemKind.BadRequest);
        }

        unquoted = unquoted.Trim('"');
        if (!long.TryParse(unquoted, out var version) || version < 1 || value.Count(character => character == '"') != 2)
        {
            throw new ContentProblemException("VALIDATION_ERROR", "If-Match must contain a quoted positive version.", ContentProblemKind.BadRequest);
        }

        return version;
    }

    private static void SetEtag(HttpContext context, long version) => context.Response.Headers.ETag = $"\"{version}\"";

    private static IResult ConcurrencyProblem(HttpContext context, ContentProblemException exception)
    {
        var etag = $"\"{exception.CurrentVersion!.Value}\"";
        context.Response.Headers.ETag = etag;
        return Results.Problem(
            detail: exception.Message,
            instance: context.Request.Path,
            statusCode: StatusCodes.Status409Conflict,
            title: "Content conflict",
            type: $"https://culinaryblog.local/problems/{exception.Code}",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = exception.Code,
                ["traceId"] = context.TraceIdentifier,
                ["correlationId"] = context.TraceIdentifier,
                ["currentETag"] = etag,
            });
    }

    private sealed record RecipeMutationMeta(long RecipeVersion);

    private sealed record RecipeMutationEnvelope<T>(T Data, RecipeMutationMeta Meta);
}
