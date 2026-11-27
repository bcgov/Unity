using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Web.Identity;

internal sealed class StatusWorkflowPermissionCacheInvalidator : ITransientDependency
{
    // Use the same defaults as seeding, plus the two assignments removed by the host migration.
    private static readonly string[] CacheKeys = StatusWorkflowRoleDefaults.GetAll()
        .Concat(new[]
        {
            (RoleName: UnityRoles.Assessor, PermissionName: GrantApplicationPermissions.Reviews.StartInitial),
            (RoleName: UnityRoles.Assessor, PermissionName: GrantApplicationPermissions.Reviews.CompleteInitial)
        })
        .Select(pair => PermissionGrantCacheItem.CalculateCacheKey(
            pair.PermissionName, RolePermissionValueProvider.ProviderName, pair.RoleName))
        .Distinct()
        .ToArray();

    private readonly IDistributedCache<PermissionGrantCacheItem> _permissionCache;
    private readonly ITenantRepository _tenantRepository;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<StatusWorkflowPermissionCacheInvalidator> _logger;

    public StatusWorkflowPermissionCacheInvalidator(
        IDistributedCache<PermissionGrantCacheItem> permissionCache,
        ITenantRepository tenantRepository,
        ICurrentTenant currentTenant,
        ILogger<StatusWorkflowPermissionCacheInvalidator> logger)
    {
        _permissionCache = permissionCache;
        _tenantRepository = tenantRepository;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Deployment must finish DbMigrator before starting the new web application.
        // Repeating cache removal on subsequent starts is safe: permission values come from the database.
        using (_currentTenant.Change(null))
        {
            var tenants = await _tenantRepository.GetListAsync(includeDetails: false, cancellationToken: cancellationToken);
            await InvalidateForCurrentTenantAsync(cancellationToken);
            foreach (var tenant in tenants)
            {
                using (_currentTenant.Change(tenant.Id))
                {
                    await InvalidateForCurrentTenantAsync(cancellationToken);
                }
            }
            _logger.LogInformation("Invalidated Status Workflow permission caches for the host and {TenantCount} tenants.",
                tenants.Count);
        }
    }

    // The background service logs failures and retries; keep errors visible to that caller.
    private Task InvalidateForCurrentTenantAsync(CancellationToken cancellationToken) =>
        _permissionCache.RemoveManyAsync(CacheKeys, hideErrors: false, considerUow: false, token: cancellationToken);
}
