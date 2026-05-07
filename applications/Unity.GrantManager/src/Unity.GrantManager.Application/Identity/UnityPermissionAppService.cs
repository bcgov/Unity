using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SimpleStateChecking;

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
        return Task.FromResult(
            CurrentUser.IsInRole("admin") ||
            CurrentUser.IsInRole(UnityRoles.SystemAdmin) ||
            CurrentUser.IsInRole(UnityRoles.ProgramManager)
        );
    }
}
