// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics.Metrics;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the media job metrics.</summary>
public static class MediaJobMetrics
{
    /// <summary>The meter name.</summary>
    public const string MeterName = "CulinaryBlog.MediaJobs";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>The completed.</summary>
    public static readonly Counter<long> Completed = Meter.CreateCounter<long>("culinary.media.jobs.completed");

    /// <summary>The failed.</summary>
    public static readonly Counter<long> Failed = Meter.CreateCounter<long>("culinary.media.jobs.failed");

    /// <summary>The orphans.</summary>
    public static readonly Counter<long> Orphans = Meter.CreateCounter<long>("culinary.media.orphans.detected");
}
