// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for category name validation tests.</summary>
public sealed class CategoryNameValidationTests
{
    /// <summary>Verifies that create and update respect business name bounds.</summary>
    /// <param name="length">The length supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    [InlineData(100, false)]
    public async Task CreateAndUpdateRespectBusinessNameBounds(int length, bool valid)
    {
        var name = new string('x', length);
        var validation = await new CategoryWriteRequestValidator().ValidateAsync(new CategoryWriteRequest(name, null, null, 0));
        Assert.Equal(valid, validation.IsValid);
        var category = Category.Create(Guid.NewGuid(), "Món chay", "mon-chay", null, null, 0);
        if (valid)
        {
            Assert.Equal(name, Category.Create(Guid.NewGuid(), name, "valid", null, null, 0).Name);
            category.Update(name, null, null, 0);
            Assert.Equal(name, category.Name);
        }
        else
        {
            Assert.Throws<DomainException>(() => Category.Create(Guid.NewGuid(), name, "invalid", null, null, 0));
            Assert.Throws<DomainException>(() => category.Update(name, null, null, 0));
            Assert.Equal("Món chay", category.Name);
        }
    }

    /// <summary>Verifies that html is rejected without mutating category.</summary>
    /// <param name="name">The name supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("<b>Món chay</b>")]
    [InlineData("<img src=x>")]
    public async Task HtmlIsRejectedWithoutMutatingCategory(string name)
    {
        var validation = await new CategoryWriteRequestValidator().ValidateAsync(new CategoryWriteRequest(name, null, null, 0));
        Assert.False(validation.IsValid);
        var category = Category.Create(Guid.NewGuid(), "Món chay", "mon-chay", null, null, 0);
        Assert.Throws<DomainException>(() => Category.Create(Guid.NewGuid(), name, "invalid", null, null, 0));
        Assert.Throws<DomainException>(() => category.Update(name, null, null, 0));
        Assert.Equal("Món chay", category.Name);
    }

    /// <summary>Verifies that only absolute HTTP(S) URLs and uploaded media paths are accepted as category images.</summary>
    /// <param name="imageUrl">The image URL supplied to this operation.</param>
    /// <param name="valid">The valid supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("https://example.com/a.jpg", true)]
    [InlineData("/api/v1/media/category/0123456789abcdef0123456789abcdef.jpg", true)]
    [InlineData("/api/v1/media/../secret", false)]
    [InlineData("/api/v1/media/category/a b.png", false)]
    [InlineData("/api/v1/media/category/x.png?y=1", false)]
    [InlineData("/api/v1/media/23eb2624-5315-9249-d357-a2b65a00c95c/medium", true)]
    [InlineData("/other/path.jpg", false)]
    [InlineData("javascript:alert(1)", false)]
    public async Task CategoryImageUrlAcceptsOnlyHttpUrlsAndUploadedMediaPaths(string imageUrl, bool valid)
    {
        var validation = await new CategoryWriteRequestValidator().ValidateAsync(new CategoryWriteRequest("Món chay", null, imageUrl, 0));
        Assert.Equal(valid, validation.IsValid);
    }

    /// <summary>Verifies that a step timer cannot exceed 24 hours.</summary>
    /// <param name="minutes">The timer length in minutes.</param>
    /// <param name="valid">Whether the timer should be accepted.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1440, true)]
    [InlineData(1440.5, false)]
    [InlineData(20_000_000_000, false)]
    public async Task StepTimerIsLimitedToTwentyFourHours(double minutes, bool valid)
    {
        var validation = await new StepWriteRequestValidator().ValidateAsync(new StepWriteRequest("Cook", "Cook rice", minutes));
        Assert.Equal(valid, validation.IsValid);
    }
}
