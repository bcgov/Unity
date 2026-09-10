using System.Security.Claims;
using Volo.Abp.Identity;
using Volo.Abp.Security.Claims;
using Volo.Abp.Users;

namespace Unity.Notifications.Web.Realtime;

public static class DisplayNameHelper
{
    public static string Resolve(ICurrentUser currentUser)
    {
        return Combine(currentUser.SurName, currentUser.Name, currentUser.UserName);
    }

    public static string Resolve(IdentityUser user)
    {
        return Combine(user.Surname, user.Name, user.UserName);
    }

    public static string Resolve(ClaimsPrincipal? user)
    {
        var surname = user?.FindFirstValue(AbpClaimTypes.SurName);
        var name = user?.FindFirstValue(AbpClaimTypes.Name);
        var userName = user?.FindFirstValue(AbpClaimTypes.UserName) ?? user?.Identity?.Name;

        return Combine(surname, name, userName);
    }

    private static string Combine(string? surname, string? name, string? userName)
    {
        var hasSurname = !string.IsNullOrWhiteSpace(surname);
        var hasName = !string.IsNullOrWhiteSpace(name);

        return (hasSurname, hasName) switch
        {
            (true, true) => $"{surname}, {name}",
            (false, true) => name!,
            (true, false) => surname!,
            _ => userName ?? "Unknown User"
        };
    }
}
