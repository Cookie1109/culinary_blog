using System.Data;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

if (args.Length != 1) throw new ArgumentException("Expected private Neon backup directory only.");
var directory = Path.GetFullPath(args[0]);
var builder = new NpgsqlConnectionStringBuilder(await File.ReadAllTextAsync(Path.Combine(directory, "render-neon-connection.txt")))
{
    Pooling = false, Timeout = 15, CommandTimeout = 15,
    Options = "-c default_transaction_read_only=on -c statement_timeout=15000 -c lock_timeout=5000",
};
if (builder.SslMode != SslMode.VerifyFull || builder.ChannelBinding != ChannelBinding.Require)
    throw new ArgumentException("Verified TLS and required channel binding expected.");
var report = new Dictionary<string, object?>
{
    ["checkedAtUtc"] = DateTimeOffset.UtcNow,
    ["sslMode"] = builder.SslMode.ToString(), ["channelBinding"] = builder.ChannelBinding.ToString(),
    ["writesPerformed"] = false, ["seedOrMigrationsExecuted"] = false,
    ["renderChanged"] = false, ["apiHostStarted"] = false,
};
try
{
    await using var connection = new NpgsqlConnection(builder.ConnectionString);
    await connection.OpenAsync();
    await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead);
    async Task<object?> Scalar(string sql) => await new NpgsqlCommand(sql, connection, transaction).ExecuteScalarAsync();
    report["database"] = await Scalar("SELECT current_database()");
    report["transactionReadOnly"] = await Scalar("SHOW transaction_read_only");
    if ((string?)report["database"] != "neondb" || (string?)report["transactionReadOnly"] != "on")
        throw new InvalidOperationException("Wrong database or session is not read-only.");
    report["npgsqlConnected"] = true;
    report["postgresVersion"] = await Scalar("SHOW server_version");
    report["identityIdDatabaseType"] = await Scalar("SELECT data_type FROM information_schema.columns WHERE table_schema='public' AND table_name='AspNetUsers' AND column_name='Id'");
    report["publicCacheStateExists"] = await Scalar("SELECT to_regclass('public.\"PublicCacheState\"') IS NOT NULL");
    report["sharedRateLimitStateExists"] = await Scalar("SELECT to_regclass('public.\"SharedRateLimitState\"') IS NOT NULL");
    report["recipeSlugsExists"] = await Scalar("SELECT to_regclass('public.\"RecipeSlugs\"') IS NOT NULL");
    report["reserveRateLimitFunctionExists"] = await Scalar("SELECT to_regprocedure('public.\"ReserveRateLimit\"(text,text,integer,integer)') IS NOT NULL");
    var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options;
    await using var db = new AppDbContext(options);
    await db.Database.UseTransactionAsync(transaction);
    var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
    var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
    report["appliedMigrations"] = applied;
    report["pendingMigrations"] = pending;
    report["compiledMigrationCount"] = db.Database.GetMigrations().Count();
    report["efHealthCanConnect"] = await db.Database.CanConnectAsync();
    var orm = new Dictionary<string, object?>();
    async Task ReadOrm(string name, Func<Task> query)
    {
        await new NpgsqlCommand("SAVEPOINT orm_check", connection, transaction).ExecuteNonQueryAsync();
        try { await query(); orm[name] = new { passed = true }; }
        catch (Exception exception)
        {
            orm[name] = new { passed = false, failureType = exception.GetType().Name, sqlState = (exception as PostgresException)?.SqlState };
            await new NpgsqlCommand("ROLLBACK TO SAVEPOINT orm_check", connection, transaction).ExecuteNonQueryAsync();
        }
        await new NpgsqlCommand("RELEASE SAVEPOINT orm_check", connection, transaction).ExecuteNonQueryAsync();
    }
    await ReadOrm("categoryIdProjection", async () => { _ = await db.Categories.AsNoTracking().Select(c => c.Id).Take(1).ToListAsync(); });
    await ReadOrm("identityIdProjection", async () => { _ = await db.Users.AsNoTracking().Select(u => u.Id).Take(1).ToListAsync(); });
    await ReadOrm("recipeAuthorProjection", async () => { _ = await db.Recipes.AsNoTracking().Select(r => r.AuthorId).Take(1).ToListAsync(); });
    await ReadOrm("recipeSlugProjection", async () => { _ = await db.RecipeSlugs.AsNoTracking().Select(s => s.Slug).Take(1).ToListAsync(); });
    report["ormReadChecks"] = orm;
    report["currentWorkingTreeSchemaReady"] = pending.Length == 0;
    await transaction.RollbackAsync();
    report["reviewCompleted"] = true;
}
catch (Exception exception)
{
    report["reviewCompleted"] = false;
    report["failureType"] = exception.GetType().Name;
    report["sqlState"] = (exception as PostgresException)?.SqlState;
    Environment.ExitCode = 1;
}
var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(Path.Combine(directory, "render-npgsql-review.json"), json);
Console.WriteLine(json);
