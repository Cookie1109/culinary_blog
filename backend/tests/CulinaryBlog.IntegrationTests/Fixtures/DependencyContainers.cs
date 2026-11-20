// Culinary Blog. Authorship is recorded in the repository history.

using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CulinaryBlog.IntegrationTests.Fixtures;

/// <summary>Represents the dependency containers.</summary>
public sealed class DependencyContainers : IAsyncLifetime
{
    /// <summary>Gets the postgre sql.</summary>
    public PostgreSqlContainer PostgreSql { get; } = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    /// <summary>Gets the redis.</summary>
    public RedisContainer Redis { get; } = new RedisBuilder("redis:7.4.5-alpine").Build();

    /// <summary>Gets the minio.</summary>
    public MinioContainer Minio { get; } = new MinioBuilder("minio/minio:RELEASE.2025-09-07T16-13-09Z")
        .WithUsername("culinary-test")
        .WithPassword("integration-test-only")
        .Build();

    /// <summary>Performs the initialize operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(this.PostgreSql.StartAsync(), this.Redis.StartAsync(), this.Minio.StartAsync());
    }

    /// <summary>Performs the dispose operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            this.PostgreSql.DisposeAsync().AsTask(),
            this.Redis.DisposeAsync().AsTask(),
            this.Minio.DisposeAsync().AsTask());
    }
}
