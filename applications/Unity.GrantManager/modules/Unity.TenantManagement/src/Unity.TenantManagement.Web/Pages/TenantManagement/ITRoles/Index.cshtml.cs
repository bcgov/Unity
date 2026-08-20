using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Unity.TenantManagement.Web.Pages.TenantManagement.ITRoles;

public class IndexModel : ITRolesPageModel
{
    public virtual Task<IActionResult> OnGetAsync()
    {
        return Task.FromResult<IActionResult>(Page());
    }
}
