using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.ITRoles;

public abstract class ITRolesPageModel : AbpPageModel
{
    protected ITRolesPageModel()
    {
        ObjectMapperContext = typeof(UnityTenantManagementWebModule);
    }
}
