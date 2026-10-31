// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for migration upgrade tests.</summary>
public sealed class MigrationUpgradeTests : IAsyncLifetime
{
    private const string PreviousReleaseMigration = "20260922150000_RecipeSearchVector";
    private const string ReleaseCandidateMigration = "20261029172426_RecipeTitleSortIndex";

    private readonly PostgreSqlContainer postgresSql = new PostgreSqlBuilder("postgres:16.10-bookworm")
        .WithDatabase("culinary_blog_migration_upgrade_test")
        .WithUsername("culinary")
        .WithPassword("integration-test-only")
        .Build();

    /// <summary>Verifies that initialize.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task InitializeAsync() => this.postgresSql.StartAsync();

    /// <summary>Verifies that dispose.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    public Task DisposeAsync() => this.postgresSql.DisposeAsync().AsTask();

    /// <summary>Verifies that previous release database upgrades to release candidate without data loss.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PreviousReleaseDatabaseUpgradesToReleaseCandidateWithoutDataLoss()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(this.postgresSql.GetConnectionString())
            .Options;
        await using var dbContext = new AppDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        await migrator.MigrateAsync(PreviousReleaseMigration);

        var authorId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var recipeId = Guid.NewGuid();
        var welcomeId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var tokenId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var mediaOutboxId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var emptyInstructionsId = Guid.NewGuid();
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

            INSERT INTO "WelcomeEmailOutbox" (
                "Id", "UserId", "Recipient", "DisplayName", "Attempts", "NextAttemptAt", "CreatedAt", "LastError")
            VALUES ({{welcomeId}}, {{authorId}}, 'migration-author@example.test', 'Migration Author',
                1, {{createdAt}}, {{createdAt}}, 'Legacy SMTP error');

