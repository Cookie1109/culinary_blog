// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Api.Security;
using Microsoft.Extensions.Configuration;

namespace CulinaryBlog.IntegrationTests;

/// <summary>Contains regression checks for release metadata tests.</summary>
public sealed class ReleaseMetadataTests
{
    /// <summary>Verifies that render uses platform revision without inventing image digests or frontend deployment.</summary>
    [Fact]
    public void RenderUsesPlatformRevisionWithoutInventingImageDigestsOrFrontendDeployment()
    {
        var configuration = CreateRenderConfiguration();
        ProductionConfigurationGuard.Validate(true, configuration);
        var metadata = ReleaseMetadata.FromConfiguration(configuration);

        Assert.Equal("Render", metadata.Provider);
        Assert.Equal(new string('a', 40), metadata.CommitSha);
        Assert.Equal("srv-test123", metadata.ApiServiceId);
        Assert.Equal("instance-test123", metadata.ApiInstanceId);
        Assert.Null(metadata.ApiImageDigest);
        Assert.Null(metadata.WebImageDigest);
        Assert.Null(metadata.WebDeploymentId);
    }

    /// <summary>Verifies that render rejects missing or invalid platform identity.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="value">The value supplied to this operation.</param>
    [Theory]
    [InlineData("RENDER", null)]
    [InlineData("RENDER", "false")]
    [InlineData("RENDER_GIT_COMMIT", "invalid")]
    [InlineData("RENDER_SERVICE_ID", "invalid")]
    [InlineData("RENDER_INSTANCE_ID", "")]
    public void RenderRejectsMissingOrInvalidPlatformIdentity(string key, string? value)
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?> { [key] = value });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, configuration));
        Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that render rejects declared revision that differs from platform revision.</summary>
    [Fact]
    public void RenderRejectsDeclaredRevisionThatDiffersFromPlatformRevision()
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?>
        {
            ["Release:CommitSha"] = new string('b', 40),
        });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, configuration));
        Assert.Contains("must match RENDER_GIT_COMMIT", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that render keeps existing secret host and https guards.</summary>
    /// <param name="key">The key supplied to this operation.</param>
    /// <param name="value">The value supplied to this operation.</param>
    [Theory]
    [InlineData("ConnectionStrings:Database", "Host=localhost;Password=local-development-only")]
    [InlineData("Jwt:SigningKey", "local-development-only")]
    [InlineData("ObjectStorage:SecretKey", "local-development-only")]
    [InlineData("AllowedHosts", "*")]
    [InlineData("Sitemap:PublicBaseUrl", "http://culinary.example.test")]
    [InlineData("Cors:AllowedOrigins:0", "http://culinary.example.test")]
    public void RenderKeepsExistingSecretHostAndHttpsGuards(string key, string value)
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?> { [key] = value });
        Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, configuration));
    }

    /// <summary>Verifies that optional vercel identity is validated when supplied.</summary>
    [Fact]
    public void OptionalVercelIdentityIsValidatedWhenSupplied()
    {
        var valid = CreateRenderConfiguration(new Dictionary<string, string?> { ["Release:WebDeploymentId"] = "dpl_TestDeployment123" });
        ProductionConfigurationGuard.Validate(true, valid);
        Assert.Equal("dpl_TestDeployment123", ReleaseMetadata.FromConfiguration(valid).WebDeploymentId);
        var invalid = CreateRenderConfiguration(new Dictionary<string, string?> { ["Release:WebDeploymentId"] = "https://app.vercel.app" });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, invalid));
        Assert.Contains("Release:WebDeploymentId", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that render does not mislabel configured legacy digests as evidence for this deployment.</summary>
    [Fact]
    public void RenderDoesNotMislabelConfiguredLegacyDigestsAsEvidenceForThisDeployment()
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?>
        {
            ["Release:ApiImageDigest"] = $"sha256:{new string('b', 64)}",
            ["Release:WebImageDigest"] = $"sha256:{new string('c', 64)}",
        });
        ProductionConfigurationGuard.Validate(true, configuration);
        var metadata = ReleaseMetadata.FromConfiguration(configuration);
        Assert.Null(metadata.ApiImageDigest);
        Assert.Null(metadata.WebImageDigest);
    }

    /// <summary>Verifies that render environment does not automatically bypass default docker release contract.</summary>
    [Fact]
    public void RenderEnvironmentDoesNotAutomaticallyBypassDefaultDockerReleaseContract()
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?>
        {
            ["Release:Provider"] = null,
            ["Release:CommitSha"] = new string('a', 40),
        });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, configuration));
        Assert.Contains("Release:ApiImageDigest", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Release:WebImageDigest", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that unknown provider fails closed.</summary>
    /// <param name="provider">The provider supplied to this operation.</param>
    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    public void UnknownProviderFailsClosed(string provider)
    {
        var configuration = CreateRenderConfiguration(new Dictionary<string, string?> { ["Release:Provider"] = provider });
        var exception = Assert.Throws<InvalidOperationException>(() => ProductionConfigurationGuard.Validate(true, configuration));
        Assert.Contains("Release:Provider", exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration CreateRenderConfiguration(IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Host=database;Database=culinary;Username=app;Password=a-deployment-secret",
            ["Jwt:SigningKey"] = "a-deployment-managed-signing-key-at-least-32-characters",
            ["ObjectStorage:AccessKey"] = "deployment-access-key",
            ["ObjectStorage:SecretKey"] = "deployment-secret-key",
            ["AllowedHosts"] = "culinary.example.test",
            ["Sitemap:PublicBaseUrl"] = "https://culinary.example.test",
            ["Cors:AllowedOrigins:0"] = "https://culinary.example.test",
            ["Release:Provider"] = "Render",
            ["RENDER"] = "true",
            ["RENDER_GIT_COMMIT"] = new string('a', 40),
            ["RENDER_SERVICE_ID"] = "srv-test123",
            ["RENDER_INSTANCE_ID"] = "instance-test123",
        };
        if (overrides is not null)
        {
            foreach (var (key, value) in overrides) values[key] = value;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
