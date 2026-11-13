// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Email;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Jobs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.Persistence;

/// <summary>Represents the app db context.</summary>
/// <param name="options">The options supplied to this operation.</param>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    /// <summary>Gets the refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();

    /// <summary>Gets the welcome email outbox.</summary>
    public DbSet<WelcomeEmailOutbox> WelcomeEmailOutbox => this.Set<WelcomeEmailOutbox>();

    /// <summary>Gets the categories.</summary>
    public DbSet<Category> Categories => this.Set<Category>();

    /// <summary>Gets the recipes.</summary>
    public DbSet<Recipe> Recipes => this.Set<Recipe>();

    /// <summary>Gets the recipe slugs.</summary>
    public DbSet<RecipeSlug> RecipeSlugs => this.Set<RecipeSlug>();

    /// <summary>Gets the recipe ingredients.</summary>
    public DbSet<RecipeIngredient> RecipeIngredients => this.Set<RecipeIngredient>();

    /// <summary>Gets the recipe steps.</summary>
    public DbSet<RecipeStep> RecipeSteps => this.Set<RecipeStep>();

    /// <summary>Gets the recipe images.</summary>
    public DbSet<RecipeImage> RecipeImages => this.Set<RecipeImage>();

    /// <summary>Gets the media outbox.</summary>
    public DbSet<MediaOutboxMessage> MediaOutbox => this.Set<MediaOutboxMessage>();

    /// <summary>Performs the on model creating operation.</summary>
    /// <param name="builder">The builder supplied to this operation.</param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.Entity<IdentityRole>().Property(item => item.Id).HasMaxLength(450);
        builder.Entity<IdentityUserClaim<string>>().Property(item => item.UserId).HasMaxLength(450);
        builder.Entity<IdentityRoleClaim<string>>().Property(item => item.RoleId).HasMaxLength(450);
        builder.Entity<IdentityUserLogin<string>>().Property(item => item.UserId).HasMaxLength(450);
        builder.Entity<IdentityUserToken<string>>().Property(item => item.UserId).HasMaxLength(450);
        builder.Entity<IdentityUserRole<string>>().Property(item => item.UserId).HasMaxLength(450);
        builder.Entity<IdentityUserRole<string>>().Property(item => item.RoleId).HasMaxLength(450);
        foreach (var entity in builder.Model.GetEntityTypes().Where(entity => typeof(BaseEntity).IsAssignableFrom(entity.ClrType)))
        {
            var mapping = builder.Entity(entity.ClrType);
            mapping.Property(nameof(BaseEntity.Id)).HasDefaultValueSql("gen_random_uuid()");
            mapping.Property(nameof(BaseEntity.CreatedAt)).HasDefaultValueSql("NOW()");
            mapping.Property(nameof(BaseEntity.IsDeleted)).HasDefaultValue(false);

            // Keep the numeric aggregate ETag and expose its binary concurrency token required by the SRS.
            mapping.Property<byte[]>("RowVersion").IsRequired().IsRowVersion()
                .HasComputedColumnSql("int8send(\"Version\")", stored: true);
        }
    }
}
