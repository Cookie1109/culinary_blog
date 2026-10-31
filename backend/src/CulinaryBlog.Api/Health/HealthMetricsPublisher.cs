// Culinary Blog. Authorship is recorded in the repository history.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Api.Health;

/// <summary>Represents the health metrics publisher.</summary>
public sealed class HealthMetricsPublisher : IHealthCheckPublisher, IDisposable
{
    /// <summary>The meter name.</summary>
    public const string MeterName = "CulinaryBlog.Health";

    private readonly ConcurrentDictionary<string, int> statuses = new(StringComparer.Ordinal);
    private readonly Meter meter = new(MeterName);

    /// <summary>Initializes a new instance of the <see cref="HealthMetricsPublisher"/> class.</summary>
    public HealthMetricsPublisher()
    {
        this.meter.CreateObservableGauge(
            "culinary.health.check",
            this.ObserveChecks,
            description: "Current dependency health (1 healthy, 0 degraded or unhealthy).");
        this.meter.CreateObservableGauge(
            "culinary.health.readiness",
            () => this.IsHealthy("postgresql") && this.IsHealthy("redis") ? 1 : 0,
            description: "Current readiness state (1 ready, 0 not ready).");
    }

    /// <summary>Publishes the health metrics publisher.</summary>
    /// <param name="report">The report supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            this.statuses[entry.Key] = entry.Value.Status == HealthStatus.Healthy ? 1 : 0;
        }

        return Task.CompletedTask;
    }

    /// <summary>Performs the dispose operation.</summary>
    public void Dispose() => this.meter.Dispose();

    private IEnumerable<Measurement<int>> ObserveChecks() => this.statuses.Select(status =>
        new Measurement<int>(status.Value, new KeyValuePair<string, object?>("dependency", status.Key)));

    private bool IsHealthy(string dependency) =>
        this.statuses.TryGetValue(dependency, out var status) && status == 1;
}
