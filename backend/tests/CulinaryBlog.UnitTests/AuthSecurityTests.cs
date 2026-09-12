using CulinaryBlog.Application.Auth;

namespace CulinaryBlog.UnitTests;

public sealed class AuthSecurityTests
{
    [Fact]
    public void RefreshTokenUses512BitsAndOnlyItsSha256HashNeedsPersistence()
    {
        var rawToken = RefreshTokenSecurity.Create();
        var padded = rawToken.Replace('-', '+').Replace('_', '/').PadRight(88, '=');
        var bytes = Convert.FromBase64String(padded);
        var hash = RefreshTokenSecurity.Hash(rawToken);

        Assert.Equal(64, bytes.Length);
        Assert.Equal(64, hash.Length);
        Assert.DoesNotContain(rawToken, hash, StringComparison.Ordinal);
        Assert.Equal(hash, RefreshTokenSecurity.Hash(rawToken));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumber!")]
    [InlineData("NoSymbol1")]
    public async Task RegisterValidatorRejectsWeakPasswords(string password)
    {
        var result = await new RegisterRequestValidator()
            .ValidateAsync(new RegisterRequest("Test Author", "author@example.com", password));

        Assert.False(result.IsValid);
    }
}
