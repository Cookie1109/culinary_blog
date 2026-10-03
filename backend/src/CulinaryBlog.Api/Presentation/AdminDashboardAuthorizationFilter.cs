using System.Net;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Api.Presentation;

public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool? _restrictNetwork;
    private readonly HashSet<string>? _allowedIps;

    public AdminDashboardAuthorizationFilter()
    {
    }

    public AdminDashboardAuthorizationFilter(bool restrictNetwork, IEnumerable<string>? allowedIps = null)
    {
        _restrictNetwork = restrictNetwork;
        _allowedIps = allowedIps is not null ? new HashSet<string>(allowedIps, StringComparer.OrdinalIgnoreCase) : null;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Admin"))
        {
            return false;
        }

        var configuration = httpContext.RequestServices?.GetService<IConfiguration>();
        var networkRestrictionEnabled = _restrictNetwork ??
            configuration?.GetValue("BackgroundJobs:NetworkRestrictionEnabled", true) ?? true;

        if (!networkRestrictionEnabled)
        {
            return true;
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        if (remoteIp is null)
        {
            return false;
        }

        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        if (IPAddress.IsLoopback(remoteIp))
        {
            return true;
        }

        var ipString = remoteIp.ToString();
        if (_allowedIps?.Contains(ipString) == true)
        {
            return true;
        }

        var configuredAllowed = configuration?.GetSection("BackgroundJobs:AllowedIpAddresses").Get<string[]>();
        if (configuredAllowed is not null && configuredAllowed.Contains(ipString, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsPrivateNetwork(remoteIp);
    }

    private static bool IsPrivateNetwork(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12 (172.16.x.x - 172.31.x.x)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            return false;
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // fc00::/7 (Unique Local Address)
            if ((bytes[0] & 0xFE) == 0xFC) return true;
            // fe80::/10 (Link-Local)
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80) return true;
        }

        return false;
    }
}
