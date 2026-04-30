using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web.Identity.Authorization;

public class PermissionOrRequirement : IAuthorizationRequirement
{
    public string[] Permissions { get; }

    public PermissionOrRequirement(params string[] permissions)
    {
        Permissions = permissions;
    }
}

public class PermissionOrAuthorizationHandler : AuthorizationHandler<PermissionOrRequirement>, ITransientDependency
{
    private readonly IPermissionChecker _permissionChecker;

    public PermissionOrAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        _permissionChecker = permissionChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionOrRequirement requirement)
    {
        var result = await _permissionChecker.IsGrantedAsync(context.User, requirement.Permissions);

        if (result.Result.Any(r => r.Value == PermissionGrantResult.Granted))
        {
            context.Succeed(requirement);
        }
    }
}
