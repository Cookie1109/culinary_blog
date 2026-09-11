using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CulinaryBlog.Api.Telemetry;

internal static class TelemetryExtensions
{
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
            metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
            if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
            }
        });

        return services;
    }
}
