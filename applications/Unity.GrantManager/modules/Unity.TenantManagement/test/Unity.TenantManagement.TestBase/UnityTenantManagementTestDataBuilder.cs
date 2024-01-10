using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;

namespace Unity.TenantManagement;

public class UnityTenantManagementTestDataBuilder : ITransientDependency
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantManager _tenantManager;

    public UnityTenantManagementTestDataBuilder(
        ITenantRepository tenantRepository,
        ITenantManager tenantManager)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
    }

    public void Build()
    {
        AsyncHelper.RunSync(AddTenantsAsync);
    }

    private async Task AddTenantsAsync()
    {
        var acme = await _tenantManager.CreateAsync("acme");
        acme.ConnectionStrings.Add(new TenantConnectionString(acme.Id, ConnectionStrings.DefaultConnectionStringName, "DefaultConnString-Value"));
        acme.ConnectionStrings.Add(new TenantConnectionString(acme.Id, "MyConnString", "MyConnString-Value"));
        await _tenantRepository.InsertAsync(acme);

        var volosoft = await _tenantManager.CreateAsync("volosoft");
        await _tenantRepository.InsertAsync(volosoft);
    }
}
