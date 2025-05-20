using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages; 

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