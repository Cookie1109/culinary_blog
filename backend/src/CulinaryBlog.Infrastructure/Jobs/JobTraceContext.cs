// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Restores persisted request context across outbox dispatch and Hangfire execution.</summary>
public static class JobTraceContext
{
    /// <summary>The activity source exported by the API telemetry pipeline.</summary>
    public const string SourceName = "CulinaryBlog.Jobs";

    private static readonly ActivitySource Source = new(SourceName);

    /// <summary>Starts a span and restores context even when an exporter is disabled.</summary>
    /// <param name="name">The dispatch or job operation.</param>
    /// <param name="kind">The producer or consumer span kind.</param>
    /// <param name="parentId">The persisted W3C parent.</param>
    /// <param name="correlationId">The originating request correlation ID.</param>
    /// <returns>The activity disposed at the operation boundary.</returns>
    internal static Activity Start(string name, ActivityKind kind, string? parentId, string? correlationId)
    {
        var activity = Source.StartActivity(name, kind, parentId);
        if (activity is null)
        {
            activity = new Activity(name);
            if (parentId is not null)
            {
                activity.SetParentId(parentId);
            }

            activity.Start();
        }

        if (correlationId is not null)
        {
            activity.SetTag("correlation.id", correlationId);
            activity.AddBaggage("correlation.id", correlationId);
        }

        return activity;
    }
}
