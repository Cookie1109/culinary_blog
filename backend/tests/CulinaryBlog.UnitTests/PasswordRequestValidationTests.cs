using CulinaryBlog.Application.Auth;

namespace CulinaryBlog.UnitTests;

public sealed class PasswordRequestValidationTests
{
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

    [Fact]
    public async Task PasswordBoundariesAndRequiredFieldsAreValidated()
    {
        var change = new ChangePasswordRequestValidator();
        var reset = new ResetPasswordRequestValidator();
        Assert.True((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", "New#Password2"))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest("", "New#Password2"))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", null!))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", "token", null!))).IsValid);
        Assert.False((await change.ValidateAsync(new ChangePasswordRequest("Old#Password1", "New#Password2" + new string('a', 128)))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("invalid", "token", "New#Password2"))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", "", "New#Password2"))).IsValid);
        Assert.False((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", new string('a', 4097), "New#Password2"))).IsValid);
        Assert.True((await reset.ValidateAsync(new ResetPasswordRequest("a@example.test", "token", "New#Password2"))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task ForgotRejectsInvalidEmail(string email)
    {
        Assert.False((await new ForgotPasswordRequestValidator().ValidateAsync(new ForgotPasswordRequest(email))).IsValid);
    }
}
