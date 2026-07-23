using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SimpleStateChecking;
using Volo.Abp.Security.Claims;

namespace Unity.GrantManager.Identity;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(PermissionAppService), typeof(IPermissionAppService))]
public class UnityPermissionAppService(
    IPermissionManager permissionManager,
    IPermissionChecker permissionChecker,
    IPermissionDefinitionManager permissionDefinitionManager,
    IResourcePermissionManager resourcePermissionManager,
    IResourcePermissionGrantRepository resourcePermissionGrantRepository,
    IOptions<PermissionManagementOptions> options,
    ISimpleStateCheckerManager<PermissionDefinition> simpleStateCheckerManager)
    : PermissionAppService(permissionManager, permissionChecker, permissionDefinitionManager, resourcePermissionManager, resourcePermissionGrantRepository, options, simpleStateCheckerManager)
{
    protected override Task<bool> HasAdminRoleAsync()
    {
        // AbpClaimTypes.Role (not UnityClaimsTypes.Role/"client_roles") - it's dynamically
        // recomputed by ABP from the DB on every request, so it reflects the user's current
        // DB-assigned roles even though the cookie no longer stamps them at login.
        var roles = CurrentUser.FindClaims(AbpClaimTypes.Role).Select(c => c.Value);
        return Task.FromResult(
            roles.Contains("admin") ||
            roles.Contains(UnityRoles.SystemAdmin) ||
            roles.Contains(UnityRoles.ProgramManager)
        );
    }
}
