// Culinary Blog. Authorship is recorded in the repository history.

using System.ComponentModel.DataAnnotations;
using CulinaryBlog.Infrastructure.Identity;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for admin seed options validation tests.</summary>
public sealed class AdminSeedOptionsValidationTests
{
    /// <summary>Verifies that disabled admin seed allows blank credentials.</summary>
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

    /// <summary>Verifies that enabled admin seed requires valid credentials.</summary>
    /// <param name="email">The email supplied to this operation.</param>
    /// <param name="password">The password supplied for this authentication operation.</param>
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
