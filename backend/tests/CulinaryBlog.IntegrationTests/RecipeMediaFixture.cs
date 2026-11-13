// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Sockets;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Media;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.IntegrationTests.Fixtures;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using Testcontainers.Minio;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Represents the recipe media fixture.</summary>
public sealed class RecipeMediaFixture : IAsyncLifetime, IAsyncDisposable
{
    /// <summary>The bucket.</summary>
    public const string Bucket = "media-test";
    private readonly AuthApiFactory factory = new();
    private IMinioClient client = null!;

    /// <summary>Gets the minio.</summary>
    public MinioContainer Minio { get; } = new MinioBuilder(MinioTestImage.Name)
        .WithUsername("media-test").WithPassword("synthetic-media-only").WithPortBinding(ReservePort(), 9000).Build();

    /// <summary>Gets the app.</summary>
    public WebApplicationFactory<Program> App { get; private set; } = null!;

    /// <summary>Gets the storage.</summary>
    public ControlledMediaStorage Storage { get; private set; } = null!;

    /// <summary>Performs the initialize operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InitializeAsync()
    {
        await MinioTestImage.PrepareAsync();
        await Task.WhenAll(this.factory.InitializeAsync(), this.Minio.StartAsync());
        this.client = new MinioClient().WithEndpoint(this.Minio.Hostname, this.Minio.GetMappedPublicPort(9000))
            .WithCredentials("media-test", "synthetic-media-only").WithTimeout(2000).Build();
        await this.client.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket));
        this.Storage = new ControlledMediaStorage(new MinioFileStorageService(this.client, Options.Create(new ObjectStorageOptions { BucketName = Bucket })));
        this.App = this.factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:GlobalPermitLimit", "10000");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1000");
            builder.UseSetting("RateLimiting:UploadPermitLimit", "1000");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService>(this.Storage);
            });
        });
        using var startup = this.App.CreateClient();
        await using var scope = this.App.Services.CreateAsyncScope();
        var options = new PostgreSqlStorageOptions { PrepareSchemaIfNecessary = true };
        _ = new PostgreSqlStorage(
            new Hangfire.PostgreSql.Factories.NpgsqlConnectionFactory(
                scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.GetConnectionString(), options, null),
            options);
    }

    /// <summary>Performs the dispose operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task DisposeAsync()
    {
        await this.App.DisposeAsync();
        await this.factory.DisposeAsync();
        await ((IAsyncLifetime)this.factory).DisposeAsync();
        this.client.Dispose();
        await this.Minio.DisposeAsync();
    }

    /// <summary>Performs the dispose operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    ValueTask IAsyncDisposable.DisposeAsync() => new(this.DisposeAsync());

    private static int ReservePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
