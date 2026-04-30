using Microsoft.AspNetCore.Authorization;
using Unity.Modules.Shared;

namespace Unity.GrantManager.Web.Pages.Applicants;

[Authorize(UnitySelector.ApplicantManagement.Default)]
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