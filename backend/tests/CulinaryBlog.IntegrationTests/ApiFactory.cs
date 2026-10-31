// Culinary Blog. Authorship is recorded in the repository history.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the api factory.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Configures web host.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.ConfigureTestServices(AdapterRateLimit.Disable);

        // These adapter tests have no Redis fixture and must never use the developer's Redis.
        builder.UseSetting("ConnectionStrings:Redis", "127.0.0.1:1,abortConnect=false,connectTimeout=100,syncTimeout=100");
    }
}
