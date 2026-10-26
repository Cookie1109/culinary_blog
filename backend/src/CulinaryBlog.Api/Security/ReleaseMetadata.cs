namespace CulinaryBlog.Api.Security;

public sealed record ReleaseMetadata(
    string Provider,
    string? CommitSha,
    string? ApiImageDigest,
    string? WebImageDigest,
    string? ApiServiceId,
    string? ApiInstanceId,
    string? WebDeploymentId)
{
    public static ReleaseMetadata FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var provider = configuration["Release:Provider"] ?? "Docker";
        if (string.Equals(provider, "Docker", StringComparison.OrdinalIgnoreCase)) provider = "Docker";
        var render = string.Equals(provider, "Render", StringComparison.OrdinalIgnoreCase);
        var commit = configuration["Release:CommitSha"];
        return new ReleaseMetadata(
            render ? "Render" : provider,
            render && string.IsNullOrWhiteSpace(commit) ? configuration["RENDER_GIT_COMMIT"] : commit,
            render ? null : configuration["Release:ApiImageDigest"],
            render ? null : configuration["Release:WebImageDigest"],
            render ? configuration["RENDER_SERVICE_ID"] : null,
            render ? configuration["RENDER_INSTANCE_ID"] : null,
            render ? configuration["Release:WebDeploymentId"] : null);
    }

    public static void Validate(IConfiguration configuration, ICollection<string> failures)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(failures);
        var metadata = FromConfiguration(configuration);
        RequirePattern(metadata.CommitSha, "Release:CommitSha", "^[0-9a-f]{40}$", failures);
        if (string.Equals(metadata.Provider, "Docker", StringComparison.OrdinalIgnoreCase))
        {
            RequirePattern(metadata.ApiImageDigest, "Release:ApiImageDigest", "^sha256:[0-9a-f]{64}$", failures);
            RequirePattern(metadata.WebImageDigest, "Release:WebImageDigest", "^sha256:[0-9a-f]{64}$", failures);
        }
        else if (metadata.Provider == "Render")
        {
            if (!string.Equals(configuration["RENDER"], "true", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("RENDER must confirm the Render runtime when Release:Provider is Render");
            }
            RequirePattern(configuration["RENDER_GIT_COMMIT"], "RENDER_GIT_COMMIT", "^[0-9a-f]{40}$", failures);
            RequirePattern(metadata.ApiServiceId, "RENDER_SERVICE_ID", "^srv-[a-z0-9]+$", failures);
            if (string.IsNullOrWhiteSpace(metadata.ApiInstanceId))
            {
                failures.Add("RENDER_INSTANCE_ID must identify the running Render instance");
            }
            if (!string.Equals(metadata.CommitSha, configuration["RENDER_GIT_COMMIT"], StringComparison.Ordinal))
            {
                failures.Add("Release:CommitSha must match RENDER_GIT_COMMIT");
            }
            if (!string.IsNullOrWhiteSpace(metadata.WebDeploymentId))
            {
                RequirePattern(metadata.WebDeploymentId, "Release:WebDeploymentId", "^dpl_[A-Za-z0-9]+$", failures);
            }
        }
        else
        {
            failures.Add("Release:Provider must be Docker or Render");
        }
    }

    private static void RequirePattern(string? value, string key, string pattern, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value)
            || !System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
        {
            failures.Add($"{key} must contain a valid deployment identity");
        }
    }
}
