// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Auth;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for password request validation tests.</summary>
public sealed class PasswordRequestValidationTests
{
    /// <summary>Verifies that change and reset reject weak passwords.</summary>
    /// <param name="password">The password supplied for this authentication operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    [InlineData("lowercase1!")]
    [InlineData("UPPERCASE1!")]
    [InlineData("NoNumbers!")]
    [InlineData("NoSymbols123")]
    public async Task ChangeAndResetRejectWeakPasswords(string password)
    {
        Assert.False((await new ChangePasswordRequestValidator().ValidateAsync(
            new ChangePasswordRequest("Old#Password1", password))).IsValid);
        Assert.False((await new ResetPasswordRequestValidator().ValidateAsync(
            new ResetPasswordRequest("author@example.test", "token", password))).IsValid);
    }

    /// <summary>Verifies that password boundaries and required fields are validated.</summary>
    /// <returns>A task representing the operation and its result.</returns>
    [Fact]
    public async Task PasswordBoundariesAndRequiredFieldsAreValidated()
    {
        var change = new ChangePasswordRequestValidator();
        var reset = new ResetPasswordRequestValidator();
        Assert.True((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", "New#Password2"))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest(string.Empty, "New#Password2"))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", null!))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", "token", null!))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", "New#Password2" + new string('a', 128)))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("invalid", "token", "New#Password2"))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", string.Empty, "New#Password2"))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", new string('a', 4097), "New#Password2"))).IsValid);
        Assert.True((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", "token", "New#Password2"))).IsValid);
    }

    /// <summary>Verifies that forgot rejects invalid email.</summary>
    /// <param name="email">The email supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task ForgotRejectsInvalidEmail(string email)
    {
        Assert.False((await new ForgotPasswordRequestValidator().ValidateAsync(new ForgotPasswordRequest(email))).IsValid);
    }
}
