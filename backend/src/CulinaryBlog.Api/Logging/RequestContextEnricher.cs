using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Api.Logging;

public sealed class RequestContextEnricher(IHttpContextAccessor accessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = accessor.HttpContext;
        if (context is null) return;
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", context.TraceIdentifier));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId",
            context.User.FindFirstValue("sub") ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous"));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", context.Request.Method));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", context.Request.Path.Value));
    }
}
