using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Web.Identity.Authorization;

// Fallback role source for when Keycloak's client_roles token claim isn't reliably populated
// (IDP-mapping differences between environments/realms). A claim with type UnityClaimsTypes.Role
// stamped directly on the AbpUserClaims row for a user (e.g. for local dev, where fixing the IDP
// mapping isn't practical) is treated as equivalent to Keycloak sending that role in the token.
public interface IUserClaimsRoleChecker
{
    Task<bool> HasAnyRoleAsync(Guid? userId, IEnumerable<string> roleNames);
}

public class UserClaimsRoleChecker : IUserClaimsRoleChecker, ITransientDependency
{
    private readonly IdentityUserManager _identityUserManager;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public UserClaimsRoleChecker(IdentityUserManager identityUserManager, IUnitOfWorkManager unitOfWorkManager)
    {
        _identityUserManager = identityUserManager;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<bool> HasAnyRoleAsync(Guid? userId, IEnumerable<string> roleNames)
    {
        if (userId == null)
        {
            return false;
        }

        // Not every caller has an ambient unit of work already open (e.g. the OIDC login pipeline
        // runs before ABP's per-request UnitOfWork middleware), and IdentityUserManager needs one
        // to create its DbContext. requiresNew:false joins an ambient UoW when one already exists
        // (normal HTTP request/authorization-handler callers) and starts a fresh one otherwise.
        using var uow = _unitOfWorkManager.Begin(requiresNew: false);

        var user = await _identityUserManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
        {
            return false;
        }

        var claims = await _identityUserManager.GetClaimsAsync(user);
        var hasAnyRole = claims.Any(c => c.Type == UnityClaimsTypes.Role && roleNames.Contains(c.Value));

        await uow.CompleteAsync();
        return hasAnyRole;
    }
}
