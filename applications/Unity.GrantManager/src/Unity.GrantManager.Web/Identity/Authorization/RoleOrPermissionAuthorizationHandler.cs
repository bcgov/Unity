using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web.Identity.Authorization;

public class RoleOrPermissionRequirement : IAuthorizationRequirement
{
    public string RoleName { get; }
    public string PermissionName { get; }

    public RoleOrPermissionRequirement(string roleName, string permissionName)
    {
        RoleName = roleName;
        PermissionName = permissionName;
    }
}

public class RoleOrPermissionAuthorizationHandler : AuthorizationHandler<RoleOrPermissionRequirement>, ITransientDependency
{
    private readonly IPermissionChecker _permissionChecker;

    public RoleOrPermissionAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleOrPermissionRequirement requirement)
    {
        if (context.User.IsInRole(requirement.RoleName))
        {
            context.Succeed(requirement);
            return;
        }

        if (await _permissionChecker.IsGrantedAsync(context.User, requirement.PermissionName))
        {
            context.Succeed(requirement);
        }
    }
}
