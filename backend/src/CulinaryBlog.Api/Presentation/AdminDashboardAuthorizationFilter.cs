// Culinary Blog. Authorship is recorded in the repository history.

using System.Net;
using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Api.Presentation;

/// <summary>Represents the admin dashboard authorization filter.</summary>
public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool? restrictNetwork;
    private readonly HashSet<string>? allowedIps;

    /// <summary>Initializes a new instance of the <see cref="AdminDashboardAuthorizationFilter"/> class.</summary>
    public AdminDashboardAuthorizationFilter()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AdminDashboardAuthorizationFilter"/> class.</summary>
    /// <param name="restrictNetwork">The restrict network supplied to this operation.</param>
    /// <param name="allowedIps">The allowed ips supplied to this operation.</param>
    public AdminDashboardAuthorizationFilter(bool restrictNetwork, IEnumerable<string>? allowedIps = null)
    {
        this.restrictNetwork = restrictNetwork;
        this.allowedIps = allowedIps is not null ? new HashSet<string>(allowedIps, StringComparer.OrdinalIgnoreCase) : null;
    }

    /// <summary>Performs the authorize operation.</summary>
    /// <param name="context">The context supplied to this operation.</param>
    /// <returns>The authorize result.</returns>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true || !user.IsInRole("Admin"))
        {
            return false;
        }

        var configuration = httpContext.RequestServices?.GetService<IConfiguration>();
        var networkRestrictionEnabled = this.restrictNetwork ??
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
        if (this.allowedIps?.Contains(ipString) == true)
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
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12 (172.16.x.x - 172.31.x.x)
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            return false;
        }

        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // fc00::/7 (Unique Local Address)
            if ((bytes[0] & 0xFE) == 0xFC)
            {
                return true;
            }

            // fe80::/10 (Link-Local)
            if (bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80)
            {
                return true;
            }
        }

        return false;
    }
}
