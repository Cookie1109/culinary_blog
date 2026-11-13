// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Auth;

namespace CulinaryBlog.UnitTests;

/// <summary>Contains regression checks for auth security tests.</summary>
public sealed class AuthSecurityTests
{
    /// <summary>Verifies that refresh token uses512 bits and only its sha256 hash needs persistence.</summary>
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

    /// <summary>Verifies that register validator rejects weak passwords.</summary>
    /// <param name="password">The password supplied for this authentication operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
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
