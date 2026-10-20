using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// The scheduled members sync (<see cref="MetabaseMembershipSyncWorker"/>): runs
/// <see cref="MetabaseTenantProvisioner.SyncMembersAsync"/> for every tenant that is set up in
/// Metabase and where Setup could run (Metabase enabled and configured, Unity.Reporting on). It is
/// what the Reporting tab's Sync Members button does for one tenant.
/// </summary>
public class MetabaseMembershipSyncService(
    ITenantRepository tenantRepository,
    IEnumerable<IPostTenantCreationStep> steps,
    MetabaseTenantProvisioner provisioner,
    ICurrentTenant currentTenant,
    ILogger<MetabaseMembershipSyncService> logger)
    : ITransientDependency
{
    public virtual async Task<(int Synced, int Skipped, int Failed)> SyncAllTenantsAsync()
    {
        List<Tenant> tenants;
        using (currentTenant.Change(null))
        {
            tenants = await tenantRepository.GetListAsync();
        }

        var metabaseStep = steps.Single(s => s.Key == MetabaseTenantRegistrationStep.StepKey);
        int synced = 0, skipped = 0, failed = 0;

        foreach (var tenant in tenants)
        {
            var groupId = tenant.GetProperty<int?>(MetabaseTenantProvisioner.GroupIdProperty);
            if (groupId == null)
            {
                skipped++;
                continue;
            }

            // In the tenant's context, so the Unity.Reporting feature is read for this tenant.
            PostTenantCreationStepCheck check;
            using (currentTenant.Change(tenant.Id, tenant.Name))
            {
                check = await metabaseStep.CanExecuteAsync(tenant.Id);
            }
            if (!check.CanExecute)
            {
                skipped++;
                continue;
            }

            try
            {
                await provisioner.SyncMembersAsync(tenant.Id, groupId.Value);
                synced++;
            }
            // Any failure, not just Metabase's: one tenant's problem mustn't stop the others. The
            // provisioner has already recorded a Metabase failure as the tenant's last sync.
            catch (Exception ex)
            {
                failed++;
                logger.LogError(ex, "[Metabase] Scheduled members sync failed for tenant {TenantId}.", tenant.Id);
            }
        }

        return (synced, skipped, failed);
    }
}
