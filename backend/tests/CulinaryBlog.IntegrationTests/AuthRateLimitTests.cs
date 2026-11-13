// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for auth rate limit tests.</summary>
/// <param name="fixture">The fixture supplied to this operation.</param>
public sealed class AuthRateLimitTests(RateLimitRedisFixture fixture) : IClassFixture<RateLimitRedisFixture>
{
    private static int address;

    /// <summary>Verifies that auth quota is shared across instances and returns problem and headers.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AuthQuotaIsSharedAcrossInstancesAndReturnsProblemAndHeaders()
    {
        using var first = new RateApiFactory(fixture);
        using var second = new RateApiFactory(fixture);
        using var a = first.CreateClient();
        using var b = second.CreateClient();
        var ip = NewIp();
        for (var index = 0; index < 11; index++)
        {
            using var request = Login(ip);
            request.Headers.Add("X-Forwarded-For", NewIp()); // An untrusted peer cannot choose a new partition.
            using var response = await (index % 2 == 0 ? a : b).SendAsync(request);
            Assert.Equal(index < 10 ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("10", Header(response, "X-RateLimit-Limit"));
            Assert.Equal(Math.Max(0, 9 - index).ToString(System.Globalization.CultureInfo.InvariantCulture), Header(response, "X-RateLimit-Remaining"));
            Assert.InRange(
                long.Parse(Header(response, "X-RateLimit-Reset"), System.Globalization.CultureInfo.InvariantCulture),
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DateTimeOffset.UtcNow.AddSeconds(61).ToUnixTimeSeconds());
            if (index == 10)
            {
                Assert.InRange(int.Parse(Header(response, "Retry-After"), System.Globalization.CultureInfo.InvariantCulture), 1, 60);
                Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
                var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(429, problem.GetProperty("status").GetInt32());
                Assert.Equal("RATE_LIMIT_EXCEEDED", problem.GetProperty("code").GetString());
                Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
            }
        }
    }

    /// <summary>Verifies that auth sliding window retains recent requests and expires old requests individually.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task AuthSlidingWindowRetainsRecentRequestsAndExpiresOldRequestsIndividually()
    {
        using var factory = new RateApiFactory(fixture);
        using var client = factory.CreateClient();
        var ip = NewIp();
        var key = $"culinary:rate:v1:{{{IPAddress.Parse(ip).MapToIPv6()}}}:auth";
        await using (var db = fixture.Context())
        {
            var peer = IPAddress.Parse(ip).MapToIPv6().ToString();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "SharedRateLimitState" ("Peer", "AuthEvents")
                SELECT {peer}, array_agg(floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint - age)
                FROM unnest(ARRAY[60000,60000,60000,60000,60000,1000,1000,1000,1000,1000]) age
                """);
        }

        // Use Redis's clock and control stored event times, without changing production windows.
        await fixture.Redis.GetDatabase().ScriptEvaluateAsync(
            """
            local t = redis.call('TIME')
            local now = t[1] * 1000 + math.floor(t[2] / 1000)
            for i=1,5 do redis.call('ZADD', KEYS[1], now - 60000, 'expired' .. i) end
            for i=1,5 do redis.call('ZADD', KEYS[1], now - 1000, 'recent' .. i) end
            return 1
            """,
            [key]);
        for (var index = 0; index < 6; index++)
        {
            using var request = Login(ip);
            using var response = await client.SendAsync(request);
            Assert.Equal(index < 5 ? HttpStatusCode.UnprocessableEntity : HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var events = await fixture.Redis.GetDatabase().SortedSetRangeByRankAsync(key);
        Assert.Equal(10, events.Length);
        Assert.Equal(5, events.Count(value => value.ToString().StartsWith("recent", StringComparison.Ordinal)));
        Assert.DoesNotContain(events, value => value.ToString().StartsWith("expired", StringComparison.Ordinal));
    }

    /// <summary>Verifies that global fixed quota is one hundred across two instances.</summary>
    /// <param name="path">The path supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("/api/v1")]
    [InlineData("/api/v1/categories")]
    public async Task GlobalFixedQuotaIsOneHundredAcrossTwoInstances(string path)
    {
        using var first = new RateApiFactory(fixture);
        using var second = new RateApiFactory(fixture);
        using var a = first.CreateClient();
        using var b = second.CreateClient();
        var ip = NewIp();
        for (var index = 0; index < 101; index++)
        {
            using var request = Request(HttpMethod.Get, path, ip);
            using var response = await (index % 2 == 0 ? a : b).SendAsync(request);
            Assert.Equal(index < 100 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("100", Header(response, "X-RateLimit-Limit"));
            Assert.Equal(Math.Max(0, 99 - index).ToString(System.Globalization.CultureInfo.InvariantCulture), Header(response, "X-RateLimit-Remaining"));
        }
    }

    /// <summary>Verifies that global fixed window resets at shared clock minute boundary.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task GlobalFixedWindowResetsAtSharedClockMinuteBoundary()
    {
        using var factory = new RateApiFactory(fixture, globalLimit: 1);
        using var client = factory.CreateClient();
        var ip = NewIp();
        var key = $"culinary:rate:v1:{{{IPAddress.Parse(ip).MapToIPv6()}}}:global";
        await using (var db = fixture.Context())
        {
            var peer = IPAddress.Parse(ip).MapToIPv6().ToString();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "SharedRateLimitState" ("Peer", "GlobalWindow", "GlobalCount")
                VALUES ({peer}, floor(extract(epoch FROM clock_timestamp()) / 60)::bigint - 1, 1)
                """);
        }

        await fixture.Redis.GetDatabase().ScriptEvaluateAsync(
            """
            local t = redis.call('TIME')
            redis.call('HSET', KEYS[1], 'window', math.floor(t[1] / 60) - 1, 'count', 1)
            return 1
            """,
            [key]);
        using var request = Request(HttpMethod.Get, "/api/v1", ip);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("0", Header(response, "X-RateLimit-Remaining"));
    }

