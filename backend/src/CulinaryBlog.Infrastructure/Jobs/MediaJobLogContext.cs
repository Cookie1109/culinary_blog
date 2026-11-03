using System.Text.Json;
using System.Text.Json.Nodes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

internal sealed record MediaJobLogContext(string CorrelationId, string UserId, string RequestPath)
{
    public static void Capture(MediaOutboxMessage message, HttpContext? context, string actor)
    {
        if (context is null) return;
        var payload = JsonNode.Parse(message.Payload)!.AsObject();
        payload[nameof(MediaJobLogContext)] = JsonSerializer.SerializeToNode(
            new MediaJobLogContext(context.TraceIdentifier, actor, context.Request.Path.Value ?? string.Empty));
        message.Payload = payload.ToJsonString();
    }

    public static Task<MediaOutboxMessage?> ReadAsync(AppDbContext dbContext, Guid outboxId, CancellationToken cancellationToken) =>
        dbContext.MediaOutbox.AsNoTracking().SingleOrDefaultAsync(item => item.Id == outboxId, cancellationToken);

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
