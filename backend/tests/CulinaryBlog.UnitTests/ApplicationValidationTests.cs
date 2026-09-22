using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class ApplicationValidationTests
{
    [Fact]
    public async Task AuthValidatorsAcceptValidRequests()
    {
        Assert.True((await new RegisterRequestValidator().ValidateAsync(
            new RegisterRequest("Test Author", "author@example.com", "Valid#Password1"))).IsValid);
        Assert.True((await new LoginRequestValidator().ValidateAsync(
            new LoginRequest("author@example.com", "Valid#Password1"))).IsValid);
        Assert.True((await new GoogleLoginRequestValidator().ValidateAsync(
            new GoogleLoginRequest("signed-id-token"))).IsValid);
        Assert.True((await new RefreshTokenRequestValidator().ValidateAsync(
            new RefreshTokenRequest("refresh-token"))).IsValid);
        Assert.True((await new UpdateProfileRequestValidator().ValidateAsync(
            new UpdateProfileRequest("Updated Author", "https://example.com/avatar.png", "Bio", true, true, true))).IsValid);
    }

    [Fact]
    public async Task AuthValidatorsRejectInvalidRequests()
    {
        Assert.False((await new LoginRequestValidator().ValidateAsync(new LoginRequest("bad", string.Empty))).IsValid);
        Assert.False((await new GoogleLoginRequestValidator().ValidateAsync(new GoogleLoginRequest(string.Empty))).IsValid);
        Assert.False((await new RefreshTokenRequestValidator().ValidateAsync(new RefreshTokenRequest(string.Empty))).IsValid);
        Assert.False((await new UpdateProfileRequestValidator().ValidateAsync(
            new UpdateProfileRequest(null, null, null))).IsValid);
        Assert.False((await new UpdateProfileRequestValidator().ValidateAsync(
            new UpdateProfileRequest(null, "javascript:alert(1)", null, false, true))).IsValid);
    }

    [Fact]
    public async Task SearchQueryRejectsInvalidPageAndFilters()
    {
        var validator = new SearchPublishedRecipesQueryValidator();
        Assert.True((await validator.ValidateAsync(
            new SearchPublishedRecipesQuery("phở", null, RecipeDifficulty.Easy, null, "relevance", 1, 12))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new SearchPublishedRecipesQuery("x", null, null, 0, "random", 0, 51))).IsValid);
    }

    [Fact]
    public async Task ContentValidatorsAcceptValidRequests()
    {
        var category = new CategoryWriteRequest("Món chính", "Mô tả", "https://example.com/category.png", 1);
        var recipe = new RecipeWriteRequest(
            "Công thức thử nghiệm",
            "Mô tả",
            Guid.NewGuid(),
            10,
            20,
            4,
            Domain.Recipes.RecipeDifficulty.Medium,
            "Hướng dẫn",
            new NutritionDto(100, 10, 20, 5, 2, 50));

        Assert.True((await new CategoryWriteRequestValidator().ValidateAsync(category)).IsValid);
        Assert.True((await new RecipeWriteRequestValidator().ValidateAsync(recipe)).IsValid);
    }

    [Fact]
    public async Task ContentValidatorsRejectUnsafeOrInvalidRequests()
    {
        var category = new CategoryWriteRequest("x", null, "javascript:alert(1)", -1);
        var recipe = new RecipeWriteRequest(
            "bad",
            string.Empty,
            Guid.Empty,
            0,
            -1,
            0,
            (Domain.Recipes.RecipeDifficulty)999,
            new string('x', 10_001),
            new NutritionDto(-1, -1, -1, -1, -1, -1));

        Assert.False((await new CategoryWriteRequestValidator().ValidateAsync(category)).IsValid);
        Assert.False((await new RecipeWriteRequestValidator().ValidateAsync(recipe)).IsValid);
    }

    [Fact]
    public void ProblemExceptionsPreserveContractFields()
    {
        var auth = new AuthProblemException("AUTH_INVALID", "Invalid authentication.");
        var content = new ContentProblemException("RECIPE_CONFLICT", "Conflict.", ContentProblemKind.Conflict, 3);

        Assert.Equal("AUTH_INVALID", auth.Code);
        Assert.Equal("Invalid authentication.", auth.Message);
        Assert.Equal("RECIPE_CONFLICT", content.Code);
        Assert.Equal(ContentProblemKind.Conflict, content.Kind);
        Assert.Equal(3, content.CurrentVersion);
    }

    [Fact]
    public void ContractRecordsPreservePayloads()
    {
        var now = DateTimeOffset.UtcNow;
        var user = new UserDto("user-id", "author@example.com", "Author", null, null, ["Author"], true, true, now);
        var session = new AuthSessionDto("access", "refresh", now.AddMinutes(15), user);
        var category = new CategoryDto(Guid.NewGuid(), "Món chính", "mon-chinh", null, null, 1, 2);
        var nutrition = new NutritionDto(100, 10, 20, 5, 2, 50);
        var author = new RecipeAuthorDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.AvatarUrl,
            user.Bio,
            user.Roles,
            user.EmailConfirmed,
            user.IsActive,
            user.CreatedAt);
        var recipe = new RecipeDto(
            Guid.NewGuid(),
            "Recipe title",
            "recipe-title",
            "Description",
            10,
            20,
            4,
            "medium",
            "published",
            null,
            category,
            author,
            now,
            now,
            1,
            "Instructions",
            nutrition,
            [],
            [],
            []);
        var page = new PageEnvelope<RecipeDto>([recipe], new PageMeta(1, 12, 1, 1, false, false));
        var detail = new CategoryDetailEnvelope(new CategoryDetailData(category, [recipe]), page.Meta);

        Assert.Equal(user, new DataEnvelope<UserDto>(session.User).Data);
        Assert.Equal("access", session.AccessToken);
        Assert.Equal(category, recipe.Category);
        Assert.Single(page.Data);
        Assert.Single(detail.Data.Recipes);
    }
}
