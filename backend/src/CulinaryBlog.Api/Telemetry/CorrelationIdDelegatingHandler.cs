// Culinary Blog. Authorship is recorded in the repository history.

using CulinaryBlog.Api.Middleware;

namespace CulinaryBlog.Api.Telemetry;

/// <summary>Represents the correlation id delegating handler.</summary>
/// <param name="accessor">The accessor supplied to this operation.</param>
internal sealed class CorrelationIdDelegatingHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    /// <summary>Performs the send operation.</summary>
    /// <param name="request">The request supplied to this operation.</param>
    /// <param name="cancellationToken">Cancels the operation when requested by the caller.</param>
    /// <returns>A task representing the operation and its result.</returns>
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = accessor.HttpContext?.TraceIdentifier;
        if (!string.IsNullOrWhiteSpace(correlationId) && !request.Headers.Contains(CorrelationIdMiddleware.HeaderName))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
