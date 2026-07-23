using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web.Identity.Authorization;

public class RoleOrPermissionRequirement : IAuthorizationRequirement
{
    public string[] RoleNames { get; }
    public string PermissionName { get; }

    public RoleOrPermissionRequirement(string[] roleNames, string permissionName)
    {
        RoleNames = roleNames;
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
        if (requirement.RoleNames.Any(context.User.IsInRole))
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
