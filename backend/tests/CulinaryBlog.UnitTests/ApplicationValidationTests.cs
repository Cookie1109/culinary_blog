// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Auth;
using CulinaryBlog.Application.Content;
using CulinaryBlog.Application.Features.Recipes.Queries;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for application validation tests.</summary>
public sealed class ApplicationValidationTests
{
    /// <summary>Verifies that nutrition rejects values that cannot be stored without overflow or rounding.</summary>
    /// <param name="input">The input supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("1000000")]
    [InlineData("1.001")]
    public async Task NutritionRejectsValuesThatCannotBeStoredWithoutOverflowOrRounding(string input)
    {
        var value = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        var request = new RecipeWriteRequest(
            "Valid title",
            "Description",
            Guid.NewGuid(),
            1,
            0,
            1,
            Domain.Recipes.RecipeDifficulty.Easy,
            null,
            new NutritionDto(Calories: value));
        var result = await new RecipeWriteRequestValidator().ValidateAsync(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Nutrition.Calories");
    }

    /// <summary>Verifies that auth validators accept valid requests.</summary>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Verifies that auth validators reject invalid requests.</summary>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Verifies that search accepts advanced filter boundaries and rejects invalid ranges.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SearchAcceptsAdvancedFilterBoundariesAndRejectsInvalidRanges()
    {
        var validator = new SearchPublishedRecipesQueryValidator();
        var valid = new SearchPublishedRecipesQuery(
            "pho",
            null,
            RecipeDifficulty.Easy,
            30,
            "-cookTime",
            1,
            50,
            Guid.NewGuid(),
            0,
            1,
            1,
            1);
        Assert.True((await validator.ValidateAsync(valid)).IsValid);
        SearchPublishedRecipesQuery[] invalid = [
            valid with { Search = null }, valid with { Search = " " },
            valid with { CategoryId = Guid.Empty }, valid with { MaxCookTime = -1 },
            valid with { MinServings = 0 }, valid with { MinPrepTime = 0 }, valid with { MaxPrepTime = 0 },
            valid with { MinPrepTime = 20, MaxPrepTime = 10 }, valid with { Difficulty = (RecipeDifficulty)999 },
        ];
        foreach (var request in invalid)
        {
            Assert.False((await validator.ValidateAsync(request)).IsValid);
        }
    }

    /// <summary>Verifies that search query rejects invalid page and filters.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task SearchQueryRejectsInvalidPageAndFilters()
    {
        var validator = new SearchPublishedRecipesQueryValidator();
        Assert.True((await validator.ValidateAsync(
            new SearchPublishedRecipesQuery("phở", null, RecipeDifficulty.Easy, null, "relevance", 1, 12))).IsValid);
        Assert.False((await validator.ValidateAsync(
            new SearchPublishedRecipesQuery("x", null, null, 0, "random", 0, 51))).IsValid);
    }

    /// <summary>Verifies that published recipe list query validates filters sort and pagination.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PublishedRecipeListQueryValidatesFiltersSortAndPagination()
    {
        var validator = new ListPublishedRecipesQueryValidator();
        Assert.True((await validator.ValidateAsync(new ListPublishedRecipesQuery(
            Guid.NewGuid(), RecipeDifficulty.Easy, 30, 4, 5, 20, "-createdAt", 1, 12))).IsValid);
        Assert.False((await validator.ValidateAsync(new ListPublishedRecipesQuery(
            Guid.Empty, null, -1, 0, 20, 10, "title;drop table recipes", 0, 51))).IsValid);
        Assert.True((await validator.ValidateAsync(new ListPublishedRecipesQuery(
            null, null, null, null, null, null, "quickest", 1, 12, "mon-viet", 30))).IsValid);
        Assert.False((await validator.ValidateAsync(new ListPublishedRecipesQuery(
            null, null, null, null, null, null, null, 1, 12, new string('x', 121), 0))).IsValid);
    }

    /// <summary>Verifies that content validators accept valid requests.</summary>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Verifies that category names reject html.</summary>
    /// <param name="name">The name supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("<b>Category</b>", false)]
    [InlineData("Plain category", true)]
    public async Task CategoryNamesRejectHtml(string name, bool valid)
    {
        var result = await new CategoryWriteRequestValidator().ValidateAsync(new CategoryWriteRequest(name));
        Assert.Equal(valid, result.IsValid);
    }

    /// <summary>Verifies that content validators reject unsafe or invalid requests.</summary>
    /// <returns>A task representing the operation and its result.</returns>
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

    /// <summary>Verifies that problem exceptions preserve contract fields.</summary>
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

    /// <summary>Verifies that contract records preserve payloads.</summary>
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
