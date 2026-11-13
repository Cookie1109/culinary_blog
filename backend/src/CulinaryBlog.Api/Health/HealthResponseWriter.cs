// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Api.Health;

/// <summary>Represents the health response writer.</summary>
internal static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Performs the write aggregate operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <param name="report">The report supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public static Task WriteAggregateAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            new { status = report.Status.ToString() },
            SerializerOptions,
            context.RequestAborted);
    }
}
