// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using System.Text.Json.Nodes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Carries media job log context data.</summary>
/// <param name="CorrelationId">The correlation id supplied to this operation.</param>
/// <param name="UserId">The identifier of the user performing or owning the operation.</param>
/// <param name="RequestPath">The request path supplied to this operation.</param>
internal sealed record MediaJobLogContext(string CorrelationId, string UserId, string RequestPath)
{
    /// <summary>Performs the capture operation.</summary>
    /// <param name="message">The message supplied to this operation.</param>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="actor">The actor supplied to this operation.</param>
    public static void Capture(MediaOutboxMessage message, HttpContext? context, string actor)
    {
        if (context is null)
        {
            return;
        }

        var payload = JsonNode.Parse(message.Payload)!.AsObject();
        payload[nameof(MediaJobLogContext)] = JsonSerializer.SerializeToNode(
            new MediaJobLogContext(context.TraceIdentifier, actor, context.Request.Path.Value ?? string.Empty));
        message.Payload = payload.ToJsonString();
    }

    /// <summary>Reads the media job log context.</summary>
    /// <param name="dbContext">The db context supplied to this operation.</param>
    /// <param name="outboxId">The outbox id supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public static Task<MediaOutboxMessage?> ReadAsync(AppDbContext dbContext, Guid outboxId, CancellationToken cancellationToken) =>
        dbContext.MediaOutbox.AsNoTracking().SingleOrDefaultAsync(item => item.Id == outboxId, cancellationToken);

    /// <summary>Performs the begin operation.</summary>
    /// <param name="logger">The logger supplied to this operation.</param>
    /// <param name="message">The message supplied to this operation.</param>
    /// <param name="outboxId">The outbox id supplied to this operation.</param>
    /// <returns>The begin result.</returns>
    public static IDisposable? Begin(ILogger logger, MediaOutboxMessage? message, Guid outboxId)
    {
        MediaJobLogContext? origin = null;
        if (message is not null)
        {
            try
            {
                using var payload = JsonDocument.Parse(message.Payload);
                if (payload.RootElement.ValueKind == JsonValueKind.Object &&
                    payload.RootElement.TryGetProperty(nameof(MediaJobLogContext), out var context))
                {
                    origin = context.Deserialize<MediaJobLogContext>();
                }
            }
            catch (JsonException)
            {
                // Logging metadata must not prevent the dispatcher from handling an invalid payload.
            }
        }

        return logger.BeginScope(new Dictionary<string, object>
        {
            ["OutboxId"] = outboxId,
            ["CorrelationId"] = origin?.CorrelationId ?? $"outbox-{outboxId:N}",
            ["UserId"] = origin?.UserId ?? "system",
            ["RequestPath"] = origin?.RequestPath ?? "background/media",
        });
    }
}
