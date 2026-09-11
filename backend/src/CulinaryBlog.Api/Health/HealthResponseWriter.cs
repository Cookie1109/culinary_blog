using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.Api.Health;

internal static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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
