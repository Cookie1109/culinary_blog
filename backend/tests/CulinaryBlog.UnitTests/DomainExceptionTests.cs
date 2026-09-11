using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.UnitTests;

public sealed class DomainExceptionTests
{
    [Fact]
    public void ConstructorPreservesStableCodeAndMessage()
    {
        var exception = new DomainException("RECIPE_INVALID", "Recipe is invalid.");

        Assert.Equal("RECIPE_INVALID", exception.Code);
        Assert.Equal("Recipe is invalid.", exception.Message);
    }
}
