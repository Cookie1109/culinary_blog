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

/// <summary>Represents the rate limit redis fixture.</summary>
public sealed class RateLimitRedisFixture : IAsyncLifetime
{
    private readonly Testcontainers.PostgreSql.PostgreSqlContainer database = new Testcontainers.PostgreSql.PostgreSqlBuilder("postgres:16.10-bookworm").Build();
    private readonly RedisContainer container = new RedisBuilder("redis:7.4.5-alpine").Build();

    /// <summary>Gets the database connection.</summary>
    public string DatabaseConnection => this.database.GetConnectionString();

    /// <summary>Gets the connection string.</summary>
    public string ConnectionString => this.container.GetConnectionString();

    /// <summary>Gets the redis.</summary>
    public IConnectionMultiplexer Redis { get; private set; } = null!;

    /// <summary>Performs the context operation.</summary>
    /// <returns>The context result.</returns>
    public AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(this.DatabaseConnection).Options);

    /// <summary>Performs the pause operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task PauseAsync() => this.container.PauseAsync();

    /// <summary>Performs the resume operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task ResumeAsync() => this.container.UnpauseAsync();

    /// <summary>Performs the initialize operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InitializeAsync()
    {
        await Task.WhenAll(this.container.StartAsync(), this.database.StartAsync());
        this.Redis = await ConnectionMultiplexer.ConnectAsync(this.ConnectionString);
        await using var db = this.Context();
        await db.Database.MigrateAsync();
    }

    /// <summary>Performs the dispose operation.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task DisposeAsync()
    {
        await this.Redis.DisposeAsync();
        await this.container.DisposeAsync();
        await this.database.DisposeAsync();
    }
}
