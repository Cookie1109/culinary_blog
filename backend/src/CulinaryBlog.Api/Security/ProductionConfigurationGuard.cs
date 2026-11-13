// Culinary Blog. Authorship is recorded in the repository history.

namespace CulinaryBlog.Api.Security;

/// <summary>Represents the production configuration guard.</summary>
public static class ProductionConfigurationGuard
{
    private static readonly string[] PlaceholderFragments =
    [
        "local-development-only",
        "culinary-local",
        "change-this",
        "replace-with",
    ];

    /// <summary>Validates the production configuration guard.</summary>
    /// <param name="isProduction">The is production supplied to this operation.</param>
    /// <param name="configuration">The configuration supplied to this operation.</param>
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
        ReleaseMetadata.Validate(configuration, failures);

        if (string.IsNullOrWhiteSpace(configuration["AllowedHosts"])
            || string.Equals(configuration["AllowedHosts"], "*", StringComparison.Ordinal))
        {
            failures.Add("AllowedHosts must use an explicit production allowlist");
        }

        ValidateHttpsUrl(configuration["Sitemap:PublicBaseUrl"], "Sitemap:PublicBaseUrl", failures);
        if (configuration.GetValue<bool>("Email:Enabled") && !configuration.GetValue<bool>("Email:EnableSsl"))
        {
            failures.Add("Email:EnableSsl must be true when production email is enabled");
        }

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
