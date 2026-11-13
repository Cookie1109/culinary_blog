// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for domain exception tests.</summary>
public sealed class DomainExceptionTests
{
    /// <summary>Verifies that constructor preserves stable code and message.</summary>
    [Fact]
    public void ConstructorPreservesStableCodeAndMessage()
    {
        var exception = new DomainException("RECIPE_INVALID", "Recipe is invalid.");

        Assert.Equal("RECIPE_INVALID", exception.Code);
        Assert.Equal("Recipe is invalid.", exception.Message);
    }
}