            INSERT INTO "AspNetRoles" ("Id", "Name", "NormalizedName") VALUES ({{roleId}}, 'Author', 'AUTHOR');
            INSERT INTO "AspNetUserRoles" ("UserId", "RoleId") VALUES ({{authorId}}, {{roleId}});
            INSERT INTO "AspNetUserClaims" ("UserId", "ClaimType", "ClaimValue") VALUES ({{authorId}}, 'synthetic', 'retained');
            INSERT INTO "AspNetRoleClaims" ("RoleId", "ClaimType", "ClaimValue") VALUES ({{roleId}}, 'synthetic', 'retained');
            INSERT INTO "AspNetUserLogins" ("LoginProvider", "ProviderKey", "UserId") VALUES ('Synthetic', 'provider-key', {{authorId}});
            INSERT INTO "AspNetUserTokens" ("UserId", "LoginProvider", "Name", "Value") VALUES ({{authorId}}, 'Synthetic', 'Synthetic', 'retained');
            INSERT INTO "RefreshTokens" ("Id", "UserId", "TokenHash", "FamilyId", "CreatedAt", "ExpiresAt", "CreatedByIp", "UserAgent")
            VALUES ({{tokenId}}, {{authorId}}, repeat('A', 64), {{familyId}}, {{createdAt}}, {{createdAt.AddDays(7)}}, '2001:db8::1', 'Synthetic legacy agent');
            INSERT INTO "RecipeIngredients" ("Id", "RecipeId", "Name", "Quantity", "OrderIndex", "CreatedAt", "IsDeleted", "Version")
            VALUES ({{imageId}}, {{recipeId}}, 'Salt', 1.125, 0, {{createdAt}}, FALSE, 1);
            INSERT INTO "RecipeSteps" ("Id", "RecipeId", "StepNumber", "Title", "Description", "CreatedAt", "IsDeleted", "Version")
            VALUES ({{imageId}}, {{recipeId}}, 1, 'Prepare', 'Keep this step', {{createdAt}}, FALSE, 1);
            INSERT INTO "RecipeImages" ("Id", "RecipeId", "ObjectKey", "ContentType", "IsPrimary", "OrderIndex", "ProcessingStatus", "CreatedAt", "IsDeleted", "Version")
            VALUES ({{imageId}}, {{recipeId}}, 'synthetic/original.png', 'image/png', TRUE, 0, 0, {{createdAt}}, FALSE, 1);
            INSERT INTO "MediaOutbox" ("Id", "Type", "Payload", "CreatedAt", "Attempts", "NextAttemptAt")
            VALUES ({{mediaOutboxId}}, 'image.resize', '{}', {{createdAt}}, 0, {{createdAt}});
            INSERT INTO "Recipes" ("Id", "Title", "Slug", "Description", "Instructions", "PrepTime", "CookTime", "Servings", "Difficulty", "Status", "CategoryId", "AuthorId", "CreatedAt", "IsDeleted", "Version")
            VALUES ({{emptyInstructionsId}}, 'Legacy empty instructions', 'legacy-empty-instructions', 'Synthetic', NULL, 1, 0, 1, 1, 0, {{categoryId}}, {{authorId}}, {{createdAt}}, FALSE, 1);
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
        var welcome = await dbContext.WelcomeEmailOutbox.SingleAsync(message => message.Id == welcomeId);
        Assert.Equal("migration-author@example.test", welcome.Recipient);
        Assert.Equal(1, welcome.Attempts);
        Assert.Equal("Legacy SMTP error", welcome.LastError);
        Assert.Null(welcome.JobId);
        Assert.Null(welcome.TraceParent);
        Assert.Null(welcome.CorrelationId);
        Assert.Equal(authorId.ToString(), welcome.UserId);
        Assert.Equal(authorId.ToString(), (await dbContext.Recipes.SingleAsync(recipe => recipe.Id == recipeId)).AuthorId);
        Assert.Equal("Migration instructions", (await dbContext.Recipes.SingleAsync(recipe => recipe.Id == recipeId)).Instructions);
        Assert.Equal(string.Empty, (await dbContext.Recipes.SingleAsync(recipe => recipe.Id == emptyInstructionsId)).Instructions);
        var token = await dbContext.RefreshTokens.SingleAsync(token => token.Id == tokenId);
        Assert.Equal(familyId, token.FamilyId);
        Assert.Equal(new string('A', 64), token.TokenHash);
        Assert.Equal("2001:db8::1", token.CreatedByIp);
        Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("Synthetic legacy agent"))), token.UserAgentHash);
        Assert.Equal("Synthetic legacy agent", token.UserAgent);
        Assert.Equal(1.125m, (await dbContext.RecipeIngredients.SingleAsync(item => item.Id == imageId)).Quantity);
        Assert.Equal("Keep this step", (await dbContext.RecipeSteps.SingleAsync(item => item.Id == imageId)).Description);
        Assert.Equal($"/api/v1/media/{imageId}/original", (await dbContext.RecipeImages.SingleAsync(item => item.Id == imageId)).OriginalUrl);
        Assert.Equal("image.resize", (await dbContext.MediaOutbox.SingleAsync(item => item.Id == mediaOutboxId)).Type);
        Assert.Equal(roleId.ToString(), (await dbContext.UserRoles.SingleAsync()).RoleId);
        Assert.Equal(authorId.ToString(), (await dbContext.UserLogins.SingleAsync()).UserId);
        Assert.Equal("retained", (await dbContext.UserTokens.SingleAsync()).Value);
        Assert.Equal("retained", (await dbContext.UserClaims.SingleAsync()).ClaimValue);
        Assert.Equal("retained", (await dbContext.RoleClaims.SingleAsync()).ClaimValue);
        var types = await dbContext.Database.SqlQueryRaw<string>("""
            SELECT table_name || '.' || column_name || ':' || data_type || ':' || coalesce(character_maximum_length::text, '') || ':' || is_nullable AS "Value"
            FROM information_schema.columns WHERE table_schema = 'public'
            """).ToListAsync();
        Assert.Contains("AspNetUsers.Id:character varying:450:NO", types);
        Assert.Contains("Recipes.AuthorId:character varying:450:NO", types);
        Assert.Contains("Recipes.Instructions:text::NO", types);
        Assert.Contains("Recipes.Difficulty:smallint::NO", types);
        Assert.Contains("Recipes.Status:smallint::NO", types);
        Assert.Contains("Recipes.RowVersion:bytea::NO", types);
        Assert.Contains("RecipeImages.OriginalUrl:character varying:500:NO", types);
        Assert.Contains("RefreshTokens.CreatedByIp:character varying:45:YES", types);
        Assert.Contains("RefreshTokens.UserAgentHash:character varying:64:YES", types);
        var generation = await dbContext.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1").SingleAsync();
        await using (var transaction = await dbContext.Database.BeginTransactionAsync())
        {
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Recipes\" SET \"Title\" = 'Rolled-back title' WHERE \"Id\" = {recipeId}");
            var pending = await dbContext.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1").SingleAsync();
            Assert.True(pending > generation);
            await transaction.RollbackAsync();
        }

        var rolledBack = await dbContext.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1").SingleAsync();
        Assert.Equal(generation, rolledBack);
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"Recipes\" SET \"Title\" = 'Committed title' WHERE \"Id\" = {recipeId}");
        var committed = await dbContext.Database.SqlQuery<long>($"SELECT \"Generation\" AS \"Value\" FROM \"PublicCacheState\" WHERE \"Id\" = 1").SingleAsync();
        Assert.True(committed > generation);

        await migrator.MigrateAsync("20261025063821_ContentSlugsAndImageUrls");
        var legacyId = await dbContext.Database.SqlQueryRaw<string>("SELECT \"Id\"::text AS \"Value\" FROM \"AspNetUsers\"").SingleAsync();
        Assert.Equal(authorId.ToString(), legacyId);
        await migrator.MigrateAsync();
        Assert.Equal(ReleaseCandidateMigration, (await dbContext.Database.GetAppliedMigrationsAsync()).Last());
        dbContext.ChangeTracker.Clear();
        Assert.Equal(familyId, (await dbContext.RefreshTokens.SingleAsync(item => item.Id == tokenId)).FamilyId);
        Assert.Equal("Committed title", (await dbContext.Recipes.SingleAsync(item => item.Id == recipeId)).Title);

        dbContext.Users.Add(new CulinaryBlog.Infrastructure.Identity.ApplicationUser { Id = "external-string-key", DisplayName = "String key", UserName = "string-key@example.test" });
        await dbContext.SaveChangesAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => migrator.MigrateAsync("20261025063821_ContentSlugsAndImageUrls"));

        // EF commits each migration separately. The Identity migration rejects the incompatible key atomically;
        // the preceding defaults migration was already reverted and must be reapplied before using the current app.
        Assert.Equal("20261025064611_SrsIdentityAndDataTypes", (await dbContext.Database.GetAppliedMigrationsAsync()).Last());
        Assert.True(await dbContext.Users.AnyAsync(user => user.Id == "external-string-key"));
        await migrator.MigrateAsync();
        Assert.Equal(ReleaseCandidateMigration, (await dbContext.Database.GetAppliedMigrationsAsync()).Last());
    }
}
