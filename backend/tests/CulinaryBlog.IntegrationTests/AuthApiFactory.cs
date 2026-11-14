// Culinary Blog. Authorship is recorded in the repository history.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Media;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.PostgreSql.Factories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the auth api factory.</summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgresSql = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_auth_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    private readonly RedisContainer redis = new RedisBuilder("redis:7.4.5-alpine").Build();

    private JobStorage? jobStorage;

    /// <summary>Gets the password reset emails.</summary>
    public ConcurrentQueue<(string Email, string Token)> PasswordResetEmails { get; } = new();

    /// <summary>Gets the redis connection string.</summary>
    public string RedisConnectionString => this.redis.GetConnectionString();

    /// <summary>Performs the initialize operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(this.postgresSql.StartAsync(), this.redis.StartAsync());
        var storageOptions = new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true };

        // Workers stay disabled; manual job tests still need an isolated Hangfire schema.
        this.jobStorage = new PostgreSqlStorage(
            new NpgsqlConnectionFactory(this.postgresSql.GetConnectionString(), storageOptions, null), storageOptions);
    }

    /// <summary>Performs the stop redis operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task StopRedisAsync() => this.redis.StopAsync();

    /// <summary>Performs the pause redis operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task PauseRedisAsync() => this.redis.PauseAsync();

    /// <summary>Performs the resume redis operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task ResumeRedisAsync() => this.redis.UnpauseAsync();

    /// <summary>Performs the dispose operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    async Task IAsyncLifetime.DisposeAsync()
    {
        (this.jobStorage as IDisposable)?.Dispose();
        await Task.WhenAll(this.postgresSql.DisposeAsync().AsTask(), this.redis.DisposeAsync().AsTask());
    }

    /// <summary>Configures web host.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", this.postgresSql.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", this.redis.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100");
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
        builder.UseSetting("Email:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            // Multiple test hosts must not inherit another host's process-global storage.
            services.RemoveAll<JobStorage>();
            services.AddSingleton(this.jobStorage ?? throw new InvalidOperationException("Initialize the isolated job storage before starting the test host."));
            services.RemoveAll<IPasswordResetEmailSender>();
            services.AddSingleton<IPasswordResetEmailSender>(new CapturedPasswordResetEmailSender(this.PasswordResetEmails));
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, InMemoryFileStorage>();
        });
    }

    private sealed class CapturedPasswordResetEmailSender(ConcurrentQueue<(string Email, string Token)> messages)
        : IPasswordResetEmailSender
    {
        public bool IsEnabled => true;

        public Task SendAsync(string email, string token, CancellationToken cancellationToken)
        {
            messages.Enqueue((email, token));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly ConcurrentDictionary<string, byte[]> objects = new(StringComparer.Ordinal);

        public async Task UploadAsync(
            string objectKey,
            Stream content,
            long length,
            string contentType,
            CancellationToken cancellationToken)
        {
            using var output = new MemoryStream();
            await content.CopyToAsync(output, cancellationToken);
            this.objects[objectKey] = output.ToArray();
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream(this.objects[objectKey], writable: false));

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            this.objects.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) =>
            Task.FromResult(this.objects.ContainsKey(objectKey));

        public async IAsyncEnumerable<string> ListKeysAsync(
            string prefix,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var key in this.objects.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return key;
            }

            await Task.CompletedTask;
        }
    }
}
