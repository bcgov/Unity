using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Integrations.Metabase;
using Unity.Modules.Shared.Features;
using Unity.Modules.Shared.PostTenantCreation;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.GrantManager.Tenants.PostCreation.Steps;

/// <summary>
/// Runs as post-tenant-creation step <c>Order = 1</c>: registers the new tenant in Metabase via
/// <see cref="MetabaseTenantProvisioner.SetupAsync"/>.
///
/// <see cref="CanExecuteAsync"/> records the step as Skipped when Metabase is disabled for the
/// environment or the tenant lacks the Unity.Reporting feature, and as Failure when a Reporting
/// tenant needs Metabase but its API key or METABASE_API_BASE endpoint is missing.
///
/// <see cref="ContinueOnError"/> is true - a Metabase outage is logged but doesn't block tenant
/// creation or later post-creation steps. Recovery from a partial failure is a manual re-enqueue
/// of this step (see <c>PostTenantCreationSequenceJob</c>); the provisioner is idempotent, so a
/// rerun is always safe.
/// </summary>
[RemoteService(false)]
[ExposeServices(typeof(IPostTenantCreationStep))]
public class MetabaseTenantRegistrationStep(
    MetabaseTenantProvisioner provisioner,
    IOptions<MetabaseOptions> metabaseOptions,
    IEndpointManagementAppService endpointManagementAppService,
    IFeatureChecker featureChecker)
    : IPostTenantCreationStep, ITransientDependency
{
    public const string StepKey = "MetabaseSync";

    public int Order => 1;

    public string Key => StepKey;

    public string StepName => "Metabase Tenant Registration";

    // A Metabase outage shouldn't block other post-creation steps from running.
    public bool ContinueOnError => true;

    // Runs inside the tenant's context (PostTenantCreationSequenceJob), so the feature check
    // reads this tenant's Unity.Reporting value.
    public virtual async Task<PostTenantCreationStepCheck> CanExecuteAsync(Guid tenantId)
    {
        if (!metabaseOptions.Value.Enabled)
        {
            return PostTenantCreationStepCheck.Skip("Metabase is not enabled for this environment.");
        }

        // Before the config checks: a tenant without Reporting never uses Metabase, so a missing
        // key or URL isn't a failure for it.
        if (!await featureChecker.IsEnabledAsync(FeatureConsts.Reporting))
        {
            return PostTenantCreationStepCheck.Skip($"The tenant does not have the {FeatureConsts.Reporting} feature.");
        }

        if (string.IsNullOrWhiteSpace(metabaseOptions.Value.ApiKey))
        {
            return PostTenantCreationStepCheck.Fail("Metabase is enabled but no API key is configured.");
        }

        if (!await IsMetabaseUrlConfiguredAsync())
        {
            return PostTenantCreationStepCheck.Fail(
                $"Metabase is enabled but the {DynamicUrlKeyNames.METABASE_API_BASE} endpoint is not set.");
        }

        return PostTenantCreationStepCheck.Run;
    }

    private async Task<bool> IsMetabaseUrlConfiguredAsync()
    {
        try
        {
            // Throws when the endpoint row is missing or its URL is blank.
            await endpointManagementAppService.GetUgmUrlByKeyNameAsync(DynamicUrlKeyNames.METABASE_API_BASE);
            return true;
        }
        catch (UserFriendlyException)
        {
            return false;
        }
    }

    public virtual Task ExecuteAsync(Guid tenantId) => provisioner.SetupAsync(tenantId);
}
