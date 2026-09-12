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

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to {operation}: {string.Join(", ", result.Errors.Select(error => error.Code))}");
        }
    }
}
