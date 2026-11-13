// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using CulinaryBlog.Infrastructure.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Verifies media outbox recovery after a database connection failure.</summary>
/// <param name="factory">Supplies the isolated database and shared job storage.</param>
public sealed class MediaOutboxResilienceTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that batch failure keeps the worker alive and the next tick dispatches persisted data.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BatchFailureKeepsWorkerAliveAndNextTickDispatchesPersistedMessage()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = new MediaOutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = MediaOutboxTypes.DeleteObjects,
            Payload = JsonSerializer.Serialize(new DeleteObjectsPayload([])),
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
        };
        db.MediaOutbox.Add(message);
        await db.SaveChangesAsync();
        var connection = "Host=127.0.0.1;Port=1;Database=unavailable;Username=synthetic;Timeout=1";
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));
        services.AddSingleton(factory.Services.GetRequiredService<IBackgroundJobClient>());
        await using var provider = services.BuildServiceProvider();
        using var worker = new MediaOutboxDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            NullLogger<MediaOutboxDispatcher>.Instance);
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await Task.WhenAny(worker.ExecuteTask!, Task.Delay(TimeSpan.FromSeconds(2)));
            Assert.False(worker.ExecuteTask!.IsCompleted, "A failed batch must not terminate the hosted service.");
            connection = db.Database.GetConnectionString()!;
            var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
            do
            {
                await db.Entry(message).ReloadAsync();
                if (message.ProcessedAt.HasValue)
                {
                    break;
                }

                await Task.Delay(100);
            }
            while (DateTimeOffset.UtcNow < deadline);

            Assert.NotNull(message.ProcessedAt);
            Assert.False(worker.ExecuteTask.IsCompleted);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
