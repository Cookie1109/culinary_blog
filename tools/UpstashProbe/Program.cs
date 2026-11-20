using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--ping-only"))
    throw new ArgumentException("Expected private env file path and optional --ping-only.");
var pingOnly = args.Length == 2;
var file = Path.GetFullPath(args[0]);
var values = File.ReadLines(file).Where(line => line.Length > 0 && !line.StartsWith('#'))
    .Select(line => line.Split('=', 2)).ToDictionary(parts => parts[0], parts => parts[1]);
var uri = new Uri(values["UPSTASH_REDIS_REST_URL"]);
if (uri.Scheme != "https" || !Regex.IsMatch(uri.Host, @"^[a-z0-9-]+\.upstash\.io$"))
    throw new ArgumentException("Expected Upstash HTTPS URL.");
var password = values.GetValueOrDefault("UPSTASH_REDIS_PASSWORD");
if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("TCP credential missing in private env file.");
var options = new ConfigurationOptions
{
    User = "default", Password = password, Ssl = true, SslHost = uri.Host,
    AbortOnConnectFail = false, ConnectTimeout = 5000, SyncTimeout = 5000,
    AsyncTimeout = 5000, ConnectRetry = 1,
};
options.EndPoints.Add(uri.Host, 6379);
var prefix = $"culinary:upstash-probe:{Guid.NewGuid():N}:";
var checks = new Dictionary<string, bool>();
var report = new Dictionary<string, object?>
{
    ["checkedAtUtc"] = DateTimeOffset.UtcNow,
    ["clientVersion"] = typeof(ConnectionMultiplexer).Assembly.GetName().Version?.ToString(),
    ["ssl"] = true, ["certificateValidationBypassed"] = false,
    ["database"] = 0, ["checks"] = checks, ["prefix"] = prefix,
    ["sourceRedisChanged"] = false, ["renderChanged"] = false,
    ["postgresMinioChanged"] = false,
};
ConnectionMultiplexer? connection = null;
ServiceProvider? services = null;
RedLockFactory? locks = null;
var cleanup = new List<RedisKey>();
var stage = "connect";
try
{
    connection = await ConnectionMultiplexer.ConnectAsync(options);
    var db = connection.GetDatabase();
    await db.PingAsync();
    checks["tcpAuthenticationAndPing"] = true;
    if (pingOnly)
    {
        report["passed"] = true;
        report["redisDataWritesPerformed"] = false;
        return;
    }
    stage = "set-get-ttl-delete";
    var basic = (RedisKey)(prefix + "value"); cleanup.Add(basic);
    checks["set"] = await db.StringSetAsync(basic, "demo-only", TimeSpan.FromSeconds(120), When.NotExists);
    checks["get"] = await db.StringGetAsync(basic) == "demo-only";
    var ttl = await db.KeyTimeToLiveAsync(basic);
    checks["ttl"] = ttl > TimeSpan.Zero && ttl <= TimeSpan.FromSeconds(120);
    checks["delete"] = await db.KeyDeleteAsync(basic) && !await db.KeyExistsAsync(basic);

    stage = "distributed-cache";
    var collection = new ServiceCollection();
    collection.AddLogging();
    collection.AddStackExchangeRedisCache(cache =>
    {
        cache.ConfigurationOptions = options.Clone();
        cache.InstanceName = prefix + "cache:";
    });
    collection.AddStackExchangeRedisOutputCache(cache =>
    {
        cache.ConfigurationOptions = options.Clone();
        cache.InstanceName = prefix + "output:";
    });
    services = collection.BuildServiceProvider();
    var cache = services.GetRequiredService<IDistributedCache>();
    cleanup.Add(prefix + "cache:item");
    await cache.SetStringAsync("item", "cached-demo", new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(120),
    });
    checks["distributedCacheWriteRead"] = await cache.GetStringAsync("item") == "cached-demo";
    await cache.RemoveAsync("item");
    checks["distributedCacheRemove"] = await cache.GetAsync("item") is null;

    stage = "tag-sets-and-lua";
    var tag = (RedisKey)(prefix + "tag");cleanup.Add(tag);
    await db.SetAddAsync(tag, "demo-member");
    await db.KeyExpireAsync(tag, TimeSpan.FromSeconds(120));
    checks["tagSets"] = await db.SetContainsAsync(tag, "demo-member");
    var counter = (RedisKey)(prefix + "counter");cleanup.Add(counter);
    var script = "local v=redis.call('HINCRBY',KEYS[1],'count',1);redis.call('PEXPIRE',KEYS[1],120000);return v";
    checks["luaHashAndExpiry"] = (long)await db.ScriptEvaluateAsync(script, [counter]) == 1;
    var sliding = (RedisKey)(prefix + "sliding");cleanup.Add(sliding);
    var window = "redis.call('ZREMRANGEBYSCORE',KEYS[1],'-inf',0);redis.call('ZADD',KEYS[1],1,'demo');redis.call('PEXPIRE',KEYS[1],120000);return redis.call('ZCARD',KEYS[1])";
    checks["luaSortedSetAndExpiry"] = (long)await db.ScriptEvaluateAsync(window, [sliding]) == 1;

    stage = "output-cache-tag-eviction";
    var output = services.GetRequiredService<IOutputCacheStore>();
    await output.SetAsync("page", "demo-page"u8.ToArray(), ["demo-tag"], TimeSpan.FromSeconds(120), default);
    checks["outputCacheWriteRead"] = (await output.GetAsync("page", default))?.SequenceEqual("demo-page"u8.ToArray()) == true;
    await output.EvictByTagAsync("demo-tag", default);
    checks["outputCacheTagEviction"] = await output.GetAsync("page", default) is null;

    stage = "redlock";
    locks = RedLockFactory.Create([new RedLockMultiplexer(connection)]);
    var resource = prefix + "lock";
    await using (var first = await locks.CreateLockAsync(resource, TimeSpan.FromSeconds(10)))
    {
        checks["redlockAcquire"] = first.IsAcquired;
        await using var second = await locks.CreateLockAsync(resource, TimeSpan.FromSeconds(10));
        checks["redlockMutualExclusion"] = !second.IsAcquired;
    }
    await using (var after = await locks.CreateLockAsync(resource, TimeSpan.FromSeconds(10)))
        checks["redlockReleaseAndReacquire"] = after.IsAcquired;

    stage = "prepare-render-value";
    var serialized = options.ToString(includePassword: true);
    var roundTrip = ConfigurationOptions.Parse(serialized);
    checks["connectionStringRoundTrip"] = roundTrip.Password == password && roundTrip.Ssl && roundTrip.User == "default";
    if (checks.Values.Any(passed => !passed)) throw new InvalidOperationException("A compatibility check failed.");
    var destination = Path.Combine(Path.GetDirectoryName(file)!, "render-redis-connection.txt");
    await File.WriteAllTextAsync(destination, serialized + Environment.NewLine);
    report["privateRenderValuePrepared"] = true;
    report["passed"] = true;
}
catch (Exception exception)
{
    // Redis exception messages may contain endpoint/configuration; emit type/stage only.
    report["passed"] = false;
    report["failureStage"] = stage;
    report["failureType"] = exception.GetType().Name;
    Environment.ExitCode = 1;
}
finally
{
    locks?.Dispose();
    if (services is not null) await services.DisposeAsync();
    if (!pingOnly && connection is not null && connection.IsConnected)
    {
        try
        {
            var db = connection.GetDatabase();
            if (cleanup.Count > 0) await db.KeyDeleteAsync(cleanup.ToArray());
            // Restrict the server-side scan to this random probe namespace only.
            var server = connection.GetServer(uri.Host, 6379);
            var remaining = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: prefix + "*", pageSize: 100)) remaining.Add(key);
            if (remaining.Count > 0) await db.KeyDeleteAsync(remaining.ToArray());
            var leftovers = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: prefix + "*", pageSize: 100)) leftovers.Add(key);
            report["ownNamespaceCleaned"] = leftovers.Count == 0;
            if (leftovers.Count != 0) { report["passed"] = false; Environment.ExitCode = 1; }
        }
        catch (Exception exception)
        {
            report["cleanupFailureType"] = exception.GetType().Name;
            report["passed"] = false; Environment.ExitCode = 1;
        }
    }
    connection?.Dispose();
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(file)!, pingOnly ? "tcp-ping.json" : "tcp-check.json"), json);
    Console.WriteLine(json);
}
