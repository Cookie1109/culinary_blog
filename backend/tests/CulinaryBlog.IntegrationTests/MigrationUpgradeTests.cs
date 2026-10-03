using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace CulinaryBlog.IntegrationTests;

public sealed class MigrationUpgradeTests : IAsyncLifetime
{
    private const string PreviousReleaseMigration = "20260922150000_RecipeSearchVector";
    private const string ReleaseCandidateMigration = "20261003120000_RecipeDiscoveryIndexes";

    private readonly PostgreSqlContainer _postgresSql = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_migration_upgrade_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    public Task InitializeAsync() => _postgresSql.StartAsync();

    public Task DisposeAsync() => _postgresSql.DisposeAsync().AsTask();

    [Fact]
    public async Task PreviousReleaseDatabaseUpgradesToReleaseCandidateWithoutDataLoss()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgresSql.GetConnectionString())
            .Options;
        await using var dbContext = new AppDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousReleaseMigration);

        var authorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO "AspNetUsers" (
                "Id", "DisplayName", "IsActive", "CreatedAt", "UserName", "NormalizedUserName",
                "Email", "NormalizedEmail", "EmailConfirmed", "PhoneNumberConfirmed",
                "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
            VALUES (
                {{authorId}}, 'Migration Author', TRUE, {{createdAt}}, 'migration-author@example.test',
                'MIGRATION-AUTHOR@EXAMPLE.TEST', 'migration-author@example.test',
                'MIGRATION-AUTHOR@EXAMPLE.TEST', TRUE, FALSE, FALSE, TRUE, 0);

            INSERT INTO "Categories" (
                "Id", "Name", "NormalizedName", "Slug", "Description", "OrderIndex",
                "CreatedAt", "IsDeleted", "Version")
            VALUES (
                {{categoryId}}, 'Món Việt migration', 'MÓN VIỆT MIGRATION', 'mon-viet-migration',
                'Synthetic migration fixture', 0, {{createdAt}}, FALSE, 1);

            INSERT INTO "Recipes" (
                "Id", "Title", "Slug", "Description", "Instructions", "PrepTime", "CookTime",
                "Servings", "Difficulty", "Status", "CategoryId", "AuthorId", "PublishedAt",
                "CreatedAt", "IsDeleted", "Version")
            VALUES (
                {{recipeId}}, 'Phở bò migration', 'pho-bo-migration', 'Synthetic migration fixture',
                'Migration instructions', 10, 20, 4, 1, 1, {{categoryId}}, {{authorId}},
                {{createdAt}}, {{createdAt}}, FALSE, 1);
            """);

        await migrator.MigrateAsync();

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        var recipeTitle = await dbContext.Recipes
            .Where(recipe => recipe.Id == recipeId)
            .Select(recipe => recipe.Title)
            .SingleAsync();
        var searchTitle = await dbContext.Database.SqlQueryRaw<string>(
                "SELECT \"SearchTitle\" AS \"Value\" FROM \"Recipes\" WHERE \"Id\" = {0}",
                recipeId)
            .SingleAsync();

        Assert.Equal("Phở bò migration", recipeTitle);
        Assert.Equal("pho bo migration", searchTitle);
        Assert.Equal(ReleaseCandidateMigration, appliedMigrations.Last());
    }
}
