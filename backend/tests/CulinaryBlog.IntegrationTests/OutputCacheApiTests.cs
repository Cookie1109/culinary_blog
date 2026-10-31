// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Caching;
using CulinaryBlog.Infrastructure.Caching;
using CulinaryBlog.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for output cache api tests.</summary>
/// <param name="factory">The factory supplied to this operation.</param>
public sealed class OutputCacheApiTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that redis output responses have resource ttls are shared and invalidated after writes.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RedisOutputResponsesHaveResourceTtlsAreSharedAndInvalidatedAfterWrites()
    {
        var outputHits = 0L;
        var outputMisses = 0L;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meter) =>
        {
            if (instrument.Meter.Name == CacheMetrics.MeterName)
            {
                meter.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            if (instrument.Name == "output.cache.hits")
            {
                Interlocked.Add(ref outputHits, value);
            }
            else if (instrument.Name == "output.cache.misses")
            {
                Interlocked.Add(ref outputMisses, value);
            }
        });
        listener.Start();
        using var first = factory.CreateClient();
        using var secondFactory = factory.WithWebHostBuilder(builder => builder.UseSetting("Database:ApplyMigrationsOnStartup", "false"));
        using var second = secondFactory.CreateClient();
        var categories = await first.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        var category = categories.GetProperty("data")[0];
        var categoryId = category.GetProperty("id").GetGuid();
        var categorySlug = category.GetProperty("slug").GetString();
        var suffix = Guid.NewGuid().ToString("N");
        using var registration = await first.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email = $"output-{suffix}@example.test",
            password = "Synthetic-Only-123!",
            displayName = "Cache author",
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var session = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        first.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        var body = new
        {
            title = "Cache recipe " + suffix,
            description = "Public cache mutation integration recipe.",
            categoryId,
            prepTime = 10,
            cookTime = 20,
            servings = 4,
            difficulty = "easy",
            instructions = "Cook thoroughly.",
        };
        using var created = await first.PostAsJsonAsync("/api/v1/recipes", body);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var recipe = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
        var id = recipe.GetProperty("id").GetGuid();
        var slug = recipe.GetProperty("slug").GetString();
        using var ingredient = await Mutation(first, HttpMethod.Post, $"/api/v1/recipes/{id}/ingredients", 1, new { name = "Rice", orderIndex = 0 });
        Assert.Equal(HttpStatusCode.Created, ingredient.StatusCode);
        using var step = await Mutation(first, HttpMethod.Post, $"/api/v1/recipes/{id}/steps", 2, new { title = "Cook", description = "Cook rice." });
        Assert.Equal(HttpStatusCode.Created, step.StatusCode);
        using var published = await Mutation(first, HttpMethod.Patch, $"/api/v1/recipes/{id}/publish", 3);
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);

        var redis = factory.Services.GetRequiredService<IConnectionMultiplexer>();
        var database = redis.GetDatabase();
        using var guest = factory.CreateClient();
        foreach (var (route, seconds) in new[]
        {
            ("/api/v1/categories", 1800), ($"/api/v1/categories/{categorySlug}", 1800),
            ($"/api/v1/recipes/{slug}", 3600), ($"/api/v1/recipes?categoryId={categoryId}", 900),
            ($"/api/v1/recipes/search?q={suffix}", 60),
        })
        {
            var before = await OutputKeys(redis);
            var url = route + (route.Contains('?', StringComparison.Ordinal) ? '&' : '?') + "probe=" + suffix;
            using var miss = await guest.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, miss.StatusCode);
            Assert.Null(miss.Headers.Age);
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Correlation-ID", "second-instance-" + suffix);
            using var hit = await second.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, hit.StatusCode);
            Assert.NotNull(hit.Headers.Age);
            Assert.True(Interlocked.Read(ref outputHits) > 0);
            Assert.True(Interlocked.Read(ref outputMisses) > 0);
            Assert.Equal("second-instance-" + suffix, Assert.Single(hit.Headers.GetValues("X-Correlation-ID")));
            var added = (await OutputKeys(redis)).Except(before).ToArray();
            Assert.NotEmpty(added);
            var values = new List<TimeSpan>();
            foreach (var key in added)
            {
                if (await database.KeyTypeAsync(key) == RedisType.String)
                {
                    values.Add((await database.KeyTimeToLiveAsync(key))!.Value);
                }
            }

            Assert.Contains(values, ttl => ttl.TotalSeconds > seconds - 15 && ttl.TotalSeconds <= seconds);
        }

        await using var versionScope = factory.Services.CreateAsyncScope();
        var generation = await versionScope.ServiceProvider.GetRequiredService<IPublicCacheVersion>().GetAsync();
        var dataKey = "culinary:v1:" + RedisApplicationCache.DataKey(PublicCacheKeys.Recipe(slug!), generation);
        var dataTtl = await database.KeyTimeToLiveAsync(dataKey);
        Assert.NotNull(dataTtl);
        Assert.InRange(dataTtl.Value.TotalSeconds, 280, 300);
        await factory.Services.GetRequiredService<IOutputCacheStore>().EvictByTagAsync($"recipe:{slug}", CancellationToken.None);
        using var evicted = await second.GetAsync($"/api/v1/recipes/{slug}?probe={suffix}");
        Assert.Null(evicted.Headers.Age);

        using var mine = await first.GetAsync($"/api/v1/me/recipes/{id}");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        Assert.True(mine.Headers.CacheControl?.NoStore);
        Assert.True(mine.Headers.CacheControl?.Private);
        Assert.Null(mine.Headers.Age);
        using var publicWithToken = await first.GetAsync($"/api/v1/recipes/{slug}?probe={suffix}");
        Assert.Null(publicWithToken.Headers.Age);
        Assert.True(publicWithToken.Headers.CacheControl?.NoStore);
        using var invalid = await guest.GetAsync("/api/v1/recipes?page=-1");
        using var invalidAgain = await second.GetAsync("/api/v1/recipes?page=-1");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidAgain.StatusCode);
        Assert.Null(invalid.Headers.Age);
        Assert.Null(invalidAgain.Headers.Age);

        foreach (var pauseRedis in new[] { false, true })
        {
            long versionBefore;
            await using (var beforeScope = factory.Services.CreateAsyncScope())
            {
                versionBefore = await beforeScope.ServiceProvider.GetRequiredService<IPublicCacheVersion>().GetAsync();
            }

            var name = $"Updated author {suffix} {pauseRedis}";
            var avatar = $"https://example.test/{suffix}/{pauseRedis}.webp";
            var bio = "Public author biography " + pauseRedis;
            if (pauseRedis)
            {
                await factory.PauseRedisAsync();
            }

            try
            {
                using var profile = await first.PatchAsJsonAsync("/api/v1/auth/me", new { displayName = name, avatarUrl = avatar, bio });
                Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
            }
            finally
            {
                if (pauseRedis)
                {
                    await factory.ResumeRedisAsync();
                }
            }

            Assert.Equal("PONG", (await database.ExecuteAsync("PING")).ToString());
            foreach (var reader in new[] { second, guest })
            {
                foreach (var route in new[] { $"/api/v1/recipes/{slug}", $"/api/v1/recipes?categoryId={categoryId}", $"/api/v1/recipes/search?q={suffix}" })
                {
                    var result = (await reader.GetFromJsonAsync<JsonElement>(route + (route.Contains('?', StringComparison.Ordinal) ? '&' : '?') + "probe=" + suffix)).GetProperty("data");
                    var item = result.ValueKind == JsonValueKind.Array ? Assert.Single(result.EnumerateArray(), entry => entry.GetProperty("id").GetGuid() == id) : result;
                    Assert.Equal(name, item.GetProperty("author").GetProperty("displayName").GetString());
                    Assert.Equal(avatar, item.GetProperty("author").GetProperty("avatarUrl").GetString());
                    Assert.Equal(bio, item.GetProperty("author").GetProperty("bio").GetString());
                }
            }

            await using var afterScope = factory.Services.CreateAsyncScope();
            Assert.True(await afterScope.ServiceProvider.GetRequiredService<IPublicCacheVersion>().GetAsync() > versionBefore);
        }

        using var update = await Mutation(first, HttpMethod.Put, $"/api/v1/recipes/{id}", 4, body with { title = "Updated cache " + suffix });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        foreach (var client in new[] { guest, second })
        {
            var updated = await client.GetFromJsonAsync<JsonElement>($"/api/v1/recipes/{slug}?probe={suffix}");
            Assert.Equal("Updated cache " + suffix, updated.GetProperty("data").GetProperty("title").GetString());
        }

        using var archived = await Mutation(first, HttpMethod.Patch, $"/api/v1/recipes/{id}/archive", 5);
        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        foreach (var client in new[] { guest, second })
        {
            using var hidden = await client.GetAsync($"/api/v1/recipes/{slug}?probe={suffix}");
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
            Assert.Null(hidden.Headers.Age);
            var search = await client.GetFromJsonAsync<JsonElement>($"/api/v1/recipes/search?q={suffix}&probe={suffix}");
            Assert.Empty(search.GetProperty("data").EnumerateArray());
        }
    }

    /// <summary>Verifies that redis outage keeps database reads and committed writes and emits failure metrics.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task RedisOutageKeepsDatabaseReadsAndCommittedWritesAndEmitsFailureMetrics()
    {
        using var isolated = new AuthApiFactory();
        await isolated.InitializeAsync();
        var paused = false;
        try
        {
            using var application = isolated.WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:Redis", isolated.RedisConnectionString + ",asyncTimeout=200,syncTimeout=200"));
            using var client = application.CreateClient();
            var suffix = Guid.NewGuid().ToString("N");
            using var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email = $"outage-{suffix}@example.test",
                password = "Synthetic-Only-123!",
                displayName = "Outage author",
            });
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            var token = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("accessToken").GetString();
            var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
            var id = categories.GetProperty("data")[0].GetProperty("id").GetGuid();
            var failures = 0L;
            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meter) =>
            {
                if (instrument.Meter.Name == CacheMetrics.MeterName)
                {
                    meter.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
            {
                if (instrument.Name is "cache.operation.failures" or "cache.invalidation.failures")
                {
                    Interlocked.Add(ref failures, value);
                }
            });
            listener.Start();
            await isolated.PauseRedisAsync();
            paused = true;
            using var read = await client.GetAsync("/api/v1/recipes?probe=" + suffix);
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
            Assert.Null(read.Headers.Age);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var write = await client.PostAsJsonAsync("/api/v1/recipes", new
            {
                title = "Outage write " + suffix,
                description = "Database commit must survive cache outage.",
                categoryId = id,
                prepTime = 10,
                cookTime = 10,
                servings = 4,
                difficulty = "easy",
                instructions = "Cook.",
            });
            Assert.Equal(HttpStatusCode.Created, write.StatusCode);
            var recipeId = (await write.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("id").GetGuid();
            using var committed = await client.GetAsync($"/api/v1/me/recipes/{recipeId}");
            Assert.Equal(HttpStatusCode.OK, committed.StatusCode);
            Assert.True(Interlocked.Read(ref failures) > 0);
            await isolated.ResumeRedisAsync();
            paused = false;
            client.DefaultRequestHeaders.Authorization = null;
            var deadline = DateTimeOffset.UtcNow.AddSeconds(15);
            while (!application.Services.GetRequiredService<IConnectionMultiplexer>().IsConnected && DateTimeOffset.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            using var recovered = await client.GetAsync("/api/v1/recipes?probe=recovered-" + suffix);
            using var cachedAgain = await client.GetAsync("/api/v1/recipes?probe=recovered-" + suffix);
            Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
            Assert.NotNull(cachedAgain.Headers.Age);
        }
        finally
        {
            if (paused)
            {
                await isolated.ResumeRedisAsync();
            }

            await ((IAsyncLifetime)isolated).DisposeAsync();
        }
    }

    /// <summary>Verifies that unpublish during redis timeout cannot resurrect published data after recovery.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task UnpublishDuringRedisTimeoutCannotResurrectPublishedDataAfterRecovery()
    {
        using var isolated = new AuthApiFactory();
        await isolated.InitializeAsync();
        var paused = false;
        try
        {
            using var firstFactory = isolated.WithWebHostBuilder(builder => builder.UseSetting(
                "ConnectionStrings:Redis", isolated.RedisConnectionString + ",asyncTimeout=200,syncTimeout=200"));
            using var first = firstFactory.CreateClient();
            using var secondFactory = firstFactory.WithWebHostBuilder(builder => builder.UseSetting("Database:ApplyMigrationsOnStartup", "false"));
            using var second = secondFactory.CreateClient();
            var suffix = Guid.NewGuid().ToString("N");
            var email = $"cache-recovery-{suffix}@example.test";
            using var registration = await first.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email,
                password = "Synthetic-Only-123!",
                displayName = "Recovery Admin",
            });
            Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
            var session = (await registration.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");
            await using (var scope = firstFactory.Services.CreateAsyncScope())
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await users.FindByIdAsync(session.GetProperty("user").GetProperty("id").GetString()!);
                Assert.NotNull(user);
                Assert.True((await users.AddToRoleAsync(user, "Admin")).Succeeded);
            }

            using var login = await first.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Synthetic-Only-123!" });
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data").GetProperty("accessToken").GetString();
            var list = await first.GetFromJsonAsync<JsonElement>("/api/v1/recipes");
            var recipe = list.GetProperty("data")[0];
            var slug = recipe.GetProperty("slug").GetString();
            var path = $"/api/v1/recipes/{slug}";
            using var warm = await first.GetAsync(path);
            using var shared = await second.GetAsync(path);
            Assert.NotNull(shared.Headers.Age);

            await isolated.PauseRedisAsync();
            paused = true;
            first.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var unpublish = await Mutation(
                first,
                HttpMethod.Patch,
                $"/api/v1/recipes/{recipe.GetProperty("id").GetGuid()}/unpublish",
                recipe.GetProperty("version").GetInt32());
            Assert.Equal(HttpStatusCode.OK, unpublish.StatusCode);
            await isolated.ResumeRedisAsync();
            paused = false;
            first.DefaultRequestHeaders.Authorization = null;

            // Let commands that timed out while Redis was paused finish processing.
            Assert.Equal("PONG", (await firstFactory.Services.GetRequiredService<IConnectionMultiplexer>().GetDatabase().ExecuteAsync("PING")).ToString());

            // Read the non-writing instance first: a local pending-invalidation list is insufficient.
            foreach (var client in new[] { second, first })
            {
                using var response = await client.GetAsync(path);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            }
        }
        finally
        {
            if (paused)
            {
                await isolated.ResumeRedisAsync();
            }

            await ((IAsyncLifetime)isolated).DisposeAsync();
        }
    }

    private static async Task<HashSet<RedisKey>> OutputKeys(IConnectionMultiplexer redis)
    {
        var keys = new HashSet<RedisKey>();
        await foreach (var key in redis.GetServer(redis.GetEndPoints()[0]).KeysAsync(pattern: "culinary:output:v1:*"))
        {
            keys.Add(key);
        }

        return keys;
    }

    private static async Task<HttpResponseMessage> Mutation(HttpClient client, HttpMethod method, string path, int version, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("If-Match", $"\"{version}\"");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request);
    }
}
