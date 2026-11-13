// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Media;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for health endpoint matrix tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class HealthEndpointMatrixTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Verifies that health status controls http status without leaking dependency details.</summary>
    /// <param name="path">The path supplied to this operation.</param>
    /// <param name="healthy">The healthy supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("/health/live", true)]
    [InlineData("/health/live", false)]
    [InlineData("/health/ready", true)]
    [InlineData("/health/ready", false)]
    [InlineData("/health", true)]
    [InlineData("/health", false)]
    public async Task HealthStatusControlsHttpStatusWithoutLeakingDependencyDetails(string path, bool healthy)
    {
        await using var custom = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Clear();
                options.Registrations.Add(new HealthCheckRegistration("synthetic-dependency", _ => new ControlledHealth(healthy), null, ["live", "ready", "full"]));
            })));
        using var client = custom.CreateClient();
        using var response = await client.GetAsync(path);
        Assert.Equal(healthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(healthy ? "{\"status\":\"Healthy\"}" : "{\"status\":\"Unhealthy\"}", await response.Content.ReadAsStringAsync());
    }

    private sealed class ControlledHealth(bool healthy) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(healthy ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("sensitive synthetic dependency detail"));
    }
}
