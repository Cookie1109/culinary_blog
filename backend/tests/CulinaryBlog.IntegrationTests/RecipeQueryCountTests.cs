// Culinary Blog. Authorship is recorded in the repository history.

using System.Data.Common;
using System.Net.Http.Json;
using System.Text.Json;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Checks that recipe list enrichment uses a fixed number of database reads.</summary>
/// <param name="factory">Supplies the isolated database.</param>
/// <param name="output">Records actual query counts.</param>
public sealed class RecipeQueryCountTests(AuthApiFactory factory, ITestOutputHelper output) : IClassFixture<AuthApiFactory>
{
    /// <summary>Verifies that increasing a page from one to fifty recipes does not introduce N+1 reads.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task ListEnrichmentQueryCountDoesNotGrowWithPageSize()
    {
        var counter = new CommandCounter();
        await using var custom = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddDbContext<AppDbContext>((_, options) => options.AddInterceptors(counter))));
        using var client = custom.CreateClient();
        counter.Reset();
        var small = await client.GetFromJsonAsync<JsonElement>("/api/v1/recipes?pageSize=1&probe=small-count");
        var smallCount = counter.Count;
        counter.Reset();
        var large = await client.GetFromJsonAsync<JsonElement>("/api/v1/recipes?pageSize=50&probe=large-count");
        var largeCount = counter.Count;
        Assert.Single(small.GetProperty("data").EnumerateArray());
        Assert.True(large.GetProperty("data").GetArrayLength() > 1);
        Assert.True(smallCount > 0);
        Assert.Equal(smallCount, largeCount);
        output.WriteLine("Recipe list reads: one row={0}, up to fifty rows={1}; actual rows={2}", smallCount, largeCount, large.GetProperty("data").GetArrayLength());
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        private int count;

        public int Count => Volatile.Read(ref this.count);

        public void Reset() => Interlocked.Exchange(ref this.count, 0);

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref this.count);
            return ValueTask.FromResult(result);
        }
    }
}
