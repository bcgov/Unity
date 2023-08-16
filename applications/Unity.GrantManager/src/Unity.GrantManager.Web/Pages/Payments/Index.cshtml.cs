using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Pages.Payments
{
    [Authorize(Roles = "unity-manager")]
    public class IndexModel : PageModel
    {        
        public IndexModel()
        {            
        }

        public void OnGet()
        {            
        }
    }
}
