using System.Diagnostics.Metrics;

namespace CulinaryBlog.Infrastructure.Jobs;

public static class MediaJobMetrics
{
    public const string MeterName = "CulinaryBlog.MediaJobs";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> Completed = Meter.CreateCounter<long>("culinary.media.jobs.completed");

    public static readonly Counter<long> Failed = Meter.CreateCounter<long>("culinary.media.jobs.failed");

    public static readonly Counter<long> Orphans = Meter.CreateCounter<long>("culinary.media.orphans.detected");
}
