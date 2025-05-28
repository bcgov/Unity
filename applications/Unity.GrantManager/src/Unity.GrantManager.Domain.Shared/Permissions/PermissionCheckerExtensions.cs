using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;

namespace Unity.GrantManager.Permissions;
public static class PermissionCheckerExtensions
{
    public static async Task<string> HasDisabledAttributeAsync(this IPermissionChecker permissionChecker, string permissionName)
    {
        return await permissionChecker.IsGrantedAsync(permissionName) ? string.Empty : "disabled";
    }
}
