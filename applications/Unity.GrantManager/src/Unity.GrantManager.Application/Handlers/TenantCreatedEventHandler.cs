using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Data;
using Unity.GrantManager.Identity;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.FeatureManagement;
using Volo.Abp.MultiTenancy;
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

        public TenantCreatedEventHandler(ITenantRepository tenantRepository,
            ICurrentTenant currentTenant,
            IUserImportAppService userImportAppService,
            IFeatureAppService featureAppService,
            GrantManagerDbMigrationService grantManagerDbMigrationService)
        {
            _tenantRepository = tenantRepository;
            _grantManagerDbMigrationService = grantManagerDbMigrationService;
            _currentTenant = currentTenant;
            _userImportAppService = userImportAppService;
            _featureAppService = featureAppService;
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
        }

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
                .Select(key => new UpdateFeatureDto { Name = key, Value = "true" })
                .ToList();
        }
    }
}
