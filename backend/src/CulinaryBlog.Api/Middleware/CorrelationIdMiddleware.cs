// Culinary Blog. Authorship is recorded in the repository history.

using System.Diagnostics;
using System.Globalization;

namespace CulinaryBlog.Api.Middleware;

/// <summary>Represents the correlation id middleware.</summary>
/// <param name="next">The next supplied to this operation.</param>
/// <param name="logger">The logger supplied to this operation.</param>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    /// <summary>The header name.</summary>
    public const string HeaderName = "X-Correlation-ID";
    private const int MaximumLength = 128;

    /// <summary>Performs the invoke operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <returns>A task representing the operation and its result.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        context.Response.OnStarting(() =>
        {
            // Output Cache may restore a stored header; correlation belongs to this request.
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation.id", correlationId);
        Activity.Current?.AddBaggage("correlation.id", correlationId);

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
        }))
        {
            await next(context).ConfigureAwait(false);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        var candidate = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(candidate) &&
            candidate.Length <= MaximumLength &&
            candidate.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
        {
            return candidate;
        }

        return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    }
}
