using Bogus;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;
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

        var categoryIds = await SeedCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedRecipesAsync(categoryIds, cancellationToken).ConfigureAwait(false);

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

    private async Task<List<Guid>> SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        var existingCategories = await dbContext.Categories.IgnoreQueryFilters()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existingCategories.Count >= 20)
        {
            return existingCategories.Select(category => category.Id).Take(20).ToList();
        }

        var existingNames = existingCategories.Select(c => c.NormalizedName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var foodThemes = new[]
        {
            "Món chính", "Món chay", "Canh và súp", "Làm bánh", "Món tráng miệng",
            "Món khai vị", "Món nướng", "Món xào", "Món kho", "Món hấp",
            "Đồ uống", "Món ăn vặt", "Món lẩu", "Salad", "Món cuốn",
            "Mì bún phở", "Đồ muối chua", "Nước sốt", "Hải sản", "Món ăn sáng"
        };

        var orderOffset = existingCategories.Count;
        var countToGenerate = 20 - existingCategories.Count;

        var categoryFaker = new Faker<Category>()
            .CustomInstantiator(f =>
            {
                var index = f.IndexGlobal + orderOffset;
                var baseName = index < foodThemes.Length ? foodThemes[index] : f.Commerce.Department();
                var name = $"{baseName} {f.Commerce.ProductAdjective()} {index + 1}".Trim();
                if (name.Length > 90)
                {
                    name = name[..90];
                }

                while (existingNames.Contains(name))
                {
                    name = $"{baseName} {f.Commerce.ProductAdjective()} {Guid.NewGuid().ToString("N")[..4]}";
                }

                existingNames.Add(name);

                var id = Guid.NewGuid();
                var slug = Slug.From(name, 120);
                var description = f.Lorem.Sentence();
                var imageUrl = $"https://picsum.photos/seed/{id}/600/400";

                return Category.Create(id, name, slug, description, imageUrl, index);
            });

        var generated = categoryFaker.Generate(countToGenerate);
        dbContext.Categories.AddRange(generated);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return existingCategories.Select(c => c.Id).Concat(generated.Select(c => c.Id)).Take(20).ToList();
    }

    private async Task SeedRecipesAsync(List<Guid> categoryIds, CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return;
        }

        var existingCount = await dbContext.Recipes.IgnoreQueryFilters()
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var countToGenerate = Math.Max(0, 100 - existingCount);

        var author = await userManager.Users.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (author is null)
        {
            author = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "author@culinaryblog.local",
                Email = "author@culinaryblog.local",
                DisplayName = "Bếp Trưởng",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            EnsureSucceeded(await userManager.CreateAsync(author, "Chef@123456").ConfigureAwait(false), "create the default author");
            EnsureSucceeded(await userManager.AddToRoleAsync(author, "Author").ConfigureAwait(false), "assign the Author role");
        }

        var dishPrefixes = new[]
        {
            "Phở bò", "Bún chả", "Cơm tấm", "Bánh mì", "Gỏi cuốn",
            "Bò kho", "Cá kho tộ", "Gà rang sả ớt", "Sườn xào chua ngọt", "Canh chua cá lóc",
            "Mì xào giòn", "Lẩu thái hải sản", "Nộm hoa chuối", "Bánh xèo", "Chả giò rế",
            "Thịt kho tàu", "Vịt om sấu", "Bún bò Huế", "Mực hấp gừng", "Tôm rim mặn ngọt"
        };

        var existingSlugs = await dbContext.Recipes.IgnoreQueryFilters()
            .Select(r => r.Slug)
            .ToHashSetAsync(cancellationToken)
            .ConfigureAwait(false);

        var recipeFaker = new Faker<Recipe>()
            .CustomInstantiator(f =>
            {
                var index = f.IndexGlobal + existingCount;
                var baseTitle = dishPrefixes[index % dishPrefixes.Length];
                var title = $"{baseTitle} {f.Commerce.ProductAdjective()} {index + 1}".Trim();
                if (title.Length > 190)
                {
                    title = title[..190];
                }

                var slug = Slug.From(title, 220);
                while (existingSlugs.Contains(slug))
                {
                    title = $"{baseTitle} {f.Commerce.ProductAdjective()} {Guid.NewGuid().ToString("N")[..4]}";
                    slug = Slug.From(title, 220);
                }

                existingSlugs.Add(slug);

                var description = f.Lorem.Paragraph();
                if (description.Length > 1900)
                {
                    description = description[..1900];
                }

                var categoryId = f.PickRandom(categoryIds);
                var prepTime = f.Random.Int(5, 60);
                var cookTime = f.Random.Int(10, 120);
                var servings = f.Random.Int(1, 8);
                var difficulty = f.PickRandom<RecipeDifficulty>();
                var instructions = f.Lorem.Paragraphs(2);
                if (instructions.Length > 9000)
                {
                    instructions = instructions[..9000];
                }

                var nutrition = RecipeNutrition.Create(
                    f.Random.Int(150, 750),
                    f.Random.Int(5, 45),
                    f.Random.Int(10, 80),
                    f.Random.Int(2, 35),
                    f.Random.Int(1, 15),
                    f.Random.Int(100, 1200));

                return Recipe.Create(
                    Guid.NewGuid(),
                    author.Id,
                    slug,
                    title,
                    description,
                    categoryId,
                    prepTime,
                    cookTime,
                    servings,
                    difficulty,
                    instructions,
                    nutrition);
            });

        var recipes = countToGenerate > 0 ? recipeFaker.Generate(countToGenerate) : [];
        if (recipes.Count > 0)
        {
            dbContext.Recipes.AddRange(recipes);
        }

        var ingredientNames = new[]
        {
            "Thịt bò", "Thịt lợn", "Thịt gà", "Tôm tươi", "Mực ống", "Cá hồi",
            "Trứng gà", "Đậu phụ", "Hành lá", "Tỏi", "Hành tím", "Gừng",
            "Sả", "Ớt tươi", "Cà chua", "Cà rốt", "Khoai tây", "Nấm hương",
            "Rau thơm", "Nước mắm", "Dầu ăn", "Hạt nêm", "Đường", "Tiêu đen",
            "Xì dầu", "Bột năng", "Bột bắp", "Bơ nhạt", "Sữa tươi", "Nước cốt dừa"
        };

        var units = new[] { "g", "kg", "ml", "muỗng canh", "thìa cà phê", "quả", "củ", "nhánh", "tép", "lát" };

        var allIngredients = new List<RecipeIngredient>();
        var faker = new Faker();

        var existingRecipesWithoutIngredients = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(r => !dbContext.RecipeIngredients.IgnoreQueryFilters().Any(i => i.RecipeId == r.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recipesNeedingIngredients = recipes.Concat(existingRecipesWithoutIngredients)
            .DistinctBy(r => r.Id)
            .ToList();

        foreach (var recipe in recipesNeedingIngredients)
        {
            var pickedIngredients = faker.PickRandom(ingredientNames, 10).Distinct().ToList();
            while (pickedIngredients.Count < 10)
            {
                var extra = faker.PickRandom(ingredientNames);
                if (!pickedIngredients.Contains(extra))
                {
                    pickedIngredients.Add(extra);
                }
            }

            for (var orderIndex = 0; orderIndex < 10; orderIndex++)
            {
                var ingredientId = Guid.NewGuid();
                var name = pickedIngredients[orderIndex];
                var quantity = Math.Round(faker.Random.Decimal(1, 500), 1);
                var unit = faker.PickRandom(units);
                var notes = faker.Random.Bool(0.2f) ? faker.Lorem.Word() : null;

                allIngredients.Add(RecipeIngredient.Create(
                    ingredientId,
                    recipe.Id,
                    name,
                    quantity,
                    unit,
                    notes,
                    orderIndex));
            }
        }

        if (allIngredients.Count > 0)
        {
            dbContext.RecipeIngredients.AddRange(allIngredients);
        }

        var stepTitles = new[]
        {
            "Sơ chế nguyên liệu",
            "Ướp gia vị",
            "Chế biến nhiệt",
            "Hoàn thiện món ăn",
            "Trình bày và thưởng thức"
        };

        var allSteps = new List<RecipeStep>();

        var existingRecipesWithoutSteps = await dbContext.Recipes.IgnoreQueryFilters()
            .Where(r => !dbContext.RecipeSteps.IgnoreQueryFilters().Any(s => s.RecipeId == r.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var recipesNeedingSteps = recipes.Concat(existingRecipesWithoutSteps)
            .DistinctBy(r => r.Id)
            .ToList();

        foreach (var recipe in recipesNeedingSteps)
        {
            for (var stepIndex = 0; stepIndex < 5; stepIndex++)
            {
                var stepId = Guid.NewGuid();
                var stepNumber = stepIndex + 1;
                var title = stepIndex < stepTitles.Length ? stepTitles[stepIndex] : $"Bước {stepNumber}";
                var description = faker.Lorem.Paragraph();
                if (description.Length > 1900)
                {
                    description = description[..1900];
                }

                var timerMinutes = faker.Random.Bool(0.6f) ? faker.Random.Int(5, 45) : (int?)null;
                var imageUrl = $"https://picsum.photos/seed/{stepId}/600/400";

                allSteps.Add(RecipeStep.Create(
                    stepId,
                    recipe.Id,
                    stepNumber,
                    title,
                    description,
                    timerMinutes,
                    imageUrl));
            }
        }

        if (allSteps.Count > 0)
        {
            dbContext.RecipeSteps.AddRange(allSteps);
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
