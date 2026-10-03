using System.Net;
using System.Security.Claims;
using CulinaryBlog.Api.Presentation;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage;
using Microsoft.AspNetCore.Http;

namespace CulinaryBlog.IntegrationTests;

public sealed class AdminDashboardAuthorizationFilterTests
{
    [Fact]
    public void UnauthenticatedUserIsDenied()
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: true);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        var context = CreateContext(httpContext);

        Assert.False(filter.Authorize(context));
    }

    [Fact]
    public void AuthenticatedNonAdminUserIsDenied()
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: true);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Author"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "TestAuth"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        var context = CreateContext(httpContext);

        Assert.False(filter.Authorize(context));
    }

    [Fact]
    public void AdminUserFromPublicIpIsDeniedWhenNetworkRestricted()
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: true);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "TestAuth"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.45");

        var context = CreateContext(httpContext);

        Assert.False(filter.Authorize(context));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.4.15")]
    [InlineData("172.20.1.5")]
    [InlineData("192.168.1.50")]
    public void AdminUserFromInternalOrLoopbackNetworkIsAllowed(string ipAddress)
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: true);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "TestAuth"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);

        var context = CreateContext(httpContext);

        Assert.True(filter.Authorize(context));
    }

    [Fact]
    public void AdminUserFromExplicitlyAllowedPublicIpIsAllowed()
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: true, ["198.51.100.99"]);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "TestAuth"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.99");

        var context = CreateContext(httpContext);

        Assert.True(filter.Authorize(context));
    }

    [Fact]
    public void AdminUserWhenNetworkRestrictionDisabledIsAllowedFromAnywhere()
    {
        var filter = new AdminDashboardAuthorizationFilter(restrictNetwork: false);
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Admin"), new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "TestAuth"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.99");

        var context = CreateContext(httpContext);

        Assert.True(filter.Authorize(context));
    }

    private sealed class TestJobStorage : JobStorage
    {
        public override IMonitoringApi GetMonitoringApi() => throw new NotImplementedException();
        public override IStorageConnection GetConnection() => throw new NotImplementedException();
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private static AspNetCoreDashboardContext CreateContext(HttpContext httpContext)
    {
        httpContext.RequestServices ??= new EmptyServiceProvider();
        return new AspNetCoreDashboardContext(new TestJobStorage(), new DashboardOptions(), httpContext);
    }
}
