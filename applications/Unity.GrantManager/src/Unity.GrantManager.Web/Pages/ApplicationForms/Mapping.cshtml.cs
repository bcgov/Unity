using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

[Authorize]
public class MappingModel : PageModel
{
    public MappingModel()
    {
    }

    public void OnGet()
    {
        // Method intentionally left empty.
    }
}
