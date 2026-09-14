using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Tenants.PostCreation;
using Unity.TenantManagement.Metabase;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.FeatureManagement;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Settings;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.Handlers
{
    public class TenantCreatedEventHandler
        : ILocalEventHandler<TenantCreatedEto>, ITransientDependency
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ICurrentTenant _currentTenant;
        private readonly IUserImportAppService _userImportAppService;
        private readonly IFeatureAppService _featureAppService;
        private readonly GrantManagerDbMigrationService _grantManagerDbMigrationService;
        private readonly IBackgroundJobManager _backgroundJobManager;
        private readonly ISettingManager _settingManager;

        public TenantCreatedEventHandler(ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IUserImportAppService userImportAppService,
            IFeatureAppService featureAppService,
            GrantManagerDbMigrationService grantManagerDbMigrationService,
            IBackgroundJobManager backgroundJobManager,
            ISettingManager settingManager)
        {
            _tenantRepository = tenantRepository;
            _grantManagerDbMigrationService = grantManagerDbMigrationService;
            _currentTenant = currentTenant;
            _userImportAppService = userImportAppService;
            _featureAppService = featureAppService;
            _backgroundJobManager = backgroundJobManager;
            _settingManager = settingManager;
        }

        public async Task HandleEventAsync(TenantCreatedEto tenantCreatedEto)
        {
            var tenant = await _tenantRepository.GetAsync(tenantCreatedEto.Id);
            var userIdentifier = tenantCreatedEto.Properties["UserIdentifier"];

            await _grantManagerDbMigrationService
                .MigrateAndSeedTenantAsync(new HashSet<string>(), tenant);

            using (_currentTenant.Change(tenant.Id))
            {
                await _userImportAppService.ImportUserAsync(new ImportUserDto()
                { Directory = "IDIR", Guid = userIdentifier, Roles = new string[] { UnityRoles.ProgramManager } });
            }

            await EnableRequestedFeaturesAsync(tenantCreatedEto, tenant.Id);
            await SaveMetabaseUserEmailsAsync(tenantCreatedEto, tenant.Id);

            // Kick off the post-tenant-creation step sequence (e.g. Metabase registration).
            // The job re-enqueues itself for each subsequent step, so this only starts step 0.
            await _backgroundJobManager.EnqueueAsync(new PostTenantCreationStepArgs
            {
                TenantId = tenant.Id,
                StepIndex = 0
            });
        }

        // Captures the Metabase user list chosen at creation time as a per-tenant setting
        // snapshot, so the (async, later-running) Metabase step reads a stable list even if the
        // Global default changes in the meantime.
        private async Task SaveMetabaseUserEmailsAsync(TenantCreatedEto eto, Guid tenantId)
        {
            var emails = await ResolveMetabaseUserEmailsAsync(
                eto.Properties, () => _settingManager.GetOrNullGlobalAsync(MetabaseSettings.UserEmails));

            await _settingManager.SetAsync(
                MetabaseSettings.UserEmails, emails, TenantSettingValueProvider.ProviderName, tenantId.ToString());
        }

        // TenantAppService.CreateAsync only adds "MetabaseUserEmails" to eto.Properties when the
        // caller explicitly set it (even to an empty string - a deliberate "no Metabase users for
        // this tenant" choice, which must still be persisted as-is). When the property is absent -
        // an older/API caller that never set it - snapshot the *current* Global default here
        // rather than leaving the tenant setting unset, so the step reads a stable list even if the
        // Global default changes before it (async, queued) actually runs.
        internal static async Task<string> ResolveMetabaseUserEmailsAsync(
            IReadOnlyDictionary<string, string> etoProperties, Func<Task<string?>> getGlobalDefaultAsync) =>
            etoProperties.TryGetValue("MetabaseUserEmails", out var emailsRaw)
                ? emailsRaw
                : await getGlobalDefaultAsync() ?? string.Empty;

        private async Task EnableRequestedFeaturesAsync(TenantCreatedEto eto, Guid tenantId)
        {
            if (!eto.Properties.TryGetValue("FeatureKeys", out var featureKeysRaw))
                return;

            var featureUpdates = BuildFeatureUpdates(featureKeysRaw);
            if (featureUpdates.Count == 0) return;

            await _featureAppService.UpdateAsync(
                "T", // TenantFeatureValueProvider.ProviderName
                tenantId.ToString(),
                new UpdateFeaturesDto { Features = featureUpdates });
        }

        internal static List<UpdateFeatureDto> BuildFeatureUpdates(string? featureKeysRaw)
        {
            if (string.IsNullOrWhiteSpace(featureKeysRaw))
                return [];

            return featureKeysRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(key => new UpdateFeatureDto { Name = key, Value = "True" })
                .ToList();
        }
    }
}
