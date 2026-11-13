// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Caching;

/// <summary>Represents the cache metrics.</summary>
public static class CacheMetrics
{
    /// <summary>The meter name.</summary>
    public const string MeterName = "CulinaryBlog.Cache";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Counts successful cache lookups.</summary>
    internal static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits");

    /// <summary>Counts cache lookups without a stored value.</summary>
    internal static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses");

    /// <summary>Counts Redis Output Cache hits for public HTTP responses.</summary>
    internal static readonly Counter<long> OutputHits = Meter.CreateCounter<long>("output.cache.hits");

    /// <summary>Counts Redis Output Cache misses for public HTTP responses.</summary>
    internal static readonly Counter<long> OutputMisses = Meter.CreateCounter<long>("output.cache.misses");

    /// <summary>Counts failed cache reads and writes.</summary>
    internal static readonly Counter<long> OperationFailures = Meter.CreateCounter<long>("cache.operation.failures");

    /// <summary>Counts failed public cache invalidations.</summary>
    internal static readonly Counter<long> InvalidationFailures = Meter.CreateCounter<long>("cache.invalidation.failures");

    /// <summary>Records cache operation duration in milliseconds.</summary>
    internal static readonly Histogram<double> Latency = Meter.CreateHistogram<double>("cache.operation.duration", "ms");
}
