// Culinary Blog. Authorship is recorded in the repository history.

using System.Threading.Channels;
using StackExchange.Redis;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Streams opaque content-change notifications across API replicas.</summary>
internal static class RecipeEventsEndpoint
{
    /// <summary>Maps the public event stream.</summary>
    /// <param name="endpoints">The route builder that receives the endpoint.</param>
    /// <returns>The endpoint builder.</returns>
    public static IEndpointRouteBuilder MapRecipeEventsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/recipe-events", StreamAsync).AllowAnonymous()
            .WithTags("Recipes").WithSummary("Stream committed recipe changes")
            .Produces<string>(contentType: "text/event-stream");
        return endpoints;
    }

    private static async Task StreamAsync(HttpContext context, IConnectionMultiplexer redis)
    {
        var cancellationToken = context.RequestAborted;
        var pending = Channel.CreateBounded<bool>(1);
        var subscription = await redis.GetSubscriber()
            .SubscribeAsync(RedisChannel.Literal("culinary:v1:recipe-changes")).ConfigureAwait(false);
        subscription.OnMessage(_ => pending.Writer.TryWrite(true));
        void Restored(object? sender, ConnectionFailedEventArgs args) => pending.Writer.TryWrite(true);
        redis.ConnectionRestored += Restored;
        try
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache, no-transform";
            context.Response.Headers["X-Accel-Buffering"] = "no";
            await context.Response.WriteAsync("retry: 3000\nevent: connected\ndata: {}\n\n", cancellationToken).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var ticks = 0;
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                var changed = pending.Reader.TryRead(out _);
                if (!changed && ++ticks < 15)
                {
                    continue;
                }

                ticks = 0;
                await context.Response.WriteAsync(changed ? "event: changed\ndata: {}\n\n" : ": heartbeat\n\n", cancellationToken).ConfigureAwait(false);
                await context.Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser closed the stream.
        }
        finally
        {
            redis.ConnectionRestored -= Restored;
            await subscription.UnsubscribeAsync().ConfigureAwait(false);
        }
    }
}