    /// <summary>Verifies that health is exempt and upload has shared five request quota.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task HealthIsExemptAndUploadHasSharedFiveRequestQuota()
    {
        using var first = new RateApiFactory(fixture, globalLimit: 1);
        using var a = first.CreateClient();
        var healthIp = NewIp();
        for (var index = 0; index < 3; index++)
        {
            using var request = Request(HttpMethod.Get, "/health/live", healthIp);
            using var response = await a.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains("X-RateLimit-Remaining"));
        }

        using var general = Request(HttpMethod.Get, "/api/v1", healthIp);
        using var generalResponse = await a.SendAsync(general);
        Assert.Equal(HttpStatusCode.OK, generalResponse.StatusCode);

        using var second = new RateApiFactory(fixture);
        using var third = new RateApiFactory(fixture);
        using var b = second.CreateClient();
        using var c = third.CreateClient();
        var uploadIp = NewIp();
        for (var index = 0; index < 6; index++)
        {
            using var request = Request(HttpMethod.Post, $"/api/v1/recipes/{Guid.NewGuid()}/images", uploadIp);
            using var response = await (index % 2 == 0 ? b : c).SendAsync(request);
            Assert.Equal(index < 5 ? HttpStatusCode.Unauthorized : HttpStatusCode.TooManyRequests, response.StatusCode);
            Assert.Equal("5", Header(response, "X-RateLimit-Limit"));
        }
    }

    /// <summary>Verifies that only known proxy can supply client ip and cors exposes quota headers.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task OnlyKnownProxyCanSupplyClientIpAndCorsExposesQuotaHeaders()
    {
        using var factory = new RateApiFactory(fixture, globalLimit: 1, trustProxy: true);
        using var client = factory.CreateClient();
        var untrusted = NewIp();
        for (var index = 0; index < 2; index++)
        {
            using var request = Request(HttpMethod.Get, "/api/v1", untrusted);
            request.Headers.Add("X-Forwarded-For", NewIp());
            using var response = await client.SendAsync(request);
            Assert.Equal(index == 0 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var firstIp = NewIp();
        var forwarded = new[] { firstIp, NewIp(), firstIp };
        for (var index = 0; index < forwarded.Length; index++)
        {
            using var request = Request(HttpMethod.Get, "/api/v1", "127.0.0.1");
            request.Headers.Add("X-Forwarded-For", forwarded[index]);
            request.Headers.Add("Origin", "https://app.example.test");
            using var response = await client.SendAsync(request);
            Assert.Equal(index < 2 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests, response.StatusCode);
            var exposed = Header(response, "Access-Control-Expose-Headers");
            foreach (var header in new[] { "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "Retry-After" })
            {
                Assert.Contains(header, exposed, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    /// <summary>Verifies that global and auth limits compose and concurrent acquisition is atomic.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task GlobalAndAuthLimitsComposeAndConcurrentAcquisitionIsAtomic()
    {
        using var low = new RateApiFactory(fixture, globalLimit: 1);
        using var client = low.CreateClient();
        var ip = NewIp();
        using var first = Login(ip);
        using var allowed = await client.SendAsync(first);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, allowed.StatusCode);
        Assert.Equal("1", Header(allowed, "X-RateLimit-Limit"));
        using var second = Login(ip);
        using var rejected = await client.SendAsync(second);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("1", Header(rejected, "X-RateLimit-Limit"));

        using var a = new RateApiFactory(fixture);
        using var b = new RateApiFactory(fixture);
        using var ca = a.CreateClient();
        using var cb = b.CreateClient();
        var concurrentIp = NewIp();
        var statuses = await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
        {
            using var request = Login(concurrentIp);
            using var response = await (index % 2 == 0 ? ca : cb).SendAsync(request);
            return response.StatusCode;
        }));
        Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.UnprocessableEntity));
        Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.TooManyRequests));
    }

    /// <summary>Verifies that redis outage and recovery preserve shared quota and headers.</summary>
    /// <param name="policy">The policy supplied to this operation.</param>
    /// <param name="limit">The limit supplied to this operation.</param>
    /// <param name="acceptedStatus">The accepted status supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("", 100, HttpStatusCode.OK)]
    [InlineData("auth", 10, HttpStatusCode.UnprocessableEntity)]
    [InlineData("upload", 5, HttpStatusCode.Unauthorized)]
    public async Task RedisOutageAndRecoveryPreserveSharedQuotaAndHeaders(string policy, int limit, HttpStatusCode acceptedStatus)
    {
        // Keep fixed-window assertions away from the real minute boundary; never change production windows.
        var second = DateTimeOffset.UtcNow.Second;
        if (second >= 55)
        {
            await Task.Delay(TimeSpan.FromSeconds(61 - second));
        }

        using var firstFactory = new RateApiFactory(fixture, redisTimeout: 200);
        using var secondFactory = new RateApiFactory(fixture, redisTimeout: 200);
        using var a = firstFactory.CreateClient();
        using var b = secondFactory.CreateClient();
        var ip = NewIp();
        for (var index = 0; index < limit - 2; index++)
        {
            using var request = QuotaRequest(policy, ip);
            using var response = await (index % 2 == 0 ? a : b).SendAsync(request);
            Assert.Equal(acceptedStatus, response.StatusCode);
        }

        await fixture.PauseAsync();
        try
        {
            for (var index = limit - 2; index <= limit; index++)
            {
                using var request = QuotaRequest(policy, ip);
                using var response = await (index % 2 == 0 ? a : b).SendAsync(request);
                Assert.Equal(index < limit ? acceptedStatus : HttpStatusCode.TooManyRequests, response.StatusCode);
                Assert.Equal(limit.ToString(System.Globalization.CultureInfo.InvariantCulture), Header(response, "X-RateLimit-Limit"));
                Assert.Equal(Math.Max(0, limit - index - 1).ToString(System.Globalization.CultureInfo.InvariantCulture), Header(response, "X-RateLimit-Remaining"));
                Assert.True(long.Parse(Header(response, "X-RateLimit-Reset"), System.Globalization.CultureInfo.InvariantCulture) >= DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                if (index == limit)
                {
                    Assert.InRange(int.Parse(Header(response, "Retry-After"), System.Globalization.CultureInfo.InvariantCulture), 1, 60);
                }
            }
        }
        finally
        {
            await fixture.ResumeAsync();
        }

        using var afterRecovery = QuotaRequest(policy, ip);
        using var stillLimited = await b.SendAsync(afterRecovery);
        Assert.Equal(HttpStatusCode.TooManyRequests, stillLimited.StatusCode);
        Assert.Equal("0", Header(stillLimited, "X-RateLimit-Remaining"));
        using var independent = QuotaRequest(policy, NewIp());
        using var available = await a.SendAsync(independent);
        Assert.Equal(acceptedStatus, available.StatusCode);
    }

    /// <summary>Verifies that concurrent redis outage across instances does not create extra auth permits.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task ConcurrentRedisOutageAcrossInstancesDoesNotCreateExtraAuthPermits()
    {
        using var firstFactory = new RateApiFactory(fixture, redisTimeout: 200);
        using var secondFactory = new RateApiFactory(fixture, redisTimeout: 200);
        using var a = firstFactory.CreateClient();
        using var b = secondFactory.CreateClient();
        var ip = NewIp();
        await fixture.PauseAsync();
        try
        {
            var statuses = await Task.WhenAll(Enumerable.Range(0, 20).Select(async index =>
            {
                using var request = Login(ip);
                using var response = await (index % 2 == 0 ? a : b).SendAsync(request);
                Assert.Equal("10", Header(response, "X-RateLimit-Limit"));
                return response.StatusCode;
            }));
            Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.UnprocessableEntity));
            Assert.Equal(10, statuses.Count(status => status == HttpStatusCode.TooManyRequests));
        }
        finally
        {
            await fixture.ResumeAsync();
        }

        using var requestAfterRecovery = Login(ip);
        using var responseAfterRecovery = await b.SendAsync(requestAfterRecovery);
        Assert.Equal(HttpStatusCode.TooManyRequests, responseAfterRecovery.StatusCode);
    }

    /// <summary>Verifies that cleanup deletes expired peers without resetting active quotas.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task CleanupDeletesExpiredPeersWithoutResettingActiveQuotas()
    {
        await using var db = fixture.Context();
        var expired = NewIp();
        var active = NewIp();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "SharedRateLimitState" ("Peer", "ExpiresAtMilliseconds") VALUES
            ({expired}, 0), ({active}, floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint + 120000)
            """);
        await new CulinaryBlog.Infrastructure.RateLimiting.RateLimitStateCleanupJob(db).RunAsync(CancellationToken.None);
        var expiredCount = await db.Database.SqlQuery<int>($"SELECT count(*)::integer AS \"Value\" FROM \"SharedRateLimitState\" WHERE \"Peer\" = {expired}").SingleAsync();
        var activeCount = await db.Database.SqlQuery<int>($"SELECT count(*)::integer AS \"Value\" FROM \"SharedRateLimitState\" WHERE \"Peer\" = {active}").SingleAsync();
        Assert.Equal(0, expiredCount);
        Assert.Equal(1, activeCount);
    }

    private static string NewIp() => $"198.51.100.{Interlocked.Increment(ref address)}";

    private static HttpRequestMessage QuotaRequest(string policy, string ip) => policy switch
    {
        "auth" => Login(ip),
        "upload" => Request(HttpMethod.Post, $"/api/v1/recipes/{Guid.NewGuid()}/images", ip),
        _ => Request(HttpMethod.Get, "/api/v1", ip),
    };

    private static string Header(HttpResponseMessage response, string name) => Assert.Single(response.Headers.GetValues(name));

    private static HttpRequestMessage Login(string ip)
    {
        var request = Request(HttpMethod.Post, "/api/v1/auth/login", ip);
        request.Content = JsonContent.Create(new { });
        return request;
    }

    private static HttpRequestMessage Request(HttpMethod method, string path, string peer)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Test-Peer", peer);
        return request;
    }

    private sealed class RateApiFactory(RateLimitRedisFixture dependencies, int globalLimit = 100, bool trustProxy = false, int redisTimeout = 1000) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("BackgroundJobs:Enabled", "false");
            builder.UseSetting("ConnectionStrings:Redis", dependencies.ConnectionString + $",asyncTimeout={redisTimeout},syncTimeout={redisTimeout}");
            builder.UseSetting("ConnectionStrings:Database", dependencies.DatabaseConnection);
            builder.UseSetting("RateLimiting:GlobalPermitLimit", globalLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("ReverseProxy:TrustForwardedHeaders", trustProxy.ToString());
            builder.UseSetting("Cors:AllowedOrigins:0", "https://app.example.test");
            builder.ConfigureTestServices(services => services.AddSingleton<IStartupFilter, PeerFilter>());
        }
    }

    private sealed class PeerFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextRequest) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["X-Test-Peer"].FirstOrDefault() ?? "127.0.0.1");
                return nextRequest(context);
            });
            next(app);
        };
    }
}
