using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CulinaryBlog.Application.Media;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CulinaryBlog.IntegrationTests;

public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresSql = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_auth_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:7.4.5-alpine").Build();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgresSql.StartAsync(), _redis.StartAsync());
    }

    public Task StopRedisAsync() => _redis.StopAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await Task.WhenAll(_postgresSql.DisposeAsync().AsTask(), _redis.DisposeAsync().AsTask());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", _postgresSql.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100");
        builder.UseSetting("Email:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, InMemoryFileStorage>();
        });
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly ConcurrentDictionary<string, byte[]> _objects = new(StringComparer.Ordinal);

        public async Task UploadAsync(
            string objectKey,
            Stream content,
            long length,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            _objects[objectKey] = output.ToArray();
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(_objects[objectKey], writable: false));

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            _objects.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult(_objects.ContainsKey(objectKey));

        public async IAsyncEnumerable<string> ListKeysAsync(
            string prefix,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var key in _objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return key;
            }

            await Task.CompletedTask;
        }
    }
}
