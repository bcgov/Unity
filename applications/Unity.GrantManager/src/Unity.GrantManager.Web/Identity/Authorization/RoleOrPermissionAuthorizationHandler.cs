using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

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
    private readonly ICurrentUser _currentUser;
    private readonly IUserClaimsRoleChecker _userClaimsRoleChecker;

    public RoleOrPermissionAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser,
        IUserClaimsRoleChecker userClaimsRoleChecker)
    {
        _permissionChecker = permissionChecker;
        _currentUser = currentUser;
        _userClaimsRoleChecker = userClaimsRoleChecker;
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

        // Fallback for environments where Keycloak's client_roles token claim isn't reliably
        // mapped - a matching role claim stamped directly on the user in the UserClaims table
        // counts the same as the token claim would.
        if (await _userClaimsRoleChecker.HasAnyRoleAsync(_currentUser.Id, requirement.RoleNames))
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
