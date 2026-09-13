using System.Security.Claims;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Recipes;
using FluentValidation;

namespace CulinaryBlog.Api.Presentation;

internal static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var categories = endpoints.MapGroup("/api/v1/categories");
        categories.MapGet("/", ListCategoriesAsync).AllowAnonymous();
        categories.MapGet("/{slug}", GetCategoryAsync).AllowAnonymous();
        categories.MapPost("/", CreateCategoryAsync).RequireAuthorization("AdminPolicy");
        categories.MapPut("/{id:guid}", UpdateCategoryAsync).RequireAuthorization("AdminPolicy");
        categories.MapDelete("/{id:guid}", DeleteCategoryAsync).RequireAuthorization("AdminPolicy");

        var recipes = endpoints.MapGroup("/api/v1/recipes");
        recipes.MapGet("/", ListPublishedRecipesAsync).AllowAnonymous();
        recipes.MapGet("/{slug}", GetPublishedRecipeAsync).AllowAnonymous();
        recipes.MapPost("/", CreateRecipeAsync).RequireAuthorization("AuthorPolicy");
        recipes.MapPut("/{id:guid}", UpdateRecipeAsync).RequireAuthorization("AuthorPolicy");
        recipes.MapDelete("/{id:guid}", DeleteRecipeAsync).RequireAuthorization("AuthorPolicy");

        var mine = endpoints.MapGroup("/api/v1/me/recipes").RequireAuthorization("AuthorPolicy");
        mine.MapGet("/", ListMyRecipesAsync);
        mine.MapGet("/{id:guid}", GetMyRecipeAsync);

        var admin = endpoints.MapGroup("/api/v1/admin/recipes").RequireAuthorization("AdminPolicy");
        admin.MapGet("/", ListAdminRecipesAsync);
        admin.MapGet("/{id:guid}", GetAdminRecipeAsync);

        return endpoints;
    }

    private static async Task<IResult> ListCategoriesAsync(
        IContentService contentService,
        CancellationToken cancellationToken) =>
        Results.Ok(new { data = await contentService.ListCategoriesAsync(cancellationToken).ConfigureAwait(false) });

    private static async Task<IResult> GetCategoryAsync(
        string slug,
        int? page,
        int? pageSize,
        IContentService contentService,
        CancellationToken cancellationToken) =>
        Results.Ok(await contentService.GetCategoryAsync(slug, page ?? 1, pageSize ?? 12, cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> CreateCategoryAsync(
        CategoryWriteRequest request,
        IValidator<CategoryWriteRequest> validator,
        IContentService contentService,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var category = await contentService.CreateCategoryAsync(request, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/v1/categories/{category.Slug}", new { data = category });
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid id,
        CategoryWriteRequest request,
        IValidator<CategoryWriteRequest> validator,
        IContentService contentService,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        return Results.Ok(new
        {
            data = await contentService.UpdateCategoryAsync(id, request, cancellationToken).ConfigureAwait(false),
        });
    }

    private static async Task<IResult> DeleteCategoryAsync(
        Guid id,
        IContentService contentService,
        CancellationToken cancellationToken)
    {
        await contentService.DeleteCategoryAsync(id, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> CreateRecipeAsync(
        RecipeWriteRequest request,
        ClaimsPrincipal principal,
        IValidator<RecipeWriteRequest> validator,
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        var recipe = await contentService.CreateRecipeAsync(GetUserId(principal), request, cancellationToken)
            .ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Created($"/api/v1/me/recipes/{recipe.Id}", new { data = recipe });
    }

    private static async Task<IResult> UpdateRecipeAsync(
        Guid id,
        RecipeWriteRequest request,
        ClaimsPrincipal principal,
        IValidator<RecipeWriteRequest> validator,
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken).ConfigureAwait(false);
        try
        {
            var recipe = await contentService.UpdateRecipeAsync(
                id,
                GetUserId(principal),
                principal.IsInRole("Admin"),
                ReadEtag(httpContext),
                request,
                cancellationToken).ConfigureAwait(false);
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
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await contentService.DeleteRecipeAsync(
                id,
                GetUserId(principal),
                principal.IsInRole("Admin"),
                ReadEtag(httpContext),
                cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        }
        catch (ContentProblemException exception)
            when (exception.Code == "RECIPE_CONCURRENCY_CONFLICT" && exception.CurrentVersion is not null)
        {
            return ConcurrencyProblem(httpContext, exception);
        }
    }

    private static async Task<IResult> GetPublishedRecipeAsync(
        string slug,
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await contentService.GetPublishedRecipeAsync(slug, cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListPublishedRecipesAsync(
        int? page,
        int? pageSize,
        IContentService contentService,
        CancellationToken cancellationToken) =>
        Results.Ok(await contentService.ListPublishedRecipesAsync(page ?? 1, pageSize ?? 12, cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> GetMyRecipeAsync(
        Guid id,
        ClaimsPrincipal principal,
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await contentService.GetMyRecipeAsync(id, GetUserId(principal), cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListMyRecipesAsync(
        RecipeStatus? status,
        int? page,
        int? pageSize,
        ClaimsPrincipal principal,
        IContentService contentService,
        CancellationToken cancellationToken) =>
        Results.Ok(await contentService.ListMyRecipesAsync(
            GetUserId(principal),
            status,
            page ?? 1,
            pageSize ?? 12,
            cancellationToken).ConfigureAwait(false));

    private static async Task<IResult> GetAdminRecipeAsync(
        Guid id,
        IContentService contentService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var recipe = await contentService.GetAdminRecipeAsync(id, cancellationToken).ConfigureAwait(false);
        SetEtag(httpContext, recipe.Version);
        return Results.Ok(new { data = recipe });
    }

    private static async Task<IResult> ListAdminRecipesAsync(
        RecipeStatus? status,
        Guid? authorId,
        int? page,
        int? pageSize,
        IContentService contentService,
        CancellationToken cancellationToken) =>
        Results.Ok(await contentService.ListAdminRecipesAsync(status, authorId, page ?? 1, pageSize ?? 12, cancellationToken).ConfigureAwait(false));

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId))
        {
            throw new ContentProblemException("AUTH_TOKEN_INVALID", "The access token is invalid.", ContentProblemKind.BadRequest);
        }

        return userId;
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
}
