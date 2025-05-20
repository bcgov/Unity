using Unity.TenantManagement.Web;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Reconciliation;

public abstract class ReconciliationPageModel : AbpPageModel
{
    protected ReconciliationPageModel()
    {
        ObjectMapperContext = typeof(UnityTenantManagementWebModule);
    }
}
