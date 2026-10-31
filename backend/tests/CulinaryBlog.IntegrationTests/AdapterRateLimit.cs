// Culinary Blog. Authorship is recorded in the repository history.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the adapter rate limit.</summary>
internal static class AdapterRateLimit
{
    // Documentation/security-header adapters have no dependency containers.
    // Quotas are exercised separately against real PostgreSQL/Redis in AuthRateLimitTests.

    /// <summary>Performs the disable operation.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    public static void Disable(IServiceCollection services) => services.PostConfigure<RateLimiterOptions>(options =>
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ => RateLimitPartition.GetNoLimiter("adapter-only")));
}
