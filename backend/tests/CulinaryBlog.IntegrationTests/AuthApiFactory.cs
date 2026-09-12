using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CulinaryBlog.IntegrationTests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSql = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_auth_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgresSql.StartAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgresSql.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgresSql.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100");
        builder.UseSetting("Email:Enabled", "false");
    }
}
