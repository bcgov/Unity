using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Identity.Authorization;

// Role-only equivalent of RoleOrPermissionRequirement (no permission fallback), for policies
// that were previously plain ASP.NET Core .RequireRole() checks - those only ever look at the
// current ClaimsPrincipal, so they can't see the UserClaims table fallback. This requirement
// checks the token role claim first, then falls back to a matching claim in the UserClaims table.
public class RoleClaimRequirement : IAuthorizationRequirement
{
    public string[] RoleNames { get; }

    public RoleClaimRequirement(string[] roleNames)
    {
        RoleNames = roleNames;
    }
}

public class RoleClaimAuthorizationHandler : AuthorizationHandler<RoleClaimRequirement>, ITransientDependency
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserClaimsRoleChecker _userClaimsRoleChecker;

    public RoleClaimAuthorizationHandler(ICurrentUser currentUser, IUserClaimsRoleChecker userClaimsRoleChecker)
    {
        _currentUser = currentUser;
        _userClaimsRoleChecker = userClaimsRoleChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleClaimRequirement requirement)
    {
        if (requirement.RoleNames.Any(context.User.IsInRole))
        {
            context.Succeed(requirement);
            return;
        }

        if (await _userClaimsRoleChecker.HasAnyRoleAsync(_currentUser.Id, requirement.RoleNames))
        {
            context.Succeed(requirement);
        }
    }
}
