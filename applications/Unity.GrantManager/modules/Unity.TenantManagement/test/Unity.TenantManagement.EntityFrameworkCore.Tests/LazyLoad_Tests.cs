using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace Unity.TenantManagement.EntityFrameworkCore;

public class LazyLoad_Tests : LazyLoad_Tests<UnityTenantManagementEntityFrameworkCoreTestModule>
{
    protected override void BeforeAddApplication(IServiceCollection services)
    {
        services.Configure<AbpDbContextOptions>(options =>
        {
            options.PreConfigure<TenantManagementDbContext>(context =>
            {
                context.DbContextOptions.UseLazyLoadingProxies();
            });
        });
    }
}
