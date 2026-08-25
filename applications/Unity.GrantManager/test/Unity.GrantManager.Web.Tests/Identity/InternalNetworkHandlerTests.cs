using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;
using Unity.GrantManager.Web.Identity.Policy;
using Xunit;

namespace Unity.GrantManager.Identity;

public class InternalNetworkHandlerTests
{
    private static Task<AuthorizationHandlerContext> BuildContextAsync(IPAddress remoteIp)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = remoteIp;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var requirement = new InternalNetworkRequirement();
        var authContext = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(),
            null);

        var handler = new InternalNetworkHandler(httpContextAccessor);
        return handler.HandleAsync(authContext).ContinueWith(_ => authContext);
    }

    [Theory]
    [InlineData("127.0.0.1")]          // IPv4 loopback
    [InlineData("::1")]                // IPv6 loopback
    [InlineData("10.0.0.1")]           // 10/8 start
    [InlineData("10.255.255.255")]     // 10/8 end
    [InlineData("172.16.0.1")]         // 172.16/12 start
    [InlineData("172.31.255.255")]     // 172.16/12 end
    [InlineData("192.168.0.1")]        // 192.168/16 start
    [InlineData("192.168.255.255")]    // 192.168/16 end
    public async Task Allows_InternalAddresses(string ip)
    {
        var ctx = await BuildContextAsync(IPAddress.Parse(ip));
        ctx.HasSucceeded.ShouldBeTrue($"{ip} should be allowed");
    }

    [Theory]
    [InlineData("8.8.8.8")]            // public internet
    [InlineData("172.15.255.255")]     // just below 172.16/12
    [InlineData("172.32.0.0")]         // just above 172.16/12
    [InlineData("192.167.255.255")]    // just below 192.168/16
    [InlineData("192.169.0.0")]        // just above 192.168/16
    [InlineData("11.0.0.0")]           // not 10/8
    [InlineData("203.0.113.1")]        // TEST-NET-3 (documentation range)
    public async Task Denies_ExternalAddresses(string ip)
    {
        var ctx = await BuildContextAsync(IPAddress.Parse(ip));
        ctx.HasSucceeded.ShouldBeFalse($"{ip} should be denied");
    }

    [Fact]
    public async Task Allows_IPv4MappedToIPv6_Loopback()
    {
        // ::ffff:127.0.0.1 — loopback mapped into IPv6
        var ip = IPAddress.Parse("::ffff:127.0.0.1");
        var ctx = await BuildContextAsync(ip);
        ctx.HasSucceeded.ShouldBeTrue("IPv4-mapped loopback should be allowed");
    }

    [Fact]
    public async Task Allows_IPv4MappedToIPv6_PrivateRange()
    {
        // ::ffff:10.0.0.1 — private range mapped into IPv6
        var ip = IPAddress.Parse("::ffff:10.0.0.1");
        var ctx = await BuildContextAsync(ip);
        ctx.HasSucceeded.ShouldBeTrue("IPv4-mapped private address should be allowed");
    }

    [Fact]
    public async Task Denies_NullRemoteIp()
    {
        var httpContext = new DefaultHttpContext();
        // RemoteIpAddress is null by default on DefaultHttpContext

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var requirement = new InternalNetworkRequirement();
        var authContext = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(),
            null);

        var handler = new InternalNetworkHandler(httpContextAccessor);
        await handler.HandleAsync(authContext);

        authContext.HasSucceeded.ShouldBeFalse("null remote IP should be denied");
    }

    [Fact]
    public async Task Denies_NullHttpContext()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var requirement = new InternalNetworkRequirement();
        var authContext = new AuthorizationHandlerContext(
            [requirement],
            new ClaimsPrincipal(),
            null);

        var handler = new InternalNetworkHandler(httpContextAccessor);
        await handler.HandleAsync(authContext);

        authContext.HasSucceeded.ShouldBeFalse("null HttpContext should be denied");
    }
}
