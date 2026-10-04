namespace CulinaryBlog.Api.Security;

public static class ProductionConfigurationGuard
{
    private static readonly string[] PlaceholderFragments =
    [
        "local-development-only",
        "culinary-local",
        "change-this",
        "replace-with",
    ];

    public static void Validate(bool isProduction, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!isProduction)
        {
            return;
        }

        var failures = new List<string>();
        RequireDeploymentValue(configuration.GetConnectionString("Database"), "ConnectionStrings:Database", failures);
        RequireDeploymentValue(configuration["Jwt:SigningKey"], "Jwt:SigningKey", failures);
        RequireDeploymentValue(configuration["ObjectStorage:AccessKey"], "ObjectStorage:AccessKey", failures);
        RequireDeploymentValue(configuration["ObjectStorage:SecretKey"], "ObjectStorage:SecretKey", failures);
        RequirePattern(
            configuration["Release:CommitSha"],
            "Release:CommitSha",
            "^[0-9a-f]{40}$",
            failures);
        RequirePattern(
            configuration["Release:ApiImageDigest"],
            "Release:ApiImageDigest",
            "^sha256:[0-9a-f]{64}$",
            failures);
        RequirePattern(
            configuration["Release:WebImageDigest"],
            "Release:WebImageDigest",
            "^sha256:[0-9a-f]{64}$",
            failures);

        if (string.IsNullOrWhiteSpace(configuration["AllowedHosts"])
            || string.Equals(configuration["AllowedHosts"], "*", StringComparison.Ordinal))
        {
            failures.Add("AllowedHosts must use an explicit production allowlist");
        }

        ValidateHttpsUrl(configuration["Sitemap:PublicBaseUrl"], "Sitemap:PublicBaseUrl", failures);
        foreach (var origin in configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        {
            ValidateHttpsUrl(origin, "Cors:AllowedOrigins", failures);
        }
        ValidateMaintenanceAnnouncement(configuration.GetSection("Operations:Maintenance"), failures);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                $"Production security configuration is invalid: {string.Join("; ", failures)}.");
        }
    }

    private static void RequireDeploymentValue(string? value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)
            || PlaceholderFragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            failures.Add($"{key} must be supplied by the deployment secret store");
        }
    }

    private static void ValidateHttpsUrl(string? value, string key, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add($"{key} must contain an absolute HTTPS URL");
        }
    }

    private static void RequirePattern(string? value, string key, string pattern, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
        {
            failures.Add($"{key} must identify the immutable deployed artifact");
        }
    }

    private static void ValidateMaintenanceAnnouncement(IConfigurationSection section, List<string> failures)
    {
        if (!section.GetValue<bool>("Enabled"))
        {
            return;
        }

        var message = section["Message"];
        var announcedAtUtc = section.GetValue<DateTimeOffset?>("AnnouncedAtUtc");
        var startsAtUtc = section.GetValue<DateTimeOffset?>("StartsAtUtc");
        var endsAtUtc = section.GetValue<DateTimeOffset?>("EndsAtUtc");

        if (string.IsNullOrWhiteSpace(message))
        {
            failures.Add("Operations:Maintenance:Message is required when maintenance is enabled");
        }
        if (announcedAtUtc is null || startsAtUtc is null || startsAtUtc - announcedAtUtc < TimeSpan.FromHours(48))
        {
            failures.Add("Operations:Maintenance must be announced at least 48 hours before it starts");
        }
        if (startsAtUtc is null || endsAtUtc is null || endsAtUtc <= startsAtUtc)
        {
            failures.Add("Operations:Maintenance:EndsAtUtc must be later than StartsAtUtc");
        }
    }
}
