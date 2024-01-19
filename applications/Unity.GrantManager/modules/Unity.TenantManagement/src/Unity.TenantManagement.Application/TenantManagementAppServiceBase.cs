using Volo.Abp.Application.Services;
using Volo.Abp.TenantManagement.Localization;

namespace Unity.TenantManagement;

public abstract class TenantManagementAppServiceBase : ApplicationService
{
    protected TenantManagementAppServiceBase()
    {
        ObjectMapperContext = typeof(UnityTenantManagementApplicationModule);
        LocalizationResource = typeof(AbpTenantManagementResource);
    }
}
