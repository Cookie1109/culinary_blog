using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Common;
using CulinaryBlog.Domain.Recipes;

namespace CulinaryBlog.UnitTests;

public sealed class IngredientNameValidationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(200, false)]
    public async Task IngredientInputUsesBusinessLimitWithoutChangingStorage(int length, bool valid)
    {
        var name = new string('x', length);
        var result = await new IngredientWriteRequestValidator().ValidateAsync(new IngredientWriteRequest(name, null, null, null, 0));
        Assert.Equal(valid, result.IsValid);
        var ingredient = RecipeIngredient.Create(Guid.NewGuid(), Guid.NewGuid(), "Muối", null, null, null, 0);
        if (valid)
        {
            ingredient.Update(name, null, null, null, 0);
            Assert.Equal(name, ingredient.Name);
        }
        else
        {
            Assert.Throws<DomainException>(() => RecipeIngredient.Create(Guid.NewGuid(), Guid.NewGuid(), name, null, null, null, 0));
            Assert.Throws<DomainException>(() => ingredient.Update(name, null, null, null, 0));
            Assert.Equal("Muối", ingredient.Name);
        }
    }
}
