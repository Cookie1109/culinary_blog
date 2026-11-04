using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CulinaryBlog.IntegrationTests.Fixtures;

public sealed class DependencyContainers : IAsyncLifetime
{
    public PostgreSqlContainer PostgreSql { get; } = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    public RedisContainer Redis { get; } = new RedisBuilder("redis:7.4.5-alpine").Build();

    public MinioContainer Minio { get; } = new MinioBuilder(MinioTestImage.Name)
        .WithUsername("culinary-test")
        .WithPassword("integration-test-only")
        .Build();

    public async Task InitializeAsync()
    {
        await MinioTestImage.PrepareAsync();
        await Task.WhenAll(PostgreSql.StartAsync(), Redis.StartAsync(), Minio.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            PostgreSql.DisposeAsync().AsTask(),
            Redis.DisposeAsync().AsTask(),
            Minio.DisposeAsync().AsTask());
    }
}
