using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Tenants.PostCreation;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.Permissions;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Data;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Integrations.Metabase;

[Authorize(IdentityConsts.ITAdminOrITOperationsPolicyName)]
public class MetabaseTenantAppService(
    ITenantRepository tenantRepository,
    IEnumerable<IPostTenantCreationStep> steps,
    IBackgroundJobManager backgroundJobManager,
    IMetabaseApiClient metabaseApiClient)
    : GrantManagerAppService, IMetabaseTenantAppService
{
    public virtual async Task<MetabaseTenantStatusDto> GetAsync(Guid tenantId)
    {
        Tenant tenant;
        using (CurrentTenant.Change(null))
        {
            tenant = await tenantRepository.GetAsync(tenantId);
        }

        var entry = tenant.GetPostTenantCreationSections()
            .FirstOrDefault(s => s.Key == MetabaseTenantRegistrationStep.StepKey);

        var status = new MetabaseTenantStatusDto
        {
            Status = entry?.Status.ToString(),
            StatusMessage = entry?.Message,
            StatusUpdatedAt = entry?.UpdatedAt,
            DatabaseId = tenant.GetProperty<int?>(MetabaseTenantProvisioner.DatabaseIdProperty),
            GroupId = tenant.GetProperty<int?>(MetabaseTenantProvisioner.GroupIdProperty),
            CollectionId = tenant.GetProperty<int?>(MetabaseTenantProvisioner.CollectionIdProperty),
            ConcurrencyStamp = tenant.ConcurrencyStamp
        };

        // The same checks the post-creation job makes, in the tenant's context so the
        // Unity.Reporting feature is read for this tenant.
        PostTenantCreationStepCheck check;
        using (CurrentTenant.Change(tenantId))
        {
            check = await GetMetabaseStep().CanExecuteAsync(tenantId);
        }

        status.CanRun = check.CanExecute;
        status.NotRunReason = check.Reason;
        status.NotRunIsFailure = check.Status == PostTenantCreationStepStatus.Failure;

        if (check.CanExecute)
        {
            await CheckConnectionAsync(status);
        }

        return status;
    }

    public virtual async Task SetupAsync(Guid tenantId)
    {
        var orderedSteps = steps.OrderBy(s => s.Order).ToList();
        var metabaseStep = GetMetabaseStep();

        using (CurrentTenant.Change(null))
        {
            var tenant = await tenantRepository.GetAsync(tenantId);
            tenant.SetPostTenantCreationStepStatus(
                metabaseStep.Key, metabaseStep.StepName, PostTenantCreationStepStatus.Waiting, null, Clock.Now);
            await tenantRepository.UpdateAsync(tenant);
        }

        // Starts the post-creation sequence at the Metabase step. Any steps ordered after it
        // would run again too; there are none today.
        await backgroundJobManager.EnqueueAsync(new PostTenantCreationStepArgs
        {
            TenantId = tenantId,
            StepIndex = orderedSteps.IndexOf(metabaseStep)
        });
    }

    public virtual async Task<bool> ResyncIfSetUpAsync(Guid tenantId)
    {
        Tenant tenant;
        using (CurrentTenant.Change(null))
        {
            tenant = await tenantRepository.GetAsync(tenantId);
        }

        var isSetUp = tenant.GetProperty<int?>(MetabaseTenantProvisioner.DatabaseIdProperty).HasValue
            || tenant.GetProperty<int?>(MetabaseTenantProvisioner.GroupIdProperty).HasValue
            || tenant.GetProperty<int?>(MetabaseTenantProvisioner.CollectionIdProperty).HasValue;
        if (!isSetUp)
        {
            return false;
        }

        // Where Setup can't run (Metabase off, no Reporting feature), queuing would only turn the
        // last result into Skipped or Failure without renaming anything.
        PostTenantCreationStepCheck check;
        using (CurrentTenant.Change(tenantId))
        {
            check = await GetMetabaseStep().CanExecuteAsync(tenantId);
        }
        if (!check.CanExecute)
        {
            return false;
        }

        await SetupAsync(tenantId);
        return true;
    }

    public virtual async Task SyncSchemaAsync(Guid tenantId)
    {
        var databaseId = await GetDatabaseIdAsync(tenantId);
        await CallMetabaseAsync(databaseId, () => metabaseApiClient.SyncDatabaseSchemaAsync(databaseId));
    }

    public virtual async Task RescanValuesAsync(Guid tenantId)
    {
        var databaseId = await GetDatabaseIdAsync(tenantId);
        await CallMetabaseAsync(databaseId, () => metabaseApiClient.RescanDatabaseValuesAsync(databaseId));
    }

    private async Task<int> GetDatabaseIdAsync(Guid tenantId)
    {
        Tenant tenant;
        using (CurrentTenant.Change(null))
        {
            tenant = await tenantRepository.GetAsync(tenantId);
        }

        return tenant.GetProperty<int?>(MetabaseTenantProvisioner.DatabaseIdProperty)
            ?? throw new UserFriendlyException("This tenant is not set up in Metabase yet. Run Setup first.");
    }

    private static async Task CallMetabaseAsync(int databaseId, Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (IntegrationServiceException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new UserFriendlyException(
                $"Metabase database #{databaseId} no longer exists. Run Resync to recreate it.");
        }
        catch (Exception ex) when (ex is IntegrationServiceException or HttpRequestException or TaskCanceledException)
        {
            throw new UserFriendlyException($"Metabase did not accept the request: {ex.Message}");
        }
    }

    private IPostTenantCreationStep GetMetabaseStep() =>
        steps.Single(s => s.Key == MetabaseTenantRegistrationStep.StepKey);

    private async Task CheckConnectionAsync(MetabaseTenantStatusDto status)
    {
        try
        {
            var isAdmin = await metabaseApiClient.CheckConnectionAsync();
            status.MetabaseUrl = (await metabaseApiClient.GetSiteUrlAsync()).TrimEnd('/');
            status.ConnectionOk = true;
            if (!isAdmin)
            {
                status.ConnectionMessage = "The Metabase API key is not an administrator's, so Setup will fail.";
            }
        }
        catch (Exception ex) when (ex is IntegrationServiceException or HttpRequestException or TaskCanceledException)
        {
            status.ConnectionOk = false;
            status.ConnectionMessage = ex.Message;
        }
    }
}
