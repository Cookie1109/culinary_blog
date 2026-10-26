using CulinaryBlog.Application.Content;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Common;

namespace CulinaryBlog.UnitTests;

public sealed class CategoryNameValidationTests
{
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
}
