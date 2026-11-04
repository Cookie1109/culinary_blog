using System.Net;
using System.Net.Sockets;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.IntegrationTests.Fixtures;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Testcontainers.Minio;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeMediaFixture : IAsyncLifetime, IAsyncDisposable
{
    private readonly AuthApiFactory factory = new();
    public MinioContainer Minio { get; } = new MinioBuilder(MinioTestImage.Name)
        .WithUsername("media-test").WithPassword("synthetic-media-only").WithPortBinding(ReservePort(), 9000).Build();
    public WebApplicationFactory<Program> App { get; private set; } = null!;
    public ControlledMediaStorage Storage { get; private set; } = null!;
    public const string Bucket = "media-test";
    private IMinioClient client = null!;

    public async Task InitializeAsync()
    {
        await MinioTestImage.PrepareAsync();
        await Task.WhenAll(factory.InitializeAsync(), Minio.StartAsync());
        client = new MinioClient().WithEndpoint(Minio.Hostname, Minio.GetMappedPublicPort(9000))
            .WithCredentials("media-test", "synthetic-media-only").WithTimeout(2000).Build();
        await client.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket));
        Storage = new ControlledMediaStorage(new MinioFileStorageService(client, Options.Create(new ObjectStorageOptions { BucketName = Bucket })));
        App = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("RateLimiting:UploadPermitLimit", "1000");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(Storage);
                // Each test host needs its own storage; JobStorage.Current is shared across parallel fixtures.
                services.RemoveAll<JobStorage>();
                services.AddSingleton<JobStorage>(provider =>
                {
                    var options = new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true };
                    return new PostgreSqlStorage(new Hangfire.PostgreSql.Factories.NpgsqlConnectionFactory(
                        provider.GetRequiredService<IConfiguration>().GetConnectionString("Database"), options, null), options);
                });
                services.RemoveAll<IBackgroundJobClient>();
                services.AddSingleton<IBackgroundJobClient>(provider =>
                {
                    _ = provider.GetRequiredService<IGlobalConfiguration>();
                    return new BackgroundJobClient(provider.GetRequiredService<JobStorage>());
                });
            });
        });
        using var startup = App.CreateClient();
        _ = App.Services.GetRequiredService<JobStorage>();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
        await factory.DisposeAsync();
        await ((IAsyncLifetime)factory).DisposeAsync();
        client.Dispose();
        await Minio.DisposeAsync();
    }

    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    private static int ReservePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}

public sealed class ControlledMediaStorage(IFileStorageService inner) : IFileStorageService
{
    public bool FailVariants { get; set; }
    public bool FailAfterOriginal { get; set; }
    public bool FailDeletes { get; set; }
    public Func<string, Task>? AfterUpload { get; set; }

    public async Task UploadAsync(string objectKey, Stream content, long length, string contentType, CancellationToken cancellationToken)
    {
        if (FailVariants && objectKey.EndsWith(".webp", StringComparison.Ordinal) && !objectKey.Contains("original", StringComparison.Ordinal))
            throw new IOException("Controlled variant failure.");
        await inner.UploadAsync(objectKey, content, length, contentType, cancellationToken);
        if (FailAfterOriginal && objectKey.Contains("original", StringComparison.Ordinal))
            throw new IOException("Controlled failure after original storage.");
        if (AfterUpload is not null) await AfterUpload(objectKey);
    }

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken) => inner.OpenReadAsync(objectKey, cancellationToken);
    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) => FailDeletes
        ? Task.FromException(new IOException("Controlled deletion failure.")) : inner.DeleteAsync(objectKey, cancellationToken);
    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken) => inner.ExistsAsync(objectKey, cancellationToken);
    public IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken cancellationToken) => inner.ListKeysAsync(prefix, cancellationToken);
}
