using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Unity.GrantManager.Web.Pages.Intakes;

[Authorize]
public class IndexModel : PageModel
{
    public IndexModel()
    {
    }

    public void OnGet()
    {
        // Method intentionally left empty.
    }
}
