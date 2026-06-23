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

    public async Task<PermissionUserMatrixResult> GetPermissionUserMatrixAsync()
    {
        var dbContext = await GetDbContextAsync();

        var permissions = await dbContext.Set<PermissionDefinitionRecord>()
            .Where(p => p.IsEnabled && p.MultiTenancySide != MultiTenancySides.Host)
            .ToListAsync();

        var permissionGrants = await dbContext.Set<PermissionGrant>()
            .Where(pg => pg.TenantId == CurrentTenant.Id)
            .ToListAsync();

        var users = await dbContext.Set<IdentityUser>()
            .Where(u => u.IsActive && !u.IsDeleted && u.TenantId == CurrentTenant.Id)
            .OrderBy(u => u.Surname ?? u.UserName)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToHashSet();

        var userRoles = await dbContext.Set<IdentityUserRole>()
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync();

        var roles = await dbContext.Set<IdentityRole>()
            .ToListAsync();

        var roleIdToName = roles.ToDictionary(r => r.Id, r => r.Name);

        // Build a lookup: userId → set of role names
        var userRoleNames = userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(ur => roleIdToName.TryGetValue(ur.RoleId, out var name) ? name : null)
                       .Where(n => n != null)
                       .ToHashSet()
            );

        var userInfos = users.Select(u => new UserInfoDto
        {
            UserId = u.Id.ToString(),
            UserName = u.UserName,
            DisplayLabel = $"{u.Surname}, {u.Name}".Trim() ?? u.UserName
        }).ToList();

        var rows = permissions
            .Select(permission => new PermissionUserMatrixRowDto
            {
                GroupName = permission.GroupName,
                PermissionName = permission.Name,
                PermissionDisplayName = permission.DisplayName,
                Depth = CalculatePermissionDepth(permission.Name, permissions),
                UserPermissions = users.ToDictionary(
                    user => user.Id.ToString(),
                    user =>
                    {
                        var sources = new List<string>();

                        var hasDirectGrant = permissionGrants.Any(pg =>
                            pg.Name == permission.Name &&
                            pg.ProviderName == "U" &&
                            pg.ProviderKey == user.Id.ToString());

                        if (hasDirectGrant)
                        {
                            sources.Add("DIRECT");
                        }

                        if (userRoleNames.TryGetValue(user.Id, out var roleNamesForUser))
                        {
                            sources.AddRange(roleNamesForUser.Where(rn => rn != null).Where(roleName => permissionGrants.Any(pg =>
                                    pg.Name == permission.Name &&
                                    pg.ProviderName == "R" &&
                                    pg.ProviderKey.Equals(roleName, StringComparison.OrdinalIgnoreCase))).Select(roleName => roleName!));
                        }

                        return new PermissionGrantSummary
                        {
                            HasPermission = sources.Count > 0,
                            GrantSources = string.Join(", ", sources)
                        };
                    })
            })
            .OrderBy(p => p.GroupName)
            .ThenBy(p => p.PermissionName)
            .ToList();

        return new PermissionUserMatrixResult
        {
            Rows = rows,
            Users = userInfos
        };
    }
}

public interface IPermissionRoleMatrixRepository
{
    Task<IList<PermissionRoleMatrixDto>> GetPermissionRoleMatrixAsync();
    Task<PermissionUserMatrixResult> GetPermissionUserMatrixAsync();
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

public class PermissionUserMatrixResult
{
    public IList<PermissionUserMatrixRowDto> Rows { get; set; } = [];
    public IList<UserInfoDto> Users { get; set; } = [];
}

public class PermissionUserMatrixRowDto
{
    public required string GroupName { get; set; }
    public required string PermissionName { get; set; }
    public required string PermissionDisplayName { get; set; }
    public int Depth { get; set; }
    public required Dictionary<string, PermissionGrantSummary> UserPermissions { get; set; }
    public bool IsDefined { get; set; } = false;
}

public class PermissionGrantSummary
{
    public bool HasPermission { get; set; }
    public string GrantSources { get; set; } = string.Empty;
}

public class UserInfoDto
{
    public required string UserId { get; set; }
    public required string UserName { get; set; }
    public required string DisplayLabel { get; set; }
}