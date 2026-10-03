using System.Security.Claims;

namespace CulinaryBlog.Api.Middleware;

public sealed partial class AuditMutationMiddleware(
    RequestDelegate next,
    ILogger<AuditMutationMiddleware> logger,
    TimeProvider timeProvider)
{
    public async Task InvokeAsync(HttpContext context)
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
        if (resourceTailLength > 2 ||
            (resourceTailLength == 2 && segments[^1] is not ("publish" or "unpublish" or "archive" or "unarchive")))
        {
            return;
        }

        var entityType = segments[resourceIndex] == "recipes" ? "recipe" : "category";
        var action = GetAction(context.Request.Method, segments);
        var entityId = GetEntityId(context, segments, resourceIndex);
        var userId = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "system";
        var auditEvent = $"{entityType}.{action}";
        var occurredAt = timeProvider.GetUtcNow();

        ContentAudit(
            logger,
            auditEvent,
            entityType,
            entityId,
            userId,
            occurredAt,
            context.TraceIdentifier,
            context.Request.Method,
            path);
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
        Message = "Audit event {AuditEvent} for {EntityType} {EntityId} by user {UserId} at {OccurredAt} with correlation {CorrelationId}; {RequestMethod} {RequestPath}")]
    private static partial void ContentAudit(
        ILogger logger,
        string auditEvent,
        string entityType,
        string entityId,
        string userId,
        DateTimeOffset occurredAt,
        string correlationId,
        string requestMethod,
        string requestPath);
}
