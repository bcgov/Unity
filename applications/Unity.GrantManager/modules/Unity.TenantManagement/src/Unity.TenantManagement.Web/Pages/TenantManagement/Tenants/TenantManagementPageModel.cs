using Unity.TenantManagement.Web;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Tenants;

public abstract class TenantManagementPageModel : AbpPageModel
{
    protected TenantManagementPageModel()
    {
        ObjectMapperContext = typeof(UnityTenantManagementWebModule);
    }
}
