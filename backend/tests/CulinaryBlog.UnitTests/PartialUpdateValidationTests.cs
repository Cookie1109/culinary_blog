// Culinary Blog. Authorship is recorded in the repository history.

using System.Text.Json;
using CulinaryBlog.Application;
using CulinaryBlog.Application.Content;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for partial update validation tests.</summary>
public sealed class PartialUpdateValidationTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Verifies that recipe updates distinguish omitted nullable and invalid fields.</summary>
    /// <param name="body">The body supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"instructions\":null,\"nutrition\":null}", true)]
    [InlineData("{\"title\":\"Fresh soup\",\"description\":\"A warm soup\",\"prepTime\":1,\"cookTime\":0,\"servings\":1,\"difficulty\":1,\"nutrition\":{\"calories\":0,\"protein\":1,\"fat\":null,\"fiber\":0,\"sodium\":1,\"carbohydrates\":1}}", true)]
    [InlineData("{\"title\":null}", false)]
    [InlineData("{\"title\":\"a\",\"description\":\"\",\"categoryId\":null,\"prepTime\":0,\"cookTime\":-1,\"servings\":0,\"difficulty\":99}", false)]
    [InlineData("{\"nutrition\":{\"calories\":-1,\"protein\":1000000,\"fat\":0.001,\"fiber\":-1,\"sodium\":-1,\"carbohydrates\":-1}}", false)]
    public void RecipeUpdatesDistinguishOmittedNullableAndInvalidFields(string body, bool valid) =>
        AssertValidation<RecipeUpdateRequest>(body, valid);

    /// <summary>Verifies that ingredient updates validate only supplied fields.</summary>
    /// <param name="body">The body supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"name\":\"Salt\",\"quantity\":null,\"unit\":null,\"notes\":null,\"orderIndex\":0}", true)]
    [InlineData("{\"name\":null,\"quantity\":0,\"orderIndex\":-1}", false)]
    [InlineData("{\"quantity\":0.0001}", false)]
    public void IngredientUpdatesValidateOnlySuppliedFields(string body, bool valid) =>
        AssertValidation<IngredientUpdateRequest>(body, valid);

    /// <summary>Verifies that step updates reject unsafe urls and invalid order.</summary>
    /// <param name="body">The body supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    [Theory]
    [InlineData("{}", true)]
    [InlineData("{\"title\":\"Boil\",\"description\":\"Boil water\",\"timerMinutes\":0,\"imageUrl\":\"https://example.test/step.jpg\",\"stepNumber\":1}", true)]
    [InlineData("{\"timerMinutes\":null,\"imageUrl\":null}", true)]
    [InlineData("{\"timerMinutes\":0.016666666666666666}", true)]
    [InlineData("{\"timerMinutes\":0.5}", true)]
    [InlineData("{\"title\":\"\",\"description\":null,\"timerMinutes\":-1,\"imageUrl\":\"javascript:alert(1)\",\"stepNumber\":0}", false)]
    public void StepUpdatesRejectUnsafeUrlsAndInvalidOrder(string body, bool valid) =>
        AssertValidation<StepUpdateRequest>(body, valid);

    /// <summary>Verifies that json round trip keeps explicit null separate from omission.</summary>
    [Fact]
    public void JsonRoundTripKeepsExplicitNullSeparateFromOmission()
    {
        var request = JsonSerializer.Deserialize<RecipeUpdateRequest>("{\"instructions\":null}", Json)!;
        Assert.False(request.Title.IsSpecified);
        Assert.True(request.Instructions.IsSpecified);
        Assert.Equal("Old title", request.Title.Or("Old title"));
        Assert.Null(request.Instructions.Or("Old instructions"));
        var roundTrip = JsonSerializer.Deserialize<RecipeUpdateRequest>(JsonSerializer.Serialize(request, Json), Json)!;
        Assert.Null(roundTrip.Instructions.Value);
    }

    private static void AssertValidation<T>(string body, bool valid)
    {
        using var provider = new ServiceCollection().AddApplication().BuildServiceProvider();
        var result = provider.GetRequiredService<IValidator<T>>().Validate(JsonSerializer.Deserialize<T>(body, Json)!);
        Assert.Equal(valid, result.IsValid);
        Assert.All(result.Errors, error => Assert.DoesNotContain("Value", error.PropertyName, StringComparison.Ordinal));
    }
}
