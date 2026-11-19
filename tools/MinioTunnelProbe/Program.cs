using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using CulinaryBlog.Application.Media;
using CulinaryBlog.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Calls the application's real DI/MinIO adapter. No database, API server or jobs are started.
if (args.Length != 3 || args[0] is not ("hold" or "verify-delete"))
{
    Console.Error.WriteLine("Usage: MinioTunnelProbe hold|verify-delete <private-env-file> <checkpoint-json>");
    return 2;
}
var mode = args[0];
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
var ct = timeout.Token;
try
{
    var values = File.ReadAllLines(args[1]).Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
        .Select(line => line.Split('=', 2)).ToDictionary(pair => pair[0], pair => (string?)pair[1]);
    foreach (var field in new[] { "Endpoint", "AccessKey", "SecretKey", "BucketName" })
        if (string.IsNullOrWhiteSpace(values.GetValueOrDefault("ObjectStorage__" + field))) throw new InvalidDataException();
    var settings = values.ToDictionary(pair => pair.Key.Replace("__", ":"), pair => pair.Value);
    settings["ConnectionStrings:Database"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused;Password=unused";
    settings["ConnectionStrings:Redis"] = "127.0.0.1:1,abortConnect=false";
    settings["BackgroundJobs:Enabled"] = "false";
    settings["Jwt:SigningKey"] = "unused-probe-config-no-api-server-started";
    var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    await using var services = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    var storage = services.GetRequiredService<IFileStorageService>();
    var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j6XcAAAAASUVORK5CYII=");
    var expectedHash = Convert.ToHexString(SHA256.HashData(png));
    string key;
    if (mode == "hold")
    {
        key = $"demo-tunnel/{Guid.NewGuid():N}/pixel.png";
        if (File.Exists(args[2]))
        {
            using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(args[2], ct));
            key = previous.RootElement.GetProperty("key").GetString()!;
            if (previous.RootElement.GetProperty("sha256").GetString() != expectedHash) throw new InvalidDataException();
        }
        if (!Regex.IsMatch(key, "^demo-tunnel/[a-f0-9]{32}/pixel\\.png$")) throw new InvalidDataException();
        // Save first so a failed/partial upload remains recoverable using verify-delete.
        await File.WriteAllTextAsync(args[2], JsonSerializer.Serialize(new { key, sha256 = expectedHash }), ct);
        await using var input = new MemoryStream(png);
        await storage.UploadAsync(key, input, png.Length, "image/png", ct);
    }
    else
    {
        using var checkpoint = JsonDocument.Parse(await File.ReadAllTextAsync(args[2], ct));
        key = checkpoint.RootElement.GetProperty("key").GetString()!;
        if (checkpoint.RootElement.GetProperty("sha256").GetString() != expectedHash) throw new InvalidDataException();
    }
    if (!Regex.IsMatch(key, "^demo-tunnel/[a-f0-9]{32}/pixel\\.png$")) throw new InvalidDataException();
    if (!await storage.ExistsAsync(key, ct)) throw new IOException("Demo object missing.");
    await using var downloaded = await storage.OpenReadAsync(key, ct);
    if (Convert.ToHexString(await SHA256.HashDataAsync(downloaded, ct)) != expectedHash) throw new IOException("Byte mismatch.");
    var listed = false;
    await foreach (var item in storage.ListKeysAsync(key[..key.LastIndexOf('/')], ct)) listed |= item == key;
    if (!listed) throw new IOException("Demo object not listed.");
    if (mode == "verify-delete")
    {
        await storage.DeleteAsync(key, ct);
        await storage.DeleteAsync(key, ct);
        if (await storage.ExistsAsync(key, ct)) throw new IOException("Demo object still exists.");
    }
    Console.WriteLine(JsonSerializer.Serialize(new { mode, endpoint = values["ObjectStorage__Endpoint"], adapter = "MinioFileStorageService", upload = mode == "hold", downloadByteExact = true, exists = true, listed = true, deleted = mode == "verify-delete" }));
    return 0;
}
catch (Exception exception)
{
    // Provider exception messages can contain signed requests; never dump them.
    var code = exception.Message.Contains("SignatureDoesNotMatch", StringComparison.Ordinal) ? "SignatureDoesNotMatch" : exception.GetType().Name;
    Console.Error.WriteLine($"MinIO probe failed: {code}. Check endpoint, gateway, credentials and checkpoint; no secret details logged.");
    return 1;
}
