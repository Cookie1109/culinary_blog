using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Infrastructure.Identity;

public sealed class DatabaseInitializer(
    AppDbContext dbContext,
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<AdminSeedOptions> adminOptions)
{
    private static readonly string[] Roles = ["Author", "Admin"];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role)).ConfigureAwait(false);
                EnsureSucceeded(result, $"create the {role} role");
            }
        }

        await SeedCategoriesAsync(cancellationToken).ConfigureAwait(false);

        var options = adminOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException("AdminSeed Email and Password are required when admin seeding is enabled.");
        }

        var admin = await userManager.FindByEmailAsync(options.Email).ConfigureAwait(false);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = options.Email,
                Email = options.Email,
                DisplayName = options.DisplayName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            EnsureSucceeded(await userManager.CreateAsync(admin, options.Password).ConfigureAwait(false), "create the admin user");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin").ConfigureAwait(false))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(admin, "Admin").ConfigureAwait(false), "assign the Admin role");
        }
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var seeds = new (Guid Id, string Name, int OrderIndex)[]
        {
            (Guid.Parse("10000000-0000-0000-0000-000000000001"), "Món chính", 0),
            (Guid.Parse("10000000-0000-0000-0000-000000000002"), "Món chay", 1),
            (Guid.Parse("10000000-0000-0000-0000-000000000003"), "Canh và súp", 2),
            (Guid.Parse("10000000-0000-0000-0000-000000000004"), "Làm bánh", 3),
            (Guid.Parse("10000000-0000-0000-0000-000000000005"), "Món tráng miệng", 4),
        };

        var existingIds = await dbContext.Categories.IgnoreQueryFilters()
            .Select(category => category.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var seed in seeds.Where(seed => !existingIds.Contains(seed.Id)))
        {
            dbContext.Categories.Add(Category.Create(
                seed.Id,
                seed.Name,
                Slug.From(seed.Name, 120),
                null,
                null,
                seed.OrderIndex));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to {operation}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }
}
