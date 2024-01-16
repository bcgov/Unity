using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager
{
    public class GrantManagerDefaultTenantSeederContributor : IDataSeedContributor, ITransientDependency
    {
        private readonly ITenantManager _tenantManager;
        private readonly ITenantRepository _tenantRepository;
        private readonly IConfiguration _configuration;

        public GrantManagerDefaultTenantSeederContributor(ITenantManager tenantManager,
            ITenantRepository tenantRepository,
            IConfiguration configuration)
        {
            _tenantManager = tenantManager;
            _tenantRepository = tenantRepository;
            _configuration = configuration;
        }

        public async Task SeedAsync(DataSeedContext context)
        {
            // The migrators run host first then tenants

            if (context.TenantId == null)
            {
                // Read the configuration and populate any default tenants
                var tenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.DefaultTenantName);

                if (tenant == null)
                {
                    var tenantConnectionString = _configuration.GetConnectionString(GrantManagerConsts.TenantConnectionStringName);
                    if (tenantConnectionString != null)
                    {
                        var newTenant = await _tenantManager.CreateAsync(GrantManagerConsts.DefaultTenantName);
                        newTenant.ConnectionStrings.Add(new TenantConnectionString(newTenant.Id, GrantManagerConsts.TenantConnectionStringName, tenantConnectionString));
                        await _tenantRepository.InsertAsync(newTenant, true);
                    }
                }
            }
        }
    }
}


