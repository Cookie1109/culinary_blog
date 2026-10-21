using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.IntegrationTests;

public sealed class FunctionalRegressionSeedTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    [Fact]
    public async Task VietnameseSeedIsCompleteAndRestartPreservesUserDataAndSeedEdits()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);

        var authors = (await userManager.GetUsersInRoleAsync("Author"))
            .Where(user => user.Email?.StartsWith("phase8-author-", StringComparison.Ordinal) == true)
            .OrderBy(user => user.Email)
            .ToList();
        var authorIds = authors.Select(user => user.Id).ToHashSet();
        var recipes = await dbContext.Recipes.IgnoreQueryFilters().ToListAsync();
        var ingredients = await dbContext.RecipeIngredients.IgnoreQueryFilters().ToListAsync();
        var steps = await dbContext.RecipeSteps.IgnoreQueryFilters().ToListAsync();
        var images = await dbContext.RecipeImages.IgnoreQueryFilters().ToListAsync();

        Assert.Equal(5, authors.Count);
        Assert.All(authors, author =>
        {
            Assert.EndsWith("@example.test", author.Email, StringComparison.Ordinal);
            Assert.Null(author.PasswordHash);
            Assert.False(string.IsNullOrWhiteSpace(author.Bio));
        });
        Assert.Equal(20, await dbContext.Categories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(100, recipes.Count);
        Assert.All(recipes, recipe => Assert.Contains(recipe.AuthorId, authorIds));
        Assert.All(recipes, recipe => Assert.Equal(RecipeStatus.Published, recipe.Status));
        Assert.Equal(100, recipes.Select(recipe => recipe.Slug).Distinct(StringComparer.Ordinal).Count());
        Assert.All(recipes, recipe => Assert.True(ingredients.Count(item => item.RecipeId == recipe.Id) >= 10));
        Assert.All(recipes, recipe => Assert.True(steps.Count(item => item.RecipeId == recipe.Id) >= 5));
        Assert.Equal(100, images.Count);
        Assert.All(images, image =>
        {
            Assert.True(image.IsPrimary);
            Assert.EndsWith("/original.jpg", image.ObjectKey, StringComparison.Ordinal);
        });

        var customCategory = Category.Create(
            Guid.NewGuid(),
            "Món chính",
            "mon-chinh",
            null,
            null,
            20);
        dbContext.Categories.Add(customCategory);
        var customRecipe = Recipe.Create(
            Guid.NewGuid(),
            authors[0].Id,
            "mon-mau-ngau-nhien",
            "Món mẫu ngẫu nhiên",
            "Nội dung người dùng cần được giữ sau cập nhật.",
            customCategory.Id,
            10,
            10,
            2,
            RecipeDifficulty.Easy,
            null,
            null);
        dbContext.Recipes.Add(customRecipe);
        var editedRecipe = recipes[0];
        editedRecipe.Update(
            editedRecipe.Slug, "Công thức mẫu đã được chỉnh sửa", editedRecipe.Description,
            editedRecipe.CategoryId, editedRecipe.PrepTime, editedRecipe.CookTime,
            editedRecipe.Servings, editedRecipe.Difficulty, "Hướng dẫn do người dùng cập nhật", editedRecipe.Nutrition);
        var editedCategory = await dbContext.Categories.SingleAsync(category => category.Id == editedRecipe.CategoryId);
        editedCategory.Update("Danh mục đã chỉnh sửa", "Giữ mô tả mới", null, 99);
        var editedIngredient = ingredients.First(ingredient => ingredient.RecipeId == editedRecipe.Id);
        editedIngredient.Update("Nguyên liệu đã chỉnh sửa", 3, "gam", "Giữ ghi chú", editedIngredient.OrderIndex);
        recipes[1].Delete();
        await dbContext.SaveChangesAsync();
        authors[0].DisplayName = "Tác giả đã cập nhật hồ sơ";
        authors[0].Bio = "Giữ giới thiệu do người dùng nhập";
        Assert.True((await userManager.UpdateAsync(authors[0])).Succeeded);

        for (var restart = 0; restart < 2; restart++)
        {
            await services.GetRequiredService<DatabaseInitializer>().InitializeAsync(CancellationToken.None);
        }

        dbContext.ChangeTracker.Clear();
        Assert.Equal(21, await dbContext.Categories.IgnoreQueryFilters().CountAsync());
        Assert.Equal(101, await dbContext.Recipes.IgnoreQueryFilters().CountAsync());
        Assert.Equal(100, await dbContext.RecipeImages.IgnoreQueryFilters().CountAsync());
        Assert.True(await dbContext.Recipes.AnyAsync(recipe => recipe.Id == customRecipe.Id));
        Assert.Equal("Công thức mẫu đã được chỉnh sửa", (await dbContext.Recipes.FindAsync(editedRecipe.Id))!.Title);
        Assert.Equal("Danh mục đã chỉnh sửa", (await dbContext.Categories.FindAsync(editedCategory.Id))!.Name);
        Assert.Equal("Nguyên liệu đã chỉnh sửa", (await dbContext.RecipeIngredients.FindAsync(editedIngredient.Id))!.Name);
        Assert.True((await dbContext.Recipes.IgnoreQueryFilters().SingleAsync(recipe => recipe.Id == recipes[1].Id)).IsDeleted);
        Assert.Equal("Tác giả đã cập nhật hồ sơ", (await userManager.FindByIdAsync(authors[0].Id.ToString()))!.DisplayName);
    }
}
