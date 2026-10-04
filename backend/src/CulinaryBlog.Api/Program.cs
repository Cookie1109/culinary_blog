using System.Globalization;
using CulinaryBlog.Api.Health;
using CulinaryBlog.Api.Middleware;
using CulinaryBlog.Api.Presentation;
using CulinaryBlog.Api.Security;
using CulinaryBlog.Api.Telemetry;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Jobs;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    ProductionConfigurationGuard.Validate(builder.Environment.IsProduction(), builder.Configuration);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "CulinaryBlog.Api")
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .WriteTo.File(
                new RenderedCompactJsonFormatter(),
                "logs/api-.json",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14);

        var seqUrl = context.Configuration["Seq:ServerUrl"];
        if (!string.IsNullOrWhiteSpace(seqUrl))
        {
            loggerConfiguration.WriteTo.Seq(seqUrl, formatProvider: CultureInfo.InvariantCulture);
        }
    });

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddPresentation(builder.Configuration)
        .AddCulinaryTelemetry(builder.Configuration);

    var app = builder.Build();

    if (builder.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders"))
    {
        app.UseForwardedHeaders();
    }
    if (app.Environment.IsProduction())
    {
        app.UseHsts();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
            diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
            diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? "anonymous");
        };
    });
    app.UseExceptionHandler();
    app.UseCors("Frontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<AuditMutationMiddleware>();

    var backgroundJobsEnabled = builder.Configuration.GetValue("BackgroundJobs:Enabled", true);
    if (backgroundJobsEnabled)
    {
        app.UseHangfireDashboard("/jobs", new DashboardOptions
        {
            Authorization = [new AdminDashboardAuthorizationFilter()],
            DashboardTitle = "Culinary Blog Jobs",
        });
    }

    if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>()
            .InitializeAsync(CancellationToken.None)
            .ConfigureAwait(false);
    }

    app.MapAuthEndpoints();
    app.MapContentEndpoints();

    if (backgroundJobsEnabled)
    {
        RecurringJob.AddOrUpdate<ImageReconciliationJob>(
            "media-reconciliation",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);
        RecurringJob.AddOrUpdate<SitemapGenerationJob>(
            "sitemap-generation",
            job => job.GenerateAsync(CancellationToken.None),
            "0 2 * * *",
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
        BackgroundJob.Enqueue<SitemapGenerationJob>(job => job.GenerateAsync(CancellationToken.None));
    }

    app.MapGet("/api/v1", () => Results.Ok(new
    {
        data = new { service = "CulinaryBlog.Api", version = "1.0.0" },
        meta = new { },
    }));
    app.MapGet("/api/v1/release", (IConfiguration configuration) => Results.Ok(new
    {
        data = new
        {
            commitSha = configuration["Release:CommitSha"],
            apiImageDigest = configuration["Release:ApiImageDigest"],
            webImageDigest = configuration["Release:WebImageDigest"],
        },
        meta = new { },
    }));
    app.MapGet("/api/v1/operations/maintenance", (IConfiguration configuration) =>
    {
        var section = configuration.GetSection("Operations:Maintenance");
        var enabled = section.GetValue<bool>("Enabled");
        var endsAtUtc = section.GetValue<DateTimeOffset?>("EndsAtUtc");

        return Results.Ok(new
        {
            data = new
            {
                enabled = enabled && endsAtUtc > DateTimeOffset.UtcNow,
                message = section["Message"],
                announcedAtUtc = section.GetValue<DateTimeOffset?>("AnnouncedAtUtc"),
                startsAtUtc = section.GetValue<DateTimeOffset?>("StartsAtUtc"),
                endsAtUtc,
            },
            meta = new { },
        });
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("live"),
        ResponseWriter = HealthResponseWriter.WriteAggregateAsync,
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
        ResponseWriter = HealthResponseWriter.WriteAggregateAsync,
    });
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("full"),
        ResponseWriter = HealthResponseWriter.WriteAggregateAsync,
    });

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    Log.Fatal(exception, "CulinaryBlog API terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}

public partial class Program;
