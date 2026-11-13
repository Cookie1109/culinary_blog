// Culinary Blog. Authorship is recorded in the repository history.

using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the hangfire retry probe.</summary>
internal static class HangfireRetryProbe
{
    /// <summary>Performs the fail operation.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    /// <param name="job">The job supplied to this operation.</param>
    /// <param name="queue">The queue supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public static async Task<string> FailAsync(IServiceProvider services, Job job, string queue)
    {
        var storage = services.GetRequiredService<JobStorage>();
        var jobs = services.GetRequiredService<IBackgroundJobClient>();
        var jobId = jobs.Create(job, new EnqueuedState(queue));
        var workerOptions = new BackgroundJobServerOptions
        {
            WorkerCount = 1,
            Queues = [queue],
            Activator = new ServiceActivator(services.GetRequiredService<IServiceScopeFactory>()),
        };
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            using var worker = new BackgroundJobServer(workerOptions, storage);
            var expected = attempt < 4 ? "Scheduled" : "Failed";
            var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
            using var connection = storage.GetConnection();
            while (connection.GetStateData(jobId)?.Name != expected && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(50);
            }

            var state = connection.GetStateData(jobId);
            Assert.Equal(expected, state?.Name);
            if (attempt < 4)
            {
                var delay = attempt switch { 1 => 60, 2 => 300, _ => 1800 };
                var enqueueAt = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(state!.Data["EnqueueAt"], System.Globalization.CultureInfo.InvariantCulture));
                Assert.InRange((enqueueAt - DateTimeOffset.UtcNow).TotalSeconds, delay - 10, delay + 2);

                // Promote the persisted scheduled job explicitly; this does not claim wall-clock backoff elapsed.
                worker.SendStop();
                worker.WaitForShutdown(TimeSpan.FromSeconds(10));
                Assert.True(jobs.ChangeState(jobId, new EnqueuedState(queue), "Scheduled"));
            }
        }

        return jobId;
    }

    /// <summary>Performs the recover operation.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    /// <param name="jobId">The job id supplied to this operation.</param>
    /// <param name="queue">The queue supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public static async Task RecoverAsync(IServiceProvider services, string jobId, string queue)
    {
        var storage = services.GetRequiredService<JobStorage>();
        Assert.True(services.GetRequiredService<IBackgroundJobClient>().ChangeState(jobId, new EnqueuedState(queue), "Failed"));
        using var worker = new BackgroundJobServer(
            new BackgroundJobServerOptions
            {
                WorkerCount = 1,
                Queues = [queue],
                Activator = new ServiceActivator(services.GetRequiredService<IServiceScopeFactory>()),
            },
            storage);
        using var connection = storage.GetConnection();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (connection.GetStateData(jobId)?.Name != "Succeeded" && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(50);
        }

        Assert.Equal("Succeeded", connection.GetStateData(jobId)?.Name);
    }

    private sealed class ServiceActivator(IServiceScopeFactory factory) : JobActivator
    {
        public override JobActivatorScope BeginScope(JobActivatorContext context) => new Scope(factory.CreateScope());

        private sealed class Scope(IServiceScope scope) : JobActivatorScope
        {
            public override object Resolve(Type type) => scope.ServiceProvider.GetRequiredService(type);

            public override void DisposeScope() => scope.Dispose();
        }
    }
}
