// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using Hangfire.Client;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.Jobs;

/// <summary>Represents the job log context filter.</summary>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class JobLogContextFilter(ILogger<JobLogContextFilter> logger) : IServerFilter, IClientFilter
{
    private const string ScopeKey = "CulinaryBlog.JobLogScope";
    private const string ActivityKey = "CulinaryBlog.JobActivity";

    /// <summary>Persists the enqueueing activity and correlation ID in shared job storage.</summary>
    /// <param name="context">The job creation context.</param>
    public void OnCreating(CreatingContext context)
    {
        context.SetJobParameter("TraceParent", Activity.Current?.Id);
        context.SetJobParameter("CorrelationId", Activity.Current?.GetBaggageItem("correlation.id"));
    }

    /// <summary>Completes client-side job creation.</summary>
    /// <param name="context">The completed job creation context.</param>
    public void OnCreated(CreatedContext context)
    {
    }

    /// <summary>Performs the on performing operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    public void OnPerforming(PerformingContext context)
    {
        var correlationId = context.GetJobParameter<string>("CorrelationId") ?? $"job-{context.BackgroundJob.Id}";
        var activity = JobTraceContext.Start(
            context.BackgroundJob.Job.Type.Name + "." + context.BackgroundJob.Job.Method.Name,
            ActivityKind.Consumer,
            context.GetJobParameter<string>("TraceParent"),
            correlationId);
        context.Items[ActivityKey] = activity;
        context.Items[ScopeKey] = logger.BeginScope(new Dictionary<string, object>
        {
            ["JobId"] = context.BackgroundJob.Id,
            ["CorrelationId"] = correlationId,
            ["TraceId"] = activity.TraceId.ToString(),
            ["RequestPath"] = $"jobs/{context.BackgroundJob.Job.Type.Name}/{context.BackgroundJob.Job.Method.Name}",
            ["UserId"] = "system",
        });
    }

    /// <summary>Performs the on performed operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    public void OnPerformed(PerformedContext context)
    {
        if (context.Items.TryGetValue(ScopeKey, out var scope))
        {
            (scope as IDisposable)?.Dispose();
        }

        if (context.Items.TryGetValue(ActivityKey, out var activity))
        {
            (activity as Activity)?.Dispose();
        }
    }
}
