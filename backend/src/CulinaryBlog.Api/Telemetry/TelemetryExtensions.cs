// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Api.Health;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Content;
using CulinaryBlog.Infrastructure.Jobs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CulinaryBlog.Api.Telemetry;

/// <summary>Represents the telemetry extensions.</summary>
internal static class TelemetryExtensions
{
    /// <summary>Adds culinary telemetry.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    /// <param name="configuration">The configuration supplied to this operation.</param>
    /// <returns>The add culinary telemetry result.</returns>
    public static IServiceCollection AddCulinaryTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["Telemetry:OtlpEndpoint"];
        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("CulinaryBlog.Api"));

        openTelemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(JobTraceContext.SourceName)
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation();

            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
            }
        });

        openTelemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter(HealthMetricsPublisher.MeterName)
                .AddMeter(CacheMetrics.MeterName)
                .AddMeter(MediaJobMetrics.MeterName)
                .AddMeter(ContentMetrics.MeterName);
            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
            }
        });

        return services;
    }
}
