// Culinary Blog. Authorship is recorded in the repository history.

using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Api.Logging;

/// <summary>Represents the request context enricher.</summary>
/// <param name="accessor">The accessor supplied to this operation.</param>
public sealed class RequestContextEnricher(IHttpContextAccessor accessor) : ILogEventEnricher
{
    /// <summary>Performs the enrich operation.</summary>
    /// <param name="logEvent">The log event supplied to this operation.</param>
    /// <param name="propertyFactory">The property factory supplied to this operation.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = accessor.HttpContext;
        if (context is not null)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", context.Request.Method));
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", context?.TraceIdentifier ?? System.Diagnostics.Activity.Current?.TraceId.ToString() ?? "system"));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", context?.Request.Path.Value ?? "background"));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "UserId",
            context?.User.FindFirstValue("sub") ?? context?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? (context is null ? "system" : "anonymous")));
    }
}
