using Microsoft.AspNetCore.Authorization;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.Reconciliation
{
    [Authorize] 
    public class IndexModel : ReconciliationPageModel
    {
        public void OnGet()
        { // Initialize data or view logic here 
        }
    }
}