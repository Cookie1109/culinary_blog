using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var databaseConnection = RequireConnectionString(configuration, "Database");
        var redisConnection = RequireConnectionString(configuration, "Redis");

        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetRequiredSection(ObjectStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(databaseConnection));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "culinary:v1:";
        });
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IApplicationCache, RedisApplicationCache>();

        services.AddSingleton<IMinioClient>(serviceProvider =>
        {
            var storage = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<ObjectStorageOptions>>()
                .Value;
            var endpoint = new Uri(storage.Endpoint, UriKind.Absolute);

            return new MinioClient()
                .WithEndpoint(endpoint.Host, endpoint.Port)
                .WithCredentials(storage.AccessKey, storage.SecretKey)
                .WithSSL(endpoint.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                .Build();
        });

        services.AddHealthChecks()
            .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready", "full"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready", "full"])
            .AddCheck<MinioHealthCheck>("minio", tags: ["full"]);

        return services;
    }

    private static string RequireConnectionString(IConfiguration configuration, string name)
    {
        var value = configuration.GetConnectionString(name);
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Connection string '{name}' is required.");
    }
}
