using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Api.Health;

public sealed class HealthMetricsPublisher : IHealthCheckPublisher, IDisposable
{
    public const string MeterName = "CulinaryBlog.Health";

    private readonly ConcurrentDictionary<string, int> _statuses = new(StringComparer.Ordinal);
    private readonly Meter _meter = new(MeterName);

    public HealthMetricsPublisher()
    {
        _meter.CreateObservableGauge(
            "culinary.health.check",
            ObserveChecks,
            description: "Current dependency health (1 healthy, 0 degraded or unhealthy).");
        _meter.CreateObservableGauge(
            "culinary.health.readiness",
            () => IsHealthy("postgresql") && IsHealthy("redis") ? 1 : 0,
            description: "Current readiness state (1 ready, 0 not ready).");
    }

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            _statuses[entry.Key] = entry.Value.Status == HealthStatus.Healthy ? 1 : 0;
        }

        return Task.CompletedTask;
    }

    public void Dispose() => _meter.Dispose();

    private IEnumerable<Measurement<int>> ObserveChecks() => _statuses.Select(status =>
        new Measurement<int>(status.Value, new KeyValuePair<string, object?>("dependency", status.Key)));

    private bool IsHealthy(string dependency) =>
        _statuses.TryGetValue(dependency, out var status) && status == 1;
}
