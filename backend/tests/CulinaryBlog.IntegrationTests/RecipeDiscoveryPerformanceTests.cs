using System.Data.Common;
using System.Text.Json;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit.Abstractions;

namespace CulinaryBlog.IntegrationTests;

public sealed class RecipeDiscoveryPerformanceTests(
    AuthApiFactory factory,
    ITestOutputHelper output) : IClassFixture<AuthApiFactory>
{
    private const int SeedRecipeCount = 10_000;
    private const int ApplicationSeedRecipeCount = 1_000;
    private const double QueryBudgetMilliseconds = 100;

    [Fact]
    public async Task MainDiscoveryQueriesHaveReviewedPlansAndMeetBaselineLatency()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var author = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = $"performance-{suffix}@example.com",
            Email = $"performance-{suffix}@example.com",
            DisplayName = "Performance Author",
            IsActive = true,
        };
        Assert.True((await userManager.CreateAsync(author)).Succeeded);

        var category = Category.Create(
            Guid.NewGuid(), $"Performance {suffix}", $"performance-{suffix}", null, null, 0);
        var fillerCategory = Category.Create(
            Guid.NewGuid(), $"Performance filler {suffix}", $"performance-filler-{suffix}", null, null, 1);
        dbContext.Categories.AddRange(category, fillerCategory);
        var publishedAt = DateTimeOffset.UtcNow;
        for (var index = 0; index < ApplicationSeedRecipeCount; index++)
        {
            var title = index == 0
                ? $"Benchmarkneedle recipe {suffix}"
                : $"Performance recipe {index:D4} {suffix}";
            var recipe = Recipe.Create(
                Guid.NewGuid(),
                author.Id,
                $"performance-{suffix}-{index:D4}",
                title,
                "Synthetic recipe used only for the Phase 5 query benchmark.",
                category.Id,
                10,
                20,
                4,
                RecipeDifficulty.Easy,
                "Benchmark instructions",
                null);
            recipe.Publish(publishedAt.AddSeconds(-index), true, 1, [1]);
            dbContext.Recipes.Add(recipe);
        }

        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "Recipes" (
                "Id", "AuthorId", "CategoryId", "CookTime", "CreatedAt", "CreatedBy",
                "Description", "Difficulty", "Instructions", "IsDeleted", "PrepTime",
                "PublishedAt", "Servings", "Slug", "Status", "Title", "Version")
            SELECT
                gen_random_uuid(), @authorId, @categoryId, 20, now(), 'performance-test',
                'Synthetic recipe used only for the Phase 5 query benchmark.', 2,
                'Benchmark instructions', FALSE, 10, now() - (series * interval '1 second'),
                4, concat('performance-filler-', @suffix, '-', series), 1,
                concat('Filler performance recipe ', series), 1
            FROM generate_series(1, @count) AS series;
            """,
            new NpgsqlParameter("authorId", author.Id),
            new NpgsqlParameter("categoryId", fillerCategory.Id),
            new NpgsqlParameter("suffix", suffix),
            new NpgsqlParameter("count", SeedRecipeCount - ApplicationSeedRecipeCount));
        dbContext.ChangeTracker.Clear();
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Recipes\";");

        var listPlans = await MeasurePlansAsync(
            dbContext.Database.GetDbConnection(),
            """
            EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)
            SELECT *
            FROM "Recipes"
            WHERE NOT "IsDeleted"
              AND "Status" = 1
              AND "CategoryId" = @categoryId
              AND "Difficulty" = 1
            ORDER BY "PublishedAt" DESC
            LIMIT 50;
            """,
            new NpgsqlParameter("categoryId", category.Id));
        var searchPlans = await MeasurePlansAsync(
            dbContext.Database.GetDbConnection(),
            """
            EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)
            SELECT *
            FROM "Recipes"
            WHERE NOT "IsDeleted"
              AND "Status" = 1
              AND (
                "SearchVector" @@ to_tsquery('simple', 'benchmarkneedle:*')
                OR "SearchTitle" % 'benchmarkneedle')
            ORDER BY
              ts_rank("SearchVector", to_tsquery('simple', 'benchmarkneedle:*')) DESC,
              similarity("SearchTitle", 'benchmarkneedle') DESC,
              "PublishedAt" DESC
            LIMIT 50;
            """);

        var listLatency = Percentile95(listPlans.Select(plan => plan.ExecutionTimeMilliseconds));
        var searchLatency = Percentile95(searchPlans.Select(plan => plan.ExecutionTimeMilliseconds));
        var listPlan = listPlans[^1].Json;
        var searchPlan = searchPlans[^1].Json;

        output.WriteLine(
            "Profile: PostgreSQL 16 Testcontainer; local loopback; {0} logical CPU(s); seed={1} recipes; 5 measured runs after 1 warm-up.",
            Environment.ProcessorCount,
            SeedRecipeCount);
        output.WriteLine("List query p95: {0:F3} ms", listLatency);
        output.WriteLine("Search query p95: {0:F3} ms", searchLatency);
        output.WriteLine("List execution plan: {0}", listPlan);
        output.WriteLine("Search execution plan: {0}", searchPlan);

        Assert.Contains("IX_Recipes_Status_CategoryId_Difficulty_PublishedAt", listPlan, StringComparison.Ordinal);
        Assert.Contains("SearchVector", searchPlan, StringComparison.Ordinal);
        Assert.Contains("SearchTitle", searchPlan, StringComparison.Ordinal);
        Assert.True(listLatency <= QueryBudgetMilliseconds, $"List query p95 was {listLatency:F3} ms.");
        Assert.True(searchLatency <= QueryBudgetMilliseconds, $"Search query p95 was {searchLatency:F3} ms.");
    }

    private static async Task<IReadOnlyList<QueryPlan>> MeasurePlansAsync(
        DbConnection connection,
        string commandText,
        params DbParameter[] parameters)
    {
        _ = await ExecutePlanAsync(connection, commandText, parameters);
        var plans = new List<QueryPlan>(5);
        for (var run = 0; run < 5; run++)
        {
            plans.Add(await ExecutePlanAsync(connection, commandText, parameters));
        }

        return plans;
    }

    private static async Task<QueryPlan> ExecutePlanAsync(
        DbConnection connection,
        string commandText,
        IReadOnlyCollection<DbParameter> parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(Clone(parameter));
        }

        var json = Assert.IsType<string>(await command.ExecuteScalarAsync());
        using var document = JsonDocument.Parse(json);
        var executionTime = document.RootElement[0].GetProperty("Execution Time").GetDouble();
        return new QueryPlan(json, executionTime);
    }

    private static NpgsqlParameter Clone(DbParameter parameter) => new()
    {
        ParameterName = parameter.ParameterName,
        Value = parameter.Value,
    };

    private static double Percentile95(IEnumerable<double> values)
    {
        var ordered = values.Order().ToArray();
        return ordered[(int)Math.Ceiling(ordered.Length * 0.95) - 1];
    }

    private sealed record QueryPlan(string Json, double ExecutionTimeMilliseconds);
}
