using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;

namespace Unity.TenantManagement;

public abstract class TenantManagementTestBase<TStartupModule> : AbpIntegratedTest<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }
}
