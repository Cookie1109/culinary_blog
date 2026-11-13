// Culinary Blog. Authorship is recorded in the repository history.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CulinaryBlog.Api.Errors;
using CulinaryBlog.Api.Health;
using CulinaryBlog.Api.Telemetry;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the dependency injection.</summary>
public static class DependencyInjection
{
    /// <summary>Adds presentation.</summary>
    /// <param name="services">The services supplied to this operation.</param>
    /// <param name="configuration">The configuration supplied to this operation.</param>
    /// <returns>The add presentation result.</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowedOrigins.Any(origin =>
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new InvalidOperationException("CORS allowed origins must be absolute HTTP or HTTPS origins.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            foreach (var proxy in configuration.GetSection("ReverseProxy:KnownProxies").Get<string[]>() ?? [])
            {
                options.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
            }

            foreach (var network in configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>() ?? [])
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });
        services.AddHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });
        services.AddCors(options => options.AddPolicy("Frontend", policy =>
        {
            if (allowedOrigins.Length == 0)
            {
                policy.SetIsOriginAllowed(_ => false);
            }
            else
            {
                policy.WithOrigins(allowedOrigins);
            }

            policy
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type", "If-Match", "X-Correlation-ID")
                .WithExposedHeaders(
                    "ETag",
                    "Retry-After",
                    "X-Correlation-ID",
                    "X-RateLimit-Limit",
                    "X-RateLimit-Remaining",
                    "X-RateLimit-Reset");
        }));
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("code", "HTTP_ERROR");
                context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.TraceIdentifier);
            };
        });
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.Configure<FormOptions>(options =>
        {
            options.MemoryBufferThreshold = 64 * 1024;
            options.MultipartBodyLengthLimit = 6 * 1024 * 1024;
        });
        services.AddHttpContextAccessor();
        services.AddOutputCache(options =>
        {
            options.MaximumBodySize = 1024 * 1024;
            options.AddPolicy("Categories", policy => policy.Expire(TimeSpan.FromMinutes(30))
                .Tag(PublicCacheKeys.CategoriesTag, PublicCacheKeys.RecipesTag).VaryByValue(ContentVersionAsync));
            options.AddPolicy("RecipeList", policy => policy.Expire(TimeSpan.FromMinutes(15))
                .Tag(PublicCacheKeys.RecipesTag, PublicCacheKeys.CategoriesTag).VaryByValue(ContentVersionAsync));

            // FR-RCP-002 specifies HTTP output 60 minutes; NFR-PERF-003 specifies data cache 5 minutes.
            options.AddPolicy("RecipeDetail", policy => policy.Expire(TimeSpan.FromMinutes(60))
                .Tag(PublicCacheKeys.RecipesTag, PublicCacheKeys.CategoriesTag, "recipes").VaryByValue(ContentVersionAsync));
            options.AddPolicy("Search", policy => policy.Expire(TimeSpan.FromMinutes(1))
                .Tag(PublicCacheKeys.RecipesTag, PublicCacheKeys.CategoriesTag).VaryByValue(ContentVersionAsync));
        });
        services.AddTransient<CorrelationIdDelegatingHandler>();
        services.AddHttpClient("default").AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

        var jwtOptions = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is required.");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };
                options.Events = JwtProblemDetailsEvents.Create();
            });
        services.AddAuthorizationBuilder()
            .AddPolicy("ActiveUser", policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement()))
            .AddPolicy("AuthorPolicy", policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("Author", "Admin")
                .AddRequirements(new ActiveUserRequirement()))
            .AddPolicy("AdminPolicy", policy => policy
                .RequireAuthenticatedUser()
                .RequireRole("Admin")
                .AddRequirements(new ActiveUserRequirement()))
            .AddPolicy("RecipeOwnerPolicy", policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new RecipeOwnerRequirement()));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Endpoint metadata selects the additional shared window in the global limiter.
            options.AddPolicy("auth", _ => RateLimitPartition.GetNoLimiter("auth"));
            options.AddPolicy("upload", _ => RateLimitPartition.GetNoLimiter("upload"));
            options.OnRejected = async (context, cancellationToken) =>
            {
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        type = "https://httpstatuses.com/429",
                        title = "Too many requests",
                        status = 429,
                        code = "RATE_LIMIT_EXCEEDED",
                        traceId = context.HttpContext.TraceIdentifier,
                    },
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken).ConfigureAwait(false);
            };
        });
        services.AddSingleton<DistributedRateLimiter>();
        services.AddOptions<RateLimiterOptions>().Configure<DistributedRateLimiter>((options, limiter) => options.GlobalLimiter = limiter);

        services.AddSingleton<IHealthCheckPublisher, HealthMetricsPublisher>();
        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(5);
            options.Period = TimeSpan.FromSeconds(10);
        });
        services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        return services;
    }

    private static async ValueTask<KeyValuePair<string, string>> ContentVersionAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var version = await context.RequestServices.GetRequiredService<IPublicCacheVersion>().GetAsync(cancellationToken).ConfigureAwait(false);
        return new("public-content-generation", version.ToString(CultureInfo.InvariantCulture));
    }
}
