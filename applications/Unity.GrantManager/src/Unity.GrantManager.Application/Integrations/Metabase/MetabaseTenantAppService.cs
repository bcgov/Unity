using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Integrations.Exceptions;
using Unity.GrantManager.Tenants.PostCreation;
using Unity.GrantManager.Tenants.PostCreation.Steps;
using Unity.Modules.Shared.Permissions;
using Unity.Modules.Shared.PostTenantCreation;
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
