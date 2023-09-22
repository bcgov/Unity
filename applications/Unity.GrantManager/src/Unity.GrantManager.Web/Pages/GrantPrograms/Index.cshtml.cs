using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages.GrantPrograms;

[Authorize]
public class IndexModel : PageModel
{
    public IndexModel()
    {
    }

    public void OnGet()
    {
        //Placeholder. Nothing to do here yet.
    }
}
