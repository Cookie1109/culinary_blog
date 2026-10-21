using System.Security.Claims;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Api.Middleware;

public sealed partial class AuditMutationMiddleware(
    RequestDelegate next,
    ILogger<AuditMutationMiddleware> logger,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext? dbContext = null)
    {
        await next(context).ConfigureAwait(false);

        if (!IsSuccessfulContentMutation(context) || !logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var path = context.Request.Path.Value!;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var resourceIndex = Array.FindIndex(
            segments,
            segment => segment is "recipes" or "categories");
        if (resourceIndex < 0)
        {
            return;
        }

        var resourceTailLength = segments.Length - resourceIndex - 1;
        var component = resourceTailLength >= 2 && segments[resourceIndex + 2] is "ingredients" or "steps" or "images"
            ? segments[resourceIndex + 2]
            : null;
        if (resourceTailLength > 3 ||
            (resourceTailLength >= 2 && component is null && segments[^1] is not ("publish" or "unpublish" or "archive" or "unarchive")))
        {
            return;
        }

        var entityType = segments[resourceIndex] == "recipes" ? "recipe" : "category";
        var action = GetAction(context.Request.Method, segments);
        var entityId = GetEntityId(context, segments, resourceIndex);
        var userId = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "system";
        var userName = context.User.FindFirstValue("name") ?? context.User.FindFirstValue(ClaimTypes.Name);
        string? entityName = null;
        if (dbContext is not null)
        {
            var id = Guid.TryParse(entityId, out var parsedId) ? parsedId : Guid.Empty;
            var entity = entityType == "recipe"
                ? await dbContext.Recipes.IgnoreQueryFilters().AsNoTracking()
                    .Where(recipe => recipe.Id == id || recipe.Slug == entityId)
                    .Select(recipe => new { recipe.Id, Name = recipe.Title }).FirstOrDefaultAsync(context.RequestAborted).ConfigureAwait(false)
                : await dbContext.Categories.IgnoreQueryFilters().AsNoTracking()
                    .Where(category => category.Id == id || category.Slug == entityId)
                    .Select(category => new { category.Id, category.Name }).FirstOrDefaultAsync(context.RequestAborted).ConfigureAwait(false);
            entityName = entity?.Name;
            if (entity is not null) entityId = entity.Id.ToString();
        }
        var auditEvent = component is null ? $"{entityType}.{action}" : $"{entityType}.{component}.{action}";
        var occurredAt = timeProvider.GetUtcNow();

        ContentAudit(
            logger,
            auditEvent,
            entityType,
            entityId,
            entityName,
            userId,
            userName,
            occurredAt,
            context.TraceIdentifier,
            context.Request.Method,
            path,
            context.Response.StatusCode);
    }

    private static bool IsSuccessfulContentMutation(HttpContext context) =>
        context.Response.StatusCode is >= 200 and < 300 &&
        (HttpMethods.IsPost(context.Request.Method) ||
         HttpMethods.IsPut(context.Request.Method) ||
         HttpMethods.IsPatch(context.Request.Method) ||
         HttpMethods.IsDelete(context.Request.Method));

    private static string GetAction(string method, string[] segments)
    {
        var lifecycleAction = segments.LastOrDefault() switch
        {
            "publish" => "published",
            "unpublish" => "unpublished",
            "archive" => "archived",
            "unarchive" => "unarchived",
            _ => null,
        };
        if (lifecycleAction is not null)
        {
            return lifecycleAction;
        }

        if (HttpMethods.IsPost(method))
        {
            return "created";
        }

        if (HttpMethods.IsDelete(method))
        {
            return "deleted";
        }

        return "updated";
    }

    private static string GetEntityId(HttpContext context, string[] segments, int resourceIndex)
    {
        if (segments.Skip(resourceIndex + 1).FirstOrDefault(segment => Guid.TryParse(segment, out _)) is { } id)
        {
            return id;
        }

        var location = context.Response.Headers.Location.FirstOrDefault();
        return string.IsNullOrWhiteSpace(location)
            ? "new"
            : location.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "new";
    }

    [LoggerMessage(
        EventId = 4100,
        EventName = "ContentAudit",
        Level = LogLevel.Information,
        Message = "Audit event {AuditEvent} for {EntityType} {EntityId} ({EntityName}) by user {UserId} ({UserName}) at {OccurredAt} with correlation {CorrelationId}; {RequestMethod} {RequestPath}; status {StatusCode}")]
    private static partial void ContentAudit(
        ILogger logger,
        string auditEvent,
        string entityType,
        string entityId,
        string? entityName,
        string userId,
        string? userName,
        DateTimeOffset occurredAt,
        string correlationId,
        string requestMethod,
        string requestPath,
        int statusCode);
}
