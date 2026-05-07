using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Unity.GrantManager.Web.Identity;

/// <summary>
/// Allows access to /metrics only from loopback or RFC-1918 private addresses.
/// This permits Prometheus to scrape pod-to-pod within the OpenShift cluster
/// while blocking external callers.
/// </summary>
public class InternalNetworkRequirement : IAuthorizationRequirement { }

public class InternalNetworkHandler(IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<InternalNetworkRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InternalNetworkRequirement requirement)
    {
        var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

        if (remoteIp is null)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        // Map IPv4-in-IPv6 (::ffff:x.x.x.x) back to IPv4 for range checks
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIp = remoteIp.MapToIPv4();
        }

        if (IsAllowed(remoteIp))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }

    private static bool IsAllowed(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10) return true;

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
        }

        return false;
    }
}
