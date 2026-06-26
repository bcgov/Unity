using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Security.Encryption;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager
{
    public class GrantManagerDefaultTenantSeederContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly ITenantManager _tenantManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly IConfiguration _configuration;
        private readonly IFeatureManager _featureManager;
        private readonly IStringEncryptionService _encryptionService;

        public GrantManagerDefaultTenantSeederContributor(ITenantManager tenantManager,
            ITenantRepository tenantRepository,
            IConfiguration configuration,
            IFeatureManager featureManager,
            IStringEncryptionService encryptionService)
        {
            _tenantManager = tenantManager;
            _tenantRepository = tenantRepository;
            _configuration = configuration;
            _featureManager = featureManager;
            _encryptionService = encryptionService;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // The migrators run host first then tenants

            if (context.TenantId == null)
            {
                await SeedDefaultTenantAsync();
                await SeedOnboardingTenantAsync();
            }
        }

        private async Task SeedDefaultTenantAsync()
        {
            var tenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);

            // Legacy check for pre v8 upgrade - we dont want migrations to fail because of this breaking changed
            if (tenant == null)
            {
                var list = await _tenantRepository.GetListAsync();
                tenant = list.Find(s => s.Name == GrantManagerConsts.DefaultTenantName);
                if (tenant != null)
                {
                    await _tenantManager.ChangeNameAsync(tenant, tenant.Name);
                }
            }

            if (tenant == null)
            {
                var tenantConnectionString = _configuration.GetConnectionString(GrantManagerConsts.DefaultTenantConnectionStringName);
                if (tenantConnectionString != null)
                {
                    var newTenant = await _tenantManager.CreateAsync(GrantManagerConsts.DefaultTenantName);
                    newTenant.ConnectionStrings.Add(new TenantConnectionString(newTenant.Id, GrantManagerConsts.DefaultTenantConnectionStringName, _encryptionService.Encrypt(tenantConnectionString)));
                    await _tenantRepository.InsertAsync(newTenant, true);
                }
            }
        }

        private async Task SeedOnboardingTenantAsync()
        {
            var tenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedOnboardingTenantName);

            if (tenant == null)
            {
                var connectionString = _configuration.GetConnectionString(GrantManagerConsts.OnboardingTenantConnectionStringConfigKey);
                if (connectionString != null)
                {
                    var newTenant = await _tenantManager.CreateAsync(GrantManagerConsts.OnboardingTenantName);
                    newTenant.ConnectionStrings.Add(new TenantConnectionString(newTenant.Id, GrantManagerConsts.DefaultTenantConnectionStringName, _encryptionService.Encrypt(connectionString)));
                    await _tenantRepository.InsertAsync(newTenant, true);

                    // "Unity.Onboarding" = SpecializationConsts.Onboarding; "T" = TenantFeatureValueProvider.ProviderName
                    await _featureManager.SetAsync(
                        "Unity.Onboarding",
                        "true",
                        "T",
                        newTenant.Id.ToString());
                }
            }
        }
    }
}
