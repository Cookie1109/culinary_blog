using System.ComponentModel.DataAnnotations;
using CulinaryBlog.Infrastructure.Identity;

namespace CulinaryBlog.IntegrationTests;

public sealed class AdminSeedOptionsValidationTests
{
    [Fact]
    public void DisabledAdminSeedAllowsBlankCredentials()
    {
        var options = new AdminSeedOptions
        {
            Enabled = false,
            Email = string.Empty,
            Password = string.Empty,
        };

        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("", "ValidPassword1!")]
    [InlineData("not-an-email", "ValidPassword1!")]
    [InlineData("admin@example.com", "")]
    public void EnabledAdminSeedRequiresValidCredentials(string email, string password)
    {
        var options = new AdminSeedOptions
        {
            Enabled = true,
            Email = email,
            Password = password,
        };

        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), results, true);

        Assert.False(isValid);
        Assert.NotEmpty(results);
    }
}
