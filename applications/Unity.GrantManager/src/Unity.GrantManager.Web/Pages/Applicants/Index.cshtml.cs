using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.Web.Pages.Applicants;

[Authorize(GrantApplicationPermissions.Applicants.ViewList)]
public class IndexModel : GrantManagerPageModel
{
    public IndexModel()
    {
    }

    public void OnGet()
    {
        // Method intentionally left empty.
    }
}