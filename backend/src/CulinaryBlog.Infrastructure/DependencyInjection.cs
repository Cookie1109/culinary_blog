using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Configuration;
using CulinaryBlog.Infrastructure.Content;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetRequiredSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<AdminSeedOptions>()
            .Bind(configuration.GetSection(AdminSeedOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) => options
            .UseNpgsql(databaseConnection)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>()));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<AppDbContext>();
        services.Configure<PasswordHasherOptions>(options =>
        {
            options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            options.IterationCount = 100_000;
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddScoped<DatabaseInitializer>();
        services.AddHostedService<WelcomeEmailWorker>();

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
