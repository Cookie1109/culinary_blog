// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Checks encrypted key persistence independently of container filesystems.</summary>
public sealed class DataProtectionPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("g06_keyring_test").WithUsername("culinary").WithPassword("integration-test-only").Build();

    /// <inheritdoc />
    public Task InitializeAsync() => this.postgres.StartAsync();

    /// <inheritdoc />
    public Task DisposeAsync() => this.postgres.DisposeAsync().AsTask();

    /// <summary>Rejects missing or public-only certificates instead of storing plaintext keys.</summary>
    [Fact]
    public void DatabaseModeRejectsMissingOrPublicOnlyCertificate()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var services = BuildServices(this.postgres.GetConnectionString(), null, "unused");
        });
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=G06 test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var services = BuildServices(this.postgres.GetConnectionString(), Convert.ToBase64String(publicOnly.Export(X509ContentType.Pfx)), "unused");
        });
    }

    /// <summary>Reuses encrypted keys and reset tokens after provider recreation and database restore.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task KeysAndResetTokensSurviveProviderRecreationAndCleanRestore()
    {
        var connection = await this.CreateDatabaseAsync();
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=G06 test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
        var secret = Convert.ToBase64String(certificate.Export(X509ContentType.Pfx));
        var blockedPath = Path.GetTempFileName();
        try
        {
            string payload;
            string resetToken;
            string userId;
            await using (var first = BuildServices(connection, secret, blockedPath))
            {
                await using var scope = first.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                var applicationMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
                var keyDb = scope.ServiceProvider.GetRequiredService<DataProtectionKeyDbContext>();
                await keyDb.Database.MigrateAsync();
                await keyDb.Database.GetService<IMigrator>().MigrateAsync("0");
                await keyDb.Database.MigrateAsync();
                Assert.Equal(applicationMigrations, (await db.Database.GetAppliedMigrationsAsync()).ToArray());
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "keyring@example.test", Email = "keyring@example.test", DisplayName = "G06 fixture" };
                Assert.True((await users.CreateAsync(user, "Synthetic#G06Password1")).Succeeded);
                userId = user.Id.ToString();
                resetToken = await users.GeneratePasswordResetTokenAsync(user);
                payload = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("G06 fixture").Protect("synthetic payload");
                var xml = await keyDb.DataProtectionKeys.Select(key => key.Xml!).ToArrayAsync();
                Assert.NotEmpty(xml);
                Assert.True(xml.All(value => value.Contains("encryptedSecret", StringComparison.Ordinal)), "Keys must be encrypted at rest.");
                Assert.True(xml.All(value => !value.Contains("<masterKey", StringComparison.Ordinal)), "No plaintext master key may be stored.");
                await Assert.ThrowsAsync<PostgresException>(() => keyDb.Database.GetService<IMigrator>().MigrateAsync("0"));
                Assert.NotEmpty(await keyDb.DataProtectionKeys.ToArrayAsync());
            }

            await using (var second = BuildServices(connection, secret, blockedPath))
            {
                Assert.Equal("synthetic payload", second.GetRequiredService<IDataProtectionProvider>().CreateProtector("G06 fixture").Unprotect(payload));
                await using var scope = second.CreateAsyncScope();
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = (await users.FindByIdAsync(userId))!;
                Assert.True((await users.ResetPasswordAsync(user, resetToken, "Synthetic#G06Password2")).Succeeded);
                resetToken = await users.GeneratePasswordResetTokenAsync(user);
            }

            var sourceDatabase = new NpgsqlConnectionStringBuilder(connection).Database!;
            var dump = await this.postgres.ExecAsync(["pg_dump", "-U", "culinary", "-d", sourceDatabase, "-Fc", "-f", "/tmp/keyring.dump"]);
            Assert.Equal(0, dump.ExitCode);
            var restoredConnection = await this.CreateDatabaseAsync();
            var restoredDatabase = new NpgsqlConnectionStringBuilder(restoredConnection).Database!;
            var restore = await this.postgres.ExecAsync(["pg_restore", "-U", "culinary", "-d", restoredDatabase, "--exit-on-error", "/tmp/keyring.dump"]);
            Assert.Equal(0, restore.ExitCode);
            await using var restored = BuildServices(restoredConnection, secret, blockedPath);
            Assert.Equal("synthetic payload", restored.GetRequiredService<IDataProtectionProvider>().CreateProtector("G06 fixture").Unprotect(payload));
            await using var restoredScope = restored.CreateAsyncScope();
            var restoredUsers = restoredScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.True((await restoredUsers.ResetPasswordAsync((await restoredUsers.FindByIdAsync(userId))!, resetToken, "Synthetic#G06Password3")).Succeeded);
            await using var original = BuildServices(connection, secret, blockedPath);
            await using var originalScope = original.CreateAsyncScope();
            var originalHashes = await KeyHashesAsync(originalScope.ServiceProvider.GetRequiredService<DataProtectionKeyDbContext>());
            var restoredHashes = await KeyHashesAsync(restoredScope.ServiceProvider.GetRequiredService<DataProtectionKeyDbContext>());
            Assert.Equal(originalHashes, restoredHashes);
            Assert.Equal(0, new FileInfo(blockedPath).Length);
        }
        finally
        {
            File.Delete(blockedPath);
        }
    }

    private static ServiceProvider BuildServices(string connection, string? certificate, string keyPath)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = connection,
            ["ConnectionStrings:Redis"] = "127.0.0.1:1,abortConnect=false",
            ["ObjectStorage:Endpoint"] = "http://127.0.0.1:1",
            ["Jwt:SigningKey"] = "synthetic-g06-keyring-test-signing-key",
            ["BackgroundJobs:Enabled"] = "false",
            ["DataProtection:PersistKeysToDatabase"] = "true",
            ["DataProtection:CertificateBase64"] = certificate,
            ["DataProtection:KeyPath"] = keyPath,
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    private static async Task<string[]> KeyHashesAsync(DataProtectionKeyDbContext db)
    {
        var xml = await db.DataProtectionKeys.OrderBy(key => key.Id).Select(key => key.Xml!).ToArrayAsync();
        return xml.Select(value => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))).ToArray();
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var name = "g06_keyring_" + Guid.NewGuid().ToString("N");
        var result = await this.postgres.ExecAsync(["psql", "-U", "culinary", "-d", "g06_keyring_test", "-c", $"CREATE DATABASE {name}"]);
        Assert.Equal(0, result.ExitCode);
        return new NpgsqlConnectionStringBuilder(this.postgres.GetConnectionString()) { Database = name }.ConnectionString;
    }
}
