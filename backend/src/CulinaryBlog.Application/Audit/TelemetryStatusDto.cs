// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Application.Content;

namespace CulinaryBlog.Application.Audit;

/// <summary>Carries telemetry status dto data.</summary>
/// <param name="SeqConfigured">The seq configured supplied to this operation.</param>
/// <param name="OtlpConfigured">The otlp configured supplied to this operation.</param>
/// <param name="OperationalNetworkRestricted">The operational network restricted supplied to this operation.</param>
public sealed record TelemetryStatusDto(
    bool SeqConfigured,
    bool OtlpConfigured,
    bool OperationalNetworkRestricted);
