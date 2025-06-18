using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.Repositories;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IPermissionRoleMatrixRepository))]
public class PermissionRoleMatrixRepository(IDbContextProvider<GrantManagerDbContext> dbContextProvider) : EfCoreRepository<GrantManagerDbContext, PermissionGrant, Guid>(dbContextProvider), IPermissionRoleMatrixRepository
{
    public async Task<IList<PermissionRoleMatrixDto>> GetPermissionRoleMatrixAsync()
    {
        var dbContext = await GetDbContextAsync();

        // Query permissionGrants and roles
        var permissions = await dbContext.Set<PermissionDefinitionRecord>()
            .Where(p => p.IsEnabled && p.MultiTenancySide != MultiTenancySides.Host)
            .ToListAsync();

        var permissionGrants = await dbContext.Set<PermissionGrant>()
            .ToListAsync();

        var roles = await dbContext.Set<IdentityRole>()
            .OrderBy(r => r.Name)
            .ToListAsync();

        var matrix = permissions
            .Select(permission => new PermissionRoleMatrixDto
            {
                GroupName = permission.GroupName,
                PermissionName = permission.Name,
                PermissionDisplayName = permission.DisplayName,
                Depth = CalculatePermissionDepth(permission.Name, permissions),
                RolePermissions = roles.ToDictionary(
                    role => role.Name,
                    role => permissionGrants.Any(pg =>
                        pg.Name == permission.Name &&
                        pg.ProviderName == "R" &&
                        pg.TenantId == CurrentTenant.Id &&
                        pg.ProviderKey.Equals(role.Name, StringComparison.CurrentCultureIgnoreCase))
                )
            })
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.PermissionName)
            .ToList();

        return matrix;
    }

    private static int CalculatePermissionDepth(string permissionName, List<PermissionDefinitionRecord> permissions)
    {
        var parentPermission = permissions.FirstOrDefault(p => p.Name == permissionName)?.ParentName;

        if (string.IsNullOrEmpty(parentPermission))
        {
            // If there is no parent, this is a top-level permission (depth = 0)
            return 0;
        }

        // Recursively calculate the depth of the parent permission
        return 1 + CalculatePermissionDepth(parentPermission, permissions);
    }
}

public interface IPermissionRoleMatrixRepository
{
    Task<IList<PermissionRoleMatrixDto>> GetPermissionRoleMatrixAsync();
}

public class PermissionRoleMatrixDto
{
    public required string GroupName { get; set; }
    public required string PermissionName { get; set; }
    public required string PermissionDisplayName { get; set; }
    public int Depth { get; set; }
    public required Dictionary<string, bool> RolePermissions { get; set; }
    public bool IsDefined { get; set; } = false;
}