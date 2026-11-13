// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for startup storage configuration tests.</summary>
[Collection(nameof(StartupStorageConfigurationScope))]
public sealed class StartupStorageConfigurationTests
{
    /// <summary>Verifies that disabled jobs resolve storage without connecting to postgres for schema preparation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task DisabledJobsResolveStorageWithoutConnectingToPostgresForSchemaPreparation()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=127.0.0.1;Port=1;Database=offline;Username=offline;Password=offline;Timeout=1",
            ["ConnectionStrings:Redis"] = "127.0.0.1:1,abortConnect=false,connectTimeout=100",
            ["BackgroundJobs:Enabled"] = "false",
            ["ObjectStorage:Endpoint"] = "http://127.0.0.1:1",
            ["Jwt:SigningKey"] = "offline-only-test-signing-key-long-enough",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();

        await Task.Run(() =>
        {
            Assert.IsType<PostgreSqlStorage>(provider.GetRequiredService<JobStorage>());
            Assert.NotNull(provider.GetRequiredService<IBackgroundJobClient>());
        }).WaitAsync(TimeSpan.FromSeconds(5));
    }
}
